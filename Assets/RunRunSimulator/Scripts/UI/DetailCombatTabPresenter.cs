using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class DetailCombatTabPresenter
{
    private readonly Func<CreatureRegistrySO> getRegistry;

    private readonly ScrollView combatHistory;
    private readonly Label combatEmpty;

    public DetailCombatTabPresenter(VisualElement root, Func<CreatureRegistrySO> getRegistry)
    {
        this.getRegistry = getRegistry;

        combatHistory = root.Q<ScrollView>("combat-history");
        combatEmpty   = root.Q<Label>("combat-empty");
    }

    public void Rebuild(CreatureDNA dna)
    {
        if (dna == null) return;
        if (combatHistory == null) return;
        combatHistory.Clear();

        int count = dna.CombatHistory?.Count ?? 0;
        if (combatEmpty != null) combatEmpty.style.display = count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        if (count == 0) return;

        for (int i = count - 1; i >= 0; i--)   // newest first
            combatHistory.Add(BuildCombatCard(dna, dna.CombatHistory[i]));
    }

    private VisualElement BuildCombatCard(CreatureDNA dna, CombatRecord rec)
    {
        bool won  = rec.Outcome == CombatOutcome.Won;
        bool draw = rec.Outcome == CombatOutcome.Draw;

        var card = new VisualElement();
        card.AddToClassList("combat-card");
        card.AddToClassList(draw ? "combat-card--draw" : won ? "combat-card--win" : "combat-card--lose");

        var header = new VisualElement();
        header.AddToClassList("combat-card__header");

        var badge = new Label(BadgeText(rec.Outcome));
        badge.AddToClassList("combat-card__badge");
        badge.AddToClassList(draw ? "combat-card__badge--draw" : won ? "combat-card__badge--win" : "combat-card__badge--lose");
        header.Add(badge);

        var date = new Label($"{rec.Date.ToLocalTime():dd/MM/yyyy HH:mm}");
        date.AddToClassList("combat-card__date");
        header.Add(date);

        card.Add(header);

        string owner = string.IsNullOrEmpty(rec.OpponentPlayerName) ? " · local" : $" · de {rec.OpponentPlayerName}";
        var opponent = new Label($"vs {rec.OpponentName}{owner}");
        opponent.AddToClassList("combat-card__opponent");
        card.Add(opponent);

        if (rec.SelfStats != null && rec.OpponentStats != null)
        {
            var body = new VisualElement();
            body.AddToClassList("combat-card__body");
            body.Add(BuildCombatColumn("Tú", rec.SelfStats, dna.BaseColor));
            body.Add(BuildCombatColumn("Rival", rec.OpponentStats, new Color(0.22f, 0.22f, 0.28f)));
            card.Add(body);
        }
        else
        {
            var noStats = new Label("Combate antiguo — sin stats registradas");
            noStats.AddToClassList("combat-card__nostats");
            card.Add(noStats);
        }

        var comment = new Label(CommentText(rec));
        comment.AddToClassList("combat-card__comment");
        card.Add(comment);

        var footer = new VisualElement();
        footer.AddToClassList("combat-card__footer");

        var play = new Button(() => CombatReplayRequest.Request(dna, rec)) { text = "▶" };
        play.AddToClassList("combat-card__play");
        play.SetEnabled(CombatReplayRequest.CanReplay(dna, rec, getRegistry()));
        footer.Add(play);

        card.Add(footer);

        return card;
    }

    private static VisualElement BuildCombatColumn(string title, CombatFighterSnapshot s, Color fallbackColor)
    {
        var col = new VisualElement();
        col.AddToClassList("combat-card__colstats");

        var head = new VisualElement();
        head.AddToClassList("combat-card__colhead");

        var swatch = new VisualElement();
        swatch.AddToClassList("combat-card__swatch");
        swatch.style.backgroundColor = SnapshotColor(s, fallbackColor);
        head.Add(swatch);

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("combat-card__col-title");
        head.Add(titleLabel);

        col.Add(head);

        var line1 = new Label($"HP {s.MaxHp:0} · ATK {s.Attack:0} · SPD {s.Speed:0}");
        line1.AddToClassList("combat-card__stat");
        col.Add(line1);

        var line2 = new Label($"DEF {s.Defense:0} · LCK {s.Luck:0} · EVA {s.Evasion:0}");
        line2.AddToClassList("combat-card__stat");
        col.Add(line2);

        AddTierChips(col, s);

        return col;
    }

    private static Color SnapshotColor(CombatFighterSnapshot s, Color fallback) =>
        !string.IsNullOrEmpty(s.ColorHex) && ColorUtility.TryParseHtmlString("#" + s.ColorHex, out var c)
            ? c
            : fallback;

    private static void AddTierChips(VisualElement col, CombatFighterSnapshot s)
    {
        var chips = new VisualElement();
        chips.AddToClassList("combat-card__chips");

        AddTierChip(chips, "Cuerpo", s.BodyTier);
        AddTierChip(chips, "Brazo",  s.ArmTier);
        AddTierChip(chips, "Ojo",    s.EyeTier);
        AddTierChip(chips, "Boca",   s.MouthTier);

        if (chips.childCount > 0) col.Add(chips);
    }

    private static void AddTierChip(VisualElement chips, string partEs, int tier)
    {
        if (tier <= 1) return;
        var chip = new Label($"{partEs} T{tier}");
        chip.AddToClassList("combat-card__tierchip");
        chips.Add(chip);
    }

    private static string CommentText(CombatRecord rec)
    {
        if (rec.Outcome == CombatOutcome.Won)
            return string.IsNullOrEmpty(rec.EvolvedSlot) ? "Victoria — sin mejora" : $"Se mejoró {PartEs(rec.EvolvedSlot)}";
        if (rec.Outcome == CombatOutcome.Lost)
            return rec.Died ? "Murió en combate" : "Regresó derrotado";
        return "Empate — sin consecuencias";
    }

    private static string PartEs(string slot) => slot switch
    {
        "Body"  => "Cuerpo",
        "Arm"   => "Brazo",
        "Eye"   => "Ojo",
        "Mouth" => "Boca",
        _       => slot,
    };

    private static string BadgeText(CombatOutcome o) => o switch
    {
        CombatOutcome.Won  => "Victoria",
        CombatOutcome.Lost => "Derrota",
        _                  => "Empate",
    };
}
}
