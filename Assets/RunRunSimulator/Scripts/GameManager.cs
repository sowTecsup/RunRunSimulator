using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

// Genetics Lab core + single source of truth for the shared assets.
// Other MonoBehaviours (CombatController, BreedingController, AsyncCombatService,
// CloudSyncService) resolve their assets via GameManager.Instance in Awake/Start
// — no serialized cross-references needed.
[DefaultExecutionOrder(-10)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
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

    [AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private CombatManagerSO combatConfig;

    [Required, AssetsOnly, BoxGroup("Setup")]
    [FormerlySerializedAs("_creatureRegistry")]
    [SerializeField] private CreatureRegistrySO creatureRegistry;

    [BoxGroup("Setup")]
    [SerializeField] private CloudSyncService cloudSync;

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

    private void Awake() => Instance = this;

    // Load is triggered by CloudSyncService.OnSignedInComplete (scoped per-player)
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

    // ── Public Methods ────────────────────────────────────────────

    // Fire-and-forget cloud push. PushAsync internally checks isSignedIn,
    // so it's safe to call even before the user has signed in.
    public void PushToCloud()
    {
        if (cloudSync != null) _ = cloudSync.PushAsync();
    }

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
        PushToCloud();
        lastMintedID = dna.UniqueID;
        Debug.Log($"[GameManager] Minted: \"{dna.CustomName}\"  {dna.UniqueID}  ({dna.Gender})");
    }

    // ── Public Getters ────────────────────────────────────────────

    public CreatureRegistrySO     Registry             => creatureRegistry;
    public CreatureDatabaseSO     Database             => database;
    public RarityOddsTableSO      RarityOddsTable      => rarityOddsTable;
    public InheritanceOddsTableSO InheritanceOddsTable => inheritanceOddsTable;
    public CombatManagerSO        CombatConfig         => combatConfig;

    [ShowInInspector, ReadOnly, LabelText("Registered Creatures"), BoxGroup("Registry")]
    public int RegistryCount => creatureRegistry?.Count ?? 0;
}
