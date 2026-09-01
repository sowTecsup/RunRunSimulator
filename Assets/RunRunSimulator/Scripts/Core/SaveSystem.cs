using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class SaveSystem
{
    private const string DB_FILENAME        = "creature_database.json";
    private const string FURNITURE_FILENAME = "furniture_registry.json";
    private const string INVENTORY_FILENAME = "player_inventory.json";
    private const string SOCIAL_FILENAME    = "social_graph.json";

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
        string json = JsonConvert.SerializeObject(registry.GetAll(), Settings);
        File.WriteAllText(DbPath, json);
    }

    public static string Serialize(Dictionary<string, CreatureDNA> data) =>
        JsonConvert.SerializeObject(data, Settings);

    public static string Serialize(CreatureDNA dna) =>
        JsonConvert.SerializeObject(dna, Settings);

    public static Dictionary<string, CreatureDNA> Deserialize(string json) =>
        JsonConvert.DeserializeObject<Dictionary<string, CreatureDNA>>(json, Settings);

    public static string SerializeFurniture(FurnitureRegistrySO registry) =>
        JsonConvert.SerializeObject(registry.GetAll(), Settings);

    public static Dictionary<string, PlacedFurniture> DeserializeFurniture(string json) =>
        JsonConvert.DeserializeObject<Dictionary<string, PlacedFurniture>>(json, Settings);

    public static string SerializeInventory(PlayerInventorySO inventory) =>
        JsonConvert.SerializeObject(inventory.GetData(), Settings);

    public static PlayerInventorySO.InventoryData DeserializeInventory(string json) =>
        JsonConvert.DeserializeObject<PlayerInventorySO.InventoryData>(json, Settings);

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

        var data = JsonConvert.DeserializeObject<Dictionary<string, CreatureDNA>>(
            File.ReadAllText(path), Settings);

        registry.LoadFrom(data);
    }

    public static void SaveFurniture(FurnitureRegistrySO registry)
    {
        string path = ScopedPath(FURNITURE_FILENAME);
        File.WriteAllText(path, JsonConvert.SerializeObject(registry.GetAll(), Settings));
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

        var data = JsonConvert.DeserializeObject<Dictionary<string, PlacedFurniture>>(
            File.ReadAllText(path), Settings);
        registry.LoadFrom(data);
    }

    public static void SaveInventory(PlayerInventorySO inventory)
    {
        string path = ScopedPath(INVENTORY_FILENAME);
        File.WriteAllText(path, JsonConvert.SerializeObject(inventory.GetData(), Settings));
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

        var data = JsonConvert.DeserializeObject<PlayerInventorySO.InventoryData>(
            File.ReadAllText(path), Settings);
        inventory.LoadFrom(data);
    }

    public static void SaveSocialGraph()
    {
        string path = ScopedPath(SOCIAL_FILENAME);
        File.WriteAllText(path, JsonConvert.SerializeObject(SocialGraphService.ExportData(), Settings));
    }

    public static void LoadSocialGraph(CreatureRegistrySO registry)
    {
        string path = ScopedPath(SOCIAL_FILENAME);
        if (!File.Exists(path))
        {
            SocialGraphService.Clear();
            return;
        }

        var data = JsonConvert.DeserializeObject<Dictionary<string, float>>(
            File.ReadAllText(path), Settings);
        SocialGraphService.ImportData(data, id => registry != null && registry.TryGet(id, out _));
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
