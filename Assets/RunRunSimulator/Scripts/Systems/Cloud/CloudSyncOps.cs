using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudSave;
using UnityEngine;
using PlayerDeleteOptions = Unity.Services.CloudSave.Models.Data.Player.DeleteOptions;
namespace MoriMonchiSimulator
{

public class CloudSyncOps
{
    private const string REGISTRY_KEY  = "creatureregistry";
    private const string META_KEY      = "sync_meta";
    private const string FURNITURE_KEY = "furnitureregistry";
    private const string INVENTORY_KEY = "playerinventory";
    private const string COMBAT_RESULTS_KEY  = "combat_results";
    private const string CANCEL_ALL_BREEDING = "cancel-all-breeding";
    private const string DEQUEUE_COMBAT      = "dequeue-combat";

    [Serializable]
    private class SyncMeta
    {
        public long LocalPulledAt     = 0;
        public long LocalKnownCloudAt = 0;
        public long CloudPushedAt     = 0;
    }

    private readonly CloudAuth auth;
    private readonly CreatureRegistrySO registry;
    private readonly FurnitureRegistrySO furnitureRegistry;
    private readonly PlayerInventorySO inventory;
    private readonly Action<string> setStatus;

    private bool isPushInProgress = false;

    public string LastPullDisplay       { get; private set; } = "---";
    public string LastKnownCloudDisplay { get; private set; } = "---";
    public string SecurityStatus        { get; private set; } = "---";

    public CloudSyncOps(CloudAuth auth, CreatureRegistrySO registry,
        FurnitureRegistrySO furnitureRegistry, PlayerInventorySO inventory,
        Action<string> setStatus)
    {
        this.auth              = auth;
        this.registry          = registry;
        this.furnitureRegistry = furnitureRegistry;
        this.inventory         = inventory;
        this.setStatus         = setStatus;
    }

    private string MetaPath =>
        Path.Combine(Application.persistentDataPath,
            string.IsNullOrEmpty(auth.PlayerID) || auth.PlayerID == "---"
                ? "sync_meta.json"
                : $"sync_meta_{auth.PlayerID}.json");

    private SyncMeta ReadLocalMeta()
    {
        if (!File.Exists(MetaPath)) return new SyncMeta();
        try { return JsonConvert.DeserializeObject<SyncMeta>(File.ReadAllText(MetaPath)) ?? new SyncMeta(); }
        catch { return new SyncMeta(); }
    }

    private void WriteLocalMeta(SyncMeta meta) =>
        File.WriteAllText(MetaPath, JsonConvert.SerializeObject(meta, Formatting.Indented));

