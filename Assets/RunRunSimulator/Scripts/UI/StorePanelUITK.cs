using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

[DisallowMultipleComponent]
public class StorePanelUITK : MonoBehaviour, IUINavigable
{
    [SerializeField] private UIDocument document;
    [SerializeField] private UIPanelType panel = UIPanelType.Store;
    [SerializeField] private StoreManager store;

    private static readonly StoreRows.Tab[] Tabs = (StoreRows.Tab[])Enum.GetValues(typeof(StoreRows.Tab));

    private const string TabClass         = "store-tab";
    private const string TabActiveClass   = "store-tab--active";
    private const string RowClass         = "store-row";
    private const string RowSelectedClass = "store-row--selected";

    private VisualElement tabsContainer;
    private ScrollView    list;
    private Label         emptyLabel;
    private Label         balanceLabel;
    private Label         notifyLabel;
    private Button        closeButton;

    private readonly List<VisualElement> tabEls = new List<VisualElement>();
    private readonly List<VisualElement> rowEls = new List<VisualElement>();
    private readonly List<StoreRows.Row> rows   = new List<StoreRows.Row>();

    private int activeTab;
    private int selectedRow = -1;
    private Coroutine notifyCoroutine;

    private ShopCatalogSO Catalog => store != null ? store.Catalog : null;

    private void OnEnable()
    {
        UIManager.OnPanelToggleRequested += OnPanelToggle;
        UIManager.OnPanelSetRequested    += OnPanelSet;
        GameEvents.OnInventoryChanged    += OnInventoryChanged;
        GameEvents.OnInventoryReloaded   += OnInventoryChanged;
    }

    private void OnDisable()
    {
        UIManager.OnPanelToggleRequested -= OnPanelToggle;
        UIManager.OnPanelSetRequested    -= OnPanelSet;
        GameEvents.OnInventoryChanged    -= OnInventoryChanged;
        GameEvents.OnInventoryReloaded   -= OnInventoryChanged;
    }

    private void Start()
    {
        Resolve();
        BuildTabs();
        UIManager.RegisterNavigable(panel, this);

        var inv = GameManager.CurrentInventory;
        RefreshBalance(inv);
        Rebuild();
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= OnCloseClicked;
        UIManager.UnregisterNavigable(panel);
    }

    private void OnPanelToggle(UIPanelType p) { if (p == panel) ResetAndRebuild(); }
    private void OnPanelSet(UIPanelType p, bool show) { if (p == panel && show) ResetAndRebuild(); }

    private void OnInventoryChanged(PlayerInventorySO inv) => RefreshBalance(inv);

    private void ResetAndRebuild()
    {
        store?.RestockIfNeeded();
        activeTab = 0;
        HighlightActiveTab();
        Rebuild();
    }

    public void OnUINavigate(Vector2 dir)
    {
        if      (dir.x >  0.5f) StepTab(1);
        else if (dir.x < -0.5f) StepTab(-1);
        else if (dir.y < -0.5f) StepRow(1);
        else if (dir.y >  0.5f) StepRow(-1);
    }

    public void OnUISubmit() => BuySelected();

    public bool OnUICancel() => false;

    private void RefreshBalance(PlayerInventorySO inv)
    {
        if (balanceLabel == null) return;
        balanceLabel.text = inv != null ? Loc.Tr("ui.store.balance", inv.Balance(Currency.Dabloons)) : "";
    }

    private void ShowNotify(string message)
    {
        if (notifyLabel == null) return;
        notifyLabel.text                  = message;
        notifyLabel.style.display         = DisplayStyle.Flex;
        if (notifyCoroutine != null) StopCoroutine(notifyCoroutine);
        notifyCoroutine = StartCoroutine(HideNotifyAfterDelay(2.5f));
    }

