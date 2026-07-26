using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// In-game combat screen (UI Toolkit), modal. This component owns three tabs:
//   • "Batalla Online" — pick one of yours, see it (stats + parts), send it to async
//                        combat (Instant or Timer). Right column = team (placeholder).
//   • "Resultados"     — your creatures in queue / with results; the right pane shows
//                        the battle log (opponent · turns · big win/lose).
//   • "Historial"      — every past combat, replayable.
// A fourth tab, "Equipo 3v3", lives in the same TabView but is owned end-to-end by
// the sibling component CombatLineupUITK (local 3v3 lineup combat, drag & drop).
//
// Lives on the always-active UIManager object, fills its own UIDocument. Action
// controller (like CombatController): reaches registry/config via GameManager and
// fights through CombatService / AsyncCombatService. Same hierarchical IUINavigable
// focus model as the breeding panel (TabBar ⇄ content ⇄ list).
[DisallowMultipleComponent]
public class CombatPanelUITK : MonoBehaviour, IUINavigable
{
    [Header("UI Toolkit setup")]
    [SerializeField] private UIDocument document;
    [SerializeField] private UIPanelType panel = UIPanelType.Combat;
    [SerializeField] private int sortingOrder = 100;

    [Header("Data / services")]
    [SerializeField] private CreatureDatabaseSO database;
    [SerializeField] private AsyncCombatService asyncCombatService;

    private enum Region { TabBar, Content }
    private Region region = Region.TabBar;

    // ── UI refs ──
    private TabView tabs;
    private Button closeButton;

    // ── State ──
    private CreatureRegistrySO registry;
    private bool wired;

