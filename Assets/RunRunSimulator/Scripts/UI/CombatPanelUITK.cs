using System;
using System.Collections.Generic;
using System.Linq;
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
public partial class CombatPanelUITK : MonoBehaviour, IUINavigable
{
    [Header("UI Toolkit setup")]
    [SerializeField] private UIDocument document;
    [SerializeField] private UIPanelType panel = UIPanelType.Combat;
    [SerializeField] private int sortingOrder = 100;

    [Header("Data / services")]
    [SerializeField] private CreatureDatabaseSO database;
    [SerializeField] private AsyncCombatService asyncCombatService;

    private enum Region { TabBar, T1List, T1Actions, T3List, T4List }
    private Region region = Region.TabBar;

    private const string Focus = "cbt-focus";

    // ── UI refs ──
    private TabView tabs;
    private Button closeButton;
    // Tab 1
    private ScrollView onlineList;
    private VisualElement onlineImg, onlineParts;
    private Label onlineName, onlineStats;
    private Button btnInstant, btnTimer;
    // Tab 3 (Resultados: cola + reloj al próximo tick)
    private Button btnRefresh;
    private ScrollView resultsList;
    private Label queueClock, queueEmpty;
    // Tab 4 (Historial)
    private DropdownField historyFilter;
    private ScrollView historyList, histLines;
    private Label historyEmpty, histOpponent, histDate, histOutcome;
    private Button histReplayBtn;

    // ── State ──
    private CreatureRegistrySO registry;
    private CombatManagerSO Config => CombatController.Instance != null ? CombatController.Instance.Config : null;

    private string onlineSelectedId = "";

    private readonly List<VisualElement> onlineCards = new List<VisualElement>();
    private readonly List<VisualElement> resultCards = new List<VisualElement>();
    private readonly List<Label> resultTimeLabels = new List<Label>();          // per-row countdown
    private readonly List<VisualElement> t3Cards = new List<VisualElement>();   // refresh + entries
    private readonly List<VisualElement> historyCards = new List<VisualElement>();
    private readonly List<HistItem> historyRendered = new List<HistItem>();   // parallel to historyCards
    private int onlineIndex, t1ActionIndex, t3Index, t4Index;

    // Flattened, date-sorted view of every creature's CombatHistory (newest first).
    private readonly List<HistItem> historyItems = new List<HistItem>();
    private string historyFilterId = "";   // "" = all creatures
    private readonly List<string> filterIds = new List<string>();   // parallel to dropdown choices

    private bool refreshBusy;
    private bool wired;
    private int lastClockSecond = -1;

    private struct HistItem { public CreatureDNA Self; public CombatRecord Rec; }
    private HistItem histCurrent;

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
        if (wired && tabs != null && tabs.selectedTabIndex == 1) UpdateClock();
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= OnClose;
        if (btnInstant  != null) btnInstant.clicked  -= OnInstant;
        if (btnTimer    != null) btnTimer.clicked    -= OnTimer;
        if (btnRefresh  != null) btnRefresh.clicked   -= DoRefresh;
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

        onlineList  = root.Q<ScrollView>("online-list");
        onlineImg   = root.Q<VisualElement>("online-img");
        onlineParts = root.Q<VisualElement>("online-parts");
        onlineName  = root.Q<Label>("online-name");
        onlineStats = root.Q<Label>("online-stats");
        btnInstant  = root.Q<Button>("btn-instant");
        btnTimer    = root.Q<Button>("btn-timer");

        btnRefresh  = root.Q<Button>("btn-refresh");
        resultsList = root.Q<ScrollView>("results-list");
        queueClock  = root.Q<Label>("queue-clock");
        queueEmpty  = root.Q<Label>("queue-empty");

        historyFilter = root.Q<DropdownField>("history-filter");
        historyList   = root.Q<ScrollView>("history-list");
        historyEmpty  = root.Q<Label>("history-empty");
        histOpponent  = root.Q<Label>("hist-opponent");
        histDate      = root.Q<Label>("hist-date");
        histLines     = root.Q<ScrollView>("hist-lines");
        histOutcome   = root.Q<Label>("hist-outcome");

        if (historyFilter != null)
            historyFilter.RegisterValueChangedCallback(_ => OnHistoryFilterChanged());

        if (closeButton != null) closeButton.clicked += OnClose;
        if (btnInstant  != null) btnInstant.clicked  += OnInstant;
        if (btnTimer    != null) btnTimer.clicked    += OnTimer;
        if (btnRefresh  != null) btnRefresh.clicked   += DoRefresh;

        wired = true;
        RebuildAll();
        ResetFocus();
    }

    private void OnClose()   => UIManager.RequestPanelToggle(panel);
    private void OnInstant() => EnqueueOnline(instant: true);
    private void OnTimer()   => EnqueueOnline(instant: false);

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
        RebuildResults();
        RebuildHistory();
    }

    private void OnPanelToggle(UIPanelType p) { if (p == panel) ResetFocus(); }
    private void OnPanelSet(UIPanelType p, bool show) { if (p == panel && show) ResetFocus(); }

    private void RebuildAll()
    {
        RebuildOnlineList();
        RebuildResults();
        RebuildHistory();
        SetCenter();
    }

    private IEnumerable<CreatureDNA> Eligible() =>
        registry == null ? Enumerable.Empty<CreatureDNA>()
        : registry.GetAll().Values
            .Where(d => !d.IsDead && !d.IsBusy && d.FightCount < MaxFights())
            .OrderBy(d => d.CustomName);

    private int MaxFights() => Config != null ? Config.MaxFightCount : 5;

    private EffectiveStats StatsOf(CreatureDNA dna) =>
        database != null ? CombatStats.GetEffectiveStats(dna, database)
                         : new EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);
}
}
