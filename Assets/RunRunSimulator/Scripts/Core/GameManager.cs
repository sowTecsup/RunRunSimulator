using Sirenix.OdinInspector;
using System;
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

    [AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private PersonalityProfileSO personalityProfiles;

    [Required, AssetsOnly, BoxGroup("Setup")]
    [FormerlySerializedAs("_creatureRegistry")]
    [SerializeField] private CreatureRegistrySO creatureRegistry;

    [Required, AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private FurnitureRegistrySO furnitureRegistry;

    [Required, AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private PlayerInventorySO inventory;

    [AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private PartVisualBankSO partVisualBank;

    [BoxGroup("Setup")]
    [SerializeField] private CloudSyncService cloudSync;

    [Title("Dev Tools")]
    [BoxGroup("Dev Tools"), SerializeField, LabelText("Dabloons to add")]
    private int devDabloonsAmount = 500;

    [Button("Add Dabloons (DEV)", ButtonSizes.Medium), GUIColor(0.9f, 0.75f, 0.2f), BoxGroup("Dev Tools")]
    private void DevAddDabloons()
    {
        if (inventory == null) { Debug.LogWarning("[GameManager] No inventory assigned."); return; }
        inventory.AddDabloons(devDabloonsAmount);
        GameEvents.InventoryChanged(inventory);
        Debug.Log($"[GameManager] +{devDabloonsAmount} Dabloons → total: {inventory.Dabloons}");
    }

    [Button("Reset Dabloons (DEV)", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.3f), BoxGroup("Dev Tools")]
    private void DevResetDabloons()
    {
        if (inventory == null) { Debug.LogWarning("[GameManager] No inventory assigned."); return; }
        inventory.ResetDabloons();
        SaveSystem.SaveInventory(inventory);
        GameEvents.InventoryChanged(inventory);
        Debug.Log("[GameManager] Dabloons reset to 0.");
    }

    [Button("Clear Furniture Owned (DEV)", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.3f), BoxGroup("Dev Tools")]
    private void DevClearFurnitureOwned()
    {
        if (inventory == null) { Debug.LogWarning("[GameManager] No inventory assigned."); return; }
        inventory.ClearFurnitureOwned();
        SaveSystem.SaveInventory(inventory);
        GameEvents.InventoryChanged(inventory);
        Debug.Log("[GameManager] Furniture owned list cleared.");
    }

    [Button("Clear World Props (DEV)", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.3f), BoxGroup("Dev Tools")]
    private void DevClearWorldProps()
    {
        if (inventory == null) { Debug.LogWarning("[GameManager] No inventory assigned."); return; }
        inventory.ClearWorldPropsStored();
        inventory.ClearHotbar();
        SaveSystem.SaveInventory(inventory);
        GameEvents.InventoryChanged(inventory);
        Debug.Log("[GameManager] World props and hotbar cleared.");
    }

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

    // GameManager is the single owner of persistence: it's the only gameplay
    // script that knows SaveSystem and CloudSync. Everyone else just fires
    // GameEvents.RegistryChanged() / FurnitureChanged() / InventoryChanged() and
    // these handlers do the save (+ cloud push for creatures).
    private void OnEnable()
    {
        GameEvents.OnRegistryChanged  += Persist;
        GameEvents.OnFurnitureChanged += PersistFurniture;
        GameEvents.OnInventoryChanged += PersistInventory;
    }

    private void OnDisable()
    {
        GameEvents.OnRegistryChanged  -= Persist;
        GameEvents.OnFurnitureChanged -= PersistFurniture;
        GameEvents.OnInventoryChanged -= PersistInventory;
    }

    private void Persist(CreatureRegistrySO registry)
    {
        SaveSystem.SaveDatabase(registry);
        PushToCloud();
    }

    // Furniture + inventory persist LOCALLY only for now (cloud sync layered on
    // later, same pattern as CloudSyncService). The event carries the asset.
    private void PersistFurniture(FurnitureRegistrySO registry) => SaveSystem.SaveFurniture(registry);
    private void PersistInventory(PlayerInventorySO inv)        => SaveSystem.SaveInventory(inv);

    // Load is triggered by CloudSyncService.OnSignedInComplete (scoped per-player)
    private void OnApplicationQuit()
    {
        CollectLooseWorldProps();
        FlushToCloud();
    }

    // Minimize / send-to-background — the reliable "I'm leaving" signal on mobile. We flush here
    // (and on quit/logout/explicit save) instead of on every stat change, so runtime needs don't
    // saturate Cloud Save with per-frame micro-updates.
    private void OnApplicationPause(bool paused)
    {
        if (!paused) return;
        CollectLooseWorldProps();
        FlushToCloud();
    }

    // Persistence-simplification rule (decided with the user): any world prop loose
    // in the scene at shutdown is swept back into the inventory, so on reload we only
    // ever rebuild from inventory data — no per-object transforms to persist. The
    // active hotbar item is the single documented exception (re-spawned on load).
    // Implemented in the WorldProp gameplay batch — see WorldPropInstance.
    private void CollectLooseWorldProps()
    {
        if (inventory == null) return;
        bool changed = false;
        foreach (var prop in FindObjectsByType<WorldPropInstance>(FindObjectsSortMode.None))
        {
            // Skip the active hotbar item: its id already persists in the hotbar slot,
            // so sweeping it too would double-count it on reload.
            if (prop == null || prop.IsHeld || string.IsNullOrEmpty(prop.ItemId)) continue;
            inventory.AddWorldProp(prop.ItemId);
            changed = true;
        }
        if (changed) SaveSystem.SaveInventory(inventory);
    }

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

    // Local save + best-effort cloud push. The single place to call on quit / pause / logout / an
    // explicit "save game". Runtime needs ride this flush rather than firing RegistryChanged per tick.
    public void FlushToCloud()
    {
        SaveSystem.SaveDatabase(creatureRegistry);
        PushToCloud();
    }

    [Button("Mint Random Creature", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f), BoxGroup("Mint")]
    public void MintRandomCreature()
    {
        var dna        = CreatureGenerator.GenerateRandom(database, rarityOddsTable);
        dna.Gender     = UnityEngine.Random.value < 0.5f ? CreatureGender.Male : CreatureGender.Female;
        dna.Personality = CreatureGenerator.RandomPersonality();
        dna.BaseHP     = UnityEngine.Random.Range(1, 11);
        dna.BaseAttack = UnityEngine.Random.Range(1, 11);
        dna.BaseSpeed  = UnityEngine.Random.Range(1, 11);
        dna.CustomName = CreatureNameBank.GetRandomName();
        dna.Stamp();

        if (!creatureRegistry.Register(dna)) return;

        GameEvents.CreatureMinted(dna);
        GameEvents.RegistryChanged(creatureRegistry);
        lastMintedID = dna.UniqueID;
        Debug.Log($"[GameManager] Minted: \"{dna.CustomName}\"  {dna.UniqueID}  ({dna.Gender})");
    }

    // ── Public Getters ────────────────────────────────────────────

    // Server time in local timezone, offset-corrected at login. Falls back to
    // DateTime.Now when offline or before the first fetch completes.
    public DateTime ServerNow =>
        cloudSync != null
            ? (DateTime.UtcNow + cloudSync.ServerOffset).ToLocalTime()
            : DateTime.Now;

    public CreatureRegistrySO     Registry             => creatureRegistry;
    public FurnitureRegistrySO    FurnitureRegistry    => furnitureRegistry;
    public PlayerInventorySO      Inventory            => inventory;
    public CreatureDatabaseSO     Database             => database;
    public RarityOddsTableSO      RarityOddsTable      => rarityOddsTable;
    public InheritanceOddsTableSO InheritanceOddsTable => inheritanceOddsTable;
    public CombatManagerSO        CombatConfig         => combatConfig;
    public PersonalityProfileSO   PersonalityProfiles  => personalityProfiles;
    public PartVisualBankSO       PartVisualBank       => partVisualBank;

    [ShowInInspector, ReadOnly, LabelText("Registered Creatures"), BoxGroup("Registry")]
    public int RegistryCount => creatureRegistry?.Count ?? 0;
}
