using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // ── Setup ─────────────────────────────────────────────────────

    [Required, AssetsOnly]
    [Title("RunRunSimulator — Genetics Lab", "Assign all assets below to begin.", TitleAlignments.Centered)]
    [BoxGroup("Setup")]
    [SerializeField] private CreatureDatabaseSO _database;

    [AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private RarityOddsTableSO _rarityOddsTable;

    [AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private InheritanceOddsTableSO _inheritanceOddsTable;

    [AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private CombatManagerSO _combatManager;

    // ── Registry ──────────────────────────────────────────────────

    [Required, AssetsOnly]
    [BoxGroup("Setup")]
    [SerializeField] private CreatureRegistrySO _creatureRegistry;

    [ShowInInspector, ReadOnly, LabelText("Registered Creatures")]
    [BoxGroup("Registry")]
    private int RegistryCount => _creatureRegistry?.Count ?? 0;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        SaveSystem.LoadInto(_creatureRegistry);
    }

    private void OnApplicationQuit()
    {
        SaveSystem.SaveDatabase(_creatureRegistry);
    }

    // ── Generate (preview only — not registered) ──────────────────

    [BoxGroup("Current Creature")]
    [SerializeField, ReadOnly, InlineProperty, HideLabel]
    private CreatureDNA _currentDNA = new CreatureDNA();

    [BoxGroup("Current Creature")]
    [SerializeField, ReadOnly, LabelText("DNA String")]
    private string _currentDNAString = "---";

    [Button("Generate Random Creature", ButtonSizes.Large), GUIColor(0.4f, 0.85f, 0.4f)]
    [BoxGroup("Current Creature")]
    private void GenerateRandomCreature()
    {
        _currentDNA       = CreatureGenerator.GenerateRandom(_database, _rarityOddsTable);
        _currentDNAString = _currentDNA.ToStringID();
        RefreshRarityBreakdown();
        Debug.Log($"[GameManager] Generated (preview): {_currentDNAString}");
    }

    // ── Mint (generate + register + save) ─────────────────────────

    [ShowInInspector, ReadOnly, LabelText("Last Minted ID")]
    [BoxGroup("Mint")]
    private string _lastMintedID = "---";

    [Button("Mint Random Creature", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f)]
    [BoxGroup("Mint")]
    public void MintRandomCreature()
    {
        var dna        = CreatureGenerator.GenerateRandom(_database, _rarityOddsTable);
        dna.Gender     = Random.value < 0.5f ? CreatureGender.Male : CreatureGender.Female;
        dna.BaseHP     = Random.Range(1, 11);
        dna.BaseAttack = Random.Range(1, 11);
        dna.BaseSpeed  = Random.Range(1, 11);
        dna.Stamp();

        if (_creatureRegistry.Register(dna))
        {
            SaveSystem.SaveDatabase(_creatureRegistry);
            _lastMintedID = dna.UniqueID;
            Debug.Log($"[GameManager] Minted: {dna.UniqueID}  ({dna.Gender})");
        }
    }

    // ── Breed ─────────────────────────────────────────────────────

    [BoxGroup("Breed")]
    [SerializeField, LabelText("Mother ID")] private string _breedMotherID = "";

    [BoxGroup("Breed")]
    [SerializeField, LabelText("Father ID")] private string _breedFatherID = "";

    [ShowInInspector, ReadOnly, LabelText("Mother Info")]
    [BoxGroup("Breed")]
    private string _motherBreedInfo = "---";

    [ShowInInspector, ReadOnly, LabelText("Father Info")]
    [BoxGroup("Breed")]
    private string _fatherBreedInfo = "---";

    [ShowInInspector, ReadOnly, LabelText("Last Child ID")]
    [BoxGroup("Breed")]
    private string _lastChildID = "---";

    [Button("Fill Random Breeders"), GUIColor(0.85f, 0.6f, 1f)]
    [BoxGroup("Breed")]
    private void FillRandomBreeders()
    {
        var all     = _creatureRegistry.GetAll().Values.ToList();
        var females = all.Where(d => !d.IsDead && d.Gender == CreatureGender.Female && d.BreedCount < BreedingService.MaxBreedCount).ToList();
        var males   = all.Where(d => !d.IsDead && d.Gender == CreatureGender.Male   && d.BreedCount < BreedingService.MaxBreedCount).ToList();

        if (females.Count == 0 || males.Count == 0)
        {
            Debug.LogError("[GameManager] Not enough valid breeders — need at least one alive Male and one alive Female under the breed limit.");
            return;
        }

        var mother = females[Random.Range(0, females.Count)];
        var father = males[Random.Range(0, males.Count)];

        _breedMotherID = mother.UniqueID;
        _breedFatherID = father.UniqueID;
        RefreshBreedInfo();
        Debug.Log($"[GameManager] Random breeders selected — Mother: {Clip(mother.UniqueID)} | Father: {Clip(father.UniqueID)}");
    }

    [Button("Breed Creatures", ButtonSizes.Large), GUIColor(1f, 0.7f, 0.85f)]
    [BoxGroup("Breed")]
    public void BreedCreatures(string motherID, string fatherID)
    {
        var odds = _inheritanceOddsTable ?? InheritanceOddsTableSO.Current;
        if (odds == null)
        {
            Debug.LogError("[GameManager] No InheritanceOddsTable assigned.");
            return;
        }

        var child = BreedingService.Breed(motherID, fatherID, _creatureRegistry, _database, odds);
        if (child == null) return;

        child.Stamp();
        if (!_creatureRegistry.Register(child)) return;

        if (_creatureRegistry.TryGet(motherID, out var mother)) mother.ChildrenIDs.Add(child.UniqueID);
        if (_creatureRegistry.TryGet(fatherID, out var father)) father.ChildrenIDs.Add(child.UniqueID);

        SaveSystem.SaveDatabase(_creatureRegistry);
        _lastChildID = child.UniqueID;
        RefreshBreedInfo();
        Debug.Log($"[GameManager] Bred child: {child.UniqueID}  ({child.Gender})");
    }

    [Button("Breed"), GUIColor(1f, 0.7f, 0.85f)]
    [BoxGroup("Breed")]
    private void BreedButton() => BreedCreatures(_breedMotherID, _breedFatherID);

    // ── Combat ────────────────────────────────────────────────────

    [BoxGroup("Combat")]
    [SerializeField, LabelText("Fighter A — UniqueID")] private string _combatAID = "";

    [BoxGroup("Combat")]
    [SerializeField, LabelText("Fighter B — UniqueID")] private string _combatBID = "";

    [ShowInInspector, ReadOnly, LabelText("Fighter A Info")]
    [BoxGroup("Combat")]
    private string _fighterAInfo = "---";

    [ShowInInspector, ReadOnly, LabelText("Fighter B Info")]
    [BoxGroup("Combat")]
    private string _fighterBInfo = "---";

    [ShowInInspector, ReadOnly, LabelText("Last Result")]
    [BoxGroup("Combat")]
    private string _lastCombatResult = "---";

    [Button("Fill Random Fighters"), GUIColor(1f, 0.65f, 0.5f)]
    [BoxGroup("Combat")]
    private void FillRandomFighters()
    {
        var config = _combatManager ?? CombatManagerSO.Current;
        if (config == null)
        {
            Debug.LogError("[GameManager] No CombatManager assigned.");
            return;
        }

        var eligible = _creatureRegistry.GetAll().Values
            .Where(d => !d.IsDead && d.FightCount < config.MaxFightCount)
            .ToList();

        if (eligible.Count < 2)
        {
            Debug.LogError("[GameManager] Not enough valid fighters — need at least 2 alive creatures under the fight limit.");
            return;
        }

        int idxA = Random.Range(0, eligible.Count);
        int idxB;
        do { idxB = Random.Range(0, eligible.Count); } while (idxB == idxA);

        _combatAID = eligible[idxA].UniqueID;
        _combatBID = eligible[idxB].UniqueID;
        RefreshCombatInfo(config);
        Debug.Log($"[GameManager] Random fighters — A: {Clip(_combatAID)} | B: {Clip(_combatBID)}");
    }

    [Button("Simulate Combat", ButtonSizes.Large), GUIColor(1f, 0.45f, 0.45f)]
    [BoxGroup("Combat")]
    private void SimulateCombatButton()
    {
        var config = _combatManager ?? CombatManagerSO.Current;
        if (config == null)
        {
            Debug.LogError("[GameManager] No CombatManager assigned.");
            return;
        }

        var result = CombatService.Simulate(_combatAID, _combatBID, _creatureRegistry, _database, config);
        if (result == null) return;

        foreach (var line in result.Log)
            Debug.Log($"[Combat] {line}");

        SaveSystem.SaveDatabase(_creatureRegistry);
        _lastCombatResult = result.Summary;
        RefreshCombatInfo(config);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void RefreshBreedInfo()
    {
        _motherBreedInfo = BuildBreedInfo(_breedMotherID);
        _fatherBreedInfo = BuildBreedInfo(_breedFatherID);
    }

    private string BuildBreedInfo(string id)
    {
        if (string.IsNullOrEmpty(id) || !_creatureRegistry.TryGet(id, out var dna))
            return "---";
        if (dna.IsDead)
            return $"{dna.Gender} | DEAD";
        return $"{dna.Gender} | Breeds: {dna.BreedCount}/{BreedingService.MaxBreedCount}";
    }

    private void RefreshCombatInfo(CombatManagerSO config)
    {
        _fighterAInfo = BuildFightInfo(_combatAID, config);
        _fighterBInfo = BuildFightInfo(_combatBID, config);
    }

    private string BuildFightInfo(string id, CombatManagerSO config)
    {
        if (string.IsNullOrEmpty(id) || !_creatureRegistry.TryGet(id, out var dna))
            return "---";
        if (dna.IsDead)
            return "DEAD — cannot fight";
        int remaining = config.MaxFightCount - dna.FightCount;
        return $"Fights left: {remaining}/{config.MaxFightCount}  (used: {dna.FightCount})";
    }

    private static string Clip(string id) => id.Length > 14 ? id[..14] + "…" : id;

    // ── Rarity Breakdown ──────────────────────────────────────────

    [Title("Rarity Breakdown")]
    [BoxGroup("Current Creature/Rarity")]
    [ShowInInspector, ReadOnly, LabelText("Body Shape"), LabelWidth(80)]
    private Rarity _rarityBodyShape;

    [BoxGroup("Current Creature/Rarity")]
    [ShowInInspector, ReadOnly, LabelText("Arms"), LabelWidth(80)]
    private Rarity _rarityArms;

    [BoxGroup("Current Creature/Rarity")]
    [ShowInInspector, ReadOnly, LabelText("Eyes"), LabelWidth(80)]
    private Rarity _rarityEyes;

    [BoxGroup("Current Creature/Rarity")]
    [ShowInInspector, ReadOnly, LabelText("Mouth"), LabelWidth(80)]
    private Rarity _rarityMouth;

    [BoxGroup("Current Creature/Rarity")]
    [ShowInInspector, ReadOnly, LabelText("Score"), LabelWidth(80)]
    private string _rarityScore = "---";

    private void RefreshRarityBreakdown()
    {
        if (_database == null) return;

        var bodyShape = _database.GetBodyShape(_currentDNA.BodyShapeID);
        var arm       = _database.GetArm(_currentDNA.ArmID);
        var eye       = _database.GetEye(_currentDNA.EyeID);
        var mouth     = _database.GetMouth(_currentDNA.MouthID);

        _rarityBodyShape = bodyShape?.Rarity ?? Rarity.Common;
        _rarityArms      = arm?.Rarity       ?? Rarity.Common;
        _rarityEyes      = eye?.Rarity       ?? Rarity.Common;
        _rarityMouth     = mouth?.Rarity     ?? Rarity.Common;

        float avg    = ((int)_rarityBodyShape + (int)_rarityArms + (int)_rarityEyes + (int)_rarityMouth) / 4f;
        _rarityScore = $"{(Rarity)Mathf.RoundToInt(avg)}  (avg {avg:F2})";
    }

    // ── Load by DNA String ─────────────────────────────────────────

    [BoxGroup("Load by ID")]
    [InfoBox("Format: BODYSHAPEID-ARMID-EYEID-MOUTHID-RRGGBB   (e.g.  BS0-A3-E1-M2-FF00AA)")]
    [SerializeField, LabelText("DNA String")]
    private string _loadIDInput = "";

    [Button("Load from ID"), GUIColor(0.4f, 0.6f, 0.95f)]
    [BoxGroup("Load by ID")]
    private void LoadFromID()
    {
        if (string.IsNullOrWhiteSpace(_loadIDInput))
        {
            Debug.LogWarning("[GameManager] No ID entered.");
            return;
        }

        _currentDNA       = CreatureDNA.FromID(_loadIDInput);
        _currentDNAString = _currentDNA.ToStringID();
        RefreshRarityBreakdown();
        Debug.Log($"[GameManager] Loaded: {_currentDNAString}");
        ValidateDNA(_currentDNA);
    }

    private void ValidateDNA(CreatureDNA dna)
    {
        if (_database == null) return;
        LogPart("Body",  _database.GetBodyShape(dna.BodyShapeID), dna.BodyShapeID);
        LogPart("Arms",  _database.GetArm(dna.ArmID),            dna.ArmID);
        LogPart("Eyes",  _database.GetEye(dna.EyeID),             dna.EyeID);
        LogPart("Mouth", _database.GetMouth(dna.MouthID),         dna.MouthID);
    }

    private static void LogPart(string label, BodyPart part, string id)
    {
        if (part != null)
            Debug.Log($"  [OK] {label,-6} → [{id}] {part.Name}  ({part.Rarity})");
        else
            Debug.LogWarning($"  [!!] {label,-6} → ID '{id}' not found in database.");
    }
}