    public void RefreshSecurityDisplay()
    {
        var meta = ReadLocalMeta();
        LastPullDisplay = meta.LocalPulledAt > 0
            ? new DateTime(meta.LocalPulledAt, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
            : "Never";
        LastKnownCloudDisplay = meta.LocalKnownCloudAt > 0
            ? new DateTime(meta.LocalKnownCloudAt, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
            : "Never";
    }

    private bool EnsureSignedIn()
    {
        if (auth.IsSignedIn) return true;
        setStatus("Not signed in");
        Debug.LogError("[CloudSync] Not signed in.");
        return false;
    }

    private async Task<bool> ValidateBeforePush()
    {
        var localMeta = ReadLocalMeta();
        if (localMeta.LocalPulledAt == 0)
        {
            SecurityStatus = "No pull registered — fresh account";
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
            SecurityStatus = "CHEAT ALERT (dev: push allowed)";
            Debug.LogWarning(
                $"[CloudSync] CHEAT ALERT: local token ({localMeta.LocalKnownCloudAt}) " +
                $"!= cloud ({cloudMeta.CloudPushedAt}).");
        }
        else
        {
            SecurityStatus = "OK";
        }

        return true;
    }

    public async Task PushAsync()
    {
        if (isPushInProgress) { Debug.Log("[CloudSync] Push already in progress — skipping concurrent request."); return; }
        if (!EnsureSignedIn()) return;

        isPushInProgress = true;
        try
        {
            setStatus("Validating...");
            if (!await ValidateBeforePush()) return;

            setStatus("Pushing...");
            long pushedAt = DateTime.UtcNow.Ticks;

            var payload = new Dictionary<string, object>
            {
                { REGISTRY_KEY, SaveSystem.Serialize(registry.GetAll()) },
                { META_KEY,     JsonConvert.SerializeObject(new SyncMeta { CloudPushedAt = pushedAt }) },
            };
            if (furnitureRegistry != null)
                payload[FURNITURE_KEY] = SaveSystem.SerializeFurniture(furnitureRegistry);
            if (inventory != null)
                payload[INVENTORY_KEY] = SaveSystem.SerializeInventory(inventory);

            await CloudSaveService.Instance.Data.Player.SaveAsync(payload);

            var localMeta               = ReadLocalMeta();
            localMeta.LocalKnownCloudAt = pushedAt;
            WriteLocalMeta(localMeta);
            RefreshSecurityDisplay();

            setStatus($"Pushed {registry.Count} creatures, {furnitureRegistry?.Count ?? 0} furniture");
            Debug.Log($"[CloudSync] Pushed {registry.Count} creatures, {furnitureRegistry?.Count ?? 0} furniture.");
        }
        catch (Exception e)
        {
            setStatus($"Push error: {e.Message}");
            Debug.LogError($"[CloudSync] Push failed: {e}");
        }
        finally
        {
            isPushInProgress = false;
        }
    }

    public async Task PullAsync()
    {
        if (!EnsureSignedIn()) return;
        try
        {
            setStatus("Pulling...");
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { REGISTRY_KEY, META_KEY, FURNITURE_KEY, INVENTORY_KEY });

            if (!result.ContainsKey(REGISTRY_KEY))
            {
                setStatus("No cloud data found — push first");
                Debug.Log("[CloudSync] No data in Cloud Save yet.");
                return;
            }

            var data = SaveSystem.Deserialize(result[REGISTRY_KEY].Value.GetAs<string>());
            registry.LoadFrom(data);
            SaveSystem.SaveDatabase(registry);
            GameEvents.RegistryReloaded(registry);

            if (result.ContainsKey(FURNITURE_KEY) && furnitureRegistry != null)
            {
                var fData = SaveSystem.DeserializeFurniture(result[FURNITURE_KEY].Value.GetAs<string>());
                furnitureRegistry.LoadFrom(fData);
                SaveSystem.SaveFurniture(furnitureRegistry);
                GameEvents.FurnitureReloaded(furnitureRegistry);
            }

            if (result.ContainsKey(INVENTORY_KEY) && inventory != null)
            {
                var iData = SaveSystem.DeserializeInventory(result[INVENTORY_KEY].Value.GetAs<string>());
                inventory.LoadFrom(iData);
                SaveSystem.SaveInventory(inventory);
                GameEvents.InventoryReloaded(inventory);
            }

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

            setStatus($"Pulled {registry.Count} creatures, {furnitureRegistry?.Count ?? 0} furniture");
            Debug.Log($"[CloudSync] Pulled {registry.Count} creatures, {furnitureRegistry?.Count ?? 0} furniture.");
        }
        catch (Exception e)
        {
            setStatus($"Pull error: {e.Message}");
            Debug.LogError($"[CloudSync] Pull failed: {e}");
        }
    }

    public async Task ResetProgressAsync()
    {
        if (!EnsureSignedIn()) return;
        try
        {
            setStatus("Resetting...");

            try { await CloudEndpoint.CallAsync(CANCEL_ALL_BREEDING, new Dictionary<string, object>()); } catch { }

            foreach (var dna in registry.GetAll().Values)
            {
                if (dna.BusyState != BusyReason.QueuedForCombat) continue;
                try { await CloudEndpoint.CallAsync(DEQUEUE_COMBAT, new Dictionary<string, object> { { "creatureId", dna.UniqueID } }); }
                catch { }
            }

            // Clear cloud keys (ignore errors if key doesn't exist)
            try { await CloudSaveService.Instance.Data.Player.DeleteAsync(REGISTRY_KEY,       new PlayerDeleteOptions()); } catch { }
            try { await CloudSaveService.Instance.Data.Player.DeleteAsync(META_KEY,           new PlayerDeleteOptions()); } catch { }
            try { await CloudSaveService.Instance.Data.Player.DeleteAsync(FURNITURE_KEY,      new PlayerDeleteOptions()); } catch { }
            try { await CloudSaveService.Instance.Data.Player.DeleteAsync(INVENTORY_KEY,      new PlayerDeleteOptions()); } catch { }
            try { await CloudSaveService.Instance.Data.Player.DeleteAsync(COMBAT_RESULTS_KEY, new PlayerDeleteOptions()); } catch { }

            // Clear local data and JSON — do NOT push back (we just cleared the cloud)
            registry.LoadFrom(new System.Collections.Generic.Dictionary<string, CreatureDNA>());
            SaveSystem.SaveDatabase(registry);
            GameEvents.RegistryReloaded(registry);

            if (furnitureRegistry != null)
            {
                furnitureRegistry.LoadFrom(null);
                SaveSystem.SaveFurniture(furnitureRegistry);
                GameEvents.FurnitureReloaded(furnitureRegistry);
            }
            if (inventory != null)
            {
                inventory.LoadFrom(null);
                SaveSystem.SaveInventory(inventory);
                GameEvents.InventoryReloaded(inventory);
            }

            // Clear local sync meta
            if (File.Exists(MetaPath)) File.Delete(MetaPath);
            RefreshSecurityDisplay();

            setStatus("Progress reset — cloud and local data cleared");
            Debug.Log("[CloudSync] All progress reset.");
        }
        catch (Exception e)
        {
            setStatus($"Reset error: {e.Message}");
            Debug.LogError($"[CloudSync] Reset failed: {e}");
        }
    }

    public async Task NotifyPendingCombatResultsAsync()
    {
        try
        {
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { "combat_results" });
            if (!data.ContainsKey("combat_results")) return;

            var json    = data["combat_results"].Value.GetAs<string>();
            var results = JsonConvert.DeserializeObject<List<object>>(json);
            if (results == null || results.Count == 0) return;

            Debug.Log($"[CloudSync] ¡Bienvenido, {auth.PlayerName}! Tienes {results.Count} MoriMochi(s) con resultado de combate pendiente. Presiona 'Check Pending Results' para aplicarlos.");
        }
        catch { /* silent — non-critical notification */ }
    }
}
}
