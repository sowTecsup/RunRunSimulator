using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// Tooltip-style popup: equips items into a MoriMochi's slot from the player's
// equipment backpack (drag to reorder, click to equip, cell 0 "None" unequips).
// Reads the slot's free-placement grid via PlayerInventorySO.GetEquipment (nulls =
// empty cells). Opened on demand by a caller (e.g. CreatureGridUITK) via
// Open(dna, slot, anchor, registry); builds its own VisualElement into the anchor's
// panel root and rebuilds it after every mutation. Owns no persistence — mutates
// inventory/dna and fires GameEvents only.
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
    [AssetsOnly] [SerializeField] private StyleSheet themeStyleSheet;

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
    private IReadOnlyList<string> cells;
    private readonly List<VisualElement> tabButtons = new List<VisualElement>();

    private bool pointerDown;
    private bool dragging;
    private Vector2 dragStart;
    private int dragStoredIndex;
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
        popup.AddToClassList("mm-theme");
        popup.AddToClassList("backpack");
        if (themeStyleSheet != null) popup.styleSheets.Add(themeStyleSheet);
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

        cells = inventory != null ? inventory.GetEquipment(slot) : null;
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(((cells?.Count ?? 0) + 2) / (float)GridSize));
        page = Mathf.Clamp(page, 0, pageCount - 1);

        popup.Clear();
        BuildHeader();
        BuildTabs();
        BuildGrid();
        BuildFooter();
        ReapplyDragVisuals();
    }

    private void BuildHeader()
    {
        var header = new VisualElement();
        header.AddToClassList("backpack__header");
        header.Add(new Label(SlotName(slot)));
        popup.Add(header);
    }

    private void BuildTabs()
    {
        tabButtons.Clear();
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(((cells?.Count ?? 0) + 2) / (float)GridSize));
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
            int displayIndex = start + i;
            var cell = new VisualElement();
            cell.AddToClassList("backpack__cell");

            if (displayIndex == 0)
            {
                cell.AddToClassList("backpack__cell--none");
                cell.Add(new Label("None"));
                cell.RegisterCallback<PointerDownEvent>(OnNoneCellPointerDown);
                grid.Add(cell);
                continue;
            }

            int storedIndex = displayIndex - 1;
            EquipmentSO item = null;
            if (cells != null && storedIndex < cells.Count && !string.IsNullOrEmpty(cells[storedIndex]))
                item = equipmentDatabase.GetByID(cells[storedIndex]);

            if (item != null)
            {
                ApplyRarityBorder(cell, item.Rarity);
                ApplyIconVisual(cell, item);

                cell.RegisterCallback<PointerDownEvent>(evt => OnCellPointerDown(evt, storedIndex, item, cell));
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

        int displayIndex = dragStoredIndex + 1;
        int start = page * GridSize;
        if (displayIndex < start || displayIndex >= start + GridSize)
        {
            dragOriginCell = null;
            return;
        }

        var gridCells = grid.Children().ToList();
        int cellIndex = displayIndex - start;
        if (cellIndex < 0 || cellIndex >= gridCells.Count) return;

        dragOriginCell = gridCells[cellIndex];
        dragOriginCell.AddToClassList("backpack__cell--dragging");
    }

    private void EquipItem(int storedIndex, EquipmentSO item)
    {
        if (item == null || dna == null) return;

        dna.Equipped ??= new Dictionary<EquipmentSlot, string>();
        string prev = dna.Equipped.TryGetValue(slot, out var prevId) ? prevId : null;
        dna.Equipped[slot] = item.ID;

        if (!devNoConsume)
        {
            inventory.RemoveEquipmentAt(slot, storedIndex);
            if (!string.IsNullOrEmpty(prev))
                inventory.AddEquipment(slot, prev);
            GameEvents.InventoryChanged(inventory);
        }

        GameEvents.RegistryChanged(registry);
        Rebuild();
    }

    private void OnNoneCellPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0) return;
        if (dna?.Equipped == null) return;
        if (!dna.Equipped.TryGetValue(slot, out var prev) || string.IsNullOrEmpty(prev)) return;

        dna.Equipped.Remove(slot);

        if (!devNoConsume)
        {
            inventory.AddEquipment(slot, prev);
            GameEvents.InventoryChanged(inventory);
        }

        GameEvents.RegistryChanged(registry);
        Rebuild();
    }

    private void OnCellPointerDown(PointerDownEvent evt, int storedIndex, EquipmentSO item, VisualElement cell)
    {
        if (evt.button != 0) return;

        pointerDown = true;
        dragging = false;
        dragStart = evt.position;
        dragStoredIndex = storedIndex;
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
        int fromStored = dragStoredIndex;
        var clickedItem = dragItem;
        Vector2 pos = evt.position;

        CancelDrag();

        if (!wasDragging)
        {
            EquipItem(fromStored, clickedItem);
            return;
        }

        int? destDisplay = ResolveDropDisplayIndex(pos);
        if (!destDisplay.HasValue || destDisplay.Value == 0) return;

        int storedTo = destDisplay.Value - 1;
        if (storedTo != fromStored)
        {
            inventory.MoveEquipment(slot, fromStored, storedTo);
            GameEvents.InventoryChanged(inventory);
            Rebuild();
        }
    }

    private int? ResolveDropDisplayIndex(Vector2 panelPos)
    {
        if (grid == null) return null;

        int cellIndex = -1;
        int i = 0;
        foreach (var child in grid.Children())
        {
            if (child.worldBound.Contains(panelPos)) { cellIndex = i; break; }
            i++;
        }
        if (cellIndex < 0) return null;

        return page * GridSize + cellIndex;
    }

    private void CreateGhost()
    {
        ghost = new VisualElement();
        ghost.AddToClassList("mm-theme");
        ghost.AddToClassList("backpack__ghost");
        ghost.pickingMode = PickingMode.Ignore;
        if (themeStyleSheet != null) ghost.styleSheets.Add(themeStyleSheet);
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
