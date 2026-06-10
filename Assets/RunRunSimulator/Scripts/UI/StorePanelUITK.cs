using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Shop screen: the visual face of a StoreManager's ShopCatalogSO. Three tabs split the
// catalog the way the player thinks about it — Furniture, WorldProps (tools), and
// Consumables (food + medicine) — and each row shows the live price (discount applied
// if today is inside the listing's window) plus a Buy button that routes to the matching
// StoreManager purchase flow. No wallet yet: price is display-only, buying is free.
//
// Opened by a world PanelTrigger via UIManager (UIPanelType.Store). Lives on the
// always-active UIManager object referencing its UIDocument (same split as the other
// UITK panels) and implements IUINavigable: left/right switch tab, up/down pick a row,
// Submit buys it.
//
// One StoreManager reference for now (single test store). Multiple stores with distinct
// catalogs is a later step — the trigger would point the panel at the right one.
[DisallowMultipleComponent]
public class StorePanelUITK : MonoBehaviour, IUINavigable
{
    [SerializeField] private UIDocument document;
    [SerializeField] private UIPanelType panel = UIPanelType.Store;
    [SerializeField] private StoreManager store;

    private enum Tab { Furniture, WorldProps, Consumables }
    private static readonly Tab[] Tabs = (Tab[])Enum.GetValues(typeof(Tab));
    private static readonly string[] TabLabels = { "Muebles", "Objetos", "Consumibles" };

    private const string TabClass         = "store-tab";
    private const string TabActiveClass   = "store-tab--active";
    private const string RowClass         = "store-row";
    private const string RowSelectedClass = "store-row--selected";

    // One displayable catalog entry, flattened so the row builder is type-agnostic.
    private struct Row
    {
        public string Name;
        public StoreShopData Shop;
        public Func<bool> Buy;   // routes to the right StoreManager flow
    }

    private VisualElement tabsContainer;
    private ScrollView list;
    private Label emptyLabel;
    private Button closeButton;

    private readonly List<VisualElement> tabEls = new List<VisualElement>();
    private readonly List<VisualElement> rowEls = new List<VisualElement>();
    private readonly List<Row> rows = new List<Row>();

    private int activeTab;
    private int selectedRow = -1;

    private ShopCatalogSO Catalog => store != null ? store.Catalog : null;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void OnEnable()
    {
        // Rebuild when our panel is opened so prices reflect the current date and any
        // catalog edits, and reset to the first tab (panel convention).
        UIManager.OnPanelToggleRequested += OnPanelToggle;
        UIManager.OnPanelSetRequested    += OnPanelSet;
    }

    private void OnDisable()
    {
        UIManager.OnPanelToggleRequested -= OnPanelToggle;
        UIManager.OnPanelSetRequested    -= OnPanelSet;
    }

    private void Start()
    {
        Resolve();
        BuildTabs();
        UIManager.RegisterNavigable(panel, this);
        Rebuild();
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= OnCloseClicked;
        UIManager.UnregisterNavigable(panel);
    }

    private void OnPanelToggle(UIPanelType p) { if (p == panel) ResetAndRebuild(); }
    private void OnPanelSet(UIPanelType p, bool show) { if (p == panel && show) ResetAndRebuild(); }

    private void ResetAndRebuild()
    {
        activeTab = 0;
        HighlightActiveTab();
        Rebuild();
    }

    // ── IUINavigable ──────────────────────────────────────────────

    public void OnUINavigate(Vector2 dir)
    {
        if      (dir.x >  0.5f) StepTab(1);
        else if (dir.x < -0.5f) StepTab(-1);
        else if (dir.y < -0.5f) StepRow(1);    // Navigate down is -y
        else if (dir.y >  0.5f) StepRow(-1);
    }

    public void OnUISubmit() => BuySelected();

    public bool OnUICancel() => false;   // let UIManager close on ESC

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

    // Flattens the active tab's listings into rows, each carrying the right Buy flow.
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
                var captured = def;
                rows.Add(new Row
                {
                    Name = NameOf(def.DisplayName, def.Id),
                    Shop = listing.Shop,
                    Buy  = () => store.BuyFurniture(captured),
                });
            }
            return;
        }

        // WorldProps = tools; Consumables = food + medicine. Both come from itemListings.
        foreach (var listing in catalog.ItemListings)
        {
            var def = listing?.Item;
            if (def == null) continue;
            if (!MatchesItemTab(tab, def.Category)) continue;

            var captured = def;
            rows.Add(new Row
            {
                Name = NameOf(def.DisplayName, def.Id),
                Shop = listing.Shop,
                Buy  = () => store.BuyWorldProp(captured),
            });
        }
    }

    private static bool MatchesItemTab(Tab tab, WorldPropCategory cat) =>
        tab == Tab.WorldProps
            ? cat == WorldPropCategory.Tool
            : cat == WorldPropCategory.Food || cat == WorldPropCategory.Medicine;

    private VisualElement BuildRow(Row row)
    {
        var el = new VisualElement();
        el.AddToClassList(RowClass);

        var name = new Label(row.Name);
        name.AddToClassList("store-row__name");
        el.Add(name);

        el.Add(BuildPrice(row.Shop));

        var buy = new Button(() => Purchase(row)) { text = "Comprar" };
        buy.AddToClassList("store-row__buy");
        el.Add(buy);

        return el;
    }

    // Price block: when a discount is active, strike the base and show the final price.
    private VisualElement BuildPrice(StoreShopData shop)
    {
        var box = new VisualElement();
        box.AddToClassList("store-row__price");

        bool discounted = shop.IsDiscountActive(DateTime.Now);
        int final = shop.FinalPrice(DateTime.Now);

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

    private void Purchase(Row row)
    {
        if (store == null) { Debug.LogWarning("[StorePanelUITK] No StoreManager assigned."); return; }
        row.Buy?.Invoke();   // furniture refreshes the browser; world prop drops a box
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
        emptyLabel    = root.Q<Label>("empty");
        closeButton   = root.Q<Button>("close-button");
        if (closeButton != null) closeButton.clicked += OnCloseClicked;
        list = root.Q<ScrollView>("list");
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
