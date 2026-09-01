using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

[DisallowMultipleComponent]
public class StoragePanelUITK : MonoBehaviour, IUINavigable
{
    [SerializeField] private UIDocument document;
    [SerializeField] private UIPanelType panel = UIPanelType.Storage;
    [SerializeField] private ItemDatabaseSO database;

    private const string RowClass         = "storage-row";
    private const string RowSelectedClass = "storage-row--selected";

    private ScrollView list;
    private Label emptyLabel;
    private Button closeButton;

    private readonly List<VisualElement> rows = new List<VisualElement>();
    private readonly List<string> rowIds = new List<string>();
    private int selectedIndex = -1;
    private PlayerInventorySO inventory;

    private void OnEnable()
    {
        inventory = GameManager.CurrentInventory;
        GameEvents.OnInventoryChanged  += Rebuild;
        GameEvents.OnInventoryReloaded += Rebuild;
    }

    private void OnDisable()
    {
        GameEvents.OnInventoryChanged  -= Rebuild;
        GameEvents.OnInventoryReloaded -= Rebuild;
    }

    private void Start()
    {
        WireChrome();
        UIManager.RegisterNavigable(panel, this);
        RebuildFromInventory();
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= OnCloseClicked;
        UIManager.UnregisterNavigable(panel);
    }

    public void OnUINavigate(Vector2 dir)
    {
        if (rows.Count == 0) return;
        int idx = selectedIndex < 0 ? 0 : selectedIndex;
        if      (dir.y < -0.5f) idx += 1;
        else if (dir.y >  0.5f) idx -= 1;
        Select(idx);
    }

    public void OnUISubmit() => EjectSelected();

    public bool OnUICancel() => false;

    private void Rebuild(PlayerInventorySO inv) { inventory = inv; RebuildFromInventory(); }

    private void RebuildFromInventory()
    {
        var container = ResolveList();
        if (container == null) return;

        container.Clear();
        rows.Clear();
        rowIds.Clear();

        var counts = new Dictionary<string, int>();
        var order  = new List<string>();
        var inv = inventory;
        if (inv != null)
        {
            foreach (var id in inv.WorldPropsStored)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (!counts.ContainsKey(id)) { counts[id] = 0; order.Add(id); }
                counts[id]++;
            }
        }

        foreach (var id in order)
        {
            var row = BuildRow(id, counts[id]);
            rows.Add(row);
            rowIds.Add(id);
            container.Add(row);
        }

        if (emptyLabel != null)
            emptyLabel.style.display = rows.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

        Select(rows.Count == 0 ? -1 : Mathf.Clamp(selectedIndex, 0, rows.Count - 1), false);
    }

    private VisualElement BuildRow(string id, int count)
    {
        var row = new VisualElement();
        row.AddToClassList(RowClass);

        var name = new Label(count > 1 ? $"{DisplayNameOf(id)}  x{count}" : DisplayNameOf(id));
        name.AddToClassList("storage-row__name");
        row.Add(name);

        var eject = new Button(() => Eject(id)) { text = Loc.Tr("ui.storage.eject") };
        eject.AddToClassList("storage-row__eject");
        row.Add(eject);

        return row;
    }

    private void Eject(string id)
    {
        if (StorageContainer.Instance == null)
        {
            Debug.LogWarning("[StoragePanelUITK] No StorageContainer in scene to eject from.");
            return;
        }
        StorageContainer.Instance.Eject(id);
    }

    private void EjectSelected()
    {
        if (selectedIndex >= 0 && selectedIndex < rowIds.Count)
            Eject(rowIds[selectedIndex]);
    }

    private void Select(int idx, bool _ = true)
    {
        selectedIndex = UiPanels.ClampSelection(rows.Count, idx);
        if (selectedIndex < 0) return;
        UiPanels.SetActiveIndex(rows, selectedIndex, RowSelectedClass);
    }

    private void WireChrome()
    {
        var root = UiPanels.RootOf(document);
        if (root == null) return;
        emptyLabel  = root.Q<Label>("empty");
        closeButton = root.Q<Button>("close-button");

        var titleLabel = root.Q<Label>(className: "storage-title");
        if (titleLabel != null) titleLabel.text = Loc.Tr("ui.storage.title");
        if (emptyLabel != null) emptyLabel.text = Loc.Tr("ui.storage.empty");
        if (closeButton != null) closeButton.clicked += OnCloseClicked;
    }

    private void OnCloseClicked() => UIManager.RequestPanelToggle(panel);

    private ScrollView ResolveList()
    {
        if (list != null && list.panel != null) return list;
        var root = UiPanels.RootOf(document);
        if (root == null) return null;
        list = root.Q<ScrollView>("list");
        return list;
    }

    private string DisplayNameOf(string id)
    {
        var def = database != null ? database.GetByID(id) : null;
        return def != null && !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : id;
    }
}
}
