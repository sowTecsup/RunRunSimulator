using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// Renders one 2-3-2 hex lineup grid (7 slots, max 3 units) inside a UITK host and
// owns the creature↔slot assignments. Pure state + render, no drag logic — the
// sibling MonoBehaviour CombatLineupUITK instantiates two of these (team A/B) and
// drives drag-and-drop through the exposed slot elements/highlight API.
public class CombatLineupBoard
{
    public const int SlotCount = 7;
    public const int TeamSize = 3;

    private static readonly CombatRow[] RowBySlot =
    {
        CombatRow.Front, CombatRow.Front,
        CombatRow.Mid, CombatRow.Mid, CombatRow.Mid,
        CombatRow.Back, CombatRow.Back,
    };

    private static readonly int[] SlotsByRow_Back  = { 5, 6 };
    private static readonly int[] SlotsByRow_Mid   = { 2, 3, 4 };
    private static readonly int[] SlotsByRow_Front = { 0, 1 };

    public event Action Changed;

    private readonly CreatureDNA[] occupants = new CreatureDNA[SlotCount];
    private readonly VisualElement[] slotElements = new VisualElement[SlotCount];
    private readonly Label[] rowLabels = new Label[SlotCount];
    private readonly VisualElement[] unitElements = new VisualElement[SlotCount];

    public int Count { get; private set; }
    public bool IsFull => Count >= TeamSize;

    public CombatLineupBoard(VisualElement host, bool mirrored)
    {
        host.Clear();

        var order = mirrored
            ? new[] { SlotsByRow_Front, SlotsByRow_Mid, SlotsByRow_Back }
            : new[] { SlotsByRow_Back, SlotsByRow_Mid, SlotsByRow_Front };

        foreach (var column in order)
        {
            var col = new VisualElement();
            col.AddToClassList("cbt-lu-col");
            host.Add(col);

            foreach (var slot in column)
                col.Add(BuildSlot(slot));
        }
    }

    private VisualElement BuildSlot(int slot)
    {
        var row = RowBySlot[slot];
        var el = new VisualElement();
        el.AddToClassList("cbt-lu-slot");
        el.AddToClassList(RowModifierClass(row));

        var label = new Label(RowLabelText(row));
        label.AddToClassList("cbt-lu-slot-row");
        el.Add(label);

        slotElements[slot] = el;
        rowLabels[slot] = label;
        return el;
    }

    private static string RowModifierClass(CombatRow row)
    {
        switch (row)
        {
            case CombatRow.Front: return "cbt-lu-slot--front";
            case CombatRow.Mid:   return "cbt-lu-slot--mid";
            default:              return "cbt-lu-slot--back";
        }
    }

    private static string RowLabelText(CombatRow row)
    {
        switch (row)
        {
            case CombatRow.Front: return "FRONT";
            case CombatRow.Mid:   return "MID";
            default:              return "BACK";
        }
    }

    public CreatureDNA GetAt(int slot) => occupants[slot];

    public bool CanPlace(int slot) => occupants[slot] == null && Count < TeamSize;

    public void Place(CreatureDNA dna, int slot)
    {
        if (!CanPlace(slot)) return;

        occupants[slot] = dna;
        Count++;

        if (rowLabels[slot] != null) rowLabels[slot].style.display = DisplayStyle.None;

        var unit = new VisualElement();
        unit.AddToClassList("cbt-lu-unit");
        unit.style.backgroundColor = dna.BaseColor;

        var roleChip = new Label(RoleText(dna.Role));
        roleChip.AddToClassList("cbt-lu-role");
        roleChip.AddToClassList(RoleModifierClass(dna.Role));
        unit.Add(roleChip);

        var nameLabel = new Label(dna.CustomName);
        nameLabel.AddToClassList("cbt-lu-unit-name");
        unit.Add(nameLabel);

        slotElements[slot].Add(unit);
        unitElements[slot] = unit;

        Changed?.Invoke();
    }

    private static string RoleText(Role role)
    {
        switch (role)
        {
            case Role.Protector: return "PRO";
            case Role.Agresivo:  return "AGR";
            default:             return "EMP";
        }
    }

    private static string RoleModifierClass(Role role)
    {
        switch (role)
        {
            case Role.Protector: return "cbt-lu-role--protector";
            case Role.Agresivo:  return "cbt-lu-role--agresivo";
            default:             return "cbt-lu-role--empatico";
        }
    }

    public CreatureDNA RemoveAt(int slot)
    {
        var dna = occupants[slot];
        if (dna == null) return null;

        occupants[slot] = null;
        Count--;

        if (unitElements[slot] != null)
        {
            unitElements[slot].RemoveFromHierarchy();
            unitElements[slot] = null;
        }
        if (rowLabels[slot] != null) rowLabels[slot].style.display = DisplayStyle.Flex;

        Changed?.Invoke();
        return dna;
    }

    public void Clear()
    {
        for (int i = 0; i < SlotCount; i++)
            RemoveAt(i);
    }

    public int SlotAtPosition(Vector2 worldPos)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            var el = slotElements[i];
            if (el != null && el.worldBound.Contains(worldPos)) return i;
        }
        return -1;
    }

    public VisualElement SlotElement(int slot) => slotElements[slot];

    public void SetSlotSize(float size)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            var el = slotElements[i];
            if (el == null) continue;
            el.style.width = size;
            el.style.height = size;
        }
    }

    public void SetDropHighlight(int slot, bool valid)
    {
        ClearHighlight();
        if (slot < 0) return;

        var el = slotElements[slot];
        if (el == null) return;

        el.AddToClassList(valid ? "cbt-lu-slot--hover" : "cbt-lu-slot--reject");
    }

    public void ClearHighlight()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            var el = slotElements[i];
            if (el == null) continue;
            el.RemoveFromClassList("cbt-lu-slot--hover");
            el.RemoveFromClassList("cbt-lu-slot--reject");
        }
    }

    public IEnumerable<(CreatureDNA Dna, int Slot)> Units
    {
        get
        {
            for (int i = 0; i < SlotCount; i++)
                if (occupants[i] != null) yield return (occupants[i], i);
        }
    }

    public IEnumerable<string> PlacedIds
    {
        get
        {
            for (int i = 0; i < SlotCount; i++)
                if (occupants[i] != null) yield return occupants[i].UniqueID;
        }
    }

    public static CombatRow RowOf(int slot) => RowBySlot[slot];
}
}
