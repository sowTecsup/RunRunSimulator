using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

// Handles UGS initialization, anonymous sign-in, Cloud Save push/pull, and tamper detection.
// Attach to any GameObject in the scene (recommended: same as GameManager).
// Requires Authentication + Cloud Save enabled in the Unity Dashboard.
public class CloudSyncService : MonoBehaviour
{
    private const string REGISTRY_KEY = "creature_registry";
    private const string META_KEY     = "sync_meta";

    private static string MetaPath =>
        Path.Combine(Application.persistentDataPath, "sync_meta.json");

    // Timestamps stored locally (sync_meta.json) and in cloud ("sync_meta" key).
    // On Pull  → writes LocalPulledAt + LocalKnownCloudAt locally.
    // On Push  → writes CloudPushedAt to cloud.
    // On Push validation → compares LocalKnownCloudAt vs CloudPushedAt to detect tampering.
    [Serializable]
    private class SyncMeta
    {
        public long LocalPulledAt     = 0;  // UTC ticks — when we last pulled from cloud
        public long LocalKnownCloudAt = 0;  // cloud's CloudPushedAt at time of last pull
        public long CloudPushedAt     = 0;  // UTC ticks — when cloud was last legitimately pushed
    }

    // ── Setup ─────────────────────────────────────────────────────

    [Required, AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private CreatureRegistrySO _registry;

    // ── Status ────────────────────────────────────────────────────

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private string _status = "Not initialized";

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Player ID")]
    private string _playerID = "---";

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Signed In")]
    private bool _isSignedIn = false;

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Last Pull")]
    private string _lastPullDisplay = "---";

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Last Known Cloud Push")]
    private string _lastKnownCloudDisplay = "---";

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Security Status")]
    private string _securityStatus = "---";

    // ── Lifecycle ─────────────────────────────────────────────────

    private async void Start() => await InitializeAsync();

    // ── Init + Auth ───────────────────────────────────────────────

    [Button("Initialize + Sign In", ButtonSizes.Large), GUIColor(0.4f, 0.85f, 0.4f)]
    [BoxGroup("Cloud Actions")]
    public async void InitButton() => await InitializeAsync();

    public async Task InitializeAsync()
    {
        try
        {
            _status = "Initializing...";

            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            _playerID   = AuthenticationService.Instance.PlayerId;
            _isSignedIn = true;
            _status     = "Signed in";
            RefreshSecurityDisplay();
            Debug.Log($"[CloudSync] Signed in — PlayerID: {_playerID}");
        }
        catch (Exception e)
        {
            _status = $"Error: {e.Message}";
            Debug.LogError($"[CloudSync] Init failed: {e}");
        }
    }

    // ── Push to Cloud ─────────────────────────────────────────────

    [Button("Push to Cloud", ButtonSizes.Large), GUIColor(1f, 0.85f, 0.3f)]
    [BoxGroup("Cloud Actions")]
    public async void PushButton() => await PushAsync();

    public async Task PushAsync()
    {
        if (!EnsureSignedIn()) return;
        try
        {
            _status = "Validating...";

            if (!await ValidateBeforePush()) return;

            _status         = "Pushing...";
            long pushedAt   = DateTime.UtcNow.Ticks;

            var payload = new Dictionary<string, object>
            {
                { REGISTRY_KEY, SaveSystem.Serialize(_registry.GetAll()) },
                { META_KEY,     JsonConvert.SerializeObject(new SyncMeta { CloudPushedAt = pushedAt }) },
            };
            await CloudSaveService.Instance.Data.Player.SaveAsync(payload);

            // Actualiza el meta local para reflejar el push exitoso
            var localMeta             = ReadLocalMeta();
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
    [BoxGroup("Cloud Actions")]
    public async void PullButton() => await PullAsync();

    public async Task PullAsync()
    {
        if (!EnsureSignedIn()) return;
        try
        {
            _status  = "Pulling...";
            var keys = new HashSet<string> { REGISTRY_KEY, META_KEY };
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (!result.ContainsKey(REGISTRY_KEY))
            {
                _status = "No cloud data found";
                Debug.Log("[CloudSync] No data in Cloud Save yet — push first.");
                return;
            }

            // Carga criaturas
            var json = result[REGISTRY_KEY].Value.GetAs<string>();
            var data = SaveSystem.Deserialize(json);
            _registry.LoadFrom(data);
            SaveSystem.SaveDatabase(_registry);

            // Lee el CloudPushedAt del servidor y lo registra localmente
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

    // Returns false and logs a cheat alert if the local meta doesn't match cloud state.
    private async Task<bool> ValidateBeforePush()
    {
        var localMeta = ReadLocalMeta();

        // Sin pull previo: cuenta nueva o primera sesión — permitir con aviso
        if (localMeta.LocalPulledAt == 0)
        {
            _securityStatus = "No pull registered — fresh account";
            Debug.Log("[CloudSync] No previous pull found — assuming fresh account.");
            return true;
        }

        // Lee el meta actual del servidor para comparar
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
            Debug.LogWarning($"[CloudSync] Could not fetch cloud meta for validation: {e.Message}");
        }

        if (cloudMeta != null && localMeta.LocalKnownCloudAt != cloudMeta.CloudPushedAt)
        {
            // TODO Etapa 2.3: cambiar a return false cuando Cloud Code firme tokens server-side.
            _securityStatus = "CHEAT ALERT (dev: push allowed)";
            Debug.LogWarning(
                $"[CloudSync] CHEAT ALERT: local sync token ({localMeta.LocalKnownCloudAt}) " +
                $"doesn't match cloud ({cloudMeta.CloudPushedAt}). " +
                $"Local JSON may have been rolled back or edited manually.");
        }

        _securityStatus = "OK";
        return true;
    }

    // ── Meta file helpers ─────────────────────────────────────────

    private static SyncMeta ReadLocalMeta()
    {
        if (!File.Exists(MetaPath)) return new SyncMeta();
        try
        {
            return JsonConvert.DeserializeObject<SyncMeta>(File.ReadAllText(MetaPath))
                   ?? new SyncMeta();
        }
        catch { return new SyncMeta(); }
    }

    private static void WriteLocalMeta(SyncMeta meta) =>
        File.WriteAllText(MetaPath, JsonConvert.SerializeObject(meta, Formatting.Indented));

    private void RefreshSecurityDisplay()
    {
        var meta = ReadLocalMeta();
        _lastPullDisplay       = meta.LocalPulledAt     > 0
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
        Debug.LogError("[CloudSync] Not signed in. Press 'Initialize + Sign In' first.");
        _status = "Not signed in";
        return false;
    }
}
