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

    // ── Rebuild ──────────────────────────────────────────────────

    public void Rebuild(CreatureDNA dna)
    {
        if (dna == null) return;

        // Final stat with its (base + bonus-from-parts/tier/equipment) breakdown.
        var baseEff = database != null
            ? CombatStats.GetEffectiveStats(dna, database)
            : new EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);
        var eff = EquipmentStats.Apply(baseEff, dna, equipmentDatabase);

        SetStat(statCon, "CON", eff.Constitution, dna.BaseConstitution);
        SetStat(statAtk, "ATK", eff.Attack,       dna.BaseAttack);
        SetStat(statSpd, "SPD", eff.Speed,        dna.BaseSpeed);
        SetStat(statDef, "DEF", eff.Defense,      dna.BaseDefense);
        SetStat(statLck, "LCK", eff.Luck,         dna.BaseLuck);
        SetStat(statEva, "EVA", eff.Evasion,      dna.BaseEvasion);

        if (identityLabel != null)
            identityLabel.text = $"Género: {dna.Gender}\nEstado: {StateOf(dna)}\nNacimiento: {Born(dna)}";

        if (roleElementLabel != null)
            roleElementLabel.text = $"{RoleName(dna.Role)} · {ElementName(dna.Element)}\n{RoleDesc(dna.Role)}";

        BuildParts(dna);

        if (progressionLabel != null)
            progressionLabel.text = $"Combates: {dna.FightCount} ({dna.WinCount} victorias)\nCrías: {dna.BreedCount}";
    }

    // ── Parts ────────────────────────────────────────────────────

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

        AddPartRow("Cuerpo", database.GetBodyShape(dna.BodyShapeID), dna.BodyTier);
        AddPartRow("Brazos", database.GetArm(dna.ArmID),             dna.ArmTier);
        AddPartRow("Ojos",   database.GetEye(dna.EyeID),             dna.EyeTier);
        AddPartRow("Boca",   database.GetMouth(dna.MouthID),         dna.MouthTier);
    }

    private void AddPartRow(string slot, BodyPart part, Tier tier)
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
            ? $"{slot}: {part.Name}  ·  {part.Set} · {part.Rarity} · Tier{(int)tier}"
            : $"{slot}: —";
        row.Add(text);

        partsContainer.Add(row);
    }

    // ── Role / Element ───────────────────────────────────────────

    private static string RoleName(Role r) => r switch
    {
        Role.Protector => "Protector",
        Role.Agresivo  => "Agresivo",
        Role.Empatico  => "Empático",
        _              => r.ToString(),
    };

    private static string RoleDesc(Role r) => r switch
    {
        Role.Protector => "Guardián calmo; escuda a sus aliados y vive tranquilo cerca del almacén.",
        Role.Agresivo  => "Territorial; caza la retaguardia enemiga y vive en el bullicio del mostrador.",
        Role.Empatico  => "Sociable; cura a sus aliados y sigue al jugador por el mostrador.",
        _              => "",
    };

    private static string ElementName(Element e) => e switch
    {
        Element.Agua         => "Agua",
        Element.Fuego        => "Fuego",
        Element.Electricidad => "Electricidad",
        Element.Planta       => "Planta",
        _                    => e.ToString(),
    };

    private static string StateOf(CreatureDNA d) =>
        d.IsSold                                  ? "SOLD"     :
        d.IsDead                                  ? "DEAD"     :
        d.BusyState == BusyReason.Breeding        ? "Breeding" :
        d.BusyState == BusyReason.QueuedForCombat ? "In Queue" :
        "Free";

    private static string Born(CreatureDNA d) =>
        d.BirthDate == default ? "—" : d.BirthDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}
}
