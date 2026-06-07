using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// In-game combat screen (UI Toolkit), modal, with three tabs:
//   • "Batalla Online" — pick one of yours, see it (stats + parts), send it to async
//                        combat (Instant or Timer). Right column = team (placeholder).
//   • "Combate Local"  — pick two of yours; they fight locally; the log shows inline.
//   • "Resultados"     — your creatures in queue / with results; the right pane shows
//                        the battle log (opponent · turns · big win/lose).
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

    private enum Region { TabBar, T1List, T1Actions, T2Center, T2ListA, T2ListB, T3List, T4List }
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
    // Tab 2
    private ScrollView faList, fbList, localLog;
    private VisualElement slotA, slotB, slotAImg, slotBImg;
    private Label slotAName, slotBName, localOutcome;
    private Button btnFight;
    // Tab 3 (Resultados: cola + reloj al próximo tick)
    private Button btnRefresh;
    private ScrollView resultsList;
    private Label queueClock, queueEmpty;
    // Tab 4 (Historial)
    private DropdownField historyFilter;
    private ScrollView historyList, histLines;
    private Label historyEmpty, histOpponent, histDate, histOutcome;

    // ── State ──
    private CreatureRegistrySO registry;
    private CombatManagerSO config;

    private string onlineSelectedId = "";
    private string fighterAId = "", fighterBId = "";

    private readonly List<VisualElement> onlineCards = new List<VisualElement>();
    private readonly List<VisualElement> faCards = new List<VisualElement>();
    private readonly List<VisualElement> fbCards = new List<VisualElement>();
    private readonly List<VisualElement> resultCards = new List<VisualElement>();
    private readonly List<Label> resultTimeLabels = new List<Label>();          // per-row countdown
    private readonly List<VisualElement> t3Cards = new List<VisualElement>();   // refresh + entries
    private readonly List<VisualElement> historyCards = new List<VisualElement>();
    private readonly List<HistItem> historyRendered = new List<HistItem>();   // parallel to historyCards
    private int onlineIndex, faIndex, fbIndex, t1ActionIndex, t2Index, t3Index, t4Index;

    // Flattened, date-sorted view of every creature's CombatHistory (newest first).
    private readonly List<HistItem> historyItems = new List<HistItem>();
    private string historyFilterId = "";   // "" = all creatures
    private readonly List<string> filterIds = new List<string>();   // parallel to dropdown choices

    private bool refreshBusy;
    private bool wired;
    private int lastClockSecond = -1;

    private struct HistItem { public CreatureDNA Self; public CombatRecord Rec; }

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        if (document != null) document.sortingOrder = sortingOrder;
        var gm = GameManager.Instance;
        registry = gm != null ? gm.Registry : null;
        config   = gm != null ? (gm.CombatConfig ?? CombatManagerSO.Current) : CombatManagerSO.Current;
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
        if (wired && tabs != null && tabs.selectedTabIndex == 2) UpdateClock();
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= OnClose;
        if (btnInstant  != null) btnInstant.clicked  -= OnInstant;
        if (btnTimer    != null) btnTimer.clicked    -= OnTimer;
        if (btnFight    != null) btnFight.clicked     -= DoLocalFight;
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

        faList      = root.Q<ScrollView>("fa-list");
        fbList      = root.Q<ScrollView>("fb-list");
        localLog    = root.Q<ScrollView>("local-log");
        slotA       = root.Q<VisualElement>("slot-a");
        slotB       = root.Q<VisualElement>("slot-b");
        slotAImg    = root.Q<VisualElement>("slot-a-img");
        slotBImg    = root.Q<VisualElement>("slot-b-img");
        slotAName   = root.Q<Label>("slot-a-name");
        slotBName   = root.Q<Label>("slot-b-name");
        localOutcome = root.Q<Label>("local-outcome");
        btnFight    = root.Q<Button>("btn-fight");

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
        if (btnFight    != null) btnFight.clicked     += DoLocalFight;
        if (btnRefresh  != null) btnRefresh.clicked   += DoRefresh;
        slotA?.RegisterCallback<ClickEvent>(_ => OpenFighterList(true));
        slotB?.RegisterCallback<ClickEvent>(_ => OpenFighterList(false));

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
        RebuildFighterLists();
        RebuildResults();
        RebuildHistory();
        SetCenter();
        RefreshSlots();
    }

    private IEnumerable<CreatureDNA> Eligible() =>
        registry == null ? Enumerable.Empty<CreatureDNA>()
        : registry.GetAll().Values
            .Where(d => !d.IsDead && !d.IsBusy && d.FightCount < MaxFights())
            .OrderBy(d => d.CustomName);

    private int MaxFights() => config != null ? config.MaxFightCount : 5;

    private CombatService.EffectiveStats StatsOf(CreatureDNA dna) =>
        database != null ? CombatService.GetEffectiveStats(dna, database)
                         : new CombatService.EffectiveStats(dna.BaseHP, dna.BaseAttack, dna.BaseSpeed);

    private void RebuildOnlineList()
    {
        if (onlineList == null) return;
        onlineList.Clear(); onlineCards.Clear();
        foreach (var dna in Eligible())
            onlineList.Add(MakeCandidate(dna, onlineCards, SelectOnline));
    }

    private void RebuildFighterLists()
    {
        if (faList == null || fbList == null) return;
        faList.Clear(); fbList.Clear(); faCards.Clear(); fbCards.Clear();
        foreach (var dna in Eligible())
        {
            faList.Add(MakeCandidate(dna, faCards, SelectFighterA));
            fbList.Add(MakeCandidate(dna, fbCards, SelectFighterB));
        }
    }

    private VisualElement MakeCandidate(CreatureDNA dna, List<VisualElement> bucket, Action<string> onClick)
    {
        var row = new VisualElement();
        row.AddToClassList("cbt-candidate");
        row.userData = dna.UniqueID;

        var eff = StatsOf(dna);
        var l = new Label($"{dna.CustomName}  ·  HP {eff.HP:0} ATK {eff.Attack:0} SPD {eff.Speed:0}  ·  {dna.FightCount}/{MaxFights()}");
        l.AddToClassList("cbt-candidate-text");
        row.Add(l);

        string id = dna.UniqueID;
        row.RegisterCallback<ClickEvent>(_ => onClick(id));
        bucket.Add(row);
        return row;
    }

    // ── Tab 1: online selection / enqueue ─────────────────────────

    private void SelectOnline(string id) { onlineSelectedId = id; SetCenter(); }

    private void SetCenter()
    {
        CreatureDNA dna = null;
        if (!string.IsNullOrEmpty(onlineSelectedId) && registry != null) registry.TryGet(onlineSelectedId, out dna);

        if (onlineImg != null) onlineImg.style.backgroundColor = dna != null ? dna.PrimaryColor : new Color(0.24f, 0.24f, 0.28f);
        if (onlineName != null) onlineName.text = dna != null ? dna.CustomName : "Selecciona un MoriMochi";

        if (onlineStats != null)
        {
            if (dna != null) { var e = StatsOf(dna); onlineStats.text = $"HP {e.HP:0}   ATK {e.Attack:0}   SPD {e.Speed:0}"; }
            else onlineStats.text = "";
        }

        if (onlineParts != null)
        {
            onlineParts.Clear();
            if (dna != null && database != null)
            {
                AddPartRow(onlineParts, database.GetBodyShape(dna.BodyShapeID));
                AddPartRow(onlineParts, database.GetArm(dna.ArmID));
                AddPartRow(onlineParts, database.GetEye(dna.EyeID));
                AddPartRow(onlineParts, database.GetMouth(dna.MouthID));
            }
        }
    }

    private static void AddPartRow(VisualElement parent, BodyPart part)
    {
        var row = new VisualElement();
        row.AddToClassList("cbt-part-row");

        var swatch = new VisualElement();
        swatch.AddToClassList("cbt-swatch");
        swatch.style.backgroundColor = part != null ? BodyPart.SetColor(part.Set) : Color.gray;
        row.Add(swatch);

        var text = new Label(part != null ? $"{part.Name} · {part.Set} · Tier{(int)part.Tier}" : "—");
        text.AddToClassList("cbt-part-text");
        row.Add(text);

        parent.Add(row);
    }

    // Fire-and-forget: EnqueueInternal flips BusyState immediately + fires
    // RegistryChanged, so the creature drops from the list right away.
    private void EnqueueOnline(bool instant)
    {
        if (string.IsNullOrEmpty(onlineSelectedId)) { Debug.LogWarning("[CombatPanel] Select a MoriMochi first."); return; }
        if (asyncCombatService == null) { Debug.LogError("[CombatPanel] AsyncCombatService not assigned."); return; }
        if (registry == null || !registry.TryGet(onlineSelectedId, out var dna)) return;

        if (instant) _ = asyncCombatService.EnqueueInstantAsync(dna);
        else         _ = asyncCombatService.EnqueueScheduledAsync(dna);

        onlineSelectedId = "";
        SetCenter();
        if (tabs != null) tabs.selectedTabIndex = 2;   // jump to Resultados
        region = Region.TabBar;
        ClearAllFocus();
        SetTabBarFocus(true);
        RebuildResults();
    }

    // ── Tab 2: local fight ────────────────────────────────────────

    private void SelectFighterA(string id) { fighterAId = id; ClearFocus(faCards); region = Region.T2Center; t2Index = 0; RefreshSlots(); ApplyT2CenterFocus(); }
    private void SelectFighterB(string id) { fighterBId = id; ClearFocus(fbCards); region = Region.T2Center; t2Index = 1; RefreshSlots(); ApplyT2CenterFocus(); }

    private void RefreshSlots()
    {
        SetSlot(slotAName, slotAImg, fighterAId);
        SetSlot(slotBName, slotBImg, fighterBId);
    }

    private void SetSlot(Label nameLabel, VisualElement img, string id)
    {
        CreatureDNA dna = null;
        if (!string.IsNullOrEmpty(id) && registry != null) registry.TryGet(id, out dna);
        if (nameLabel != null) nameLabel.text = dna != null ? dna.CustomName : "Vacío";
        if (img != null) img.style.backgroundColor = dna != null ? dna.PrimaryColor : new Color(0.24f, 0.24f, 0.28f);
    }

    private void DoLocalFight()
    {
        if (string.IsNullOrEmpty(fighterAId) || string.IsNullOrEmpty(fighterBId)) { Debug.LogWarning("[CombatPanel] Pick two fighters."); return; }
        if (fighterAId == fighterBId) { Debug.LogWarning("[CombatPanel] Pick two DIFFERENT fighters."); return; }
        if (config == null) { Debug.LogError("[CombatPanel] No CombatManager config."); return; }

        var result = CombatService.Simulate(fighterAId, fighterBId, registry, database, config);
        if (result == null) return;

        GameEvents.CombatCompleted(result);
        GameEvents.RegistryChanged(registry);   // triggers RebuildAll (fighters changed)

        // Inline log (set AFTER the rebuild above so it isn't wiped).
        if (localLog != null)
        {
            localLog.Clear();
            foreach (var line in result.Log)
            {
                var l = new Label(line);
                l.AddToClassList("cbt-log-line");
                localLog.Add(l);
            }
        }
        if (localOutcome != null)
            localOutcome.text = result.IsDraw
                ? "Empate"
                : $"Ganó {result.WinnerName}" + (result.LoserDied ? $"  ·  murió {result.LoserName}" : "");

        RefreshSlots();
    }

    // ── Tab 3: results ────────────────────────────────────────────

    private async void DoRefresh()
    {
        if (asyncCombatService == null) { Debug.LogError("[CombatPanel] AsyncCombatService not assigned."); return; }
        if (refreshBusy) return;
        refreshBusy = true;
        if (btnRefresh != null) { btnRefresh.SetEnabled(false); btnRefresh.text = "Revisando..."; btnRefresh.AddToClassList("cbt-action--busy"); }

        await asyncCombatService.PollResultsAsync();   // applies → fires OnCombatLogged per result

        if (btnRefresh != null) { btnRefresh.SetEnabled(true); btnRefresh.text = "Revisar resultados"; btnRefresh.RemoveFromClassList("cbt-action--busy"); }
        refreshBusy = false;
        RebuildResults();
    }

    // Resultados now shows ONLY what's still in the queue (name + the shared
    // countdown to the next server tick). Finished fights move to Historial.
    private void RebuildResults()
    {
        if (resultsList == null) return;
        resultsList.Clear(); resultCards.Clear(); resultTimeLabels.Clear();

        int queued = 0;
        if (registry != null)
            foreach (var d in registry.GetAll().Values
                         .Where(x => x.BusyState == BusyReason.QueuedForCombat)
                         .OrderBy(x => x.CustomName))
            {
                AddQueueRow(d.CustomName, d.UniqueID, d.QueuedAt);
                queued++;
            }

        if (queueEmpty != null) queueEmpty.style.display = queued == 0 ? DisplayStyle.Flex : DisplayStyle.None;

        // t3 focus order: refresh button first, then the rows.
        t3Cards.Clear();
        if (btnRefresh != null) t3Cards.Add(btnRefresh);
        t3Cards.AddRange(resultCards);

        lastClockSecond = -1;   // force the clock to repaint next Update
        UpdateClock();
    }

    private void AddQueueRow(string name, string id, DateTime queuedAt)
    {
        var row = new VisualElement();
        row.AddToClassList("cbt-result-row");
        row.userData = id;

        var n = new Label(name); n.AddToClassList("cbt-result-name");
        var q = new Label(queuedAt == default ? "" : $"encolado {queuedAt.ToLocalTime():HH:mm}");
        q.AddToClassList("cbt-result-queued");
        var t = new Label("--:--"); t.AddToClassList("cbt-result-time");
        row.Add(n); row.Add(q); row.Add(t);

        resultsList.Add(row);
        resultCards.Add(row);
        resultTimeLabels.Add(t);
    }

    // The server cron runs at minute :00 of every UTC hour. Both the big clock and
    // each queue row count down to that boundary; throttled to once per second.
    private void UpdateClock()
    {
        if (queueClock == null) return;

        var now  = DateTime.UtcNow;
        if (now.Second == lastClockSecond) return;
        lastClockSecond = now.Second;

        var next = now.Date.AddHours(now.Hour + 1);
        var span = next - now;
        string text = $"{span.Minutes:00}:{span.Seconds:00}";

        queueClock.text = text;
        foreach (var lbl in resultTimeLabels) lbl.text = text;
    }

    // ── Tab 4: history (replayable, all creatures) ────────────────

    private void RebuildHistory()
    {
        historyItems.Clear();
        if (registry != null)
            foreach (var dna in registry.GetAll().Values)
            {
                if (dna.CombatHistory == null) continue;
                foreach (var rec in dna.CombatHistory)
                    historyItems.Add(new HistItem { Self = dna, Rec = rec });
            }
        historyItems.Sort((a, b) => b.Rec.Date.CompareTo(a.Rec.Date));

        RebuildHistoryFilter();
        RebuildHistoryList();
    }

    // Dropdown choices: "Todos" + every creature that actually has history. The
    // parallel filterIds list maps the selected index back to a UniqueID.
    private void RebuildHistoryFilter()
    {
        if (historyFilter == null) return;

        var choices = new List<string> { "Todos" };
        filterIds.Clear();
        filterIds.Add("");

        var seen = new HashSet<string>();
        foreach (var it in historyItems)
            if (seen.Add(it.Self.UniqueID))
            {
                choices.Add(it.Self.CustomName);
                filterIds.Add(it.Self.UniqueID);
            }

        historyFilter.choices = choices;
        int idx = filterIds.IndexOf(historyFilterId);
        if (idx < 0) { idx = 0; historyFilterId = ""; }
        historyFilter.SetValueWithoutNotify(choices[idx]);
    }

    private void OnHistoryFilterChanged()
    {
        int idx = historyFilter != null ? historyFilter.index : 0;
        historyFilterId = (idx >= 0 && idx < filterIds.Count) ? filterIds[idx] : "";
        RebuildHistoryList();
    }

    private void RebuildHistoryList()
    {
        if (historyList == null) return;
        historyList.Clear(); historyCards.Clear(); historyRendered.Clear();

        int shown = 0;
        foreach (var it in historyItems)
        {
            if (!string.IsNullOrEmpty(historyFilterId) && it.Self.UniqueID != historyFilterId) continue;
            AddHistoryRow(it);
            shown++;
        }

        if (historyEmpty != null) historyEmpty.style.display = shown == 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void AddHistoryRow(HistItem it)
    {
        var rec = it.Rec;
        var row = new VisualElement();
        row.AddToClassList("cbt-result-row");
        row.userData = historyCards.Count;   // index into the filtered render order

        var n = new Label($"{it.Self.CustomName}  vs  {rec.OpponentName}");
        n.AddToClassList("cbt-result-name");

        var s = new Label(OutcomeShort(rec.Outcome, rec.Died));
        s.AddToClassList("cbt-result-status");
        s.AddToClassList(OutcomeClass(rec.Outcome));

        var meta = new Label(rec.Date.ToLocalTime().ToString("dd/MM HH:mm"));
        meta.AddToClassList("cbt-hist-meta");

        row.Add(n); row.Add(s); row.Add(meta);

        var captured = it;
        row.RegisterCallback<ClickEvent>(_ => ShowHistory(captured));
        historyList.Add(row);
        historyCards.Add(row);
        historyRendered.Add(it);
    }

    private void ShowHistoryByRenderIndex(int i)
    {
        if (i >= 0 && i < historyRendered.Count) ShowHistory(historyRendered[i]);
    }

    private void ShowHistory(HistItem it)
    {
        if (histLines == null) return;
        histLines.Clear();

        var rec = it.Rec;
        string opp = string.IsNullOrEmpty(rec.OpponentPlayerName)
            ? rec.OpponentName
            : $"{rec.OpponentName} ({rec.OpponentPlayerName})";
        if (histOpponent != null) histOpponent.text = $"{it.Self.CustomName}  vs  {opp}";
        if (histDate != null) histDate.text = rec.Date.ToLocalTime().ToString("dddd dd/MM/yyyy · HH:mm");

        if (rec.Turns != null)
            foreach (var t in rec.Turns)
            {
                var l = new Label(
                    $"R{t.TurnNumber}  {t.AttackerName} → {t.DefenderName}  ·  {t.Damage:0} daño{(t.WasCrit ? " ¡CRIT!" : "")}  ·  HP {t.DefenderHpAfter:0}");
                l.AddToClassList("cbt-log-line");
                histLines.Add(l);
            }

        if (histOutcome != null)
        {
            string evolved = rec.Outcome == CombatOutcome.Won && !string.IsNullOrEmpty(rec.EvolvedSlot)
                ? $"  ·  evolucionó {rec.EvolvedSlot}" : "";
            string died = rec.Died ? "  ·  murió" : "";
            histOutcome.text = OutcomeLong(rec.Outcome) + evolved + died;
            histOutcome.EnableInClassList("cbt-log-outcome--win",  rec.Outcome == CombatOutcome.Won);
            histOutcome.EnableInClassList("cbt-log-outcome--lose", rec.Outcome == CombatOutcome.Lost);
        }
    }

    private static string OutcomeShort(CombatOutcome o, bool died) => o switch
    {
        CombatOutcome.Won  => "Ganó",
        CombatOutcome.Lost => died ? "Murió" : "Perdió",
        _                  => "Empate",
    };

    private static string OutcomeLong(CombatOutcome o) => o switch
    {
        CombatOutcome.Won  => "¡Victoria!",
        CombatOutcome.Lost => "Derrota",
        _                  => "Empate",
    };

    private static string OutcomeClass(CombatOutcome o) => o switch
    {
        CombatOutcome.Won  => "cbt-result-status--win",
        CombatOutcome.Lost => "cbt-result-status--lose",
        _                  => "cbt-result-status--draw",
    };

    // ── IUINavigable ──────────────────────────────────────────────

    public void OnUINavigate(Vector2 dir)
    {
        int h = dir.x >  0.5f ? 1 : dir.x < -0.5f ? -1 : 0;
        int v = dir.y < -0.5f ? 1 : dir.y >  0.5f ? -1 : 0;   // down = +1
        if (h == 0 && v == 0) return;

        switch (region)
        {
            case Region.TabBar:
                if (h != 0 && tabs != null) tabs.selectedTabIndex = Mathf.Clamp(tabs.selectedTabIndex + h, 0, 3);
                else if (v > 0) EnterContent();
                break;

            case Region.T1List:
                MoveCards(onlineCards, ref onlineIndex, h + v, onlineList, GoTabBar);
                break;

            case Region.T1Actions:
                if (h != 0) { t1ActionIndex = Mathf.Clamp(t1ActionIndex + h, 0, 1); ApplyT1ActionsFocus(); }
                else if (v < 0) { ClearT1ActionsFocus(); region = Region.T1List; HighlightCards(onlineCards, onlineIndex); }
                break;

            case Region.T2Center: MoveT2Center(h + v); break;
            case Region.T2ListA:  MoveCards(faCards, ref faIndex, h + v, faList, null); break;
            case Region.T2ListB:  MoveCards(fbCards, ref fbIndex, h + v, fbList, null); break;
            case Region.T3List:   MoveT3(h + v); break;
            case Region.T4List:   MoveT4(h + v); break;
        }
    }

    public void OnUISubmit()
    {
        switch (region)
        {
            case Region.TabBar: EnterContent(); break;

            case Region.T1List:
                if (InRange(onlineCards, onlineIndex))
                {
                    SelectOnline((string)onlineCards[onlineIndex].userData);
                    ClearFocus(onlineCards);
                    region = Region.T1Actions; t1ActionIndex = 0; ApplyT1ActionsFocus();
                }
                break;

            case Region.T1Actions: EnqueueOnline(t1ActionIndex == 0); break;

            case Region.T2Center:
                if      (t2Index == 0) OpenFighterList(true);
                else if (t2Index == 1) OpenFighterList(false);
                else                   DoLocalFight();
                break;

            case Region.T2ListA: if (InRange(faCards, faIndex)) SelectFighterA((string)faCards[faIndex].userData); break;
            case Region.T2ListB: if (InRange(fbCards, fbIndex)) SelectFighterB((string)fbCards[fbIndex].userData); break;

            case Region.T3List:
                if (t3Index == 0) DoRefresh();
                break;

            case Region.T4List:
                if (t4Index >= 0 && t4Index < historyCards.Count && historyCards[t4Index].userData is int hi)
                    ShowHistoryByRenderIndex(hi);
                break;
        }
    }

    public bool OnUICancel()
    {
        switch (region)
        {
            case Region.T1Actions: ClearT1ActionsFocus(); region = Region.T1List; HighlightCards(onlineCards, onlineIndex); return true;
            case Region.T2ListA:   ClearFocus(faCards); region = Region.T2Center; ApplyT2CenterFocus(); return true;
            case Region.T2ListB:   ClearFocus(fbCards); region = Region.T2Center; ApplyT2CenterFocus(); return true;
            case Region.T1List:
            case Region.T2Center:
            case Region.T3List:
            case Region.T4List:
                ClearAllFocus(); region = Region.TabBar; SetTabBarFocus(true); return true;
            default: return false;
        }
    }

    // ── Navigation helpers ────────────────────────────────────────

    private void GoTabBar() { region = Region.TabBar; SetTabBarFocus(true); }

    private void EnterContent()
    {
        SetTabBarFocus(false);
        int t = tabs != null ? tabs.selectedTabIndex : 0;
        if (t == 0)
        {
            region = Region.T1List; onlineIndex = 0;
            if (onlineCards.Count > 0) { HighlightCards(onlineCards, 0); onlineList.ScrollTo(onlineCards[0]); }
        }
        else if (t == 1)
        {
            region = Region.T2Center; t2Index = 0; ApplyT2CenterFocus();
        }
        else if (t == 2)
        {
            region = Region.T3List; t3Index = 0; HighlightT3();
        }
        else
        {
            region = Region.T4List; t4Index = 0; HighlightT4();
            if (historyCards.Count > 0) historyList.ScrollTo(historyCards[0]);
        }
    }

    private void MoveCards(List<VisualElement> cards, ref int idx, int delta, ScrollView scroll, Action exitUp)
    {
        if (cards.Count == 0) { if (delta < 0 && exitUp != null) exitUp(); return; }
        int next = idx + delta;
        if (next < 0) { if (exitUp != null) { ClearFocus(cards); exitUp(); } return; }
        idx = Mathf.Clamp(next, 0, cards.Count - 1);
        HighlightCards(cards, idx);
        scroll?.ScrollTo(cards[idx]);
    }

    private void MoveT2Center(int delta)
    {
        int next = t2Index + delta;
        if (next < 0) { ClearT2CenterFocus(); region = Region.TabBar; SetTabBarFocus(true); return; }
        t2Index = Mathf.Clamp(next, 0, 2);
        ApplyT2CenterFocus();
    }

    private void MoveT3(int delta)
    {
        if (t3Cards.Count == 0) return;
        int next = t3Index + delta;
        if (next < 0) { ClearT3Focus(); region = Region.TabBar; SetTabBarFocus(true); return; }
        t3Index = Mathf.Clamp(next, 0, t3Cards.Count - 1);
        HighlightT3();
        var el = t3Cards[t3Index];
        if (t3Index > 0) resultsList?.ScrollTo(el);
    }

    private void OpenFighterList(bool isA)
    {
        ClearT2CenterFocus();
        if (isA)
        {
            region = Region.T2ListA; faIndex = 0;
            if (faCards.Count > 0) { HighlightCards(faCards, 0); faList.ScrollTo(faCards[0]); }
        }
        else
        {
            region = Region.T2ListB; fbIndex = 0;
            if (fbCards.Count > 0) { HighlightCards(fbCards, 0); fbList.ScrollTo(fbCards[0]); }
        }
    }

    // ── Focus visuals ─────────────────────────────────────────────

    private static void HighlightCards(List<VisualElement> cards, int idx)
    {
        for (int i = 0; i < cards.Count; i++) cards[i].EnableInClassList(Focus, i == idx);
    }
    private static void ClearFocus(List<VisualElement> cards)
    {
        foreach (var c in cards) c.RemoveFromClassList(Focus);
    }

    private void ApplyT1ActionsFocus()
    {
        btnInstant?.EnableInClassList(Focus, t1ActionIndex == 0);
        btnTimer?.EnableInClassList(Focus, t1ActionIndex == 1);
    }
    private void ClearT1ActionsFocus() { btnInstant?.RemoveFromClassList(Focus); btnTimer?.RemoveFromClassList(Focus); }

    private void ApplyT2CenterFocus()
    {
        slotA?.EnableInClassList(Focus, t2Index == 0);
        slotB?.EnableInClassList(Focus, t2Index == 1);
        btnFight?.EnableInClassList(Focus, t2Index == 2);
    }
    private void ClearT2CenterFocus() { slotA?.RemoveFromClassList(Focus); slotB?.RemoveFromClassList(Focus); btnFight?.RemoveFromClassList(Focus); }

    private void HighlightT3()
    {
        for (int i = 0; i < t3Cards.Count; i++) t3Cards[i].EnableInClassList(Focus, i == t3Index);
    }
    private void ClearT3Focus() { foreach (var c in t3Cards) c.RemoveFromClassList(Focus); }

    private void MoveT4(int delta)
    {
        if (historyCards.Count == 0) { if (delta < 0) { region = Region.TabBar; SetTabBarFocus(true); } return; }
        int next = t4Index + delta;
        if (next < 0) { ClearT4Focus(); region = Region.TabBar; SetTabBarFocus(true); return; }
        t4Index = Mathf.Clamp(next, 0, historyCards.Count - 1);
        HighlightT4();
        historyList?.ScrollTo(historyCards[t4Index]);
        ShowHistoryByRenderIndex(t4Index);
    }

    private void HighlightT4()
    {
        for (int i = 0; i < historyCards.Count; i++) historyCards[i].EnableInClassList(Focus, i == t4Index);
    }
    private void ClearT4Focus() { foreach (var c in historyCards) c.RemoveFromClassList(Focus); }

    private void SetTabBarFocus(bool on) { tabs?.EnableInClassList("tabbar-focused", on); }

    private void ClearAllFocus()
    {
        ClearFocus(onlineCards); ClearFocus(faCards); ClearFocus(fbCards);
        ClearT1ActionsFocus(); ClearT2CenterFocus(); ClearT3Focus(); ClearT4Focus();
        SetTabBarFocus(false);
    }

    private void ResetFocus()
    {
        if (!wired) return;
        if (tabs != null) tabs.selectedTabIndex = 0;
        ClearAllFocus();
        region = Region.TabBar;
        SetTabBarFocus(true);
    }

    private static bool InRange(List<VisualElement> list, int i) => i >= 0 && i < list.Count;
}
