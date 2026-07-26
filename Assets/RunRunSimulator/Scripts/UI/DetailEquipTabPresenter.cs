using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class DetailEquipTabPresenter
{
    private readonly CreatureDatabaseSO database;
    private readonly EquipmentDatabaseSO equipmentDatabase;
    private readonly EquipmentPaletteSO equipmentPalette;
    private readonly EquipmentBackpackUITK backpack;
    private readonly Func<CreatureRegistrySO> getRegistry;

    private readonly VisualElement teamPortrait, equipStats;
    private readonly ScrollView equipCards;

    public DetailEquipTabPresenter(VisualElement root, CreatureDatabaseSO database,
        EquipmentDatabaseSO equipmentDatabase, EquipmentPaletteSO equipmentPalette,
        EquipmentBackpackUITK backpack, Func<CreatureRegistrySO> getRegistry)
    {
        this.database = database;
        this.equipmentDatabase = equipmentDatabase;
        this.equipmentPalette = equipmentPalette;
        this.backpack = backpack;
        this.getRegistry = getRegistry;

        teamPortrait = root.Q<VisualElement>("equip-portrait");
        equipCards   = root.Q<ScrollView>("equip-cards");
        equipStats   = root.Q<VisualElement>("equip-stats");
    }

    public void Rebuild(CreatureDNA dna)
    {
        if (dna == null) return;

        if (teamPortrait != null) MonchiPortraitUI.Apply(teamPortrait, dna);
        BuildEquipCards(dna);
        BuildEquipStats(dna);
    }

    private void BuildEquipCards(CreatureDNA dna)
    {
        if (equipCards == null) return;
        equipCards.Clear();
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            AddEquipCard(dna, slot);
    }

    private void AddEquipCard(CreatureDNA dna, EquipmentSlot slot)
    {
        var item = ResolveEquip(dna, slot);

        var card = new VisualElement();
        card.AddToClassList("equip-card");
        if (item == null) card.AddToClassList("equip-card--empty");
        card.style.borderLeftColor = SlotColor(slot);

        // Diagonal accent (behind content) in the rarity color — added first so the
        // icon/info draw on top. Empty slots get no accent.
        if (item != null)
        {
            var diag = new VisualElement();
            diag.AddToClassList("equip-card__diag");
            diag.pickingMode = PickingMode.Ignore;
            var dc = RarityColor(item.Rarity);
            dc.a = 0.5f;
            diag.generateVisualContent += ctx => PaintDiagonal(ctx, dc);
            card.Add(diag);
        }

        var icon = new VisualElement();
        icon.AddToClassList("equip-card__icon");
        if (item != null && item.Icon != null)
            icon.style.backgroundImage = new StyleBackground(Background.FromSprite(item.Icon));
        else if (item != null)
            icon.style.backgroundColor = item.IconColor;
        card.Add(icon);

        var info = new VisualElement();
        info.AddToClassList("equip-card__info");

        var name = new Label(item != null
            ? (string.IsNullOrEmpty(item.Name) ? item.ID : item.Name)
            : Loc.Tr("ui.equip.slot_empty", LocEnumMaps.EquipmentSlotName(slot)));
        name.AddToClassList("equip-card__name");
        if (item != null) name.style.color = RarityColor(item.Rarity);
        info.Add(name);

        var meta = new Label(item != null ? Loc.Tr("ui.equip.slot_rarity", LocEnumMaps.EquipmentSlotName(item.Slot), LocEnumMaps.RarityName(item.Rarity)) : Loc.Tr("ui.equip.empty"));
        meta.AddToClassList("equip-card__meta");
        info.Add(meta);

        if (item != null && !string.IsNullOrEmpty(item.Description))
        {
            var desc = new Label(item.Description);
            desc.AddToClassList("equip-card__desc");
            info.Add(desc);
        }

        if (item != null)
        {
            var effText = EffectsText(item);
            if (!string.IsNullOrEmpty(effText))
            {
                var eff = new Label(effText);
                eff.AddToClassList("equip-card__effects");
                info.Add(eff);
            }
        }

        card.Add(info);

        if (item != null)
        {
            var modsText = ModifiersText(item);
            if (!string.IsNullOrEmpty(modsText))
            {
                var procs = new VisualElement();
                procs.AddToClassList("equip-card__procs");
                procs.pickingMode = PickingMode.Ignore;

                var mods = new Label(modsText);
                mods.AddToClassList("equip-card__mods");
                procs.Add(mods);

                card.Add(procs);
            }
        }

        card.RegisterCallback<ClickEvent>(_ =>
        {
            if (backpack != null) backpack.Open(dna, slot, card, getRegistry());
        });

        equipCards.Add(card);
    }

    private void BuildEquipStats(CreatureDNA dna)
    {
        if (equipStats == null) return;
        equipStats.Clear();

        var baseEff = database != null
            ? CombatStats.GetEffectiveStats(dna, database)
            : new EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);
        var finalEff = EquipmentStats.Apply(baseEff, dna, equipmentDatabase);

        AddStatRow(LocEnumMaps.StatAbbrev(StatType.Constitution), baseEff.Constitution, finalEff.Constitution);
        AddStatRow(LocEnumMaps.StatAbbrev(StatType.Attack),       baseEff.Attack,       finalEff.Attack);
        AddStatRow(LocEnumMaps.StatAbbrev(StatType.Speed),        baseEff.Speed,        finalEff.Speed);
        AddStatRow(LocEnumMaps.StatAbbrev(StatType.Defense),      baseEff.Defense,      finalEff.Defense);
        AddStatRow(LocEnumMaps.StatAbbrev(StatType.Luck),         baseEff.Luck,         finalEff.Luck);
        AddStatRow(LocEnumMaps.StatAbbrev(StatType.Evasion),      baseEff.Evasion,      finalEff.Evasion);
    }

    private void AddStatRow(string name, float baseVal, float finalVal)
    {
        var row = new VisualElement();
        row.AddToClassList("equip-stat");

        var n = new Label(name);
        n.AddToClassList("equip-stat__name");
        row.Add(n);

        float d = finalVal - baseVal;
        var v = new Label(Mathf.Approximately(d, 0f) ? $"{finalVal:0.#}" : $"{baseVal:0.#} → {finalVal:0.#}");
        v.AddToClassList("equip-stat__val");
        if (d > 0f)      v.AddToClassList("equip-stat__val--up");
        else if (d < 0f) v.AddToClassList("equip-stat__val--down");
        row.Add(v);

        equipStats.Add(row);
    }

    private EquipmentSO ResolveEquip(CreatureDNA dna, EquipmentSlot slot)
    {
        if (equipmentDatabase == null || dna.Equipped == null) return null;
        return dna.Equipped.TryGetValue(slot, out var id) ? equipmentDatabase.GetByID(id) : null;
    }

    // Draws the right-side diagonal wedge filled with the rarity color. Slants 45°
    // (bottom reaches further left), leaving the left side for the icon/text.
    private static void PaintDiagonal(MeshGenerationContext ctx, Color color)
    {
        var rect = ctx.visualElement.contentRect;
        float w = rect.width, h = rect.height;
        if (w <= 0f || h <= 0f) return;

        // Diagonal crosses the card's center → splits it into two equal halves.
        float topX = w * 0.5f - h * 0.5f;
        float botX = topX + h;

        var p = ctx.painter2D;
        p.fillColor = color;
        p.BeginPath();
        p.MoveTo(new Vector2(w - topX, 0f));
        p.LineTo(new Vector2(w, 0f));
        p.LineTo(new Vector2(w, h));
        p.LineTo(new Vector2(w - botX, h));
        p.ClosePath();
        p.Fill();
    }

    private Color RarityColor(Rarity r) =>
        equipmentPalette != null ? equipmentPalette.RarityColor(r) : BodyPart.RarityColor(r);

    private Color SlotColor(EquipmentSlot s) =>
        equipmentPalette != null ? equipmentPalette.SlotColor(s) : new Color(0.35f, 0.35f, 0.43f);

    private static string EffectsText(EquipmentSO item)
    {
        if (item.Effects == null) return null;
        var sb = new System.Text.StringBuilder();
        foreach (var e in item.Effects)
        {
            if (!(e is StatModifierEffect)) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("• ").Append(e.Summary());
        }
        return sb.Length == 0 ? null : sb.ToString();
    }

    private static string ModifiersText(EquipmentSO item)
    {
        if (item.Effects == null) return null;
        var sb = new System.Text.StringBuilder();
        foreach (var e in item.Effects)
        {
            if (!(e is ItemUseEffect use)) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("◆ ").Append(use.Summary());
        }
        return sb.Length == 0 ? null : sb.ToString();
    }
}
}
