using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// Tooltip-style popup: equips items into a MoriMochi's slot from the player's
// equipment backpack (drag to reorder, click to equip). Opened on demand by a
// caller (e.g. CreatureGridUITK) via Open(dna, slot, anchor, registry); builds its
// own VisualElement into the anchor's panel root and rebuilds it after every
// mutation. Owns no persistence — mutates inventory/dna and fires GameEvents only.
[DisallowMultipleComponent]
public class EquipmentBackpackUITK : MonoBehaviour
{
    private const int GridSize = 9;
    private const float DragThreshold = 6f;
    private const float GhostHalfSize = 32f;

    [Title("Data")]
    [Required, AssetsOnly] [SerializeField] private PlayerInventorySO inventory;
    [Required, AssetsOnly] [SerializeField] private EquipmentDatabaseSO equipmentDatabase;
    [Required, AssetsOnly] [SerializeField] private EquipmentPaletteSO equipmentPalette;

    [Title("UI Toolkit setup")]
    [Required, AssetsOnly] [SerializeField] private StyleSheet styleSheet;

    [Title("Dev")]
    [SerializeField] private bool devNoConsume;

    private VisualElement root;
    private VisualElement anchor;
    private VisualElement popup;
    private VisualElement grid;
    private Label nameLabel;

    private CreatureDNA dna;
    private EquipmentSlot slot;
    private CreatureRegistrySO registry;

    private int page;
    private readonly List<(int GlobalIndex, EquipmentSO Item)> filtered = new List<(int, EquipmentSO)>();
    private readonly List<VisualElement> tabButtons = new List<VisualElement>();

    private bool pointerDown;
    private bool dragging;
    private Vector2 dragStart;
    private int dragGlobalIndex;
    private EquipmentSO dragItem;
    private VisualElement dragOriginCell;
    private VisualElement dragTargetCell;
    private VisualElement ghost;

    public void Open(CreatureDNA dna, EquipmentSlot slot, VisualElement anchor, CreatureRegistrySO registry)
    {
        if (anchor == null || anchor.panel == null) return;
        Close();

        this.dna = dna;
        this.slot = slot;
        this.anchor = anchor;
        this.registry = registry;
        page = 0;

        root = anchor.panel.visualTree;

        popup = new VisualElement();
        popup.AddToClassList("backpack");
        if (styleSheet != null) popup.styleSheets.Add(styleSheet);
        popup.RegisterCallback<PointerMoveEvent>(OnPopupPointerMove);
        popup.RegisterCallback<PointerUpEvent>(OnPopupPointerUp);

        root.Add(popup);
        PositionPopup();

        root.RegisterCallback<PointerDownEvent>(OnOutsidePointerDown, TrickleDown.TrickleDown);

        Rebuild();
    }

    public void Close()
    {
        CancelDrag();

        if (root != null)
            root.UnregisterCallback<PointerDownEvent>(OnOutsidePointerDown, TrickleDown.TrickleDown);

        if (popup != null)
        {
            popup.UnregisterCallback<PointerMoveEvent>(OnPopupPointerMove);
            popup.UnregisterCallback<PointerUpEvent>(OnPopupPointerUp);
            popup.RemoveFromHierarchy();
        }

        popup = null;
        grid = null;
        nameLabel = null;
        root = null;
        anchor = null;
        dna = null;
        registry = null;
    }

    private void OnDisable() => Close();

    private void PositionPopup()
    {
        if (popup == null || anchor == null) return;

        var bound = anchor.worldBound;
        popup.style.left = bound.xMax + 8f;
        popup.style.top = bound.yMin;
        popup.RegisterCallback<GeometryChangedEvent>(OnPopupGeometryChanged);
    }

    private void OnPopupGeometryChanged(GeometryChangedEvent evt)
    {
        popup.UnregisterCallback<GeometryChangedEvent>(OnPopupGeometryChanged);
        if (popup == null || root == null) return;

        float maxLeft = Mathf.Max(0f, root.worldBound.width  - popup.resolvedStyle.width);
        float maxTop  = Mathf.Max(0f, root.worldBound.height - popup.resolvedStyle.height);
        popup.style.left = Mathf.Clamp(popup.resolvedStyle.left, 0f, maxLeft);
        popup.style.top  = Mathf.Clamp(popup.resolvedStyle.top,  0f, maxTop);
    }

    private void OnOutsidePointerDown(PointerDownEvent evt)
    {
        if (popup == null) return;
        if (popup.worldBound.Contains(evt.position)) return;
        Close();
    }

    private void Rebuild()
    {
        if (popup == null) return;

        RebuildFiltered();
        popup.Clear();
        BuildHeader();
        BuildTabs();
        BuildGrid();
        BuildFooter();
        ReapplyDragVisuals();
    }

    private void RebuildFiltered()
    {
        filtered.Clear();
        if (inventory == null || equipmentDatabase == null) return;

        var owned = inventory.EquipmentOwned;
        for (int i = 0; i < owned.Count; i++)
        {
            var item = equipmentDatabase.GetByID(owned[i]);
            if (item != null && item.Slot == slot)
                filtered.Add((i, item));
        }

        int maxPage = Mathf.Max(0, Mathf.CeilToInt(filtered.Count / (float)GridSize) - 1);
        page = Mathf.Clamp(page, 0, maxPage);
    }

