using UnityEditor;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class CreatureRegistryDevTools
{
    private static bool TryFindRegistry(out CreatureRegistrySO registry)
    {
        registry = null;
        var guids = AssetDatabase.FindAssets("t:CreatureRegistrySO");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[CreatureRegistryDevTools] No se encontró ningún CreatureRegistrySO en el proyecto.");
            return false;
        }
        if (guids.Length > 1)
        {
            Debug.LogWarning("[CreatureRegistryDevTools] Se encontró más de un CreatureRegistrySO — abortando.");
            return false;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        registry = AssetDatabase.LoadAssetAtPath<CreatureRegistrySO>(path);
        return registry != null;
    }

    [MenuItem("MoriMonchi/Registry/Sync From JSON")]
    private static void SyncFromJson()
    {
        if (!TryFindRegistry(out var registry)) return;
        SaveSystem.LoadInto(registry);
    }

    [MenuItem("MoriMonchi/Registry/Reroll Roles & Elements (current)")]
    private static void RerollRolesAndElements()
    {
        if (!TryFindRegistry(out var registry)) return;
        if (registry.Count == 0)
        {
            Debug.LogWarning("[CreatureRegistryDevTools] No hay criaturas para rerollear.");
            return;
        }

        registry.RerollRolesAndElements();
        SaveSystem.SaveDatabase(registry);

        if (Application.isPlaying)
            GameEvents.RegistryReloaded(registry);

        Debug.Log($"[CreatureRegistryDevTools] {registry.Count} roles/elementos rerolleados. " +
                  "Pulsá 'Push to Cloud' en CloudSyncService para subir a Cloud Save.");
    }

    [MenuItem("MoriMonchi/Registry/Wipe Registry (DEV)")]
    private static void WipeRegistry()
    {
        if (!TryFindRegistry(out var registry)) return;

        int had = registry.Wipe();
        SaveSystem.SaveDatabase(registry);

        if (Application.isPlaying)
            GameEvents.RegistryReloaded(registry);

        Debug.Log($"[CreatureRegistryDevTools] Registro borrado ({had} criaturas). " +
                  "Pulsá 'Push to Cloud' en CloudSyncService para limpiar Cloud Save.");
    }
}
}
