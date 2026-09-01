using System;
namespace MoriMonchiSimulator
{

public static class GameEvents
{
    public static event Action<CreatureRegistrySO> OnRegistryChanged;
    public static void RegistryChanged(CreatureRegistrySO registry) => OnRegistryChanged?.Invoke(registry);

    public static event Action<CreatureRegistrySO> OnRegistryReloaded;
    public static void RegistryReloaded(CreatureRegistrySO registry) => OnRegistryReloaded?.Invoke(registry);

    public static event Action<CreatureDNA, CreatureDNA, CreatureDNA> OnBreedingCompleted;
    public static void BreedingCompleted(CreatureDNA mother, CreatureDNA father, CreatureDNA child) =>
        OnBreedingCompleted?.Invoke(mother, father, child);

    public static event Action<FurnitureRegistrySO> OnFurnitureChanged;
    public static void FurnitureChanged(FurnitureRegistrySO registry) => OnFurnitureChanged?.Invoke(registry);

    public static event Action<FurnitureRegistrySO> OnFurnitureReloaded;
    public static void FurnitureReloaded(FurnitureRegistrySO registry) => OnFurnitureReloaded?.Invoke(registry);

    public static event Action OnNavMeshWillRebake;
    public static void NavMeshWillRebake() => OnNavMeshWillRebake?.Invoke();

    public static event Action OnNavMeshRebaked;
    public static void NavMeshRebaked() => OnNavMeshRebaked?.Invoke();

    public static event Action<PlayerInventorySO> OnInventoryChanged;
    public static void InventoryChanged(PlayerInventorySO inventory) => OnInventoryChanged?.Invoke(inventory);

    public static event Action<PlayerInventorySO> OnInventoryReloaded;
    public static void InventoryReloaded(PlayerInventorySO inventory) => OnInventoryReloaded?.Invoke(inventory);

    public static event Action<NpcAgent, CreatureDNA, int> OnCustomerSold;
    public static void CustomerSold(NpcAgent agent, CreatureDNA mm, int finalPrice) => OnCustomerSold?.Invoke(agent, mm, finalPrice);
}
}
