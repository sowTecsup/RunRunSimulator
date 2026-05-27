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

    private static string DbPath => Path.Combine(Application.persistentDataPath, DB_FILENAME);

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

    public static void LoadInto(CreatureRegistrySO registry)
    {
        if (!File.Exists(DbPath))
        {
            Debug.Log("[SaveSystem] No save file found — starting fresh.");
            registry.LoadFrom(null);
            return;
        }

        var data = JsonConvert.DeserializeObject<Dictionary<string, CreatureDNA>>(
            File.ReadAllText(DbPath), Settings);

        registry.LoadFrom(data);
        Debug.Log($"[SaveSystem] Loaded {registry.Count} creatures from {DbPath}");
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
