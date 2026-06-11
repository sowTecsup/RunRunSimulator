using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Shop screen: the visual face of a StoreManager's ShopCatalogSO. Three tabs split the
// catalog the way the player thinks about it — Furniture, WorldProps (tools), and
// Consumables (food + medicine). Each row shows the live price, the remaining stock, and
// a Buy button. The panel enforces wallet + stock via StoreManager and shows a transient
// toast ("¡Sin fondos!", "¡Agotado!") on failure.
//
// Opened by a world PanelTrigger via UIManager (UIPanelType.Store). Implements
// IUINavigable: left/right switch tab, up/down pick a row, Submit buys it.
[DisallowMultipleComponent]
public class StorePanelUITK : MonoBehaviour, IUINavigable
{
    [SerializeField] private UIDocument document;
    [SerializeField] private UIPanelType panel = UIPanelType.Store;
    [SerializeField] private StoreManager store;

    private enum Tab { Furniture, WorldProps, Consumables }
    private static readonly Tab[]   Tabs      = (Tab[])Enum.GetValues(typeof(Tab));
    private static readonly string[] TabLabels = { "Muebles", "Objetos", "Consumibles" };

    private const string TabClass         = "store-tab";
    private const string TabActiveClass   = "store-tab--active";
    private const string RowClass         = "store-row";
    private const string RowSelectedClass = "store-row--selected";

    // One displayable catalog entry, flattened so the row builder is type-agnostic.
    private struct Row
    {
        public string          Name;
        public StoreShopData   Shop;        // class reference — mutations hit the catalog
        public Func<BuyResult> Buy;
    }

    private VisualElement tabsContainer;
    private ScrollView    list;
    private Label         emptyLabel;
    private Label         balanceLabel;
    private Label         notifyLabel;
    private Button        closeButton;

    private readonly List<VisualElement> tabEls = new List<VisualElement>();
    private readonly List<VisualElement> rowEls = new List<VisualElement>();
    private readonly List<Row>           rows   = new List<Row>();

    private int activeTab;
    private int selectedRow = -1;
    private Coroutine notifyCoroutine;

    private ShopCatalogSO Catalog => store != null ? store.Catalog : null;

    // ── Lifecycle ─────────────────────────────────────────────────

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

        var inv = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
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

    // ── IUINavigable ──────────────────────────────────────────────

    public void OnUINavigate(Vector2 dir)
    {
        if      (dir.x >  0.5f) StepTab(1);
        else if (dir.x < -0.5f) StepTab(-1);
        else if (dir.y < -0.5f) StepRow(1);
        else if (dir.y >  0.5f) StepRow(-1);
    }

    public void OnUISubmit() => BuySelected();

    public bool OnUICancel() => false;

    // ── Balance ───────────────────────────────────────────────────

    private void RefreshBalance(PlayerInventorySO inv)
    {
        if (balanceLabel == null) return;
        balanceLabel.text = inv != null ? $"Saldo: {inv.Dabloons:N0} Dabloons" : "";
    }

    // ── Toast notify ──────────────────────────────────────────────

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

    // ── Tabs ──────────────────────────────────────────────────────

