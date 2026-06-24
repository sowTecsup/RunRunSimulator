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
using PlayerDeleteOptions = Unity.Services.CloudSave.Models.Data.Player.DeleteOptions;
namespace MoriMonchiSimulator
{

public partial class CloudSyncService
{
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
    [Button("Reset All Progress (DEV)", ButtonSizes.Medium), GUIColor(0.9f, 0.2f, 0.2f)]
    [BoxGroup("Cloud Actions"), EnableIf("isSignedIn")]
    private async void ResetProgressButton() => await ResetProgressAsync();

    public async Task ResetProgressAsync()
    {
        if (!EnsureSignedIn()) return;
        try
        {
            status = "Resetting...";

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
    private async void PushButton() => await PushAsync();

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

            status = $"Pushed {registry.Count} creatures, {furnitureRegistry?.Count ?? 0} furniture";
            Debug.Log($"[CloudSync] Pushed {registry.Count} creatures, {furnitureRegistry?.Count ?? 0} furniture.");
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
    private async void PullButton() => await PullAsync();

    public async Task PullAsync()
    {
        if (!EnsureSignedIn()) return;
        try
        {
            status     = "Pulling...";
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { REGISTRY_KEY, META_KEY, FURNITURE_KEY, INVENTORY_KEY });

            if (!result.ContainsKey(REGISTRY_KEY))
            {
                status = "No cloud data found — push first";
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

            status = $"Pulled {registry.Count} creatures, {furnitureRegistry?.Count ?? 0} furniture";
            Debug.Log($"[CloudSync] Pulled {registry.Count} creatures, {furnitureRegistry?.Count ?? 0} furniture.");
        }
        catch (Exception e)
        {
            status = $"Pull error: {e.Message}";
            Debug.LogError($"[CloudSync] Pull failed: {e}");
        }
    }
}
}
