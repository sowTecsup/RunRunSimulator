using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// Always-on play-mode hotbar HUD: 6 slots at the bottom, the active one highlighted.
// Pure display — it owns no state, it reads the inventory's hotbarSlots and the
// HotbarController's active index and re-renders on HotbarController.OnHotbarChanged
// (and the inventory reload). Items show their DisplayName (resolved via the item DB)
// until real icons exist.
//
// Lives on an always-active object referencing its UIDocument (same split as
// CreatureGridUITK) so it keeps refreshing even if toggled hidden.
[DisallowMultipleComponent]
public class HotbarHUDUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private ItemDatabaseSO database;

    private const string SlotClass       = "hotbar-slot";
    private const string SlotActiveClass = "hotbar-slot--active";

    private readonly List<Label> slotNames = new List<Label>();
    private bool built;

    private PlayerInventorySO Inventory =>
        GameManager.Instance != null ? GameManager.Instance.Inventory : null;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void OnEnable()
    {
        HotbarController.OnHotbarChanged       += Refresh;
        GameEvents.OnInventoryReloaded         += OnInventoryReloaded;
        BuildModeController.OnBuildModeChanged += OnBuildModeChanged;
    }

    private void OnDisable()
    {
        HotbarController.OnHotbarChanged       -= Refresh;
        GameEvents.OnInventoryReloaded         -= OnInventoryReloaded;
        BuildModeController.OnBuildModeChanged -= OnBuildModeChanged;
    }

    private void Start()
    {
        BuildSlots();
        Refresh();
    }

    private void OnInventoryReloaded(PlayerInventorySO inv) => Refresh();

    // The hotbar only has meaning in play (carry/use props). Build mode uses the
    // furniture browser instead, so hide the bar while building.
    private void OnBuildModeChanged(bool building)
    {
        var root = document != null ? document.rootVisualElement : null;
        if (root != null) root.style.display = building ? DisplayStyle.None : DisplayStyle.Flex;
    }

    // ── Build / refresh ───────────────────────────────────────────

    // Builds the 6 slot boxes once. Each slot carries an index label and a name label.
    private void BuildSlots()
    {
        var root = document != null ? document.rootVisualElement : null;
        if (root == null) return;
        var container = root.Q<VisualElement>("slots");
        if (container == null) return;

        container.Clear();
        slotNames.Clear();

        for (int i = 0; i < PlayerInventorySO.HotbarSize; i++)
        {
            var slot = new VisualElement { name = $"slot-{i}" };
            slot.AddToClassList(SlotClass);
            slot.pickingMode = PickingMode.Ignore;

            var index = new Label((i + 1).ToString());
            index.AddToClassList("hotbar-slot__index");
            index.pickingMode = PickingMode.Ignore;
            slot.Add(index);

            var nameLabel = new Label("");
            nameLabel.AddToClassList("hotbar-slot__name");
            nameLabel.pickingMode = PickingMode.Ignore;
            slot.Add(nameLabel);

            slotNames.Add(nameLabel);
            container.Add(slot);
        }
        built = true;
    }

    private void Refresh()
    {
        if (!built) BuildSlots();
        if (!built) return;

        var inv = Inventory;
        int active = HotbarController.Instance != null ? HotbarController.Instance.ActiveSlot : -1;

        for (int i = 0; i < slotNames.Count; i++)
        {
            string id   = inv != null ? inv.GetHotbarSlot(i) : null;
            slotNames[i].text = DisplayNameOf(id);

            // The slot box is the label's parent.
            var slot = slotNames[i].parent;
            slot?.EnableInClassList(SlotActiveClass, i == active);
        }
    }

    private string DisplayNameOf(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        var def = database != null ? database.GetById(id) : null;
        return def != null && !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : id;
    }
}
}