    private void BuildTabs()
    {
        if (tabsContainer == null) return;
        tabsContainer.Clear();
        tabEls.Clear();

        for (int i = 0; i < Tabs.Length; i++)
        {
            int idx = i;
            var tab = new Label(TabLabels[i]);
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

    private void HighlightActiveTab()
    {
        for (int i = 0; i < tabEls.Count; i++)
            tabEls[i].EnableInClassList(TabActiveClass, i == activeTab);
    }

    // ── Build / refresh ───────────────────────────────────────────

    private void Rebuild()
    {
        var container = ResolveList();
        if (container == null) return;

        container.Clear();
        rowEls.Clear();
        rows.Clear();

        CollectRows((Tab)activeTab);

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

    private void CollectRows(Tab tab)
    {
        var catalog = Catalog;
        if (catalog == null) return;

        if (tab == Tab.Furniture)
        {
            foreach (var listing in catalog.FurnitureListings)
            {
                var def = listing?.Furniture;
                if (def == null) continue;
                var capturedDef  = def;
                var capturedShop = listing.Shop;
                rows.Add(new Row
                {
                    Name = NameOf(def.DisplayName, def.Id),
                    Shop = capturedShop,
                    Buy  = () => store.BuyFurniture(capturedDef, capturedShop),
                });
            }
            return;
        }

        foreach (var listing in catalog.ItemListings)
        {
            var def = listing?.Item;
            if (def == null) continue;
            if (!MatchesItemTab(tab, def.Category)) continue;
            var capturedDef  = def;
            var capturedShop = listing.Shop;
            rows.Add(new Row
            {
                Name = NameOf(def.DisplayName, def.Id),
                Shop = capturedShop,
                Buy  = () => store.BuyWorldProp(capturedDef, capturedShop),
            });
        }
    }

    private static bool MatchesItemTab(Tab tab, WorldPropCategory cat) =>
        tab == Tab.WorldProps
            ? cat == WorldPropCategory.Tool
            : cat == WorldPropCategory.Food || cat == WorldPropCategory.Medicine;

    private VisualElement BuildRow(Row row)
    {
        var  serverNow      = GameManager.Instance != null ? GameManager.Instance.ServerNow : DateTime.Now;
        bool discountActive = Catalog?.IsDiscountActive(serverNow) ?? false;

        var el = new VisualElement();
        el.AddToClassList(RowClass);

        var name = new Label(row.Name);
        name.AddToClassList("store-row__name");
        el.Add(name);

        el.Add(BuildPrice(row.Shop, discountActive));
        el.Add(BuildStock(row.Shop));

        bool canBuy = row.Shop == null || row.Shop.InStock;
        var buy = new Button(() => Purchase(row)) { text = "Comprar" };
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
            var was = new Label($"¢{shop.BasePrice}");
            was.AddToClassList("store-price__was");
            box.Add(was);
        }

        var now = new Label($"¢{final}");
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
            label.text = "Agotado";
            label.AddToClassList("store-row__stock--empty");
        }
        else
        {
            label.text = $"x{shop.CurrentStock}";
            if (shop.CurrentStock <= 2) label.AddToClassList("store-row__stock--low");
        }
        return label;
    }

    // ── Purchase ──────────────────────────────────────────────────

    private void Purchase(Row row)
    {
        if (store == null) { Debug.LogWarning("[StorePanelUITK] No StoreManager assigned."); return; }

        var result = row.Buy.Invoke();
        switch (result)
        {
            case BuyResult.Success:
                Rebuild();   // refresh stock counts + button state
                break;
            case BuyResult.OutOfStock:
                ShowNotify("¡Artículo agotado!");
                break;
            case BuyResult.InsufficientFunds:
                ShowNotify("¡Dabloons insuficientes!");
                break;
            case BuyResult.AlreadyOwned:
                ShowNotify("¡Ya tienes ese mueble!");
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
        if (rowEls.Count == 0) { selectedRow = -1; return; }
        selectedRow = Mathf.Clamp(idx, 0, rowEls.Count - 1);
        for (int i = 0; i < rowEls.Count; i++)
            rowEls[i].EnableInClassList(RowSelectedClass, i == selectedRow);
        if (list != null) list.ScrollTo(rowEls[selectedRow]);
    }

    // ── Plumbing ──────────────────────────────────────────────────

    private void Resolve()
    {
        var root = document != null ? document.rootVisualElement : null;
        if (root == null) return;

        tabsContainer = root.Q<VisualElement>("tabs");
        list          = root.Q<ScrollView>("list");
        emptyLabel    = root.Q<Label>("empty");
        balanceLabel  = root.Q<Label>("balance");
        notifyLabel   = root.Q<Label>("notify");
        closeButton   = root.Q<Button>("close-button");

        if (notifyLabel  != null) notifyLabel.style.display  = DisplayStyle.None;
        if (closeButton  != null) closeButton.clicked        += OnCloseClicked;
    }

    private ScrollView ResolveList()
    {
        if (list != null && list.panel != null) return list;
        var root = document != null ? document.rootVisualElement : null;
        if (root == null) return null;
        list = root.Q<ScrollView>("list");
        return list;
    }

    private void OnCloseClicked() => UIManager.RequestPanelToggle(panel);

    private static string NameOf(string display, string id) =>
        string.IsNullOrEmpty(display) ? id : display;
}
