using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public partial class CombatPanelUITK
{
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
        var l = new Label($"{dna.CustomName}  ·  CON {eff.Constitution:0} ATK {eff.Attack:0} SPD {eff.Speed:0} DEF {eff.Defense:0} LCK {eff.Luck:0} EVA {eff.Evasion:0}  ·  {dna.FightCount}/{MaxFights()}");
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

        if (onlineImg != null) onlineImg.style.backgroundColor = dna != null ? dna.BaseColor : new Color(0.24f, 0.24f, 0.28f);
        if (onlineName != null) onlineName.text = dna != null ? dna.CustomName : "Selecciona un MoriMochi";

        if (onlineStats != null)
        {
            if (dna != null) { var e = StatsOf(dna); onlineStats.text = $"CON {e.Constitution:0}   ATK {e.Attack:0}   SPD {e.Speed:0}   DEF {e.Defense:0}   LCK {e.Luck:0}   EVA {e.Evasion:0}"; }
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
        if (img != null) img.style.backgroundColor = dna != null ? dna.BaseColor : new Color(0.24f, 0.24f, 0.28f);
    }

    private void DoLocalFight()
    {
        if (string.IsNullOrEmpty(fighterAId) || string.IsNullOrEmpty(fighterBId)) { Debug.LogWarning("[CombatPanel] Pick two fighters."); return; }
        if (fighterAId == fighterBId) { Debug.LogWarning("[CombatPanel] Pick two DIFFERENT fighters."); return; }
        if (Config == null) { Debug.LogError("[CombatPanel] No CombatManager config."); return; }

        var result = CombatService.Simulate(fighterAId, fighterBId, registry, database, Config);
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
}
}
