using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class DetailInfoTabPresenter
{
    private readonly CreatureDatabaseSO database;
    private readonly EquipmentDatabaseSO equipmentDatabase;

    private readonly Label statCon, statAtk, statSpd, statDef, statLck, statEva, identityLabel, roleElementLabel, progressionLabel;
    private readonly VisualElement partsContainer;

    public DetailInfoTabPresenter(VisualElement root, CreatureDatabaseSO database, EquipmentDatabaseSO equipmentDatabase)
    {
        this.database = database;
        this.equipmentDatabase = equipmentDatabase;

        statCon          = root.Q<Label>("stat-con");
        statAtk          = root.Q<Label>("stat-atk");
        statSpd          = root.Q<Label>("stat-spd");
        statDef          = root.Q<Label>("stat-def");
        statLck          = root.Q<Label>("stat-lck");
        statEva          = root.Q<Label>("stat-eva");
        identityLabel    = root.Q<Label>("identity");
        roleElementLabel = root.Q<Label>("role-element");
        partsContainer   = root.Q<VisualElement>("parts");
        progressionLabel = root.Q<Label>("progression");
    }

    public void Rebuild(CreatureDNA dna)
    {
        if (dna == null) return;

        var baseEff = database != null
            ? CreatureStats.GetEffectiveStats(dna, database)
            : new EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);
        var eff = EquipmentStats.Apply(baseEff, dna, equipmentDatabase);

        SetStat(statCon, "CON", eff.Constitution, dna.BaseConstitution);
        SetStat(statAtk, "ATK", eff.Attack,       dna.BaseAttack);
        SetStat(statSpd, "SPD", eff.Speed,        dna.BaseSpeed);
        SetStat(statDef, "DEF", eff.Defense,      dna.BaseDefense);
        SetStat(statLck, "LCK", eff.Luck,         dna.BaseLuck);
        SetStat(statEva, "EVA", eff.Evasion,      dna.BaseEvasion);

        if (identityLabel != null)
            identityLabel.text = Loc.Tr("ui.detail.identity", LocEnumMaps.GenderName(dna.Gender), CreatureDisplay.StateOf(dna), Born(dna));

        if (roleElementLabel != null)
            roleElementLabel.text = Loc.Tr("ui.detail.roleline", LocEnumMaps.RoleName(dna.Role), LocEnumMaps.ElementName(dna.Element), RoleDesc(dna.Role));

        BuildParts(dna);

        if (progressionLabel != null)
            progressionLabel.text = Loc.Tr("ui.detail.progression", dna.BreedCount);
    }

    private static void SetStat(Label label, string name, float final, float baseVal)
    {
        if (label == null) return;
        float bonus = final - baseVal;
        label.text = $"{name}  {final:0}   ({baseVal:0} + {bonus:0})";
    }

    private void BuildParts(CreatureDNA dna)
    {
        if (partsContainer == null) return;
        partsContainer.Clear();
        if (database == null) return;

        AddPartRow(PartRole.Body, database.GetBodyShape(dna.BodyShapeID), dna.BodyTier);
        AddPartRow(PartRole.Horn, database.GetHorn(dna.HornID),           dna.HornTier);
        AddPartRow(PartRole.Back, database.GetBack(dna.BackID),           dna.BackTier);
        AddPartRow(PartRole.Wing, database.GetWing(dna.WingID),           dna.WingTier);
        AddPartRow(PartRole.Face, database.GetFace(dna.FaceID),           Tier.Tier1);
    }

    private void AddPartRow(PartRole slot, BodyPart part, Tier tier)
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
            ? Loc.Tr("ui.detail.partrow", SlotName(slot), part.Name, part.Set, LocEnumMaps.RarityName(part.Rarity), (int)tier)
            : Loc.Tr("ui.detail.partrow.empty", SlotName(slot));
        row.Add(text);

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
