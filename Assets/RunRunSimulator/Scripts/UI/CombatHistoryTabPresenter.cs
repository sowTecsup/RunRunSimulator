using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class CombatHistoryTabPresenter : ITabPresenter
{
    private const string Focus = "cbt-focus";

    private readonly Func<CreatureRegistrySO> getRegistry;

    private readonly DropdownField historyFilter;
    private readonly ScrollView historyList, histLines;
    private readonly Label historyEmpty, histOpponent, histDate, histOutcome;
    private Button histReplayBtn;

    private struct HistItem { public CreatureDNA Self; public CombatRecord Rec; }
    private HistItem histCurrent;

    // Flattened, date-sorted view of every creature's CombatHistory (newest first).
    private readonly List<HistItem> historyItems = new List<HistItem>();
    private string historyFilterId = "";   // "" = all creatures
    private readonly List<string> filterIds = new List<string>();   // parallel to dropdown choices

    private readonly List<VisualElement> historyCards = new List<VisualElement>();
    private readonly List<HistItem> historyRendered = new List<HistItem>();   // parallel to historyCards
    private int t4Index;

    public CombatHistoryTabPresenter(VisualElement root, Func<CreatureRegistrySO> getRegistry)
    {
        this.getRegistry = getRegistry;

        historyFilter = root.Q<DropdownField>("history-filter");
        historyList   = root.Q<ScrollView>("history-list");
        historyEmpty  = root.Q<Label>("history-empty");
        histOpponent  = root.Q<Label>("hist-opponent");
        histDate      = root.Q<Label>("hist-date");
        histLines     = root.Q<ScrollView>("hist-lines");
        histOutcome   = root.Q<Label>("hist-outcome");

        if (historyFilter != null)
            historyFilter.RegisterValueChangedCallback(_ => OnHistoryFilterChanged());
    }

    // ── ITabPresenter ─────────────────────────────────────────────

    public void Enter()
    {
        t4Index = 0;
        HighlightT4();
        if (historyCards.Count > 0) historyList.ScrollTo(historyCards[0]);
    }

    public bool Navigate(int h, int v)
    {
        int delta = h + v;
        if (historyCards.Count == 0)
        {
            if (delta < 0) return false;
            return true;
        }
        int next = t4Index + delta;
        if (next < 0) { ClearT4Focus(); return false; }
        t4Index = Mathf.Clamp(next, 0, historyCards.Count - 1);
        HighlightT4();
        historyList?.ScrollTo(historyCards[t4Index]);
        ShowHistoryByRenderIndex(t4Index);
        return true;
    }

    public void Submit()
    {
        if (t4Index >= 0 && t4Index < historyCards.Count && historyCards[t4Index].userData is int hi)
            ShowHistoryByRenderIndex(hi);
    }

    public bool Cancel() => false;

    public void ClearFocus() => ClearT4Focus();

    public void Rebuild() => RebuildHistory();

    public void Teardown() { }

    // ── Tab 4: history (replayable, all creatures) ────────────────

    private void RebuildHistory()
    {
        var registry = getRegistry();
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
        histCurrent = it;

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

            if (histReplayBtn == null)
            {
                histReplayBtn = new Button { text = "▶ Ver replay" };
                histReplayBtn.AddToClassList("cbt-replay-btn");
                histReplayBtn.clicked += () =>
                {
                    if (histCurrent.Self != null && CombatReplayRequest.CanReplay(histCurrent.Self, histCurrent.Rec, getRegistry()))
                        CombatReplayRequest.Request(histCurrent.Self, histCurrent.Rec);
                };
                int idx = histOutcome.parent.IndexOf(histOutcome);
                histOutcome.parent.Insert(idx + 1, histReplayBtn);
            }
        }

        histReplayBtn?.SetEnabled(CombatReplayRequest.CanReplay(it.Self, it.Rec, getRegistry()));
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

    private void HighlightT4()
    {
        for (int i = 0; i < historyCards.Count; i++) historyCards[i].EnableInClassList(Focus, i == t4Index);
    }
    private void ClearT4Focus() { foreach (var c in historyCards) c.RemoveFromClassList(Focus); }
}
}
