using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

[DisallowMultipleComponent]
public class HotbarHUDUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private ItemDatabaseSO database;

    private const string SlotClass       = "hotbar-slot";
    private const string SlotActiveClass = "hotbar-slot--active";

    private readonly List<Label> slotNames = new List<Label>();
    private bool built;
    private PlayerInventorySO inventory;

    private void OnEnable()
    {
        inventory = GameManager.CurrentInventory;
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

    private void OnInventoryReloaded(PlayerInventorySO inv) { inventory = inv; Refresh(); }

    private void OnBuildModeChanged(bool building)
    {
        var root = UiPanels.RootOf(document);
        if (root != null) root.style.display = building ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void BuildSlots()
    {
        var root = UiPanels.RootOf(document);
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

        var inv = inventory;
        int active = HotbarController.Instance != null ? HotbarController.Instance.ActiveSlot : -1;

        for (int i = 0; i < slotNames.Count; i++)
        {
            string id   = inv != null ? inv.GetHotbarSlot(i) : null;
            slotNames[i].text = DisplayNameOf(id);

            var slot = slotNames[i].parent;
            slot?.EnableInClassList(SlotActiveClass, i == active);
        }
    }

    private string DisplayNameOf(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        var def = database != null ? database.GetByID(id) : null;
        return def != null && !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : id;
    }
}
}
