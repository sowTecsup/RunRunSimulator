using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Serialization;
namespace MoriMonchiSimulator
{

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
    [SerializeField] private RoleWorldProfileSO roleWorldProfiles;

    [Required, AssetsOnly, BoxGroup("Setup")]
    [FormerlySerializedAs("_creatureRegistry")]
    [SerializeField] private CreatureRegistrySO creatureRegistry;

    [Required, AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private FurnitureRegistrySO furnitureRegistry;

    [Required, AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private PlayerInventorySO inventory;

    [AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private MonchiVisualBankSO monchiVisualBank;

    [AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private FurTypeDatabaseSO furTypeDatabase;

    [AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private EquipmentDatabaseSO equipmentDatabase;

    [BoxGroup("Setup")]
    [SerializeField] private CloudSyncService cloudSync;

    [ShowInInspector, ReadOnly, LabelText("Last Minted ID")]
    [BoxGroup("Mint")]
    private string lastMintedID = "---";

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake() => Instance = this;

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
        SaveSystem.SaveSocialGraph();
        PushToCloud();
    }

    public void FlushForSceneChange()
    {
        CollectLooseWorldProps();
        FlushToCloud();
    }

    [Button("Mint Random Creature", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f), BoxGroup("Mint")]
    public void MintRandomCreature()
    {
        var dna        = CreatureGenerator.GenerateRandom(database, furTypeDatabase);
        dna.Gender     = UnityEngine.Random.value < 0.5f ? CreatureGender.Male : CreatureGender.Female;
        dna.Element = CreatureGenerator.RandomElement();
        dna.Role = CreatureGenerator.RandomRole();
        (dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed) = CreatureGenerator.RandomBaseStats();
        dna.Sociability = CreatureGenerator.RandomDial();
        dna.Boldness    = CreatureGenerator.RandomDial();
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
    public RoleWorldProfileSO     RoleWorldProfiles    => roleWorldProfiles;
    public MonchiVisualBankSO     MonchiVisualBank     => monchiVisualBank;
    public FurTypeDatabaseSO      FurTypeDatabase      => furTypeDatabase;
    public EquipmentDatabaseSO    EquipmentDatabase    => equipmentDatabase;

    [ShowInInspector, ReadOnly, LabelText("Registered Creatures"), BoxGroup("Registry")]
    public int RegistryCount => creatureRegistry?.Count ?? 0;
}
}
