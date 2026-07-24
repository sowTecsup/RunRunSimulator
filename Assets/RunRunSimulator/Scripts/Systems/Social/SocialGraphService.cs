using System;
using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class SocialGraphService
{
    private static readonly Dictionary<string, float> deltas = new Dictionary<string, float>();

    public static bool Dirty { get; private set; }

    public static float EffectiveAffinity(CreatureDNA a, CreatureDNA b, SocialTuningSO t)
    {
        float seed = SocialAffinity.Compute(a, b, t);

        if (a == null || b == null || string.IsNullOrEmpty(a.UniqueID) || string.IsNullOrEmpty(b.UniqueID))
            return seed;

        return Mathf.Clamp(seed + GetDelta(a.UniqueID, b.UniqueID), -1f, 1f);
    }

    public static void RecordInteraction(string idA, string idB, SocialInteractionKind kind)
    {
        SocialTuningSO t = SocialTuningSO.Current;
        if (t == null || string.IsNullOrEmpty(idA) || string.IsNullOrEmpty(idB))
            return;

        float gain = 0f;
        switch (kind)
        {
            case SocialInteractionKind.PlayChase:
                gain = t.PlayAffinityGain;
                break;
            case SocialInteractionKind.SleepTogether:
                gain = t.SleepAffinityGain;
                break;
            case SocialInteractionKind.GremlinFight:
                gain = -t.FightAffinityLoss;
                break;
        }

        string key = PairKey(idA, idB);
        float current = deltas.TryGetValue(key, out var existing) ? existing : 0f;
        deltas[key] = Mathf.Clamp(current + gain, -t.HistoryDeltaClamp, t.HistoryDeltaClamp);
        Dirty = true;
    }

    public static Dictionary<string, float> ExportData()
    {
        Dirty = false;
        return new Dictionary<string, float>(deltas);
    }

    public static void ImportData(Dictionary<string, float> data, Func<string, bool> idExists)
    {
        deltas.Clear();

        if (data != null)
        {
            foreach (var pair in data)
            {
                if (idExists != null)
                {
                    string[] ids = pair.Key.Split('|');
                    if (ids.Length != 2 || !idExists(ids[0]) || !idExists(ids[1]))
                        continue;
                }

                deltas[pair.Key] = pair.Value;
            }
        }

        Dirty = false;
    }

    public static void Clear()
    {
        deltas.Clear();
        Dirty = false;
    }

    private static float GetDelta(string idA, string idB) =>
        deltas.TryGetValue(PairKey(idA, idB), out var value) ? value : 0f;

    private static string PairKey(string idA, string idB)
    {
        bool aFirst = string.CompareOrdinal(idA, idB) <= 0;
        string first = aFirst ? idA : idB;
        string second = aFirst ? idB : idA;
        return first + "|" + second;
    }
}
}
