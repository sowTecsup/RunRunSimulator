using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class DetailInfoTabPresenter
{
    private readonly CreatureDatabaseSO database;
    private readonly CareGateSO careGate;

    private readonly VisualElement needHealthFill, needEnergyFill, needAffectFill;
    private readonly VisualElement needHealthRow, needEnergyRow, needAffectRow;
    private readonly Label exploreStatus, identityLabel, roleElementLabel, progressionLabel;
    private readonly VisualElement partsContainer;

    public DetailInfoTabPresenter(VisualElement root, CreatureDatabaseSO database, CareGateSO careGate)
    {
        this.database = database;
        this.careGate = careGate;

        needHealthRow    = root.Q<VisualElement>("need-health");
        needEnergyRow    = root.Q<VisualElement>("need-energy");
        needAffectRow    = root.Q<VisualElement>("need-affect");
        needHealthFill   = root.Q<VisualElement>("need-health-fill");
        needEnergyFill   = root.Q<VisualElement>("need-energy-fill");
        needAffectFill   = root.Q<VisualElement>("need-affect-fill");
        exploreStatus    = root.Q<Label>("explore-status");
        identityLabel    = root.Q<Label>("identity");
        roleElementLabel = root.Q<Label>("role-element");
        partsContainer   = root.Q<VisualElement>("parts");
        progressionLabel = root.Q<Label>("progression");
    }

    public void Rebuild(CreatureDNA dna)
    {
        if (dna == null) return;

        SetNeedBar(needHealthFill, NeedType.Health, dna.Needs.Health);
        SetNeedBar(needEnergyFill, NeedType.Energy, dna.Needs.Energy);
        SetNeedBar(needAffectFill, NeedType.Affect, dna.Needs.Affect);

        bool canExplore = careGate != null && CreatureAvailability.CanExplore(dna, careGate);
        NeedType? weakest = careGate != null ? CreatureAvailability.WeakestNeed(dna, careGate) : null;

        SetNeedHighlight(needHealthRow, weakest == NeedType.Health);
        SetNeedHighlight(needEnergyRow, weakest == NeedType.Energy);
        SetNeedHighlight(needAffectRow, weakest == NeedType.Affect);

        if (exploreStatus != null)
        {
            exploreStatus.text = canExplore ? Loc.Tr("ui.detail.explore.ready") : Loc.Tr("ui.detail.explore.blocked");
            exploreStatus.EnableInClassList("explore-status--ready", canExplore);
            exploreStatus.EnableInClassList("explore-status--blocked", !canExplore);
        }

        if (identityLabel != null)
            identityLabel.text = Loc.Tr("ui.detail.identity", LocEnumMaps.GenderName(dna.Gender), CreatureDisplay.StateOf(dna), Born(dna));

        if (roleElementLabel != null)
            roleElementLabel.text = Loc.Tr("ui.detail.roleline", LocEnumMaps.RoleName(dna.Role), LocEnumMaps.ElementName(dna.Element), RoleDesc(dna.Role));

        BuildParts(dna);

        if (progressionLabel != null)
            progressionLabel.text = Loc.Tr("ui.detail.progression", dna.BreedCount);
    }

    private static void SetNeedBar(VisualElement fill, NeedType need, float value)
    {
        if (fill == null) return;
        fill.style.width = Length.Percent(NeedsDisplay.Fill01(need, value) * 100f);
        fill.RemoveFromClassList("exp-bar--good");
        fill.RemoveFromClassList("exp-bar--warn");
        fill.RemoveFromClassList("exp-bar--crit");
        fill.AddToClassList(NeedsDisplay.ColorClass(need, value));
    }

    private static void SetNeedHighlight(VisualElement row, bool highlight) =>
        row?.EnableInClassList("detail-need--blocked", highlight);

    private void BuildParts(CreatureDNA dna)
    {
        if (partsContainer == null) return;
        partsContainer.Clear();
        if (database == null) return;

        AddPartRow(PartRole.Body, database.GetBodyShape(dna.BodyShapeID));
        AddEvolvablePartRow(PartRole.Horn, database.GetHorn(dna.HornID), dna.HornTier, dna.HornPotential);
        AddEvolvablePartRow(PartRole.Back, database.GetBack(dna.BackID), dna.BackTier, dna.BackPotential);
        AddEvolvablePartRow(PartRole.Wing, database.GetWing(dna.WingID), dna.WingTier, dna.WingPotential);
        AddPartRow(PartRole.Face, database.GetFace(dna.FaceID));
    }

    private void AddPartRow(PartRole slot, BodyPart part)
    {
        var row = new VisualElement();
        row.AddToClassList("part-row");

        var swatch = new VisualElement();
        swatch.AddToClassList("part-swatch");
        swatch.style.backgroundColor = part != null ? BodyPart.SetColor(part.Set) : Color.gray;
        row.Add(swatch);

        var text = new Label();
        text.AddToClassList("part-text");
        text.text = part != null
            ? Loc.Tr("ui.detail.partrow", SlotName(slot), part.Name, part.Set, LocEnumMaps.RarityName(part.Rarity))
            : Loc.Tr("ui.detail.partrow.empty", SlotName(slot));
        row.Add(text);

        partsContainer.Add(row);
    }

    private void AddEvolvablePartRow(PartRole slot, BodyPart part, Tier tier, int potential)
    {
        var row = new VisualElement();
        row.AddToClassList("part-row");

        var swatch = new VisualElement();
        swatch.AddToClassList("part-swatch");
        swatch.style.backgroundColor = part != null ? BodyPart.SetColor(part.Set) : Color.gray;
        row.Add(swatch);

        var text = new Label();
        text.AddToClassList("part-text");
        text.text = part != null
            ? Loc.Tr("ui.detail.partrow.level", SlotName(slot), part.Name, part.Set, LocEnumMaps.RarityName(part.Rarity), (int)tier, potential)
            : Loc.Tr("ui.detail.partrow.level.empty", SlotName(slot), (int)tier, potential);
        row.Add(text);

        var action = new VisualElement();
        action.AddToClassList("part-action");
        row.Add(action);

        partsContainer.Add(row);
    }

    private static string SlotName(PartRole r) => r switch
    {
        PartRole.Body => Loc.Tr("ui.detail.slot.body"),
        PartRole.Horn => Loc.Tr("ui.detail.slot.horn"),
        PartRole.Back => Loc.Tr("ui.detail.slot.back"),
        PartRole.Wing => Loc.Tr("ui.detail.slot.wing"),
        PartRole.Face => Loc.Tr("ui.detail.slot.face"),
        _             => r.ToString(),
    };

    private static string RoleDesc(Role r) => r switch
    {
        Role.Protector => Loc.Tr("ui.detail.roledesc.protector"),
        Role.Agresivo  => Loc.Tr("ui.detail.roledesc.agresivo"),
        Role.Empatico  => Loc.Tr("ui.detail.roledesc.empatico"),
        _              => "",
    };

    private static string Born(CreatureDNA d) =>
        d.BirthDate == default ? "—" : d.BirthDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}
}
