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
        public Label         TurnMarker;
        public VisualElement AllyMarksRow;
        public VisualElement EnemyMarksRow;
        public VisualElement StatesRow;
        public VisualElement AffinityDot0;
        public VisualElement AffinityDot1;
        public Label         EnergyLabel;
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
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnVisualCombatStart -= HandleStart;
        CombatVisualEvents.OnActionOrder       -= HandleOrder;
        CombatVisualEvents.OnUnitAffinity      -= HandleAffinity;
        CombatVisualEvents.OnActiveUnit        -= HandleActiveUnit;
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

        var teamGap = new VisualElement();
        teamGap.AddToClassList("cv-ob-team-gap");
        orderBar.Add(teamGap);

        BuildTeam(CombatVisualSide.B, ctx.SnapsB, ctx.TeamB);
    }

    private void BuildTeam(CombatVisualSide side, List<CombatFighterSnapshot> snaps, List<CreatureDNA> team)
    {
        if (snaps == null) return;

        for (int i = 0; i < snaps.Count; i++)
        {
            var element = team != null && i < team.Count && team[i] != null ? team[i].Element : Element.Agua;
            var card    = CreateCard(side, snaps[i], element);
            cards[(side, i)] = card;
            orderBar.Add(card.Root);
        }
    }

    private OrderCard CreateCard(CombatVisualSide side, CombatFighterSnapshot snap, Element element)
    {
        var cardRoot = new VisualElement();
        cardRoot.AddToClassList("cv-order-card");
        cardRoot.AddToClassList(side == CombatVisualSide.A ? "cv-order-card--self" : "cv-order-card--opp");

        var turnMarker = new Label("▼");
        turnMarker.AddToClassList("cv-ob-turn-marker");
        turnMarker.style.display = DisplayStyle.None;
        cardRoot.Add(turnMarker);

        var allyRow = new VisualElement();
        allyRow.AddToClassList("cv-ob-mark-row");
        cardRoot.Add(allyRow);

        var body = new VisualElement();
        body.AddToClassList("cv-ob-body");
        cardRoot.Add(body);

        var swatch = new VisualElement();
        swatch.AddToClassList("cv-ob-swatch");
        swatch.style.backgroundColor = SnapshotColor(snap);
        body.Add(swatch);

        var nameLabel = new Label(snap.Name);
        nameLabel.AddToClassList("cv-ob-name");
        body.Add(nameLabel);

        var roleLabel = new Label(RoleText(snap.Role));
        roleLabel.AddToClassList("cv-ob-role");
        body.Add(roleLabel);

        var identity  = Identity(element);
        var elemLabel = new Label(identity.DisplayName);
        elemLabel.AddToClassList("cv-ob-elem");
        elemLabel.style.color = identity.UiColor;
        body.Add(elemLabel);

        var affinityRow = new VisualElement();
        affinityRow.AddToClassList("cv-ob-affinity-row");
        cardRoot.Add(affinityRow);

        var dot0 = new VisualElement();
        dot0.AddToClassList("cv-ob-dot");
        affinityRow.Add(dot0);

        var dot1 = new VisualElement();
        dot1.AddToClassList("cv-ob-dot");
        affinityRow.Add(dot1);

        var energyLabel = new Label("");
        energyLabel.AddToClassList("cv-ob-energy");
        affinityRow.Add(energyLabel);

        var statesRow = new VisualElement();
        statesRow.AddToClassList("cv-ob-states-row");
        cardRoot.Add(statesRow);

        var enemyRow = new VisualElement();
        enemyRow.AddToClassList("cv-ob-mark-row");
        cardRoot.Add(enemyRow);

        return new OrderCard
        {
            Root          = cardRoot,
            TurnMarker    = turnMarker,
            AllyMarksRow  = allyRow,
            EnemyMarksRow = enemyRow,
            StatesRow     = statesRow,
            AffinityDot0  = dot0,
            AffinityDot1  = dot1,
            EnergyLabel   = energyLabel,
        };
    }

    private void HandleOrder(List<CombatOrderEntry> order)
    {
        if (!EnsureRefs() || !hasCtx || order == null) return;

        foreach (var entry in order)
        {
            if (!cards.TryGetValue((entry.Side, entry.Index), out var card)) continue;
            ApplyState(card, entry);
        }
    }

    private void HandleActiveUnit(CombatVisualSide side, int index)
    {
        if (!EnsureRefs()) return;

        foreach (var kvp in cards)
        {
            bool isActive = kvp.Key.Side == side && kvp.Key.Index == index;
            kvp.Value.Root.EnableInClassList("cv-order-card--active", isActive);
            kvp.Value.TurnMarker.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void ApplyState(OrderCard card, CombatOrderEntry entry)
    {
        card.Root.EnableInClassList("cv-order-card--dead", !entry.Alive);

        var state = entry.State;
        BuildMarkRow(card.AllyMarksRow, state?.ElementMarks, true);
        BuildMarkRow(card.EnemyMarksRow, state?.ElementMarks, false);
        BuildStatesRow(card.StatesRow, state?.ArmedStates);
        SetAffinity(card, state?.Affinity ?? 0, state?.Energy ?? 0);
    }

    private void HandleAffinity(CombatVisualSide side, int index, int affinity, int energy)
    {
        if (!EnsureRefs() || !cards.TryGetValue((side, index), out var card)) return;

        if (affinity >= 0)
        {
            card.AffinityDot0.EnableInClassList("cv-ob-dot--filled", affinity >= 1);
            card.AffinityDot1.EnableInClassList("cv-ob-dot--filled", affinity >= 2);
        }
        if (energy >= 0)
            card.EnergyLabel.text = energy > 0 ? $"⚡{energy}" : "";
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
        var chip = new VisualElement();
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

    private void SetAffinity(OrderCard card, int affinity, int energy)
    {
        card.AffinityDot0.EnableInClassList("cv-ob-dot--filled", affinity >= 1);
        card.AffinityDot1.EnableInClassList("cv-ob-dot--filled", affinity >= 2);
        card.EnergyLabel.text = energy > 0 ? $"⚡{energy}" : "";
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

    private static Color SnapshotColor(CombatFighterSnapshot s) =>
        !string.IsNullOrEmpty(s.ColorHex) && ColorUtility.TryParseHtmlString("#" + s.ColorHex, out var c) ? c : Color.gray;

    private static string RoleText(Role role) => role switch
    {
        Role.Protector => "Protector",
        Role.Agresivo  => "Agresivo",
        Role.Empatico  => "Empático",
        _              => role.ToString(),
    };

    private void SetVisible(bool v)
    {
        if (orderBar == null) return;
        orderBar.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
}
