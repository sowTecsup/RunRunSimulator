using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class CombatOnlineTabPresenter : ITabPresenter
{
    private const string Focus = "cbt-focus";

    private enum SubFocus { List, Actions }

    private readonly Func<CreatureRegistrySO> getRegistry;
    private readonly CreatureDatabaseSO database;
    private readonly AsyncCombatService asyncCombatService;
    private readonly Action onEnqueued;

    private readonly ScrollView onlineList;
    private readonly VisualElement onlineImg, onlineParts;
    private readonly Label onlineName, onlineStats;
    private readonly Button btnInstant, btnTimer;

    private string onlineSelectedId = "";
    private readonly List<VisualElement> onlineCards = new List<VisualElement>();
    private int onlineIndex, t1ActionIndex;
    private SubFocus focus = SubFocus.List;

    private CombatManagerSO Config => CombatController.Instance != null ? CombatController.Instance.Config : null;

    public CombatOnlineTabPresenter(VisualElement root, Func<CreatureRegistrySO> getRegistry,
        CreatureDatabaseSO database, AsyncCombatService asyncCombatService, Action onEnqueued)
    {
        this.getRegistry = getRegistry;
        this.database = database;
        this.asyncCombatService = asyncCombatService;
        this.onEnqueued = onEnqueued;

        onlineList  = root.Q<ScrollView>("online-list");
        onlineImg   = root.Q<VisualElement>("online-img");
        onlineParts = root.Q<VisualElement>("online-parts");
        onlineName  = root.Q<Label>("online-name");
        onlineStats = root.Q<Label>("online-stats");
        btnInstant  = root.Q<Button>("btn-instant");
        btnTimer    = root.Q<Button>("btn-timer");

        if (btnInstant != null) { btnInstant.text = Loc.Tr("ui.combat.online.instant"); btnInstant.clicked += OnInstantClicked; }
        if (btnTimer   != null) { btnTimer.text   = Loc.Tr("ui.combat.online.timer");   btnTimer.clicked   += OnTimerClicked; }
    }

    private void OnInstantClicked() => EnqueueOnline(instant: true);
    private void OnTimerClicked()   => EnqueueOnline(instant: false);

    // ── ITabPresenter ────────────────────────────────────────────

    public void Enter()
    {
        focus = SubFocus.List;
        onlineIndex = 0;
        if (onlineCards.Count > 0) { HighlightCards(0); onlineList?.ScrollTo(onlineCards[0]); }
    }

    public bool Navigate(int h, int v)
    {
        if (focus == SubFocus.List)
        {
            int delta = h + v;
            if (onlineCards.Count == 0) return delta < 0 ? false : true;
            int next = onlineIndex + delta;
            if (next < 0) { ClearFocus(onlineCards); return false; }
            onlineIndex = Mathf.Clamp(next, 0, onlineCards.Count - 1);
            HighlightCards(onlineIndex);
            onlineList?.ScrollTo(onlineCards[onlineIndex]);
            return true;
        }

        if (h != 0) { t1ActionIndex = Mathf.Clamp(t1ActionIndex + h, 0, 1); ApplyT1ActionsFocus(); }
        else if (v < 0) { ClearT1ActionsFocus(); focus = SubFocus.List; HighlightCards(onlineIndex); }
        return true;
    }

    public void Submit()
    {
        if (focus == SubFocus.List)
        {
            if (onlineIndex >= 0 && onlineIndex < onlineCards.Count)
            {
                SelectOnline((string)onlineCards[onlineIndex].userData);
                ClearFocus(onlineCards);
                focus = SubFocus.Actions;
                t1ActionIndex = 0;
                ApplyT1ActionsFocus();
            }
        }
        else
        {
            EnqueueOnline(t1ActionIndex == 0);
        }
    }

    public bool Cancel()
    {
        if (focus == SubFocus.Actions)
        {
            ClearT1ActionsFocus();
            focus = SubFocus.List;
            HighlightCards(onlineIndex);
            return true;
        }
        return false;
    }

    public void ClearFocus()
    {
        ClearFocus(onlineCards);
        ClearT1ActionsFocus();
        focus = SubFocus.List;
    }

    public void Rebuild()
    {
        RebuildOnlineList();
        SetCenter();
    }

    public void Teardown()
    {
        if (btnInstant != null) btnInstant.clicked -= OnInstantClicked;
        if (btnTimer   != null) btnTimer.clicked   -= OnTimerClicked;
    }

    // ── Data ─────────────────────────────────────────────────────

    private void RebuildOnlineList()
    {
        if (onlineList == null) return;
        onlineList.Clear(); onlineCards.Clear();
        foreach (var dna in Eligible())
            onlineList.Add(MakeCandidate(dna));
    }

    private VisualElement MakeCandidate(CreatureDNA dna)
    {
        var row = new VisualElement();
        row.AddToClassList("cbt-candidate");
        row.userData = dna.UniqueID;

        var eff = StatsOf(dna);
        var l = new Label(Loc.Tr("ui.combat.online.candidate",
            dna.CustomName,
            LocEnumMaps.StatAbbrev(StatType.Constitution), eff.Constitution,
            LocEnumMaps.StatAbbrev(StatType.Attack),       eff.Attack,
            LocEnumMaps.StatAbbrev(StatType.Speed),        eff.Speed,
            LocEnumMaps.StatAbbrev(StatType.Defense),      eff.Defense,
            LocEnumMaps.StatAbbrev(StatType.Luck),         eff.Luck,
            LocEnumMaps.StatAbbrev(StatType.Evasion),      eff.Evasion,
            dna.FightCount, MaxFights()));
        l.AddToClassList("cbt-candidate-text");
        row.Add(l);

        string id = dna.UniqueID;
        row.RegisterCallback<ClickEvent>(_ => SelectOnline(id));
        onlineCards.Add(row);
        return row;
    }

    private void SelectOnline(string id) { onlineSelectedId = id; SetCenter(); }

    private void SetCenter()
    {
        var registry = getRegistry();
        CreatureDNA dna = null;
        if (!string.IsNullOrEmpty(onlineSelectedId) && registry != null) registry.TryGet(onlineSelectedId, out dna);

        if (onlineImg != null) MonchiPortraitUI.Apply(onlineImg, dna);
        if (onlineName != null) onlineName.text = dna != null ? dna.CustomName : Loc.Tr("ui.combat.online.select_prompt");

        if (onlineStats != null)
        {
            if (dna != null)
            {
                var e = StatsOf(dna);
                onlineStats.text = Loc.Tr("ui.combat.online.stats",
                    LocEnumMaps.StatAbbrev(StatType.Constitution), e.Constitution,
                    LocEnumMaps.StatAbbrev(StatType.Attack),       e.Attack,
                    LocEnumMaps.StatAbbrev(StatType.Speed),        e.Speed,
                    LocEnumMaps.StatAbbrev(StatType.Defense),      e.Defense,
                    LocEnumMaps.StatAbbrev(StatType.Luck),         e.Luck,
                    LocEnumMaps.StatAbbrev(StatType.Evasion),      e.Evasion);
            }
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
        var registry = getRegistry();
        if (string.IsNullOrEmpty(onlineSelectedId)) { Debug.LogWarning("[CombatPanel] Select a MoriMochi first."); return; }
        if (asyncCombatService == null) { Debug.LogError("[CombatPanel] AsyncCombatService not assigned."); return; }
        if (registry == null || !registry.TryGet(onlineSelectedId, out var dna)) return;

        if (instant) _ = asyncCombatService.EnqueueInstantAsync(dna);
        else         _ = asyncCombatService.EnqueueScheduledAsync(dna);

        onlineSelectedId = "";
        SetCenter();
        onEnqueued();
    }

    private IEnumerable<CreatureDNA> Eligible()
    {
        var registry = getRegistry();
        return registry == null ? Enumerable.Empty<CreatureDNA>()
            : registry.GetAll().Values
                .Where(d => !d.IsDead && !d.IsBusy && d.FightCount < MaxFights())
                .OrderBy(d => d.CustomName);
    }

    private int MaxFights() => Config != null ? Config.MaxFightCount : 5;

    private EffectiveStats StatsOf(CreatureDNA dna) =>
        database != null ? CombatStats.GetEffectiveStats(dna, database)
                         : new EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);

    // ── Focus visuals ────────────────────────────────────────────

    private void HighlightCards(int idx)
    {
        for (int i = 0; i < onlineCards.Count; i++) onlineCards[i].EnableInClassList(Focus, i == idx);
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
}
}
