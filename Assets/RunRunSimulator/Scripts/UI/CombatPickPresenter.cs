using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class CombatPickPresenter
{
    public event Action<CreatureDNA> FightRequested;
    public event Action CloseRequested;

    private readonly Label title;
    private readonly ScrollView list;
    private readonly Button fightButton;
    private readonly Button closeButton;

    private readonly List<VisualElement> cards = new List<VisualElement>();
    private readonly List<CreatureDNA> dnas = new List<CreatureDNA>();
    private readonly List<bool> eligible = new List<bool>();
    private int selected = -1;

    public CombatPickPresenter(VisualElement view)
    {
        title = view.Q<Label>("pick-title");
        if (title != null) title.text = Loc.Tr("ui.rps.pick.title");

        list = view.Q<ScrollView>("pick-list");

        fightButton = view.Q<Button>("pick-fight");
        if (fightButton != null)
        {
            fightButton.text = Loc.Tr("ui.rps.pick.fight");
            fightButton.clicked += Submit;
        }

        closeButton = view.Q<Button>("pick-close");
        if (closeButton != null)
        {
            closeButton.text = Loc.Tr("ui.rps.close");
            closeButton.clicked += () => CloseRequested?.Invoke();
        }
    }

    public void Rebuild(CreatureRegistrySO registry, CombatTuningSO tuning, DateTime now)
    {
        list?.Clear();
        cards.Clear();
        dnas.Clear();
        eligible.Clear();

        if (registry != null)
        {
            foreach (var dna in registry.GetAll().Values)
            {
                if (dna.IsDead || dna.IsSold) continue;

                bool ok = DragonRpsGenes.CanFight(dna, tuning, now);
                var card = BuildCard(dna, ok, now);

                int index = cards.Count;
                card.RegisterCallback<ClickEvent>(_ => Select(index));

                cards.Add(card);
                dnas.Add(dna);
                eligible.Add(ok);
                list?.Add(card);
            }
        }

        int firstEligible = -1;
        for (int i = 0; i < eligible.Count; i++)
        {
            if (eligible[i]) { firstEligible = i; break; }
        }

        Select(cards.Count == 0 ? -1 : (firstEligible >= 0 ? firstEligible : 0));
    }

    private VisualElement BuildCard(CreatureDNA dna, bool ok, DateTime now)
    {
        var card = new VisualElement();
        card.AddToClassList("rps-card");
        if (!ok) card.AddToClassList("rps-card--off");

        var icon = new VisualElement();
        icon.AddToClassList("rps-card__icon");
        MonchiPortraitUI.Apply(icon, dna);
        card.Add(icon);

        var name = new Label(dna.CustomName);
        name.AddToClassList("rps-card__name");
        card.Add(name);

        var state = new Label(StateTextFor(dna, ok, now));
        state.AddToClassList("rps-card__state");
        card.Add(state);

        return card;
    }

    private string StateTextFor(CreatureDNA dna, bool ok, DateTime now)
    {
        if (ok) return dna.HornPotential + "·" + dna.WingPotential + "·" + dna.BackPotential;
        if (dna.IsBusy) return Loc.Tr("ui.rps.pick.busy");
        if (dna.CombatCooldownUntil > now.Ticks) return Loc.Tr("ui.rps.pick.cooldown", new DateTime(dna.CombatCooldownUntil).ToString("HH:mm"));
        return Loc.Tr("ui.rps.pick.tired");
    }

    private void Select(int idx)
    {
        selected = UiPanels.ClampSelection(cards.Count, idx);
        UiPanels.SetActiveIndex(cards, selected, "rps-card--selected");
        fightButton?.SetEnabled(selected >= 0 && eligible[selected]);
        if (selected >= 0) list?.ScrollTo(cards[selected]);
    }

    public void Move(float dx)
    {
        if (dx > 0.5f) Select(selected + 1);
        else if (dx < -0.5f) Select(selected - 1);
    }

    public void Submit()
    {
        if (selected >= 0 && eligible[selected]) FightRequested?.Invoke(dnas[selected]);
    }
}
}
