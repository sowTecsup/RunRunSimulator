using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    // ── Private Fields ────────────────────────────────────────────

    [Required, AssetsOnly]
    [Title("RunRunSimulator — Genetics Lab", "Assign all assets below to begin.", TitleAlignments.Centered)]
    [BoxGroup("Setup")]
    [FormerlySerializedAs("_database")]
    [SerializeField] private CreatureDatabaseSO database;

    [AssetsOnly, BoxGroup("Setup")]
    [FormerlySerializedAs("_rarityOddsTable")]
    [SerializeField] private RarityOddsTableSO rarityOddsTable;

    [AssetsOnly, BoxGroup("Setup")]
    [FormerlySerializedAs("_inheritanceOddsTable")]
    [SerializeField] private InheritanceOddsTableSO inheritanceOddsTable;

    [Required, AssetsOnly, BoxGroup("Setup")]
    [FormerlySerializedAs("_creatureRegistry")]
    [SerializeField] private CreatureRegistrySO creatureRegistry;

    [BoxGroup("Setup")]
    [SerializeField] private CombatController combatController;

    [BoxGroup("Current Creature")]
    [FormerlySerializedAs("_currentDNA")]
    [SerializeField, ReadOnly, InlineProperty, HideLabel]
    private CreatureDNA currentDNA = new CreatureDNA();

    [BoxGroup("Current Creature")]
    [FormerlySerializedAs("_currentDNAString")]
    [SerializeField, ReadOnly, LabelText("DNA String")]
    private string currentDNAString = "---";

    [ShowInInspector, ReadOnly, LabelText("Last Minted ID")]
    [BoxGroup("Mint")]
    private string lastMintedID = "---";

    [BoxGroup("Breed")]
    [FormerlySerializedAs("_breedMotherID")]
    [SerializeField, LabelText("Mother ID")]
    private string breedMotherID = "";

    [BoxGroup("Breed")]
    [FormerlySerializedAs("_breedFatherID")]
    [SerializeField, LabelText("Father ID")]
    private string breedFatherID = "";

    [ShowInInspector, ReadOnly, LabelText("Mother Info"), BoxGroup("Breed")]
    private string motherBreedInfo = "---";

    [ShowInInspector, ReadOnly, LabelText("Father Info"), BoxGroup("Breed")]
    private string fatherBreedInfo = "---";

    [ShowInInspector, ReadOnly, LabelText("Last Child ID"), BoxGroup("Breed")]
    private string lastChildID = "---";

    [Title("Rarity Breakdown")]
    [ShowInInspector, ReadOnly, LabelText("Body Shape"), LabelWidth(80)]
    [BoxGroup("Current Creature/Rarity")]
    private Rarity rarityBodyShape;

    [ShowInInspector, ReadOnly, LabelText("Arms"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private Rarity rarityArms;

    [ShowInInspector, ReadOnly, LabelText("Eyes"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private Rarity rarityEyes;

    [ShowInInspector, ReadOnly, LabelText("Mouth"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private Rarity rarityMouth;

    [ShowInInspector, ReadOnly, LabelText("Score"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private string rarityScore = "---";

    [BoxGroup("Load by ID")]
    [InfoBox("Format: BODYSHAPEID-ARMID-EYEID-MOUTHID-RRGGBB   (e.g.  BS0-A3-E1-M2-FF00AA)")]
    [FormerlySerializedAs("_loadIDInput")]
    [SerializeField, LabelText("DNA String")]
    private string loadIDInput = "";

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()           => SaveSystem.LoadInto(creatureRegistry);
    private void OnApplicationQuit() => SaveSystem.SaveDatabase(creatureRegistry);

    // ── Private Methods ───────────────────────────────────────────

    [Button("Generate Random Creature", ButtonSizes.Large), GUIColor(0.4f, 0.85f, 0.4f)]
    [BoxGroup("Current Creature")]
    private void GenerateRandomCreature()
    {
        currentDNA       = CreatureGenerator.GenerateRandom(database, rarityOddsTable);
        currentDNAString = currentDNA.ToStringID();
        RefreshRarityBreakdown();
        Debug.Log($"[GameManager] Generated (preview): {currentDNAString}");
    }

    [Button("Fill Random Breeders"), GUIColor(0.85f, 0.6f, 1f), BoxGroup("Breed")]
    private void FillRandomBreeders()
    {
        var all     = creatureRegistry.GetAll().Values.ToList();
        var females = all.Where(d => !d.IsDead && d.Gender == CreatureGender.Female && d.BreedCount < BreedingService.MaxBreedCount).ToList();
        var males   = all.Where(d => !d.IsDead && d.Gender == CreatureGender.Male   && d.BreedCount < BreedingService.MaxBreedCount).ToList();

        if (females.Count == 0 || males.Count == 0)
        {
            Debug.LogError("[GameManager] Not enough valid breeders — need at least one alive Male and one alive Female under the breed limit.");
            return;
        }

        var mother = females[Random.Range(0, females.Count)];
        var father = males[Random.Range(0, males.Count)];
        breedMotherID = mother.UniqueID;
        breedFatherID = father.UniqueID;
        RefreshBreedInfo();
        Debug.Log($"[GameManager] Random breeders — Mother: {Clip(mother.UniqueID)} | Father: {Clip(father.UniqueID)}");
    }

    [Button("Breed"), GUIColor(1f, 0.7f, 0.85f), BoxGroup("Breed")]
    private void BreedButton() => BreedCreatures(breedMotherID, breedFatherID);

    [Button("Load from ID"), GUIColor(0.4f, 0.6f, 0.95f), BoxGroup("Load by ID")]
    private void LoadFromID()
    {
        if (string.IsNullOrWhiteSpace(loadIDInput)) { Debug.LogWarning("[GameManager] No ID entered."); return; }

        currentDNA       = CreatureDNA.FromID(loadIDInput);
        currentDNAString = currentDNA.ToStringID();
        RefreshRarityBreakdown();
        Debug.Log($"[GameManager] Loaded: {currentDNAString}");
        ValidateDNA(currentDNA);
    }

    private void RefreshRarityBreakdown()
    {
        if (database == null) return;

        var bodyShape = database.GetBodyShape(currentDNA.BodyShapeID);
        var arm       = database.GetArm(currentDNA.ArmID);
        var eye       = database.GetEye(currentDNA.EyeID);
        var mouth     = database.GetMouth(currentDNA.MouthID);

        rarityBodyShape = bodyShape?.Rarity ?? Rarity.Common;
        rarityArms      = arm?.Rarity       ?? Rarity.Common;
        rarityEyes      = eye?.Rarity       ?? Rarity.Common;
        rarityMouth     = mouth?.Rarity     ?? Rarity.Common;

        float avg   = ((int)rarityBodyShape + (int)rarityArms + (int)rarityEyes + (int)rarityMouth) / 4f;
        rarityScore = $"{(Rarity)Mathf.RoundToInt(avg)}  (avg {avg:F2})";
    }

    private void RefreshBreedInfo()
    {
        motherBreedInfo = BuildBreedInfo(breedMotherID);
        fatherBreedInfo = BuildBreedInfo(breedFatherID);
    }

    private string BuildBreedInfo(string id)
    {
        if (string.IsNullOrEmpty(id) || !creatureRegistry.TryGet(id, out var dna)) return "---";
        return dna.IsDead
            ? $"\"{dna.CustomName}\"  {dna.Gender} | DEAD"
            : $"\"{dna.CustomName}\"  {dna.Gender} | Breeds: {dna.BreedCount}/{BreedingService.MaxBreedCount}";
    }

    private void ValidateDNA(CreatureDNA dna)
    {
        if (database == null) return;
        LogPart("Body",  database.GetBodyShape(dna.BodyShapeID), dna.BodyShapeID);
        LogPart("Arms",  database.GetArm(dna.ArmID),             dna.ArmID);
        LogPart("Eyes",  database.GetEye(dna.EyeID),             dna.EyeID);
        LogPart("Mouth", database.GetMouth(dna.MouthID),         dna.MouthID);
    }

    private static void LogPart(string label, BodyPart part, string id)
    {
        if (part != null) Debug.Log($"  [OK] {label,-6} → [{id}] {part.Name}  ({part.Rarity})");
        else              Debug.LogWarning($"  [!!] {label,-6} → ID '{id}' not found in database.");
    }

    private static string Clip(string id) => id.Length > 14 ? id[..14] + "…" : id;

    // ── Public Methods ────────────────────────────────────────────

    [Button("Mint Random Creature", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f), BoxGroup("Mint")]
    public void MintRandomCreature()
    {
        var dna        = CreatureGenerator.GenerateRandom(database, rarityOddsTable);
        dna.Gender     = Random.value < 0.5f ? CreatureGender.Male : CreatureGender.Female;
        dna.BaseHP     = Random.Range(1, 11);
        dna.BaseAttack = Random.Range(1, 11);
        dna.BaseSpeed  = Random.Range(1, 11);
        dna.CustomName = CreatureNameBank.GetRandomName();
        dna.Stamp();

        if (!creatureRegistry.Register(dna)) return;

        SaveSystem.SaveDatabase(creatureRegistry);
        lastMintedID = dna.UniqueID;
        Debug.Log($"[GameManager] Minted: \"{dna.CustomName}\"  {dna.UniqueID}  ({dna.Gender})");
    }

    [Button("Breed Creatures", ButtonSizes.Large), GUIColor(1f, 0.7f, 0.85f), BoxGroup("Breed")]
    public void BreedCreatures(string motherID, string fatherID)
    {
        var odds = inheritanceOddsTable ?? InheritanceOddsTableSO.Current;
        if (odds == null) { Debug.LogError("[GameManager] No InheritanceOddsTable assigned."); return; }

        var child = BreedingService.Breed(motherID, fatherID, creatureRegistry, database, odds);
        if (child == null) return;

        child.CustomName = CreatureNameBank.GetRandomName();
        child.Stamp();
        if (!creatureRegistry.Register(child)) return;

        if (creatureRegistry.TryGet(motherID, out var mother)) mother.ChildrenIDs.Add(child.UniqueID);
        if (creatureRegistry.TryGet(fatherID, out var father)) father.ChildrenIDs.Add(child.UniqueID);

        SaveSystem.SaveDatabase(creatureRegistry);
        lastChildID = child.UniqueID;
        RefreshBreedInfo();
        Debug.Log($"[GameManager] Bred child: \"{child.CustomName}\"  {child.UniqueID}  ({child.Gender})");
    }

    // ── Getters ───────────────────────────────────────────────────

    [ShowInInspector, ReadOnly, LabelText("Registered Creatures"), BoxGroup("Registry")]
    public int RegistryCount => creatureRegistry?.Count ?? 0;
}
