using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Serialization;

// Dashboard requirements:
//   Authentication → enable Anonymous + Unity Player Accounts
//   Cloud Save     → enable
// Attach to same GameObject as GameManager. Assign CreatureRegistrySO in Setup.
public partial class CloudSyncService : MonoBehaviour
{
    private const string REGISTRY_KEY  = "creatureregistry";
    private const string META_KEY      = "sync_meta";
    private const string FURNITURE_KEY = "furnitureregistry";
    private const string INVENTORY_KEY = "playerinventory";

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

    private CreatureRegistrySO  registry;
    private FurnitureRegistrySO furnitureRegistry;
    private PlayerInventorySO   inventory;

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

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Server Time Offset")]
    private string serverOffsetDisplay = "---";

    // Difference between server UTC and local UTC, fetched once at login.
    // Usage: (DateTime.UtcNow + ServerOffset).ToLocalTime() = server-adjusted local time.
    private TimeSpan _serverOffset = TimeSpan.Zero;
    public  TimeSpan ServerOffset  => _serverOffset;

    private bool isPushInProgress = false;

    // ── Lifecycle ─────────────────────────────────────────────────

    private async void Start()
    {
        registry          = GameManager.Instance.Registry;
        furnitureRegistry = GameManager.Instance.FurnitureRegistry;
        inventory         = GameManager.Instance.Inventory;
        await InitializeAsync();
    }

    private void OnDestroy()
    {
        PlayerAccountService.Instance.SignedIn -= OnPlayerAccountSignedIn;
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
}
