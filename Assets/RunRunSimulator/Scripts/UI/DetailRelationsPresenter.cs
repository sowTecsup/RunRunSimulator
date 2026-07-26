using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class DetailRelationsPresenter
{
    private readonly Func<CreatureRegistrySO> getRegistry;

    private readonly VisualElement good;
    private readonly VisualElement bad;
    private readonly Label empty;

    public DetailRelationsPresenter(VisualElement root, Func<CreatureRegistrySO> getRegistry)
    {
        this.getRegistry = getRegistry;

        good  = root.Q<VisualElement>("relations-good");
        bad   = root.Q<VisualElement>("relations-bad");
        empty = root.Q<Label>("relations-empty");
    }

    public void Rebuild(CreatureDNA dna)
    {
        good?.Clear();
        bad?.Clear();

        var registry = getRegistry();
        var tuning = SocialTuningSO.Current;

        if (dna == null || registry == null || tuning == null)
        {
            if (empty != null) empty.style.display = DisplayStyle.Flex;
            return;
        }

        var friends = new List<(CreatureDNA dna, float aff)>();
        var foes = new List<(CreatureDNA dna, float aff)>();

        foreach (var other in registry.GetAll().Values)
        {
            if (other == null) continue;
            if (other.UniqueID == dna.UniqueID) continue;
            if (other.IsDead) continue;

            float aff = SocialGraphService.EffectiveAffinity(dna, other, tuning);
            if (aff >= tuning.RelationsFriendThreshold) friends.Add((other, aff));
            else if (aff <= tuning.RelationsFoeThreshold) foes.Add((other, aff));
        }

        friends.Sort((a, b) => b.aff.CompareTo(a.aff));
        foes.Sort((a, b) => a.aff.CompareTo(b.aff));

        if (good != null)
            foreach (var (other, _) in friends)
                good.Add(BuildChip(other, isGood: true));

        if (bad != null)
            foreach (var (other, _) in foes)
                bad.Add(BuildChip(other, isGood: false));

        bool any = friends.Count > 0 || foes.Count > 0;
        if (empty != null) empty.style.display = any ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private static VisualElement BuildChip(CreatureDNA dna, bool isGood)
    {
        var chip = new VisualElement();
        chip.AddToClassList("relation-chip");
        chip.AddToClassList(isGood ? "relation-chip--good" : "relation-chip--bad");

        var swatch = new VisualElement();
        swatch.AddToClassList("relation-swatch");
        MonchiPortraitUI.Apply(swatch, dna);
        chip.Add(swatch);

        var name = new Label(ChipName(dna));
        name.AddToClassList("relation-name");
        chip.Add(name);

        var glyph = new Label(isGood ? "❤" : "✖");
        glyph.AddToClassList("relation-glyph");
        glyph.AddToClassList(isGood ? "relation-glyph--good" : "relation-glyph--bad");
        chip.Add(glyph);

        return chip;
    }

    private static string ChipName(CreatureDNA dna)
    {
        if (dna == null) return Loc.Tr("ui.detail.tree.unknown");
        return !string.IsNullOrEmpty(dna.CustomName) ? dna.CustomName : dna.ToStringID();
    }
}
}
