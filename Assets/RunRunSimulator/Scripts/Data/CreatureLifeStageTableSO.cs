using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

// Maps a creature's age (in whole days) to a visible LifeStage. Each entry is the age at which
// the creature ENTERS that stage; GetStage returns the highest stage whose threshold is reached.
// Display-only (drives the NameTag) — never part of the genetic string. Referenced by
// BreedingController (no singleton), the NameTag reads it from BreedingController.Instance.
[CreateAssetMenu(fileName = "CreatureLifeStageTable", menuName = "RunRunSimulator/Life Stage Table")]
public class CreatureLifeStageTableSO : SerializedScriptableObject
{
    [Title("Entry Day Thresholds (LifeStage → age in days to enter it)")]
    [InfoBox("A creature enters a stage once its AgeDays reaches that stage's threshold. Newborn should stay at 0.")]
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Stage", ValueLabel = "Enters at (days)")]
    private Dictionary<LifeStage, int> entryDayThreshold = new Dictionary<LifeStage, int>();

    public LifeStage GetStage(int ageDays)
    {
        if (entryDayThreshold == null || entryDayThreshold.Count == 0) return LifeStage.Newborn;

        var stage = LifeStage.Newborn;
        int best = int.MinValue;
        foreach (var kv in entryDayThreshold)
            if (ageDays >= kv.Value && kv.Value >= best)
            {
                best  = kv.Value;
                stage = kv.Key;
            }
        return stage;
    }

    public string Label(LifeStage stage) => stage switch
    {
        LifeStage.Newborn => "Recién nacido",
        LifeStage.Child   => "Cría",
        LifeStage.Teen    => "Adolescente",
        LifeStage.Adult   => "Adulto",
        LifeStage.Elder   => "Anciano",
        _                 => stage.ToString(),
    };

    [Button("Seed Defaults", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f)]
    private void SeedDefaults()
    {
        entryDayThreshold = new Dictionary<LifeStage, int>
        {
            { LifeStage.Newborn, 0  },
            { LifeStage.Child,   1  },
            { LifeStage.Teen,    3  },
            { LifeStage.Adult,   7  },
            { LifeStage.Elder,   20 },
        };
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