    private IEnumerator HideNotifyAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (notifyLabel != null) notifyLabel.style.display = DisplayStyle.None;
        notifyCoroutine = null;
    }

    private void BuildTabs()
    {
        if (tabsContainer == null) return;
        tabsContainer.Clear();
        tabEls.Clear();

        for (int i = 0; i < Tabs.Length; i++)
        {
            int idx = i;
            var tab = new Label(StoreRows.TabLabel(Tabs[i]));
            tab.AddToClassList(TabClass);
            tab.RegisterCallback<ClickEvent>(_ => SetTab(idx));
            tabsContainer.Add(tab);
            tabEls.Add(tab);
        }
        HighlightActiveTab();
    }

    private void StepTab(int dir) =>
        SetTab(((activeTab + dir) % Tabs.Length + Tabs.Length) % Tabs.Length);

    private void SetTab(int idx)
    {
        activeTab = Mathf.Clamp(idx, 0, Tabs.Length - 1);
        HighlightActiveTab();
        Rebuild();
    }

    private void HighlightActiveTab() => UiPanels.SetActiveIndex(tabEls, activeTab, TabActiveClass);

    private void Rebuild()
    {
        var container = ResolveList();
        if (container == null) return;

        container.Clear();
        rowEls.Clear();
        rows.Clear();

        StoreRows.Collect((StoreRows.Tab)activeTab, Catalog, store, rows);

        foreach (var row in rows)
        {
            var el = BuildRow(row);
            rowEls.Add(el);
            container.Add(el);
        }

        if (emptyLabel != null)
            emptyLabel.style.display = rows.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

        Select(rows.Count == 0 ? -1 : Mathf.Clamp(selectedRow, 0, rows.Count - 1));
    }

    private VisualElement BuildRow(StoreRows.Row row)
    {
        var  serverNow      = GameManager.Now;
        bool discountActive = Catalog?.IsDiscountActive(serverNow) ?? false;

        var el = new VisualElement();
        el.AddToClassList(RowClass);

        var name = new Label(row.Name);
        name.AddToClassList("store-row__name");
        el.Add(name);

        el.Add(BuildPrice(row.Shop, discountActive));
        el.Add(BuildStock(row.Shop));

        bool canBuy = row.Shop == null || row.Shop.InStock;
        var buy = new Button(() => Purchase(row)) { text = Loc.Tr("ui.store.buy") };
        buy.AddToClassList("store-row__buy");
        buy.SetEnabled(canBuy);
        if (!canBuy) buy.AddToClassList("store-row__buy--disabled");
        el.Add(buy);

        return el;
    }

    private VisualElement BuildPrice(StoreShopData shop, bool discountActive)
    {
        var box = new VisualElement();
        box.AddToClassList("store-row__price");

        bool discounted = discountActive && shop?.DiscountBase > 0f;
        int  final      = shop != null ? shop.FinalPrice(discounted) : 0;

        if (discounted)
        {
            var was = new Label(Loc.Tr("ui.store.price", shop.BasePrice));
            was.AddToClassList("store-price__was");
            box.Add(was);
        }

        var now = new Label(Loc.Tr("ui.store.price", final));
        now.AddToClassList(discounted ? "store-price__now--sale" : "store-price__now");
        box.Add(now);

        return box;
    }

    private Label BuildStock(StoreShopData shop)
    {
        var label = new Label();
        label.AddToClassList("store-row__stock");

        if (shop == null || shop.IsUnlimited)
        {
            label.style.display = DisplayStyle.None;
            return label;
        }

        if (shop.CurrentStock <= 0)
        {
            label.text = Loc.Tr("ui.store.stock_empty");
            label.AddToClassList("store-row__stock--empty");
        }
        else
        {
            label.text = Loc.Tr("ui.store.stock_count", shop.CurrentStock);
            if (shop.CurrentStock <= 2) label.AddToClassList("store-row__stock--low");
        }
        return label;
    }

    private void Purchase(StoreRows.Row row)
    {
        if (store == null) { Debug.LogWarning("[StorePanelUITK] No StoreManager assigned."); return; }

        var result = row.Buy.Invoke();
        switch (result)
        {
            case BuyResult.Success:
                Rebuild();
                break;
            case BuyResult.OutOfStock:
                ShowNotify(Loc.Tr("ui.store.toast.out_of_stock"));
                break;
            case BuyResult.InsufficientFunds:
                ShowNotify(Loc.Tr("ui.store.toast.insufficient_funds"));
                break;
            case BuyResult.AlreadyOwned:
                ShowNotify(Loc.Tr("ui.store.toast.already_owned"));
                break;
        }
    }

    private void BuySelected()
    {
        if (selectedRow >= 0 && selectedRow < rows.Count) Purchase(rows[selectedRow]);
    }

    private void StepRow(int dir)
    {
        if (rows.Count == 0) return;
        Select(((selectedRow + dir) % rows.Count + rows.Count) % rows.Count);
    }

    private void Select(int idx)
    {
        selectedRow = UiPanels.ClampSelection(rowEls.Count, idx);
        if (selectedRow < 0) return;
        UiPanels.SetActiveIndex(rowEls, selectedRow, RowSelectedClass);
        if (list != null) list.ScrollTo(rowEls[selectedRow]);
    }

    private void Resolve()
    {
        var root = UiPanels.RootOf(document);
        if (root == null) return;

        tabsContainer = root.Q<VisualElement>("tabs");
        list          = root.Q<ScrollView>("list");
        emptyLabel    = root.Q<Label>("empty");
        balanceLabel  = root.Q<Label>("balance");
        notifyLabel   = root.Q<Label>("notify");
        closeButton   = root.Q<Button>("close-button");

        var titleLabel = root.Q<Label>(className: "store-title");
        if (titleLabel  != null) titleLabel.text          = Loc.Tr("ui.store.title");
        if (emptyLabel  != null) emptyLabel.text          = Loc.Tr("ui.store.empty");
        if (notifyLabel  != null) notifyLabel.style.display  = DisplayStyle.None;
        if (closeButton  != null) closeButton.clicked        += OnCloseClicked;
    }

    private ScrollView ResolveList()
    {
        if (list != null && list.panel != null) return list;
        var root = UiPanels.RootOf(document);
        if (root == null) return null;
        list = root.Q<ScrollView>("list");
        return list;
    }

    private void OnCloseClicked() => UIManager.RequestPanelToggle(panel);
}
}
