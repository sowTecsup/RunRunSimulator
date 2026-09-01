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

    public static PlayerInventorySO CurrentInventory => Instance != null ? Instance.Inventory : null;
    public static DateTime Now => Instance != null ? Instance.ServerNow : DateTime.Now;

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

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

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

    private void PersistFurniture(FurnitureRegistrySO registry) => SaveSystem.SaveFurniture(registry);
    private void PersistInventory(PlayerInventorySO inv)        => SaveSystem.SaveInventory(inv);

    private void OnApplicationQuit()
    {
        CollectLooseWorldProps();
        FlushToCloud();
    }

    private void OnApplicationPause(bool paused)
    {
        if (!paused) return;
        CollectLooseWorldProps();
        FlushToCloud();
    }

    private void CollectLooseWorldProps()
    {
        if (inventory == null) return;
        bool changed = false;
        foreach (var prop in FindObjectsByType<WorldPropInstance>(FindObjectsSortMode.None))
        {
            if (prop == null || prop.IsHeld || string.IsNullOrEmpty(prop.ItemId)) continue;
            inventory.AddWorldProp(prop.ItemId);
            changed = true;
        }
        if (changed) SaveSystem.SaveInventory(inventory);
    }

    public void PushToCloud()
    {
        if (cloudSync != null) _ = cloudSync.PushAsync();
    }

    public void FlushToCloud()
    {
        SaveSystem.SaveDatabase(creatureRegistry);
        SaveSystem.SaveSocialGraph();
        PushToCloud();
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

        GameEvents.RegistryChanged(creatureRegistry);
        lastMintedID = dna.UniqueID;
        Debug.Log($"[GameManager] Minted: \"{dna.CustomName}\"  {dna.UniqueID}  ({dna.Gender})");
    }

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
