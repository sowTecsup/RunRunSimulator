using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

// Requires: com.unity.nuget.newtonsoft-json in Package Manager.
// Saves/loads the full CreatureRegistrySO (including genealogy tree) to Application.persistentDataPath.
public static class SaveSystem
{
    private const string DB_FILENAME = "creature_database.json";

    private static string _userScope = "";

    // Namespaces the local save file by player ID so multiple Unity instances
    // (e.g. Multiplayer Play Mode clones) don't overwrite each other's data.
    public static void SetUserScope(string playerId) => _userScope = playerId ?? "";

    private static string DbPath => Path.Combine(
        Application.persistentDataPath,
        string.IsNullOrEmpty(_userScope)
            ? DB_FILENAME
            : $"creature_database_{_userScope}.json");

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
        Debug.Log($"[SaveSystem] Saved {registry.Count} creatures → {DbPath}");
    }

    public static string Serialize(Dictionary<string, CreatureDNA> data) =>
        JsonConvert.SerializeObject(data, Settings);

    public static string Serialize(CreatureDNA dna) =>
        JsonConvert.SerializeObject(dna, Settings);

    public static Dictionary<string, CreatureDNA> Deserialize(string json) =>
        JsonConvert.DeserializeObject<Dictionary<string, CreatureDNA>>(json, Settings);

    public static void LoadInto(CreatureRegistrySO registry)
    {
        string path        = DbPath;
        string defaultPath = Path.Combine(Application.persistentDataPath, DB_FILENAME);

        // One-time migration: when first signing in with a scope, inherit any
        // pre-existing unscoped save so the user doesn't see their data "disappear".
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
        Debug.Log($"[SaveSystem] Loaded {registry.Count} creatures from {path}");
    }

    // Serializes UnityEngine.Color as a 6-character hex string (e.g. "FF00AA").
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
