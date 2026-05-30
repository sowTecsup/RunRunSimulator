using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

// Read-only grid view of every registered MoriMochi. Driven entirely by events:
// the registry arrives in the event payload, so this view never reaches into
// GameManager. Purely a display surface (Etapa 1.2): never mutates the registry.
public class CreatureGridView : MonoBehaviour
{
    // Last registry handed to us by an event — NOT a GameManager lookup. Cached
    // only so the manual Refresh button has something to rebuild from.
    private CreatureRegistrySO source;

    // ── Grid ──────────────────────────────────────────────────────

    [BoxGroup("Creature Grid")]
    [ShowInInspector, ReadOnly, LabelText("Registered")]
    private int Total => rows?.Count ?? 0;

    [BoxGroup("Creature Grid")]
    [TableList(IsReadOnly = true, AlwaysExpanded = true, ShowIndexLabels = true)]
    [SerializeField]
    private List<CreatureRow> rows = new List<CreatureRow>();

    // ── Lifecycle ─────────────────────────────────────────────────

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

    // ── Private Methods ───────────────────────────────────────────

    // Event handler — rebuilds straight from the payload, no lookup.
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

    // ── Row ───────────────────────────────────────────────────────

    [Serializable]
    [GUIColor(nameof(RowTint))]
    private class CreatureRow
    {
        [ReadOnly, TableColumnWidth(140)] public string Name;
        [ReadOnly, TableColumnWidth(55, Resizable = false)] public Color Color;
        [ReadOnly, TableColumnWidth(70, Resizable = false)] public CreatureGender Gender;
        [ReadOnly, TableColumnWidth(45, Resizable = false)] public float HP;
        [ReadOnly, TableColumnWidth(45, Resizable = false)] public float ATK;
        [ReadOnly, TableColumnWidth(45, Resizable = false)] public float SPD;
        [ReadOnly, TableColumnWidth(70, Resizable = false), LabelText("Fights (W)")] public string Fights;
        [ReadOnly, TableColumnWidth(55, Resizable = false)] public int Breeds;
        [ReadOnly, TableColumnWidth(120)] public string Mother;
        [ReadOnly, TableColumnWidth(120)] public string Father;
        [ReadOnly, TableColumnWidth(80)]  public string State;
        [ReadOnly, TableColumnWidth(125)] public string Born;

        public static CreatureRow From(CreatureDNA d, CreatureRegistrySO registry) => new CreatureRow
        {
            Name   = string.IsNullOrEmpty(d.CustomName) ? d.ToStringID() : d.CustomName,
            Color  = d.PrimaryColor,
            Gender = d.Gender,
            HP     = d.BaseHP,
            ATK    = d.BaseAttack,
            SPD    = d.BaseSpeed,
            Fights = $"{d.FightCount} ({d.WinCount})",
            Breeds = d.BreedCount,
            Mother = ParentName(d.MotherID, registry),
            Father = ParentName(d.FatherID, registry),
            State  = StateOf(d),
            Born   = d.BirthDate == default
                ? "—"
                : d.BirthDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
        };

        // Resolves a parent's display name from its UniqueID. "—" if no parent
        // (minted, not bred); "???" if the parent is no longer in the registry.
        private static string ParentName(string parentID, CreatureRegistrySO registry) =>
            string.IsNullOrEmpty(parentID)        ? "—"   :
            registry.TryGet(parentID, out var p)  ? p.CustomName :
                                                    "???";

        private static string StateOf(CreatureDNA d) =>
            d.IsDead                                  ? "DEAD"     :
            d.BusyState == BusyReason.Breeding        ? "Breeding" :
            d.BusyState == BusyReason.QueuedForCombat ? "In Queue" :
            "Free";

        // Red = dead, amber = busy, green = free. Tints the whole row.
        private Color RowTint =>
            State == "DEAD" ? new Color(1f, 0.55f, 0.55f) :
            State == "Free" ? new Color(0.6f, 0.95f, 0.65f) :
                              new Color(1f, 0.9f, 0.5f);
    }
}
