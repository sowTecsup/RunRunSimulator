using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class ArenaCastSource
{
    public static List<CreatureDNA> LoadLocal()
    {
        var scoped = SaveSystem.LoadDatabaseCopy();
        if (scoped != null)
        {
            var scopedResult = AliveOrdered(scoped);
            Debug.Log($"[ArenaCastSource] {scopedResult.Count} MoriMonchis vivos del jugador");
            return scopedResult;
        }

        var result = new List<CreatureDNA>();
        string directory = Application.persistentDataPath;
        if (!Directory.Exists(directory)) return result;

        var files = Directory.GetFiles(directory, "creature_database*.json");
        if (files.Length == 0) return result;

        Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));

        try
        {
            var data = SaveSystem.Deserialize(File.ReadAllText(files[0]));
            if (data == null) return result;

            result = AliveOrdered(data);
            Debug.Log($"[ArenaCastSource] {result.Count} MoriMonchis vivos en {Path.GetFileName(files[0])}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ArenaCastSource] No se pudo leer {files[0]}: {e.Message}");
            result.Clear();
        }

        return result;
    }

    private static List<CreatureDNA> AliveOrdered(Dictionary<string, CreatureDNA> data)
    {
        var result = new List<CreatureDNA>();
        foreach (var dna in data.Values)
            if (dna != null && !dna.IsDead) result.Add(dna);

        result.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        return result;
    }

    public static List<CreatureDNA> Pick(List<CreatureDNA> pool, int count, int seed)
    {
        var picked = new List<CreatureDNA>();
        if (pool == null || pool.Count == 0 || count <= 0) return picked;

        var order = new List<CreatureDNA>(pool);
        var rng = new System.Random(seed);
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        for (int i = 0; i < order.Count && picked.Count < count; i++)
            picked.Add(order[i]);

        return picked;
    }
}
}
