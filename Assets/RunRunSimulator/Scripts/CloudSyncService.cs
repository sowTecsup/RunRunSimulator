using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Serialization;

// Dashboard requirements:
//   Authentication → enable Anonymous + Unity Player Accounts
//   Cloud Save     → enable
// Attach to same GameObject as GameManager. Assign CreatureRegistrySO in Setup.
public class CloudSyncService : MonoBehaviour
{
    private const string REGISTRY_KEY = "creatureregistry";
    private const string META_KEY     = "sync_meta";

    private string MetaPath =>
        Path.Combine(Application.persistentDataPath,
            string.IsNullOrEmpty(playerID) ? "sync_meta.json" : $"sync_meta_{playerID}.json");

    [Serializable]
    private class SyncMeta
    {
        public long LocalPulledAt     = 0;
        public long LocalKnownCloudAt = 0;
        public long CloudPushedAt     = 0;
    }

    // ── Private Fields ────────────────────────────────────────────

    // ── Cached References ─────────────────────────────────────────

    private CreatureRegistrySO registry;

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private string status = "Not initialized";

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Player ID")]
    private string playerID = "---";

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Player Name")]
    private string playerName = "---";

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Signed In")]
    private bool isSignedIn = false;

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Auth Method")]
    private string authMethod = "---";

    [BoxGroup("Account"), LabelText("New Name"), EnableIf("isSignedIn")]
    [FormerlySerializedAs("_newNameInput")]
    [SerializeField] private string newNameInput = "";

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Last Pull")]
    private string lastPullDisplay = "---";

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Last Known Cloud Push")]
    private string lastKnownCloudDisplay = "---";

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Security Status")]
    private string securityStatus = "---";

    private bool isPushInProgress = false;

    // ── Lifecycle ─────────────────────────────────────────────────

    private async void Start()
    {
        registry = GameManager.Instance.Registry;
        await InitializeAsync();
    }

    private void OnDestroy()
    {
        PlayerAccountService.Instance.SignedIn -= OnPlayerAccountSignedIn;
    }

    // ── Private Methods ───────────────────────────────────────────

