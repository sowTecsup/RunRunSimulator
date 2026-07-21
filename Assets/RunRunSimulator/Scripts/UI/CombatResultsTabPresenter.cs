using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class CombatResultsTabPresenter : ITabPresenter
{
    private const string Focus = "cbt-focus";

    private readonly Func<CreatureRegistrySO> getRegistry;
    private readonly AsyncCombatService asyncCombatService;

    private readonly Button btnRefresh;
    private readonly ScrollView resultsList;
    private readonly Label queueClock, queueEmpty;

    private readonly List<VisualElement> resultCards = new List<VisualElement>();
    private readonly List<Label> resultTimeLabels = new List<Label>();          // per-row countdown
    private readonly List<VisualElement> t3Cards = new List<VisualElement>();   // refresh + entries
    private int t3Index;

    private bool refreshBusy;
    private int lastClockSecond = -1;

    public CombatResultsTabPresenter(VisualElement root, Func<CreatureRegistrySO> getRegistry,
        AsyncCombatService asyncCombatService)
    {
        this.getRegistry = getRegistry;
        this.asyncCombatService = asyncCombatService;

        btnRefresh  = root.Q<Button>("btn-refresh");
        resultsList = root.Q<ScrollView>("results-list");
        queueClock  = root.Q<Label>("queue-clock");
        queueEmpty  = root.Q<Label>("queue-empty");

        if (btnRefresh != null) btnRefresh.clicked += DoRefresh;
    }

    // ── ITabPresenter ────────────────────────────────────────────

    public void Enter()
    {
        t3Index = 0;
        HighlightT3();
    }

    public bool Navigate(int h, int v)
    {
        int delta = h + v;
        if (t3Cards.Count == 0) return true;
        int next = t3Index + delta;
        if (next < 0) { ClearT3Focus(); return false; }
        t3Index = Mathf.Clamp(next, 0, t3Cards.Count - 1);
        HighlightT3();
        var el = t3Cards[t3Index];
        if (t3Index > 0) resultsList?.ScrollTo(el);
        return true;
    }

    public void Submit()
    {
        if (t3Index == 0) DoRefresh();
    }

    public bool Cancel() => false;

    public void ClearFocus() => ClearT3Focus();

    public void Rebuild() => RebuildResults();

    public void Tick() => UpdateClock();

    public void Teardown()
    {
        if (btnRefresh != null) btnRefresh.clicked -= DoRefresh;
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

        var registry = getRegistry();
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

    private void HighlightT3()
    {
        for (int i = 0; i < t3Cards.Count; i++) t3Cards[i].EnableInClassList(Focus, i == t3Index);
    }
    private void ClearT3Focus() { foreach (var c in t3Cards) c.RemoveFromClassList(Focus); }
}
}
