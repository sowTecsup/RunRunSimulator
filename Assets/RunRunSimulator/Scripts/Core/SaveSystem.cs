using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class SaveSystem
{
    private const string DB_FILENAME        = "creature_database.json";
    private const string FURNITURE_FILENAME = "furniture_registry.json";
    private const string INVENTORY_FILENAME = "player_inventory.json";
    private const string SOCIAL_FILENAME    = "social_graph.json";
    private const string WORLD_FILENAME     = "world_state.json";

    private static string _userScope = "";

    public static void SetUserScope(string playerId) => _userScope = playerId ?? "";

    private static string DbPath => ScopedPath(DB_FILENAME);

    private static string ScopedPath(string filename)
    {
        if (string.IsNullOrEmpty(_userScope))
            return Path.Combine(Application.persistentDataPath, filename);

        string ext  = Path.GetExtension(filename);
        string stem = Path.GetFileNameWithoutExtension(filename);
        return Path.Combine(Application.persistentDataPath, $"{stem}_{_userScope}{ext}");
    }

    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        Converters        = new List<JsonConverter> { new UnityColorConverter(), new StringEnumConverter() },
        Formatting        = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    public static void SaveDatabase(CreatureRegistrySO registry)
    {
        File.WriteAllText(DbPath, Serialize(registry.GetData()));
    }

    public static string Serialize(RegistryData data) =>
        SaveMigrations.Write(JToken.FromObject(data, JsonSerializer.Create(Settings)), DateTime.UtcNow.Ticks);

    public static string Serialize(CreatureDNA dna) =>
        JsonConvert.SerializeObject(dna, Settings);

    public static RegistryData Deserialize(string json)
    {
        SaveEnvelope env = SaveMigrations.Read(json, SaveKind.Registry);
        if (env.Data == null || env.Data.Type == JTokenType.Null)
            return new RegistryData { Alive = new Dictionary<string, CreatureDNA>(), Departed = new Dictionary<string, CreatureDNA>() };

        return env.Data.ToObject<RegistryData>(JsonSerializer.Create(Settings));
    }

    public static string SerializeFurniture(FurnitureRegistrySO registry) =>
        SaveMigrations.Write(JToken.FromObject(registry.GetAll(), JsonSerializer.Create(Settings)), DateTime.UtcNow.Ticks);

    public static Dictionary<string, PlacedFurniture> DeserializeFurniture(string json)
    {
        SaveEnvelope env = SaveMigrations.Read(json, SaveKind.Furniture);
        if (env.Data == null || env.Data.Type == JTokenType.Null)
            return new Dictionary<string, PlacedFurniture>();

        return env.Data.ToObject<Dictionary<string, PlacedFurniture>>(JsonSerializer.Create(Settings));
    }

    public static string SerializeInventory(PlayerInventorySO inventory) =>
        SaveMigrations.Write(JToken.FromObject(inventory.GetData(), JsonSerializer.Create(Settings)), DateTime.UtcNow.Ticks);

    public static PlayerInventorySO.InventoryData DeserializeInventory(string json)
    {
        SaveEnvelope env = SaveMigrations.Read(json, SaveKind.Inventory);
        if (env.Data == null || env.Data.Type == JTokenType.Null)
            return null;

        return env.Data.ToObject<PlayerInventorySO.InventoryData>(JsonSerializer.Create(Settings));
    }

    public static void LoadInto(CreatureRegistrySO registry)
    {
        string path        = DbPath;
        string defaultPath = Path.Combine(Application.persistentDataPath, DB_FILENAME);

        if (!File.Exists(path) && !string.IsNullOrEmpty(_userScope) && File.Exists(defaultPath))
        {
            File.Copy(defaultPath, path);
            Debug.Log($"[SaveSystem] Migrated unscoped save → {path}");
        }

        if (!File.Exists(path))
        {
            Debug.Log("[SaveSystem] No save file found — starting fresh.");
            registry.LoadFrom(null);
            return;
        }

        registry.LoadFrom(Deserialize(File.ReadAllText(path)));
    }

    public static Dictionary<string, CreatureDNA> LoadDatabaseCopy()
    {
        if (string.IsNullOrEmpty(_userScope)) return null;

        string path = DbPath;
        if (!File.Exists(path))
        {
            string defaultPath = Path.Combine(Application.persistentDataPath, DB_FILENAME);
            if (File.Exists(defaultPath)) path = defaultPath;
            else return null;
        }

        return Deserialize(File.ReadAllText(path)).Alive;
    }

    public static void SaveFurniture(FurnitureRegistrySO registry)
    {
        string path = ScopedPath(FURNITURE_FILENAME);
        File.WriteAllText(path, SerializeFurniture(registry));
        Debug.Log($"[SaveSystem] Saved {registry.Count} placed furniture → {path}");
    }

    public static void LoadFurniture(FurnitureRegistrySO registry)
    {
        string path = ScopedPath(FURNITURE_FILENAME);
        if (!File.Exists(path))
        {
            Debug.Log("[SaveSystem] No furniture save found — starting fresh.");
            registry.LoadFrom(null);
            return;
        }

        registry.LoadFrom(DeserializeFurniture(File.ReadAllText(path)));
    }

    public static void SaveInventory(PlayerInventorySO inventory)
    {
        string path = ScopedPath(INVENTORY_FILENAME);
        File.WriteAllText(path, SerializeInventory(inventory));
        Debug.Log($"[SaveSystem] Saved inventory → {path}");
    }

    public static void LoadInventory(PlayerInventorySO inventory)
    {
        string path = ScopedPath(INVENTORY_FILENAME);
        if (!File.Exists(path))
        {
            Debug.Log("[SaveSystem] No inventory save found — starting fresh.");
            inventory.LoadFrom(null);
            return;
        }

        inventory.LoadFrom(DeserializeInventory(File.ReadAllText(path)));
    }

    public static void SaveSocialGraph()
    {
        string path = ScopedPath(SOCIAL_FILENAME);
        File.WriteAllText(path, SerializeSocialGraph());
    }

    public static void LoadSocialGraph(CreatureRegistrySO registry)
    {
        string path = ScopedPath(SOCIAL_FILENAME);
        if (!File.Exists(path))
        {
            SocialGraphService.Clear();
            return;
        }

        var data = DeserializeSocialGraph(File.ReadAllText(path));
        SocialGraphService.ImportData(data, id => registry != null && registry.TryGet(id, out _));
    }

    public static string SerializeSocialGraph() =>
        SaveMigrations.Write(JToken.FromObject(SocialGraphService.ExportData(), JsonSerializer.Create(Settings)), DateTime.UtcNow.Ticks);

    public static Dictionary<string, float> DeserializeSocialGraph(string json)
    {
        SaveEnvelope env = SaveMigrations.Read(json, SaveKind.Social);
        if (env.Data == null || env.Data.Type == JTokenType.Null)
            return new Dictionary<string, float>();

        return env.Data.ToObject<Dictionary<string, float>>(JsonSerializer.Create(Settings));
    }

    public static void SaveWorldState(WorldStateSO world)
    {
        string path = ScopedPath(WORLD_FILENAME);
        File.WriteAllText(path, SerializeWorldState(world));
    }

    public static void LoadWorldState(WorldStateSO world)
    {
        string path = ScopedPath(WORLD_FILENAME);
        if (!File.Exists(path))
        {
            Debug.Log("[SaveSystem] No world state save found — starting fresh.");
            world.LoadFrom(null);
            return;
        }

        world.LoadFrom(DeserializeWorldState(File.ReadAllText(path)));
    }

    public static string SerializeWorldState(WorldStateSO world) =>
        SaveMigrations.Write(JToken.FromObject(world.GetData(), JsonSerializer.Create(Settings)), DateTime.UtcNow.Ticks);

    public static WorldStateData DeserializeWorldState(string json)
    {
        SaveEnvelope env = SaveMigrations.Read(json, SaveKind.World);
        if (env.Data == null || env.Data.Type == JTokenType.Null)
            return null;

        return env.Data.ToObject<WorldStateData>(JsonSerializer.Create(Settings));
    }

    public static long LatestLocalSavedAt()
    {
        long latest = 0;

        long registryAt  = SavedAtOf(DbPath, SaveKind.Registry);
        long furnitureAt = SavedAtOf(ScopedPath(FURNITURE_FILENAME), SaveKind.Furniture);
        long inventoryAt = SavedAtOf(ScopedPath(INVENTORY_FILENAME), SaveKind.Inventory);
        long socialAt    = SavedAtOf(ScopedPath(SOCIAL_FILENAME), SaveKind.Social);
        long worldAt     = SavedAtOf(ScopedPath(WORLD_FILENAME), SaveKind.World);

        if (registryAt  > latest) latest = registryAt;
        if (furnitureAt > latest) latest = furnitureAt;
        if (inventoryAt > latest) latest = inventoryAt;
        if (socialAt    > latest) latest = socialAt;
        if (worldAt     > latest) latest = worldAt;

        return latest;
    }

    private static long SavedAtOf(string path, SaveKind kind)
    {
        if (!File.Exists(path)) return 0;

        try { return SaveMigrations.Read(File.ReadAllText(path), kind).SavedAtTicks; }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Could not read save date of {path}: {e.Message}");
            return 0;
        }
    }

    public static void BackupLocal(string suffix)
    {
        BackupFile(DbPath, suffix);
        BackupFile(ScopedPath(FURNITURE_FILENAME), suffix);
        BackupFile(ScopedPath(INVENTORY_FILENAME), suffix);
        BackupFile(ScopedPath(SOCIAL_FILENAME), suffix);
        BackupFile(ScopedPath(WORLD_FILENAME), suffix);
    }

    private static void BackupFile(string path, string suffix)
    {
        if (!File.Exists(path)) return;

        string ext  = Path.GetExtension(path);
        string stem = path.Substring(0, path.Length - ext.Length);
        string backupPath = $"{stem}.{suffix}.bak.json";

        try { File.Copy(path, backupPath, true); }
        catch (Exception e) { Debug.LogWarning($"[SaveSystem] Backup failed for {path}: {e.Message}"); }
    }

    private class UnityColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
            => writer.WriteValue(ColorUtility.ToHtmlStringRGB(value));

        public override Color ReadJson(
            JsonReader reader, Type objectType, Color existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            ColorUtility.TryParseHtmlString("#" + reader.Value, out Color c);
            return c;
        }
    }
}
}
