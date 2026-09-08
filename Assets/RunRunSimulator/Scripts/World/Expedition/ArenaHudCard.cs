using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator
{
public class ArenaHudCard
{
    private static readonly OrderPillar[] PillarOrder = { OrderPillar.Loot, OrderPillar.Contact, OrderPillar.Posture };
    private static readonly string[] PillarChipClass = { "hud-chip--loot", "hud-chip--contact", "hud-chip--team" };

    public MoriMochiAgent Agent { get; }
    public VisualElement Root { get; }

    private readonly VisualElement carry;
    private readonly Label action;
    private string lastAction;
    private readonly List<VisualElement> carrySlots = new();
    private VisualElement carryFill;
    private int lastCapacity = -1;
    private int lastCarried = -1;
    private bool lastMining;
    private float lastMiningProgress = -1f;
    private readonly RadialSlot[] radials = new RadialSlot[3];
    private readonly VisualElement[] powers = new VisualElement[3];
    private readonly float[] lastFired = { -1f, -1f, -1f };
    private readonly bool[] lastDim = new bool[3];
    private int lastKnocked;
    private float hitUntil;
    private bool lastSelected;
    private bool lastChase;

    public ArenaHudCard(MoriMochiAgent agent, Action<MoriMochiAgent> onTapped)
    {
        Agent = agent;

        var root2 = new VisualElement();
        root2.pickingMode = PickingMode.Position;
        root2.AddToClassList("hud-card");
        Color color = agent.DNA.BaseColor;
        color.a = 1f;
        root2.style.borderTopColor = color;
        Root = root2;
        Root.RegisterCallback<ClickEvent>(_ => onTapped?.Invoke(Agent));

        var header = new VisualElement();
        header.pickingMode = PickingMode.Ignore;
        header.AddToClassList("hud-card__header");

        var swatch = new VisualElement();
        swatch.pickingMode = PickingMode.Ignore;
        swatch.AddToClassList("hud-card__swatch");
        swatch.style.backgroundColor = color;
        header.Add(swatch);

        var text = new VisualElement();
        text.pickingMode = PickingMode.Ignore;
        text.AddToClassList("hud-card__text");

        var name = new Label(agent.DNA.CustomName);
        name.pickingMode = PickingMode.Ignore;
        name.AddToClassList("hud-card__name");
        text.Add(name);

        var posture = new Label(ArenaOrderCatalog.ArchetypeShort(agent.Orders));
        posture.pickingMode = PickingMode.Ignore;
        posture.AddToClassList("hud-card__posture");
        text.Add(posture);

        header.Add(text);
        root2.Add(header);

        var pillars = new VisualElement();
        pillars.pickingMode = PickingMode.Ignore;
        pillars.AddToClassList("hud-card__pillars");
        for (int i = 0; i < PillarOrder.Length; i++)
        {
            var pillar = PillarOrder[i];
            var chipLabel = new Label(ArenaOrderCatalog.ChoiceLabel(pillar, ArenaOrderRules.Choice(agent.Orders, pillar)));
            chipLabel.pickingMode = PickingMode.Ignore;
            chipLabel.AddToClassList("hud-chip");
            chipLabel.AddToClassList(PillarChipClass[i]);
            pillars.Add(chipLabel);
        }
        root2.Add(pillars);

        var actionLabel = new Label();
        actionLabel.pickingMode = PickingMode.Ignore;
        actionLabel.AddToClassList("hud-card__action");
        root2.Add(actionLabel);
        action = actionLabel;

        var carryElement = new VisualElement();
        carryElement.pickingMode = PickingMode.Ignore;
        carryElement.AddToClassList("hud-card__carry");
        root2.Add(carryElement);
        carry = carryElement;

        var powersRow = new VisualElement();
        powersRow.pickingMode = PickingMode.Ignore;
        powersRow.AddToClassList("hud-card__powers");

        for (int i = 0; i < 3; i++)
        {
            var power = new VisualElement();
            power.pickingMode = PickingMode.Ignore;
            power.AddToClassList("hud-power");

            var radial = new RadialSlot();
            radial.AddToClassList("hud-power__radial");

            var ability = agent.Ability(i);
            var label = new Label(ability != null ? ability.Name : "—");
            label.pickingMode = PickingMode.Ignore;
            label.AddToClassList("hud-power__label");

            radial.FillColor = ability != null ? ability.Color : Color.gray;
            if (ability == null) power.AddToClassList("hud-power--empty");

            power.Add(radial);
            power.Add(label);
            powersRow.Add(power);

            radials[i] = radial;
            powers[i] = power;
        }

        root2.Add(powersRow);

        lastKnocked = agent.ClashTimesKnocked;
    }

    public void Refresh(bool selected)
    {
        var agent = Agent;

        string actionText = LocEnumMaps.IntentName(agent.Intent) + (agent.TrustedGuardian != null ? " · custodiado" : "");
        if (actionText != lastAction)
        {
            action.text = actionText;
            lastAction = actionText;
        }

        RefreshCarry();
        RefreshPowers();

        int knocked = agent.ClashTimesKnocked;
        if (knocked > lastKnocked)
        {
            lastKnocked = knocked;
            Root.AddToClassList("hud-card--hit");
            hitUntil = Time.time + 0.6f;
        }
        if (hitUntil > 0f && Time.time >= hitUntil)
        {
            Root.RemoveFromClassList("hud-card--hit");
            hitUntil = 0f;
        }

        if (selected != lastSelected)
        {
            Root.EnableInClassList("hud-card--selected", selected);
            lastSelected = selected;
        }

        bool chase = agent.IsChasing;
        if (chase != lastChase)
        {
            Root.EnableInClassList("hud-card--chase", chase);
            lastChase = chase;
        }
    }

    private void RefreshCarry()
    {
        var agent = Agent;
        int capacity = agent.CarryCapacity;
        if (capacity != lastCapacity)
        {
            lastCapacity = capacity;
            carrySlots.Clear();
            carry.Clear();
            for (int i = 0; i < capacity; i++)
            {
                var slot = new VisualElement();
                slot.pickingMode = PickingMode.Ignore;
                slot.AddToClassList("hud-slot");
                carry.Add(slot);
                carrySlots.Add(slot);
            }
            lastCarried = -1;
            lastMining = false;
            lastMiningProgress = -1f;
        }

        int carried = agent.Carried;
        bool mining = agent.Intent == CreatureIntent.Taking && carried < capacity;
        if (carried != lastCarried || mining != lastMining)
        {
            for (int i = 0; i < carrySlots.Count; i++)
            {
                carrySlots[i].EnableInClassList("hud-slot--full", i < carried);
                if (carryFill != null && carryFill.parent == carrySlots[i]) carrySlots[i].Remove(carryFill);
            }

            if (mining)
            {
                carryFill ??= new VisualElement { pickingMode = PickingMode.Ignore };
                carryFill.AddToClassList("hud-slot__fill");
                carrySlots[carried].Add(carryFill);
            }

            lastCarried = carried;
            lastMining = mining;
            lastMiningProgress = -1f;
        }

        if (mining)
        {
            float progress = agent.MiningProgress;
            if (Mathf.Abs(progress - lastMiningProgress) > 0.01f)
            {
                carryFill.style.width = Length.Percent(progress * 100f);
                lastMiningProgress = progress;
            }
        }
    }

    private void RefreshPowers()
    {
        var agent = Agent;
        bool fleeing = agent.Orders.Contact == ContactChoice.Flee;
        for (int i = 0; i < 3; i++)
        {
            var radial = radials[i];
            radial.Charge01 = agent.AbilityCharge01(i);

            float fired = agent.AbilityFiredAt(i);
            if (fired > lastFired[i])
            {
                lastFired[i] = fired;
                radial.Pulse();
            }

            var ability = agent.Ability(i);
            bool dim = ability != null && ability.Kind == AbilityKind.Damage && fleeing;
            if (dim != lastDim[i])
            {
                powers[i].EnableInClassList("hud-power--dim", dim);
                lastDim[i] = dim;
            }
        }
    }
}
}
