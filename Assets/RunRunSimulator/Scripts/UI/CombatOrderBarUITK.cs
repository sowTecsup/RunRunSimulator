using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class CombatOrderBarUITK : MonoBehaviour
{
    [SerializeField] private UIDocument     document;
    [SerializeField] private ElementTableSO elementTable;

    private static readonly HashSet<ElementalState> NegativeStates = new HashSet<ElementalState>
    {
        ElementalState.Boiling, ElementalState.Debilidad, ElementalState.Confuso,
        ElementalState.Leech, ElementalState.Mareado, ElementalState.PisoTierra,
    };

    private class OrderCard
    {
        public VisualElement Root;
        public VisualElement Slot;
        public VisualElement AllyMarksRow;
        public VisualElement EnemyMarksRow;
        public VisualElement StatesRow;
        public VisualElement AffinityDot0;
        public VisualElement AffinityDot1;
        public Color         ElementColor;
        public List<CombatElementMark> Marks  = new List<CombatElementMark>();
        public List<ElementalState>    States = new List<ElementalState>();
    }

    private VisualElement root;
    private VisualElement orderBar;
    private VisualElement tooltip;
    private Label         tooltipLabel;

    private CombatVisualContext ctx;
    private bool                hasCtx;

    private readonly Dictionary<(CombatVisualSide Side, int Index), OrderCard> cards =
        new Dictionary<(CombatVisualSide, int), OrderCard>();

    private void OnEnable()
    {
        CombatVisualEvents.OnVisualCombatStart += HandleStart;
        CombatVisualEvents.OnActionOrder       += HandleOrder;
        CombatVisualEvents.OnUnitAffinity      += HandleAffinity;
        CombatVisualEvents.OnActiveUnit        += HandleActiveUnit;
        CombatVisualEvents.OnUnitElement       += HandleUnitElement;
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnVisualCombatStart -= HandleStart;
        CombatVisualEvents.OnActionOrder       -= HandleOrder;
        CombatVisualEvents.OnUnitAffinity      -= HandleAffinity;
        CombatVisualEvents.OnActiveUnit        -= HandleActiveUnit;
        CombatVisualEvents.OnUnitElement       -= HandleUnitElement;
    }

    private void Start()
    {
        EnsureRefs();
        SetVisible(false);
    }

    private bool EnsureRefs()
    {
        if (document == null) return false;
        var currentRoot = document.rootVisualElement;
        if (currentRoot == null) return false;

        if (currentRoot != root)
        {
            root         = currentRoot;
            orderBar     = null;
            tooltip      = null;
            tooltipLabel = null;
            cards.Clear();
        }

        if (orderBar == null)
        {
            orderBar = root.Q<VisualElement>("order-bar");
            if (orderBar == null) return false;
        }

        if (tooltip == null)
        {
            tooltip = new VisualElement { name = "cv-ob-tooltip", pickingMode = PickingMode.Ignore };
            tooltip.AddToClassList("cv-ob-tooltip");
            tooltipLabel = new Label();
            tooltipLabel.AddToClassList("cv-ob-tooltip-text");
            tooltip.Add(tooltipLabel);
            tooltip.style.display = DisplayStyle.None;
            root.Add(tooltip);
        }

        return true;
    }

    private void HandleStart(CombatVisualContext c)
    {
        if (!EnsureRefs()) return;
        ctx    = c;
        hasCtx = true;
        BuildCards();
        SetVisible(true);
    }

    private void BuildCards()
    {
        orderBar.Clear();
        cards.Clear();

        BuildTeam(CombatVisualSide.A, ctx.SnapsA, ctx.TeamA);
        BuildTeam(CombatVisualSide.B, ctx.SnapsB, ctx.TeamB);
    }

    private void BuildTeam(CombatVisualSide side, List<CombatFighterSnapshot> snaps, List<CreatureDNA> team)
    {
        if (snaps == null) return;

        for (int i = 0; i < snaps.Count; i++)
        {
            var dna     = team != null && i < team.Count ? team[i] : null;
            var element = dna != null ? dna.Element : Element.Agua;
            var card    = CreateCard(side, snaps[i], element, dna);
            cards[(side, i)] = card;

            var slot = new VisualElement();
            slot.AddToClassList("cv-ob-slot");
            slot.Add(card.Root);
            slot.Add(card.StatesRow);
            card.Slot = slot;
            orderBar.Add(slot);

            int unitIndex = i;
            slot.RegisterCallback<PointerEnterEvent>(_ => CombatVisualEvents.UnitHover(side, unitIndex, true));
            slot.RegisterCallback<PointerLeaveEvent>(_ => CombatVisualEvents.UnitHover(side, unitIndex, false));
        }
    }

    private OrderCard CreateCard(CombatVisualSide side, CombatFighterSnapshot snap, Element element, CreatureDNA dna)
    {
        var cardRoot = new VisualElement();
        cardRoot.AddToClassList("cv-order-card");
        cardRoot.AddToClassList(side == CombatVisualSide.A ? "cv-order-card--self" : "cv-order-card--opp");

        var banner = new VisualElement();
        banner.AddToClassList("cv-ob-team-banner");
        banner.AddToClassList(side == CombatVisualSide.A ? "cv-ob-team-banner--self" : "cv-ob-team-banner--opp");
        cardRoot.Add(banner);

        var body = new VisualElement();
        body.AddToClassList("cv-ob-body-row");
        cardRoot.Add(body);

        var headshot = new VisualElement();
        headshot.AddToClassList("cv-ob-headshot");
        MonchiPortraitUI.ApplyHeadshot(headshot, dna);
        RegisterTooltip(headshot, $"{snap.Name} — Elemento: {Identity(element).DisplayName}");
        body.Add(headshot);

        var roleChip = new Label(RoleText(snap.Role));
        roleChip.AddToClassList("cv-ob-role-chip");
        roleChip.AddToClassList(RoleChipClass(snap.Role));
        RegisterTooltip(roleChip, $"Rol: {RoleText(snap.Role)}");
        body.Add(roleChip);

        var elementColor = MarkColor(element);

        var affinityRow = new VisualElement();
        affinityRow.AddToClassList("cv-ob-affinity-row");
        cardRoot.Add(affinityRow);

        var dot0 = new VisualElement();
        dot0.AddToClassList("cv-ob-dot");
        affinityRow.Add(dot0);

        var affinityPlus = new Label("+");
        affinityPlus.AddToClassList("cv-ob-affinity-plus");
        affinityRow.Add(affinityPlus);

        var dot1 = new VisualElement();
        dot1.AddToClassList("cv-ob-dot");
        affinityRow.Add(dot1);

        var affinityEquals = new Label("=");
        affinityEquals.AddToClassList("cv-ob-affinity-equals");
        affinityRow.Add(affinityEquals);

        var affinityElement = new Label(Identity(element).DisplayName);
        affinityElement.AddToClassList("cv-ob-affinity-element");
        affinityElement.style.backgroundColor = elementColor;
        RegisterTooltip(affinityElement, $"Cada acción llena un punto; con 2 se aplica su marca de {Identity(element).DisplayName} sobre sí mismo");
        affinityRow.Add(affinityElement);

        var marksSplit = new VisualElement();
        marksSplit.AddToClassList("cv-ob-marks-split");
        cardRoot.Add(marksSplit);

        var allyCol = new VisualElement();
        allyCol.AddToClassList("cv-ob-marks-col");
        allyCol.AddToClassList("cv-ob-marks-col--ally");
        marksSplit.Add(allyCol);

        var enemyCol = new VisualElement();
        enemyCol.AddToClassList("cv-ob-marks-col");
        enemyCol.AddToClassList("cv-ob-marks-col--enemy");
        marksSplit.Add(enemyCol);

        var statesRow = new VisualElement();
        statesRow.AddToClassList("cv-ob-states-row");

        return new OrderCard
        {
            Root          = cardRoot,
            AllyMarksRow  = allyCol,
            EnemyMarksRow = enemyCol,
            StatesRow     = statesRow,
            AffinityDot0  = dot0,
            AffinityDot1  = dot1,
            ElementColor  = elementColor,
        };
    }

    private void HandleOrder(List<CombatOrderEntry> order)
    {
        if (!EnsureRefs() || !hasCtx || order == null) return;

        foreach (var entry in order)
        {
            if (!cards.TryGetValue((entry.Side, entry.Index), out var card)) continue;
            ApplyState(card, entry);
            orderBar.Add(card.Slot);
        }
    }

    private void HandleActiveUnit(CombatVisualSide side, int index)
    {
        if (!EnsureRefs()) return;

        foreach (var kvp in cards)
        {
            bool isActive = kvp.Key.Side == side && kvp.Key.Index == index;
            kvp.Value.Root.EnableInClassList("cv-order-card--active", isActive);
        }
    }

    private void ApplyState(OrderCard card, CombatOrderEntry entry)
    {
        card.Root.EnableInClassList("cv-order-card--dead", !entry.Alive);

        var state = entry.State;
        card.Marks  = state?.ElementMarks != null ? new List<CombatElementMark>(state.ElementMarks) : new List<CombatElementMark>();
        card.States = state?.ArmedStates  != null ? new List<ElementalState>(state.ArmedStates)     : new List<ElementalState>();
        RebuildElements(card);
        SetAffinity(card, state?.Affinity ?? 0);
    }

    private void HandleUnitElement(CombatElementEventData d)
    {
        if (!EnsureRefs() || !cards.TryGetValue((d.Side, d.Index), out var card)) return;

        switch (d.Kind)
        {
            case ElementEventKind.MarkApplied:
                card.Marks.Add(new CombatElementMark { Element = d.Element, AllySource = d.AllySource });
                break;
            case ElementEventKind.MarkRemoved:
                RemoveMark(card.Marks, d.Element, d.AllySource);
                break;
            case ElementEventKind.Reaction:
                RemoveMark(card.Marks, d.Element, d.AllySource);
                RemoveMark(card.Marks, d.ElementB, d.AllySource);
                if (System.Enum.TryParse<ElementalState>(d.ReactionName, out var reacted)
                 && !card.States.Contains(reacted))
                    card.States.Add(reacted);
                break;
            case ElementEventKind.StateArmed:
                if (!card.States.Contains(d.State)) card.States.Add(d.State);
                break;
            case ElementEventKind.StateConsumed:
            case ElementEventKind.StateRemoved:
                card.States.Remove(d.State);
                break;
        }
        RebuildElements(card);
    }

    private static void RemoveMark(List<CombatElementMark> marks, Element element, bool ally)
    {
        for (int i = 0; i < marks.Count; i++)
            if (marks[i].Element == element && marks[i].AllySource == ally) { marks.RemoveAt(i); return; }
    }

    private void RebuildElements(OrderCard card)
    {
        BuildMarkRow(card.AllyMarksRow, card.Marks, true);
        BuildMarkRow(card.EnemyMarksRow, card.Marks, false);
        BuildStatesRow(card.StatesRow, card.States);
    }

    private void HandleAffinity(CombatVisualSide side, int index, int affinity)
    {
        if (!EnsureRefs() || !cards.TryGetValue((side, index), out var card)) return;

        SetAffinity(card, affinity);
    }

    private void BuildMarkRow(VisualElement row, List<CombatElementMark> marks, bool ally)
    {
        row.Clear();
        if (marks == null) return;

        foreach (var mark in marks)
        {
            if (mark.AllySource != ally) continue;
            row.Add(CreateMarkChip(mark));
        }
    }

    private VisualElement CreateMarkChip(CombatElementMark mark)
    {
        var chip = new Label(Identity(mark.Element).DisplayName);
        chip.AddToClassList("cv-ob-mark-chip");
        chip.style.backgroundColor = MarkColor(mark.Element);

        string source = mark.AllySource ? "aliada" : "enemiga";
        string text   = $"Marca {Identity(mark.Element).DisplayName} ({source}) — reacciona al juntarse con otra marca {source} de distinto elemento";
        RegisterTooltip(chip, text);
        return chip;
    }

    private void BuildStatesRow(VisualElement row, List<ElementalState> states)
    {
        row.Clear();
        if (states == null) return;

        foreach (var state in states)
            row.Add(CreateStateChip(state));
    }

    private VisualElement CreateStateChip(ElementalState state)
    {
        var def  = StateOf(state);
        var chip = new Label(def.DisplayName);
        chip.AddToClassList("cv-ob-state-chip");
        chip.AddToClassList(NegativeStates.Contains(state) ? "cv-ob-state-chip--negative" : "cv-ob-state-chip--positive");

        RegisterTooltip(chip, $"{def.DisplayName}: {def.Description}");
        return chip;
    }

    private void SetAffinity(OrderCard card, int affinity)
    {
        ApplyAffinityDot(card.AffinityDot0, affinity >= 1, card.ElementColor);
        ApplyAffinityDot(card.AffinityDot1, affinity >= 2, card.ElementColor);
    }

    private static void ApplyAffinityDot(VisualElement dot, bool filled, Color color)
    {
        dot.EnableInClassList("cv-ob-dot--filled", filled);
        dot.style.backgroundColor = filled ? (StyleColor)color : StyleKeyword.Null;
    }

    private void RegisterTooltip(VisualElement chip, string text)
    {
        chip.RegisterCallback<PointerEnterEvent>(_ => ShowTooltip(chip, text));
        chip.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
    }

    private void ShowTooltip(VisualElement anchor, string text)
    {
        if (tooltip == null || tooltipLabel == null) return;
        tooltipLabel.text     = text;
        tooltip.style.display = DisplayStyle.Flex;

        var bound = anchor.worldBound;
        tooltip.style.left = bound.x;
        tooltip.style.top  = bound.yMax + 4f;
    }

    private void HideTooltip()
    {
        if (tooltip == null) return;
        tooltip.style.display = DisplayStyle.None;
    }

    private ElementIdentity Identity(Element e) =>
        elementTable != null ? elementTable.GetIdentity(e) : new ElementIdentity { DisplayName = e.ToString(), UiColor = Color.white };

    private StateDefinition StateOf(ElementalState s) =>
        elementTable != null ? elementTable.GetState(s) : new StateDefinition { DisplayName = s.ToString() };

    private Color MarkColor(Element e)
    {
        var color = Identity(e).UiColor;
        return color.a <= 0f ? Color.white : color;
    }

    private static string RoleText(Role role) => role switch
    {
        Role.Protector => "Protector",
        Role.Agresivo  => "Agresivo",
        Role.Empatico  => "Empático",
        _              => role.ToString(),
    };

    private static string RoleChipClass(Role role) => role switch
    {
        Role.Protector => "cv-ob-role-chip--protector",
        Role.Agresivo  => "cv-ob-role-chip--agresivo",
        Role.Empatico  => "cv-ob-role-chip--empatico",
        _              => "cv-ob-role-chip",
    };

    private void SetVisible(bool v)
    {
        if (orderBar == null) return;
        orderBar.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
}
