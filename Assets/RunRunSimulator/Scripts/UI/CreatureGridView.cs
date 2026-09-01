using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class CreatureGridView : MonoBehaviour
{
    private CreatureRegistrySO source;

    [BoxGroup("Creature Grid"), AssetsOnly]
    [SerializeField] private EquipmentDatabaseSO equipmentDb;

    [BoxGroup("Creature Grid")]
    [ShowInInspector, ReadOnly, LabelText("Registered")]
    private int Total => rows?.Count ?? 0;

    [BoxGroup("Creature Grid")]
    [TableList(IsReadOnly = true, AlwaysExpanded = true, ShowIndexLabels = true)]
    [SerializeField]
    private List<CreatureRow> rows = new List<CreatureRow>();

    private void OnEnable()
    {
        GameEvents.OnRegistryChanged  += RefreshGrid;
        GameEvents.OnRegistryReloaded += RefreshGrid;
    }

    private void OnDisable()
    {
        GameEvents.OnRegistryChanged  -= RefreshGrid;
        GameEvents.OnRegistryReloaded -= RefreshGrid;
    }

    private void RefreshGrid(CreatureRegistrySO registry)
    {
        source = registry;
        Rebuild();
    }

    [BoxGroup("Creature Grid")]
    [Button("Refresh Grid", ButtonSizes.Large), GUIColor(0.5f, 0.85f, 1f)]
    private void Rebuild()
    {
        rows = source == null
            ? new List<CreatureRow>()
            : source.GetAll().Values
                .OrderByDescending(d => d.BirthDate)
                .Select(d => CreatureRow.From(d, source, equipmentDb))
                .ToList();
    }

    [Serializable]
    [GUIColor(nameof(RowTint))]
    private class CreatureRow
    {
        [ReadOnly, TableColumnWidth(140)] public string Name;
        [ReadOnly, TableColumnWidth(55, Resizable = false)] public Color Color;
        [ReadOnly, TableColumnWidth(70, Resizable = false)] public CreatureGender Gender;
        [ReadOnly, TableColumnWidth(45, Resizable = false)] public float CON;
        [ReadOnly, TableColumnWidth(45, Resizable = false)] public float ATK;
        [ReadOnly, TableColumnWidth(45, Resizable = false)] public float SPD;
        [ReadOnly, TableColumnWidth(45, Resizable = false)] public float DEF;
        [ReadOnly, TableColumnWidth(45, Resizable = false)] public float LCK;
        [ReadOnly, TableColumnWidth(45, Resizable = false)] public float EVA;
        [ReadOnly, TableColumnWidth(170)] public string Equip;
        [ReadOnly, TableColumnWidth(55, Resizable = false)] public int Breeds;
        [ReadOnly, TableColumnWidth(120)] public string Mother;
        [ReadOnly, TableColumnWidth(120)] public string Father;
        [ReadOnly, TableColumnWidth(80)]  public string State;
        [ReadOnly, TableColumnWidth(125)] public string Born;

        public static CreatureRow From(CreatureDNA d, CreatureRegistrySO registry, EquipmentDatabaseSO equipmentDb) => new CreatureRow
        {
            Name   = string.IsNullOrEmpty(d.CustomName) ? d.ToStringID() : d.CustomName,
            Color  = d.BaseColor,
            Gender = d.Gender,
            CON    = d.BaseConstitution,
            ATK    = d.BaseAttack,
            SPD    = d.BaseSpeed,
            DEF    = d.BaseDefense,
            LCK    = d.BaseLuck,
            EVA    = d.BaseEvasion,
            Equip  = EquipSummary(d, equipmentDb),
            Breeds = d.BreedCount,
            Mother = ParentName(d.MotherID, registry),
            Father = ParentName(d.FatherID, registry),
            State  = CreatureDisplay.StateOf(d),
            Born   = d.BirthDate == default
                ? "—"
                : d.BirthDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
        };

        private static string ParentName(string parentID, CreatureRegistrySO registry) =>
            string.IsNullOrEmpty(parentID)        ? "—"   :
            registry.TryGet(parentID, out var p)  ? p.CustomName :
                                                    "???";

        private static string EquipSummary(CreatureDNA d, EquipmentDatabaseSO db)
        {
            if (d.Equipped == null || d.Equipped.Count == 0) return "—";
            return string.Join(", ", d.Equipped
                .OrderBy(kv => kv.Key)
                .Select(kv => db != null ? (db.GetByID(kv.Value)?.Name ?? kv.Value) : kv.Value));
        }

        private Color RowTint =>
            State == Loc.Tr("status.dead") ? new Color(1f, 0.55f, 0.55f) :
            State == Loc.Tr("status.free") ? new Color(0.6f, 0.95f, 0.65f) :
                                             new Color(1f, 0.9f, 0.5f);
    }
}
}
