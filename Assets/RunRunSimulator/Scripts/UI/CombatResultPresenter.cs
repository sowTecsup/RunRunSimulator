using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class CombatResultPresenter
{
    public event Action AgainRequested;
    public event Action CloseRequested;

    private readonly VisualElement card;
    private readonly Label title;
    private readonly Label score;
    private readonly Label line;
    private readonly Button again;
    private readonly Button close;

    private readonly List<VisualElement> buttons;
    private int selected = -1;

    public CombatResultPresenter(VisualElement view)
    {
        card = view.Q("result-card");
        title = view.Q<Label>("result-title");
        score = view.Q<Label>("result-score");
        line = view.Q<Label>("result-line");

        again = view.Q<Button>("result-again");
        if (again != null)
        {
            again.text = Loc.Tr("ui.rps.again");
            again.clicked += () => AgainRequested?.Invoke();
        }

        close = view.Q<Button>("result-close");
        if (close != null)
        {
            close.text = Loc.Tr("ui.rps.close");
            close.clicked += () => CloseRequested?.Invoke();
        }

        buttons = new List<VisualElement> { again, close };
    }

    public void Show(CombatOutcome outcome, CreatureDNA player, CreatureDNA rival)
    {
        card?.EnableInClassList("rps-result--win", outcome.Won);
        card?.EnableInClassList("rps-result--lose", !outcome.Won);

        if (title != null) title.text = Loc.Tr(outcome.Won ? "ui.rps.result.win" : "ui.rps.result.lose");
        if (score != null) score.text = Loc.Tr("ui.rps.result.score", outcome.HitsPlayer, outcome.HitsRival, outcome.Rounds);
        if (line != null)
        {
            line.text = outcome.Won
                ? Loc.Tr("ui.rps.result.material", outcome.MaterialGained)
                : Loc.Tr("ui.rps.result.cooldown", player.CustomName, new DateTime(outcome.CooldownUntilTicks).ToString("HH:mm"));
        }

        Select(0);

        card?.AddToClassList("rps-result--enter");
        card?.schedule.Execute(() => card.RemoveFromClassList("rps-result--enter")).ExecuteLater(40);
    }

    private void Select(int idx)
    {
        selected = UiPanels.ClampSelection(buttons.Count, idx);
        UiPanels.SetActiveIndex(buttons, selected, "rps-selected");
    }

    public void Move(float dx)
    {
        if (dx > 0.5f) Select(selected + 1);
        else if (dx < -0.5f) Select(selected - 1);
    }

    public void Submit()
    {
        if (selected == 0) AgainRequested?.Invoke();
        else if (selected == 1) CloseRequested?.Invoke();
    }
}
}