    private CombatOnlineTabPresenter online;
    private CombatResultsTabPresenter results;
    private CombatHistoryTabPresenter history;

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
        GameEvents.OnCombatLogged     += OnCombatLogged;
        UIManager.OnPanelToggleRequested += OnPanelToggle;
        UIManager.OnPanelSetRequested    += OnPanelSet;
    }

    private void OnDisable()
    {
        GameEvents.OnRegistryChanged  -= OnRegistry;
        GameEvents.OnRegistryReloaded -= OnRegistry;
        GameEvents.OnCombatLogged     -= OnCombatLogged;
        UIManager.OnPanelToggleRequested -= OnPanelToggle;
        UIManager.OnPanelSetRequested    -= OnPanelSet;
    }

    private void Start()
    {
        Wire();
        UIManager.RegisterNavigable(panel, this);
    }

    // Tick the queue countdown only while the Resultados tab is visible.
    private void Update()
    {
        if (wired && tabs != null && tabs.selectedTabIndex == 1) results.Tick();
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= OnClose;
        online?.Teardown();
        results?.Teardown();
        history?.Teardown();
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

        var titleLabel = root.Q<Label>("title");
        if (titleLabel != null) titleLabel.text = Loc.Tr("ui.combat.title");

        var tabOnline  = root.Q<Tab>("tab-online");
        var tabResults = root.Q<Tab>("tab-results");
        var tabHistory = root.Q<Tab>("tab-history");
        var tabLineup  = root.Q<Tab>("tab-lineup");
        if (tabOnline  != null) tabOnline.label  = Loc.Tr("ui.combat.tab.online");
        if (tabResults != null) tabResults.label = Loc.Tr("ui.combat.tab.results");
        if (tabHistory != null) tabHistory.label = Loc.Tr("ui.combat.tab.history");
        if (tabLineup  != null) tabLineup.label  = Loc.Tr("ui.combat.tab.lineup");

        if (tabOnline != null)
        {
            var sideTitles = tabOnline.Query<Label>(className: "cbt-col-title").ToList();
            if (sideTitles.Count > 0) sideTitles[0].text = Loc.Tr("ui.combat.available");
            if (sideTitles.Count > 1) sideTitles[1].text = Loc.Tr("ui.combat.team");
            var placeholder = tabOnline.Q<Label>(className: "cbt-placeholder");
            if (placeholder != null) placeholder.text = Loc.Tr("ui.combat.comingsoon");
        }

        if (tabResults != null)
        {
            var clockCaption = tabResults.Q<Label>(className: "cbt-clock-caption");
            if (clockCaption != null) clockCaption.text = Loc.Tr("ui.combat.nextcombat");
        }

        if (tabLineup != null)
        {
            var rosterTitles = tabLineup.Query<Label>(className: "cbt-col-title").ToList();
            if (rosterTitles.Count > 0) rosterTitles[0].text = Loc.Tr("ui.combat.teama");
            if (rosterTitles.Count > 1) rosterTitles[1].text = Loc.Tr("ui.combat.teamb");
        }

        online  = new CombatOnlineTabPresenter(root, () => registry, database, asyncCombatService, OnEnqueued);
        results = new CombatResultsTabPresenter(root, () => registry, asyncCombatService);
        history = new CombatHistoryTabPresenter(root, () => registry);

        if (closeButton != null) closeButton.clicked += OnClose;

        wired = true;
        RebuildAll();
        ResetFocus();
    }

    private void OnClose() => UIManager.RequestPanelToggle(panel);

    private void OnEnqueued()
    {
        if (tabs != null) tabs.selectedTabIndex = 1;   // jump to Resultados
        region = Region.TabBar;
        ClearAllFocus();
        SetTabBarFocus(true);
        results.Rebuild();
    }

    // ── Data / events ─────────────────────────────────────────────

    private void OnRegistry(CreatureRegistrySO reg)
    {
        registry = reg;
        if (!wired) { Wire(); return; }
        RebuildAll();
    }

    // A finished combat already landed in the creatures' CombatHistory by the time
    // this fires; just refresh the queue (it left the pool) and the history list.
    private void OnCombatLogged(CombatLogEntry entry)
    {
        if (!wired) return;
        results.Rebuild();
        history.Rebuild();
    }

    private void OnPanelToggle(UIPanelType p) { if (p == panel) ResetFocus(); }
    private void OnPanelSet(UIPanelType p, bool show) { if (p == panel && show) ResetFocus(); }

    private void RebuildAll()
    {
        online.Rebuild();
        results.Rebuild();
        history.Rebuild();
    }

    // ── IUINavigable ──────────────────────────────────────────────

    private ITabPresenter ActivePresenter()
    {
        int t = tabs != null ? tabs.selectedTabIndex : 0;
        switch (t)
        {
            case 0: return online;
            case 1: return results;
            case 2: return history;
            default: return null;
        }
    }

    public void OnUINavigate(Vector2 dir)
    {
        int h = dir.x >  0.5f ? 1 : dir.x < -0.5f ? -1 : 0;
        int v = dir.y < -0.5f ? 1 : dir.y >  0.5f ? -1 : 0;   // down = +1
        if (h == 0 && v == 0) return;

        if (region == Region.TabBar)
        {
            if (h != 0 && tabs != null) tabs.selectedTabIndex = Mathf.Clamp(tabs.selectedTabIndex + h, 0, 3);
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
        if (region == Region.TabBar) EnterContent();
        else ActivePresenter()?.Submit();
    }

    public bool OnUICancel()
    {
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
        int t = tabs != null ? tabs.selectedTabIndex : 0;
        if (t == 3) { SetTabBarFocus(true); return; }   // Equipo 3v3: mouse-driven by CombatLineupUITK

        var p = ActivePresenter();
        if (p == null) return;
        SetTabBarFocus(false);
        region = Region.Content;
        p.Enter();
    }

    private void SetTabBarFocus(bool on) { tabs?.EnableInClassList("tabbar-focused", on); }

    private void ClearAllFocus()
    {
        online?.ClearFocus();
        results?.ClearFocus();
        history?.ClearFocus();
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
