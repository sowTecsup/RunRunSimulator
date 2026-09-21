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
                .Select(d => CreatureRow.From(d, source))
                .ToList();
    }

    [Serializable]
    [GUIColor(nameof(RowTint))]
    private class CreatureRow
    {
        [ReadOnly, TableColumnWidth(140)] public string Name;
        [ReadOnly, TableColumnWidth(55, Resizable = false)] public Color Color;
        [ReadOnly, TableColumnWidth(70, Resizable = false)] public CreatureGender Gender;
        [ReadOnly, TableColumnWidth(55, Resizable = false)] public int Breeds;
        [ReadOnly, TableColumnWidth(120)] public string Mother;
        [ReadOnly, TableColumnWidth(120)] public string Father;
        [ReadOnly, TableColumnWidth(80)]  public string State;
        [ReadOnly, TableColumnWidth(125)] public string Born;

        public static CreatureRow From(CreatureDNA d, CreatureRegistrySO registry) => new CreatureRow
        {
            Name   = string.IsNullOrEmpty(d.CustomName) ? d.ToStringID() : d.CustomName,
            Color  = d.BaseColor,
            Gender = d.Gender,
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

        private Color RowTint =>
            State == Loc.Tr("status.dead") ? new Color(1f, 0.55f, 0.55f) :
            State == Loc.Tr("status.free") ? new Color(0.6f, 0.95f, 0.65f) :
                                             new Color(1f, 0.9f, 0.5f);
    }
}
}
