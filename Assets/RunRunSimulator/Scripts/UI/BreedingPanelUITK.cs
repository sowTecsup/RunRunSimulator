using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// In-game breeding screen (UI Toolkit), modal. This component owns two tabs:
//   • "Criar"     — pick a Father + Mother, preview both (stats + parts), see the
//                   estimated time, and start the breed (server-timed).
//   • "Incubando" — the eggs currently breeding as cards (Mother 💗 Father → time
//                   left); when an egg's timer hits 0 a Hatch button appears.
//
// Lives on the always-active UIManager object (like the grid/detail controllers)
// and fills its own UIDocument. It's a thin core: navigation and content for each
// tab are delegated to BreedingBreedTabPresenter / BreedingEggsTabPresenter, both
// reached via GameManager's registry and AsyncBreedingService.
//
// Keyboard/gamepad: implements IUINavigable with a hierarchical focus model
// (TabBar ⇄ content). ESC steps one level up (OnUICancel consumes it) and only
// closes the panel when already at the TabBar.
[DisallowMultipleComponent]
public class BreedingPanelUITK : MonoBehaviour, IUINavigable
{
    [Header("UI Toolkit setup")]
    [SerializeField] private UIDocument document;
    [SerializeField] private UIPanelType panel = UIPanelType.Breeding;
    [SerializeField] private int sortingOrder = 100;

    [Header("Data / services")]
    [Tooltip("Resolves part names/sets and effective stats. Shared SO asset.")]
    [SerializeField] private CreatureDatabaseSO database;
    [Tooltip("Starts the server-timed breed and hatches ready eggs.")]
    [SerializeField] private AsyncBreedingService asyncBreedingService;

    private enum Region { TabBar, Content }
    private Region region = Region.TabBar;

    // ── UI refs ──
    private TabView tabs;
    private Button closeButton;

    // ── State ──
    private CreatureRegistrySO registry;
    private bool wired;