    private void BuildHeader()
    {
        var header = new VisualElement();
        header.AddToClassList("backpack__header");

        header.Add(new Label(SlotName(slot)));

        var devToggle = new Toggle("DEV") { value = devNoConsume };
        devToggle.AddToClassList("backpack__dev");
        devToggle.RegisterValueChangedCallback(evt => devNoConsume = evt.newValue);
        header.Add(devToggle);

        if (HasEquipped())
        {
            var unequip = new Button(OnUnequipClicked) { text = "Quitar" };
            unequip.AddToClassList("backpack__unequip");
            header.Add(unequip);
        }

        var close = new Button(Close) { text = "✕" };
        close.AddToClassList("backpack__close");
        header.Add(close);

        popup.Add(header);
    }

    private void BuildTabs()
    {
        tabButtons.Clear();
        int pageCount = Mathf.CeilToInt(filtered.Count / (float)GridSize);
        if (pageCount <= 1) return;

        var tabs = new VisualElement();
        tabs.AddToClassList("backpack__tabs");

        for (int i = 0; i < pageCount; i++)
        {
            int p = i;
            var tab = new Button(() => { page = p; Rebuild(); }) { text = (i + 1).ToString() };
            tab.AddToClassList("backpack__tab");
            tab.EnableInClassList("backpack__tab--active", p == page);
            tabs.Add(tab);
            tabButtons.Add(tab);
        }

        popup.Add(tabs);
    }

    private void BuildGrid()
    {
        grid = new VisualElement();
        grid.AddToClassList("backpack__grid");

        int start = page * GridSize;
        for (int i = 0; i < GridSize; i++)
        {
            int filteredIndex = start + i;
            var cell = new VisualElement();
            cell.AddToClassList("backpack__cell");

            if (filteredIndex < filtered.Count)
            {
                var (globalIndex, item) = filtered[filteredIndex];
                ApplyRarityBorder(cell, item.Rarity);
                ApplyIconVisual(cell, item);

                cell.RegisterCallback<PointerDownEvent>(evt => OnCellPointerDown(evt, globalIndex, item, cell));
                cell.RegisterCallback<PointerEnterEvent>(_ => ShowName(item));
                cell.RegisterCallback<PointerLeaveEvent>(_ => ShowName(null));
            }
            else
            {
                cell.AddToClassList("backpack__cell--empty");
            }

            grid.Add(cell);
        }

        popup.Add(grid);
    }

    private void BuildFooter()
    {
        nameLabel = new Label(string.Empty);
        nameLabel.AddToClassList("backpack__name");
        popup.Add(nameLabel);
    }

    private void ReapplyDragVisuals()
    {
        if (!dragging || grid == null) return;

        int start = page * GridSize;
        int filteredIndex = filtered.FindIndex(e => e.GlobalIndex == dragGlobalIndex);
        if (filteredIndex < start || filteredIndex >= start + GridSize)
        {
            dragOriginCell = null;
            return;
        }

        var cells = grid.Children().ToList();
        int cellIndex = filteredIndex - start;
        if (cellIndex < 0 || cellIndex >= cells.Count) return;

        dragOriginCell = cells[cellIndex];
        dragOriginCell.AddToClassList("backpack__cell--dragging");
    }

    private void EquipItem(int globalIndex, EquipmentSO item)
    {
        if (item == null || dna == null) return;

        dna.Equipped ??= new Dictionary<EquipmentSlot, string>();
        string prev = dna.Equipped.TryGetValue(slot, out var prevId) ? prevId : null;
        dna.Equipped[slot] = item.ID;

        if (!devNoConsume)
        {
            inventory.RemoveEquipmentAt(globalIndex);
            if (!string.IsNullOrEmpty(prev))
                inventory.AddEquipment(prev);
            GameEvents.InventoryChanged(inventory);
        }

        GameEvents.RegistryChanged(registry);
        Rebuild();
    }

    private void OnUnequipClicked()
    {
        if (dna?.Equipped == null) return;
        if (!dna.Equipped.TryGetValue(slot, out var prev) || string.IsNullOrEmpty(prev)) return;

        dna.Equipped.Remove(slot);
        inventory.AddEquipment(prev);
        GameEvents.InventoryChanged(inventory);
        GameEvents.RegistryChanged(registry);
        Rebuild();
    }

    private bool HasEquipped() =>
        dna?.Equipped != null && dna.Equipped.TryGetValue(slot, out var id) && !string.IsNullOrEmpty(id);

    private void OnCellPointerDown(PointerDownEvent evt, int globalIndex, EquipmentSO item, VisualElement cell)
    {
        if (evt.button != 0) return;

        pointerDown = true;
        dragging = false;
        dragStart = evt.position;
        dragGlobalIndex = globalIndex;
        dragItem = item;
        dragOriginCell = cell;

        popup.CapturePointer(evt.pointerId);
    }

