using System;
using System.Collections.Generic;
using MoriMonchiSimulator.DragonRps;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class CombatDuelPresenter
{
    public event Action<int> CardPlayed;

    private readonly VisualElement sidePlayer;
    private readonly VisualElement sideRival;
    private readonly Label log;
    private readonly VisualElement hand;

    private Label[] playerIntact;
    private Label[] rivalIntact;
    private VisualElement[] playerPips;
    private VisualElement[] rivalPips;

    private readonly List<VisualElement> handButtons = new List<VisualElement>();
    private readonly List<DragonAction> handActions = new List<DragonAction>();
    private readonly RpsTriangleElement triangle;
    private int selected = -1;

    public CombatDuelPresenter(VisualElement view)
    {
        sidePlayer = view.Q("side-player");
        sideRival = view.Q("side-rival");
        log = view.Q<Label>("clash-log");
        hand = view.Q("hand");

        triangle = new RpsTriangleElement();
        var clash = view.Q("clash");
        clash?.Insert(0, triangle);
        triangle.SetLabels(ActionName(DragonAction.Horns), ActionName(DragonAction.Wings), ActionName(DragonAction.Back));
    }

    public void Begin(DragonRpsSession session, CreatureDNA player, CreatureDNA rival)
    {
        sidePlayer?.Clear();
        sideRival?.Clear();

        BuildSide(sidePlayer, player, session.Player, out playerIntact, out playerPips);
        BuildSide(sideRival, rival, session.Foe, out rivalIntact, out rivalPips);

        Rebuild(session, null);
    }

    private void BuildSide(VisualElement side, CreatureDNA dna, DragonRpsSide state, out Label[] intact, out VisualElement[] pips)
    {
        var portrait = new VisualElement();
        portrait.AddToClassList("mm-swatch");
        portrait.AddToClassList("rps-portrait");
        MonchiPortraitUI.Apply(portrait, dna);
        side?.Add(portrait);

        var name = new Label(dna.CustomName);
        name.AddToClassList("rps-name");
        side?.Add(name);

        var powerTitle = new Label(Loc.Tr("ui.rps.power"));
        powerTitle.AddToClassList("rps-row-title");
        side?.Add(powerTitle);

        var powerRow = new VisualElement();
        powerRow.AddToClassList("rps-row");
        side?.Add(powerRow);

        for (int t = 0; t < DragonRpsRules.ActionCount; t++)
        {
            var chip = new VisualElement();
            chip.AddToClassList("rps-power");

            var type = new Label(ActionName((DragonAction)t));
            type.AddToClassList("rps-power__type");
            chip.Add(type);

            var value = new Label(state.Dragon.Power[t].ToString());
            value.AddToClassList("rps-power__value");
            chip.Add(value);

            powerRow.Add(chip);
        }

        var intactTitle = new Label(Loc.Tr("ui.rps.intact"));
        intactTitle.AddToClassList("rps-row-title");
        side?.Add(intactTitle);

        var intactRow = new VisualElement();
        intactRow.AddToClassList("rps-row");
        side?.Add(intactRow);

        intact = new Label[DragonRpsRules.ActionCount];
        for (int t = 0; t < DragonRpsRules.ActionCount; t++)
        {
            var label = new Label();
            label.AddToClassList("rps-intact");
            intact[t] = label;
            intactRow.Add(label);
        }

        var hitsTitle = new Label(Loc.Tr("ui.rps.hits"));
        hitsTitle.AddToClassList("rps-row-title");
        side?.Add(hitsTitle);

        var hitsRow = new VisualElement();
        hitsRow.AddToClassList("rps-row");
        side?.Add(hitsRow);

        pips = new VisualElement[DragonRpsRules.HitsToWin];
        for (int i = 0; i < DragonRpsRules.HitsToWin; i++)
        {
            var pip = new VisualElement();
            pip.AddToClassList("rps-pip");
            pips[i] = pip;
            hitsRow.Add(pip);
        }
    }

    public void Rebuild(DragonRpsSession session, string logLine)
    {
        RebuildSide(session.Player, playerIntact, playerPips);
        RebuildSide(session.Foe, rivalIntact, rivalPips);

        hand?.Clear();
        handButtons.Clear();
        handActions.Clear();

        for (int i = 0; i < session.Player.Hand.Count; i++)
        {
            int index = i;
            var action = session.Player.Hand[i];
            var b = new Button(() => CardPlayed?.Invoke(index)) { text = ActionName(action) };
            b.AddToClassList("rps-action");
            b.AddToClassList(action == DragonAction.Horns ? "rps-action--horns" : action == DragonAction.Wings ? "rps-action--wings" : "rps-action--back");
            b.SetEnabled(!session.Finished);

            handButtons.Add(b);
            handActions.Add(action);
            hand?.Add(b);
        }

        selected = UiPanels.ClampSelection(handButtons.Count, selected);
        UiPanels.SetActiveIndex(handButtons, selected, "rps-action--selected");
        SyncTriangle();

        if (log != null)
        {
            log.text = logLine ?? Loc.Tr("ui.rps.duel.start");
            if (logLine != null)
            {
                log.AddToClassList("rps-log--flash");
                log.schedule.Execute(() => log.RemoveFromClassList("rps-log--flash")).ExecuteLater(450);
            }
        }
    }

    private void SyncTriangle()
    {
        triangle.Highlight = selected >= 0 && selected < handActions.Count ? (int)handActions[selected] : -1;
    }

    private void RebuildSide(DragonRpsSide state, Label[] intact, VisualElement[] pips)
    {
        int[] rem = state.RemainingByType();
        for (int t = 0; t < DragonRpsRules.ActionCount; t++)
        {
            intact[t].text = ActionName((DragonAction)t) + " ×" + rem[t];
            intact[t].EnableInClassList("rps-intact--none", rem[t] == 0);
        }

        for (int i = 0; i < pips.Length; i++)
        {
            pips[i].EnableInClassList("rps-pip--hit", i < state.Hits);
        }
    }

    private string ActionName(DragonAction a) =>
        a == DragonAction.Horns ? Loc.Tr("ui.rps.action.horns") : a == DragonAction.Wings ? Loc.Tr("ui.rps.action.wings") : Loc.Tr("ui.rps.action.back");

    private void Select(int idx)
    {
        selected = UiPanels.ClampSelection(handButtons.Count, idx);
        UiPanels.SetActiveIndex(handButtons, selected, "rps-action--selected");
        SyncTriangle();
    }

    public void Move(float dx)
    {
        if (dx > 0.5f) Select(selected + 1);
        else if (dx < -0.5f) Select(selected - 1);
    }

    public void Submit()
    {
        if (selected >= 0) CardPlayed?.Invoke(selected);
    }

    public string Describe(DragonRpsRoundInfo round)
    {
        string text;
        if (round.Mirror)
        {
            text = Loc.Tr("ui.rps.round.mirror", ActionName(round.Player), round.PlayerPower, round.FoePower)
                + " · " + (round.Scorer == 0 ? Loc.Tr("ui.rps.round.null") : round.Scorer == 1 ? Loc.Tr("ui.rps.round.youhit") : Loc.Tr("ui.rps.round.foehit"));
        }
        else if (round.Scorer == 1)
        {
            text = Loc.Tr("ui.rps.round.you", ActionName(round.Player), ActionName(round.Foe));
        }
        else
        {
            text = Loc.Tr("ui.rps.round.foe", ActionName(round.Foe), ActionName(round.Player));
        }

        if (round.Reshuffled) text += " · " + Loc.Tr("ui.rps.reshuffle");
        return text;
    }
}
}
