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

// Dashboard requirements:
//   Authentication → enable Anonymous + Unity Player Accounts
//   Cloud Save     → enable
// Attach to same GameObject as GameManager. Assign CreatureRegistrySO in Setup.
public class CloudSyncService : MonoBehaviour
{
    private const string REGISTRY_KEY = "creature_registry";
    private const string META_KEY     = "sync_meta";

    private static string MetaPath =>
        Path.Combine(Application.persistentDataPath, "sync_meta.json");

    [Serializable]
    private class SyncMeta
    {
        public long LocalPulledAt     = 0;
        public long LocalKnownCloudAt = 0;
        public long CloudPushedAt     = 0;
    }

    // ── Setup ─────────────────────────────────────────────────────
    [Required, AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private CreatureRegistrySO _registry;

    // ── Status ────────────────────────────────────────────────────
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private string _status = "Not initialized";

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Player ID")]
    private string _playerID = "---";

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Player Name")]
    private string _playerName = "---";

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Signed In")]
    private bool _isSignedIn = false;

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Auth Method")]
    private string _authMethod = "---";

    // ── Account ───────────────────────────────────────────────────
    [BoxGroup("Account"), LabelText("New Name"), EnableIf("_isSignedIn")]
    [SerializeField] private string _newNameInput = "";

    [Button("Update Name"), GUIColor(0.9f, 0.9f, 0.4f)]
    [BoxGroup("Account"), EnableIf("_isSignedIn")]
    public async void UpdateNameButton()
    {
        if (string.IsNullOrWhiteSpace(_newNameInput)) return;
        await UpdatePlayerNameAsync(_newNameInput);
    }

    // ── Security ──────────────────────────────────────────────────
    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Last Pull")]
    private string _lastPullDisplay = "---";

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Last Known Cloud Push")]
    private string _lastKnownCloudDisplay = "---";

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Security Status")]
    private string _securityStatus = "---";

    // ── Lifecycle ─────────────────────────────────────────────────
    private async void Start() => await InitializeAsync();

    private void OnDestroy()
    {
        PlayerAccountService.Instance.SignedIn -= OnPlayerAccountSignedIn;
    }

    // ── Initialization ────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        try
        {
            _status = "Initializing...";

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
                _status = "Resuming session...";
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                await OnSignedInComplete("Session resumed");
                return;
            }

            _status = "Ready — press 'Sign In with Unity Account'";
        }
        catch (Exception e)
        {
            _status = $"Init error: {e.Message}";
            Debug.LogError($"[CloudSync] Init failed: {e}");
        }
    }

    private void SetupAuthEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
            Debug.Log($"[CloudSync] Auth signed in — ID: {AuthenticationService.Instance.PlayerId}");
        AuthenticationService.Instance.SignInFailed += err =>
            Debug.LogError($"[CloudSync] Sign-in failed: {err}");
        AuthenticationService.Instance.SignedOut += () =>
        {
            _isSignedIn = false;
            _authMethod = "---";
            _status     = "Signed out";
        };
        AuthenticationService.Instance.Expired += () =>
        {
            _isSignedIn = false;
            _authMethod = "---";
            _status     = "Session expired — sign in again";
            Debug.LogWarning("[CloudSync] Session expired.");
        };
    }

    // ── Sign In ───────────────────────────────────────────────────
    [Button("Sign In with Unity Account", ButtonSizes.Large), GUIColor(0.4f, 0.6f, 1f)]
    [BoxGroup("Cloud Actions"), EnableIf("@!_isSignedIn")]
    public async void SignInButton()
    {
        try
        {
            _status = "Opening sign-in...";
            await PlayerAccountService.Instance.StartSignInAsync();
            // Flow continues in OnPlayerAccountSignedIn (event-driven by the browser callback)
        }
        catch (Exception e)
        {
            _status = $"Sign-in error: {e.Message}";
            Debug.LogError($"[CloudSync] StartSignInAsync failed: {e}");
        }
    }

    private async void OnPlayerAccountSignedIn()
    {
        try
        {
            _status = "Authenticating...";
            await AuthenticationService.Instance.SignInWithUnityAsync(
                PlayerAccountService.Instance.AccessToken);
            await OnSignedInComplete("Unity Account");
        }
        catch (AuthenticationException ex)
        {
            _status = $"Auth error: {ex.Message}";
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            _status = $"Request failed: {ex.Message}";
            Debug.LogException(ex);
        }
    }

    private async Task OnSignedInComplete(string method)
    {
        _playerID   = AuthenticationService.Instance.PlayerId;
        _isSignedIn = true;
        _authMethod = method;
        _playerName = await SafeGetPlayerName();
        _status     = $"Signed in ({method})";
        RefreshSecurityDisplay();
        Debug.Log($"[CloudSync] Signed in via '{method}' — ID: {_playerID}, Name: {_playerName}");
    }

    // ── Sign Out ──────────────────────────────────────────────────
    // Signs out for the current session. Session token is preserved so the next
    // launch auto-resumes silently (persistent session behavior).
    [Button("Sign Out", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
    [BoxGroup("Cloud Actions"), EnableIf("_isSignedIn")]
    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        PlayerAccountService.Instance.SignOut();
        _playerID   = "---";
        _playerName = "---";
        // _isSignedIn + _status updated by the SignedOut event handler above
    }

    // ── Update Player Name ────────────────────────────────────────
    public async Task UpdatePlayerNameAsync(string newName)
    {
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            _playerName   = await SafeGetPlayerName();
            _newNameInput = "";
            _status       = $"Name updated: {_playerName}";
            Debug.Log($"[CloudSync] Player name updated → {_playerName}");
        }
        catch (Exception e)
        {
            _status = $"Name update error: {e.Message}";
            Debug.LogError($"[CloudSync] UpdatePlayerName failed: {e}");
        }
    }

    // ── Reset Progress ────────────────────────────────────────────
    [Button("Reset All Progress (DEV)", ButtonSizes.Medium), GUIColor(0.9f, 0.2f, 0.2f)]
    [BoxGroup("Cloud Actions"), EnableIf("_isSignedIn")]
    public async void ResetProgressButton() => await ResetProgressAsync();

    public async Task ResetProgressAsync()
    {
        if (!EnsureSignedIn()) return;
        try
        {
            _status = "Resetting...";

            // Clear cloud keys (ignore errors if key doesn't exist)
            try { await CloudSaveService.Instance.Data.Player.DeleteAsync(REGISTRY_KEY); } catch { }
            try { await CloudSaveService.Instance.Data.Player.DeleteAsync(META_KEY);     } catch { }

            // Clear local registry and JSON
            _registry.LoadFrom(new System.Collections.Generic.Dictionary<string, CreatureDNA>());
            SaveSystem.SaveDatabase(_registry);

            // Clear local sync meta
            if (File.Exists(MetaPath)) File.Delete(MetaPath);
            RefreshSecurityDisplay();

            _status = "Progress reset — cloud and local data cleared";
            Debug.Log("[CloudSync] All progress reset.");
        }
        catch (Exception e)
        {
            _status = $"Reset error: {e.Message}";
            Debug.LogError($"[CloudSync] Reset failed: {e}");
        }
    }

    // ── Push to Cloud ─────────────────────────────────────────────
    [Button("Push to Cloud", ButtonSizes.Large), GUIColor(1f, 0.85f, 0.3f)]
    [BoxGroup("Cloud Actions"), EnableIf("_isSignedIn")]
    public async void PushButton() => await PushAsync();

    public async Task PushAsync()
    {
        if (!EnsureSignedIn()) return;
        try
        {
            _status = "Validating...";
            if (!await ValidateBeforePush()) return;

            _status       = "Pushing...";
            long pushedAt = DateTime.UtcNow.Ticks;

            await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object>
            {
                { REGISTRY_KEY, SaveSystem.Serialize(_registry.GetAll()) },
                { META_KEY,     JsonConvert.SerializeObject(new SyncMeta { CloudPushedAt = pushedAt }) },
            });

            var localMeta               = ReadLocalMeta();
            localMeta.LocalKnownCloudAt = pushedAt;
            WriteLocalMeta(localMeta);
            RefreshSecurityDisplay();

            _status = $"Pushed {_registry.Count} creatures";
            Debug.Log($"[CloudSync] Pushed {_registry.Count} creatures.");
        }
        catch (Exception e)
        {
            _status = $"Push error: {e.Message}";
            Debug.LogError($"[CloudSync] Push failed: {e}");
        }
    }

    // ── Pull from Cloud ───────────────────────────────────────────
    [Button("Pull from Cloud", ButtonSizes.Large), GUIColor(0.5f, 0.85f, 1f)]
    [BoxGroup("Cloud Actions"), EnableIf("_isSignedIn")]
    public async void PullButton() => await PullAsync();

    public async Task PullAsync()
    {
        if (!EnsureSignedIn()) return;
        try
        {
            _status    = "Pulling...";
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { REGISTRY_KEY, META_KEY });

            if (!result.ContainsKey(REGISTRY_KEY))
            {
                _status = "No cloud data found — push first";
                Debug.Log("[CloudSync] No data in Cloud Save yet.");
                return;
            }

            var data = SaveSystem.Deserialize(result[REGISTRY_KEY].Value.GetAs<string>());
            _registry.LoadFrom(data);
            SaveSystem.SaveDatabase(_registry);

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

            _status = $"Pulled {_registry.Count} creatures";
            Debug.Log($"[CloudSync] Pulled {_registry.Count} creatures.");
        }
        catch (Exception e)
        {
            _status = $"Pull error: {e.Message}";
            Debug.LogError($"[CloudSync] Pull failed: {e}");
        }
    }

    // ── Security validation ───────────────────────────────────────
    private async Task<bool> ValidateBeforePush()
    {
        var localMeta = ReadLocalMeta();
        if (localMeta.LocalPulledAt == 0)
        {
            _securityStatus = "No pull registered — fresh account";
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
            _securityStatus = "CHEAT ALERT (dev: push allowed)";
            Debug.LogWarning(
                $"[CloudSync] CHEAT ALERT: local token ({localMeta.LocalKnownCloudAt}) " +
                $"!= cloud ({cloudMeta.CloudPushedAt}).");
        }
        else
        {
            _securityStatus = "OK";
        }

        return true;
    }

    // ── Meta file helpers ─────────────────────────────────────────
    private static SyncMeta ReadLocalMeta()
    {
        if (!File.Exists(MetaPath)) return new SyncMeta();
        try { return JsonConvert.DeserializeObject<SyncMeta>(File.ReadAllText(MetaPath)) ?? new SyncMeta(); }
        catch { return new SyncMeta(); }
    }

    private static void WriteLocalMeta(SyncMeta meta) =>
        File.WriteAllText(MetaPath, JsonConvert.SerializeObject(meta, Formatting.Indented));

    private void RefreshSecurityDisplay()
    {
        var meta = ReadLocalMeta();
        _lastPullDisplay = meta.LocalPulledAt > 0
            ? new DateTime(meta.LocalPulledAt, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
            : "Never";
        _lastKnownCloudDisplay = meta.LocalKnownCloudAt > 0
            ? new DateTime(meta.LocalKnownCloudAt, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
            : "Never";
    }

    // ── Helpers ───────────────────────────────────────────────────
    private bool EnsureSignedIn()
    {
        if (_isSignedIn) return true;
        _status = "Not signed in";
        Debug.LogError("[CloudSync] Not signed in.");
        return false;
    }

    private async Task<string> SafeGetPlayerName()
    {
        try { return await AuthenticationService.Instance.GetPlayerNameAsync() ?? "---"; }
        catch { return "---"; }
    }
}
