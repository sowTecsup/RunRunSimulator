using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[Serializable]
public class DayBlockDef
{
    public string NameKey;
    public int    StartHour;
    public bool   CustomersOpen;
    public bool   ExpeditionOpen;
}

[CreateAssetMenu(fileName = "DaySchedule", menuName = "RunRunSimulator/World/Day Schedule")]
public class DayScheduleSO : ScriptableObject
{
    [Title("Schedule")]
    [SerializeField, Min(1f)] private float realSecondsPerDay = 1440f;

    [TableList(AlwaysExpanded = true)]
    [SerializeField] private List<DayBlockDef> blocks = new List<DayBlockDef>();

    public float RealSecondsPerDay => realSecondsPerDay;
    public IReadOnlyList<DayBlockDef> Blocks => blocks;
    public float GameMinutesPerRealSecond => realSecondsPerDay > 0f ? 1440f / realSecondsPerDay : 1f;

    public int BlockIndexAt(float minuteOfDay)
    {
        if (blocks == null || blocks.Count == 0) return -1;

        float hour = minuteOfDay / 60f;
        int best = -1;
        int bestStartHour = int.MinValue;
        for (int i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block == null) continue;
            if (block.StartHour <= hour && block.StartHour >= bestStartHour)
            {
                bestStartHour = block.StartHour;
                best = i;
            }
        }
        return best >= 0 ? best : blocks.Count - 1;
    }

    public DayBlockDef BlockAt(float minuteOfDay)
    {
        int index = BlockIndexAt(minuteOfDay);
        return index >= 0 ? blocks[index] : null;
    }

    [Button("Seed Defaults", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f)]
    private void SeedDefaults()
    {
        blocks = new List<DayBlockDef>
        {
            new DayBlockDef { NameKey = "ui.clock.block.free",   StartHour = 6,  CustomersOpen = false, ExpeditionOpen = false },
            new DayBlockDef { NameKey = "ui.clock.block.shop",   StartHour = 9,  CustomersOpen = true,  ExpeditionOpen = false },
            new DayBlockDef { NameKey = "ui.clock.block.manage", StartHour = 18, CustomersOpen = false, ExpeditionOpen = false },
            new DayBlockDef { NameKey = "ui.clock.block.night",  StartHour = 23, CustomersOpen = false, ExpeditionOpen = true },
        };
        realSecondsPerDay = 1440f;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