    private BreedingBreedTabPresenter breed;
    private BreedingEggsTabPresenter eggs;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        if (document != null) document.sortingOrder = sortingOrder;
        var gm = GameManager.Instance;
        registry = gm != null ? gm.Registry : null;
    }

    private void OnEnable()
    {
        GameEvents.OnRegistryChanged  += OnRegistry;
        GameEvents.OnRegistryReloaded += OnRegistry;
        UIManager.OnPanelToggleRequested += OnPanelToggle;
        UIManager.OnPanelSetRequested    += OnPanelSet;
    }

    private void OnDisable()
    {
        GameEvents.OnRegistryChanged  -= OnRegistry;
        GameEvents.OnRegistryReloaded -= OnRegistry;
        UIManager.OnPanelToggleRequested -= OnPanelToggle;
        UIManager.OnPanelSetRequested    -= OnPanelSet;
    }

    private void Start()
    {
        Wire();
        UIManager.RegisterNavigable(panel, this);
    }

    // Tick the egg countdown only while the Incubando tab is visible.
    private void Update()
    {
        if (wired && tabs != null && tabs.selectedTabIndex == 1) eggs.Tick();
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= OnClose;
        breed?.Teardown();
        eggs?.Teardown();
        UIManager.UnregisterNavigable(panel);
    }

    // ── Wiring ────────────────────────────────────────────────────

    private void Wire()
    {
        if (wired) return;
        var root = document != null ? document.rootVisualElement : null;
        if (root == null) return;

        tabs        = root.Q<TabView>("tabs");
        closeButton = root.Q<Button>("close-button");

        WireStaticLabels(root);

        breed = new BreedingBreedTabPresenter(root, () => registry, database, asyncBreedingService, OnBred);
        eggs  = new BreedingEggsTabPresenter(root, () => registry, asyncBreedingService);

        if (closeButton != null) closeButton.clicked += OnClose;

        wired = true;
        RebuildAll();
        ResetFocus();
    }

    private static void WireStaticLabels(VisualElement root)
    {
        SetLabelText(root, "title", "ui.breeding.title");
        SetTabLabel(root, "tab-criar", "ui.breeding.tab.breed");
        SetTabLabel(root, "tab-incubando", "ui.breeding.tab.eggs");
        SetButtonText(root, "breed-button", "ui.breeding.breed.action");

        SetSideColumnTitle(root, "father-list", "ui.breeding.column.fathers");
        SetSideColumnTitle(root, "mother-list", "ui.breeding.column.mothers");
        SetSlotRoleLabel(root, "father-slot", "ui.breeding.role.father");
        SetSlotRoleLabel(root, "mother-slot", "ui.breeding.role.mother");
    }

    private static void SetLabelText(VisualElement root, string name, string key)
    {
        var label = root.Q<Label>(name);
        if (label != null) label.text = Loc.Tr(key);
    }

    private static void SetButtonText(VisualElement root, string name, string key)
    {
        var btn = root.Q<Button>(name);
        if (btn != null) btn.text = Loc.Tr(key);
    }

    private static void SetTabLabel(VisualElement root, string tabName, string key)
    {
        var tab = root.Q<Tab>(tabName);
        if (tab != null) tab.label = Loc.Tr(key);
    }

    private static void SetSideColumnTitle(VisualElement root, string listName, string key)
    {
        var label = root.Q<ScrollView>(listName)?.parent?.Q<Label>(className: "breed-col-title");
        if (label != null) label.text = Loc.Tr(key);
    }

    private static void SetSlotRoleLabel(VisualElement root, string slotName, string key)
    {
        var label = root.Q<VisualElement>(slotName)?.Q<Label>(className: "breed-slot-role");
        if (label != null) label.text = Loc.Tr(key);
    }

    private void OnClose() => UIManager.RequestPanelToggle(panel);

    private void OnBred()
    {
        if (tabs != null) tabs.selectedTabIndex = 1;   // jump to Incubando
        region = Region.TabBar;
        ClearAllFocus();
        SetTabBarFocus(true);
    }

    // ── Data / events ─────────────────────────────────────────────

    private void OnRegistry(CreatureRegistrySO reg)
    {
        registry = reg;
        if (!wired) { Wire(); return; }   // Wire() already rebuilds
        RebuildAll();
    }

    private void OnPanelToggle(UIPanelType p) { if (p == panel) ResetFocus(); }
    private void OnPanelSet(UIPanelType p, bool show) { if (p == panel && show) ResetFocus(); }

    private void RebuildAll()
    {
        breed.Rebuild();
        eggs.Rebuild();
    }

    // ── IUINavigable ──────────────────────────────────────────────

    private ITabPresenter ActivePresenter()
    {
        int t = tabs != null ? tabs.selectedTabIndex : 0;
        switch (t)
        {
            case 0: return breed;
            case 1: return eggs;
            default: return null;
        }
    }

    public void OnUINavigate(Vector2 dir)
    {
        if (breed != null && breed.Busy) return;

        int h = dir.x >  0.5f ? 1 : dir.x < -0.5f ? -1 : 0;
        int v = dir.y < -0.5f ? 1 : dir.y >  0.5f ? -1 : 0;   // down = +1
        if (h == 0 && v == 0) return;

        if (region == Region.TabBar)
        {
            if (h != 0 && tabs != null) tabs.selectedTabIndex = Mathf.Clamp(tabs.selectedTabIndex + h, 0, 1);
            else if (v > 0) EnterContent();
        }
        else
        {
            var p = ActivePresenter();
            if (p == null || !p.Navigate(h, v)) { region = Region.TabBar; SetTabBarFocus(true); }
        }
    }

    public void OnUISubmit()
    {
        if (breed != null && breed.Busy) return;

        if (region == Region.TabBar) EnterContent();
        else ActivePresenter()?.Submit();
    }

    public bool OnUICancel()
    {
        if (breed != null && breed.Busy) return true;

        if (region == Region.TabBar) return false;

        var p = ActivePresenter();
        if (p != null && p.Cancel()) return true;

        ClearAllFocus();
        region = Region.TabBar;
        SetTabBarFocus(true);
        return true;
    }

    private void EnterContent()
    {
        var p = ActivePresenter();
        if (p == null) return;
        SetTabBarFocus(false);
        region = Region.Content;
        p.Enter();
    }

    private void SetTabBarFocus(bool on) { tabs?.EnableInClassList("tabbar-focused", on); }

    private void ClearAllFocus()
    {
        breed?.ClearFocus();
        eggs?.ClearFocus();
        SetTabBarFocus(false);
    }

    private void ResetFocus()
    {
        if (!wired) return;
        tabs.selectedTabIndex = 0;
        ClearAllFocus();
        region = Region.TabBar;
        SetTabBarFocus(true);
    }
}
}