    private void SetupAuthEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
            Debug.Log($"[CloudSync] Auth signed in — ID: {AuthenticationService.Instance.PlayerId}");
        AuthenticationService.Instance.SignInFailed += err =>
            Debug.LogError($"[CloudSync] Sign-in failed: {err}");
        AuthenticationService.Instance.SignedOut += () =>
        {
            isSignedIn = false;
            authMethod = "---";
            status     = "Signed out";
        };
        AuthenticationService.Instance.Expired += () =>
        {
            isSignedIn = false;
            authMethod = "---";
            status     = "Session expired — sign in again";
            Debug.LogWarning("[CloudSync] Session expired.");
        };
    }

    private async void OnPlayerAccountSignedIn()
    {
        try
        {
            status = "Authenticating...";
            await AuthenticationService.Instance.SignInWithUnityAsync(
                PlayerAccountService.Instance.AccessToken);
            await OnSignedInComplete("Unity Account");
        }
        catch (AuthenticationException ex)
        {
            status = $"Auth error: {ex.Message}";
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            status = $"Request failed: {ex.Message}";
            Debug.LogException(ex);
        }
    }

    private async Task OnSignedInComplete(string method)
    {
        playerID   = AuthenticationService.Instance.PlayerId;
        isSignedIn = true;
        authMethod = method;
        playerName = await SafeGetPlayerName();
        status     = $"Signed in ({method})";
        RefreshSecurityDisplay();
        Debug.Log($"[CloudSync] Signed in via '{method}' — ID: {playerID}, Name: {playerName}");

        // Scope local save by player + auto-sync from cloud
        SaveSystem.SetUserScope(playerID);
        SaveSystem.LoadInto(registry);
        await PullAsync();
        await NotifyPendingCombatResultsAsync();
    }

    private async Task<bool> ValidateBeforePush()
    {
        var localMeta = ReadLocalMeta();
        if (localMeta.LocalPulledAt == 0)
        {
            securityStatus = "No pull registered — fresh account";
            return true;
        }

        SyncMeta cloudMeta = null;
        try
        {
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { META_KEY });
            if (result.ContainsKey(META_KEY))
                cloudMeta = JsonConvert.DeserializeObject<SyncMeta>(
                    result[META_KEY].Value.GetAs<string>());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CloudSync] Could not fetch cloud meta: {e.Message}");
        }

        if (cloudMeta != null && localMeta.LocalKnownCloudAt != cloudMeta.CloudPushedAt)
        {
            // TODO Etapa 2.3: cambiar a return false cuando Cloud Code firme tokens server-side.
            securityStatus = "CHEAT ALERT (dev: push allowed)";
            Debug.LogWarning(
                $"[CloudSync] CHEAT ALERT: local token ({localMeta.LocalKnownCloudAt}) " +
                $"!= cloud ({cloudMeta.CloudPushedAt}).");
        }
        else
        {
            securityStatus = "OK";
        }

        return true;
    }

    private SyncMeta ReadLocalMeta()
    {
        if (!File.Exists(MetaPath)) return new SyncMeta();
        try { return JsonConvert.DeserializeObject<SyncMeta>(File.ReadAllText(MetaPath)) ?? new SyncMeta(); }
        catch { return new SyncMeta(); }
    }

    private void WriteLocalMeta(SyncMeta meta) =>
        File.WriteAllText(MetaPath, JsonConvert.SerializeObject(meta, Formatting.Indented));

    private void RefreshSecurityDisplay()
    {
        var meta = ReadLocalMeta();
        lastPullDisplay = meta.LocalPulledAt > 0
            ? new DateTime(meta.LocalPulledAt, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
            : "Never";
        lastKnownCloudDisplay = meta.LocalKnownCloudAt > 0
            ? new DateTime(meta.LocalKnownCloudAt, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
            : "Never";
    }

    private async Task NotifyPendingCombatResultsAsync()
    {
        try
        {
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { "combat_results" });
            if (!data.ContainsKey("combat_results")) return;

            var json    = data["combat_results"].Value.GetAs<string>();
            var results = JsonConvert.DeserializeObject<List<object>>(json);
            if (results == null || results.Count == 0) return;

            Debug.Log($"[CloudSync] ¡Bienvenido, {playerName}! Tienes {results.Count} MoriMochi(s) con resultado de combate pendiente. Presiona 'Check Pending Results' para aplicarlos.");
        }
        catch { /* silent — non-critical notification */ }
    }

    private bool EnsureSignedIn()
    {
        if (isSignedIn) return true;
        status = "Not signed in";
        Debug.LogError("[CloudSync] Not signed in.");
        return false;
    }

    private async Task<string> SafeGetPlayerName()
    {
        try { return await AuthenticationService.Instance.GetPlayerNameAsync() ?? "---"; }
        catch { return "---"; }
    }

    // ── Public Methods ────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        try
        {
            status = "Initializing...";

            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            SetupAuthEvents();
            PlayerAccountService.Instance.SignedIn += OnPlayerAccountSignedIn;

            if (AuthenticationService.Instance.IsSignedIn)
            {
                await OnSignedInComplete("Already signed in");
                return;
            }

            // Resume cached session silently — works for any prior auth method
            if (AuthenticationService.Instance.SessionTokenExists)
            {
                status = "Resuming session...";
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                await OnSignedInComplete("Session resumed");
                return;
            }

            status = "Ready — press 'Sign In with Unity Account'";
        }
        catch (Exception e)
        {
            status = $"Init error: {e.Message}";
            Debug.LogError($"[CloudSync] Init failed: {e}");
        }
    }

    [Button("Sign In Anonymous (DEV)", ButtonSizes.Medium), GUIColor(0.6f, 0.6f, 0.6f)]
    [BoxGroup("Cloud Actions"), EnableIf("@!isSignedIn")]
    public async void SignInAnonButton()
    {
        try
        {
            status = "Signing in anonymously...";
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            await OnSignedInComplete("Anonymous");
        }
        catch (Exception e)
        {
            status = $"Anon sign-in error: {e.Message}";
            Debug.LogError($"[CloudSync] SignInAnonymously failed: {e}");
        }
    }

    [Button("Sign In with Unity Account", ButtonSizes.Large), GUIColor(0.4f, 0.6f, 1f)]
    [BoxGroup("Cloud Actions"), EnableIf("@!isSignedIn")]
    public async void SignInButton()
    {
        try
        {
            status = "Opening sign-in...";
            await PlayerAccountService.Instance.StartSignInAsync();
            // Flow continues in OnPlayerAccountSignedIn (event-driven by the browser callback)
        }
        catch (Exception e)
        {
            status = $"Sign-in error: {e.Message}";
            Debug.LogError($"[CloudSync] StartSignInAsync failed: {e}");
        }
    }

    [Button("Sign Out", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
    [BoxGroup("Cloud Actions"), EnableIf("isSignedIn")]
    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        PlayerAccountService.Instance.SignOut();
        playerID   = "---";
        playerName = "---";
        // isSignedIn + status updated by the SignedOut event handler above
    }

    [Button("Update Name"), GUIColor(0.9f, 0.9f, 0.4f)]
    [BoxGroup("Account"), EnableIf("isSignedIn")]
    public async void UpdateNameButton()
    {
        if (string.IsNullOrWhiteSpace(newNameInput)) return;
        await UpdatePlayerNameAsync(newNameInput);
    }

    public async Task UpdatePlayerNameAsync(string newName)
    {
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            playerName   = await SafeGetPlayerName();
            newNameInput = "";
            status       = $"Name updated: {playerName}";
            Debug.Log($"[CloudSync] Player name updated → {playerName}");
        }
        catch (Exception e)
        {
            status = $"Name update error: {e.Message}";
            Debug.LogError($"[CloudSync] UpdatePlayerName failed: {e}");
        }
    }

    [Button("Reset All Progress (DEV)", ButtonSizes.Medium), GUIColor(0.9f, 0.2f, 0.2f)]
    [BoxGroup("Cloud Actions"), EnableIf("isSignedIn")]
    public async void ResetProgressButton() => await ResetProgressAsync();

    public async Task ResetProgressAsync()
    {
        if (!EnsureSignedIn()) return;
        try
        {
            status = "Resetting...";

            // Clear cloud keys (ignore errors if key doesn't exist)
            try { await CloudSaveService.Instance.Data.Player.DeleteAsync(REGISTRY_KEY); } catch { }
            try { await CloudSaveService.Instance.Data.Player.DeleteAsync(META_KEY);     } catch { }

            // Clear local registry and JSON
            registry.LoadFrom(new System.Collections.Generic.Dictionary<string, CreatureDNA>());
            SaveSystem.SaveDatabase(registry);

            // Clear local sync meta
            if (File.Exists(MetaPath)) File.Delete(MetaPath);
            RefreshSecurityDisplay();

            status = "Progress reset — cloud and local data cleared";
            Debug.Log("[CloudSync] All progress reset.");
        }
        catch (Exception e)
        {
            status = $"Reset error: {e.Message}";
            Debug.LogError($"[CloudSync] Reset failed: {e}");
        }
    }

    [Button("Push to Cloud", ButtonSizes.Large), GUIColor(1f, 0.85f, 0.3f)]
    [BoxGroup("Cloud Actions"), EnableIf("isSignedIn")]
    public async void PushButton() => await PushAsync();

    public async Task PushAsync()
    {
        if (isPushInProgress) { Debug.Log("[CloudSync] Push already in progress — skipping concurrent request."); return; }
        if (!EnsureSignedIn()) return;

        isPushInProgress = true;
        try
        {
            status = "Validating...";
            if (!await ValidateBeforePush()) return;

            status    = "Pushing...";
            long pushedAt = DateTime.UtcNow.Ticks;

            await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object>
            {
                { REGISTRY_KEY, SaveSystem.Serialize(registry.GetAll()) },
                { META_KEY,     JsonConvert.SerializeObject(new SyncMeta { CloudPushedAt = pushedAt }) },
            });

            var localMeta               = ReadLocalMeta();
            localMeta.LocalKnownCloudAt = pushedAt;
            WriteLocalMeta(localMeta);
            RefreshSecurityDisplay();

            status = $"Pushed {registry.Count} creatures";
            Debug.Log($"[CloudSync] Pushed {registry.Count} creatures.");
        }
        catch (Exception e)
        {
            status = $"Push error: {e.Message}";
            Debug.LogError($"[CloudSync] Push failed: {e}");
        }
        finally
        {
            isPushInProgress = false;
        }
    }

    [Button("Pull from Cloud", ButtonSizes.Large), GUIColor(0.5f, 0.85f, 1f)]
    [BoxGroup("Cloud Actions"), EnableIf("isSignedIn")]
    public async void PullButton() => await PullAsync();

    public async Task PullAsync()
    {
        if (!EnsureSignedIn()) return;
        try
        {
            status     = "Pulling...";
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { REGISTRY_KEY, META_KEY });

            if (!result.ContainsKey(REGISTRY_KEY))
            {
                status = "No cloud data found — push first";
                Debug.Log("[CloudSync] No data in Cloud Save yet.");
                return;
            }

            var data = SaveSystem.Deserialize(result[REGISTRY_KEY].Value.GetAs<string>());
            registry.LoadFrom(data);
            SaveSystem.SaveDatabase(registry);

            long cloudPushedAt = 0;
            if (result.ContainsKey(META_KEY))
            {
                var cloudMeta = JsonConvert.DeserializeObject<SyncMeta>(
                    result[META_KEY].Value.GetAs<string>());
                cloudPushedAt = cloudMeta?.CloudPushedAt ?? 0;
            }

            WriteLocalMeta(new SyncMeta
            {
                LocalPulledAt     = DateTime.UtcNow.Ticks,
                LocalKnownCloudAt = cloudPushedAt,
            });
            RefreshSecurityDisplay();

            status = $"Pulled {registry.Count} creatures";
            Debug.Log($"[CloudSync] Pulled {registry.Count} creatures.");
        }
        catch (Exception e)
        {
            status = $"Pull error: {e.Message}";
            Debug.LogError($"[CloudSync] Pull failed: {e}");
        }
    }
}