    private void OnPopupPointerMove(PointerMoveEvent evt)
    {
        if (!pointerDown) return;

        if (!dragging)
        {
            if (Vector2.Distance(evt.position, dragStart) < DragThreshold) return;
            dragging = true;
            CreateGhost();
            dragOriginCell?.AddToClassList("backpack__cell--dragging");
        }

        PositionGhost(evt.position);
        UpdateDropTarget(evt.position);
        SwitchPageUnderPointer(evt.position);
    }

    private void SwitchPageUnderPointer(Vector2 panelPos)
    {
        for (int i = 0; i < tabButtons.Count; i++)
        {
            if (i == page || !tabButtons[i].worldBound.Contains(panelPos)) continue;
            page = i;
            Rebuild();
            return;
        }
    }

    private void OnPopupPointerUp(PointerUpEvent evt)
    {
        if (!pointerDown) return;
        pointerDown = false;

        if (popup.HasPointerCapture(evt.pointerId))
            popup.ReleasePointer(evt.pointerId);

        bool wasDragging = dragging;
        int fromGlobal = dragGlobalIndex;
        var clickedItem = dragItem;
        Vector2 pos = evt.position;

        CancelDrag();

        if (!wasDragging)
        {
            EquipItem(fromGlobal, clickedItem);
            return;
        }

        int? dest = ResolveDropGlobalIndex(pos);
        if (dest.HasValue && dest.Value != fromGlobal)
        {
            inventory.MoveEquipment(fromGlobal, dest.Value);
            GameEvents.InventoryChanged(inventory);
            Rebuild();
        }
    }

    private int? ResolveDropGlobalIndex(Vector2 panelPos)
    {
        if (grid == null || filtered.Count == 0) return null;

        int cellIndex = -1;
        int i = 0;
        foreach (var child in grid.Children())
        {
            if (child.worldBound.Contains(panelPos)) { cellIndex = i; break; }
            i++;
        }
        if (cellIndex < 0) return null;

        int filteredIndex = page * GridSize + cellIndex;
        if (filteredIndex < filtered.Count)
            return filtered[filteredIndex].GlobalIndex;

        return filtered[filtered.Count - 1].GlobalIndex + 1;
    }

    private void CreateGhost()
    {
        ghost = new VisualElement();
        ghost.AddToClassList("backpack__ghost");
        ghost.pickingMode = PickingMode.Ignore;
        if (styleSheet != null) ghost.styleSheets.Add(styleSheet);
        ApplyIconVisual(ghost, dragItem);
        root.Add(ghost);
    }

    private void PositionGhost(Vector2 panelPos)
    {
        if (ghost == null) return;
        ghost.style.left = panelPos.x - GhostHalfSize;
        ghost.style.top  = panelPos.y - GhostHalfSize;
    }

    private void UpdateDropTarget(Vector2 panelPos)
    {
        dragTargetCell?.RemoveFromClassList("backpack__cell--drop-target");
        dragTargetCell = null;

        if (grid == null) return;
        foreach (var child in grid.Children())
        {
            if (!child.worldBound.Contains(panelPos)) continue;
            dragTargetCell = child;
            break;
        }

        dragTargetCell?.AddToClassList("backpack__cell--drop-target");
    }

    private void CancelDrag()
    {
        pointerDown = false;
        dragging = false;

        if (ghost != null)
        {
            ghost.RemoveFromHierarchy();
            ghost = null;
        }

        dragOriginCell?.RemoveFromClassList("backpack__cell--dragging");
        dragOriginCell = null;

        dragTargetCell?.RemoveFromClassList("backpack__cell--drop-target");
        dragTargetCell = null;

        dragItem = null;
    }

    private void ShowName(EquipmentSO item)
    {
        if (nameLabel == null) return;

        if (item == null)
        {
            nameLabel.text = string.Empty;
            return;
        }

        nameLabel.text = $"{(string.IsNullOrEmpty(item.Name) ? item.ID : item.Name)} · {item.Rarity}";
        nameLabel.style.color = RarityColor(item.Rarity);
    }

    private static void ApplyIconVisual(VisualElement el, EquipmentSO item)
    {
        if (item == null) return;

        if (item.Icon != null)
            el.style.backgroundImage = new StyleBackground(Background.FromSprite(item.Icon));
        else
            el.style.backgroundColor = item.IconColor;
    }

    private void ApplyRarityBorder(VisualElement el, Rarity rarity)
    {
        var c = RarityColor(rarity);
        el.style.borderTopColor    = c;
        el.style.borderBottomColor = c;
        el.style.borderLeftColor   = c;
        el.style.borderRightColor  = c;
    }

    private Color RarityColor(Rarity r) =>
        equipmentPalette != null ? equipmentPalette.RarityColor(r) : BodyPart.RarityColor(r);

    private static string SlotName(EquipmentSlot s) => s switch
    {
        EquipmentSlot.Weapon => "Arma",
        EquipmentSlot.Armor  => "Armadura",
        EquipmentSlot.Amulet => "Amuleto",
        _                    => s.ToString(),
    };
}
}
