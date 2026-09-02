using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "PlayerInventory", menuName = "RunRunSimulator/Player/Player Inventory")]
public class PlayerInventorySO : SerializedScriptableObject
{
    public const int HotbarSize = 6;

    [Title("Furniture owned (F# ids — set)")]
    [OdinSerialize, ReadOnly]
    private List<string> furnitureOwned = new List<string>();

    [Title("World props stored (I# ids — list, dupes = multiple instances)")]
    [OdinSerialize, ReadOnly]
    private List<string> worldPropsStored = new List<string>();

    [Title("Equipment grids (EQ# ids per slot — null entry = empty cell, index = cell)")]
    [OdinSerialize, ReadOnly]
    private Dictionary<EquipmentSlot, List<string>> equipmentGrids = new Dictionary<EquipmentSlot, List<string>>();

    [Title("Hotbar (I# ids, 6 slots — persists)")]
    [OdinSerialize, ReadOnly]
    private string[] hotbarSlots = new string[HotbarSize];

    [Title("Dabloons (currency)")]
    [OdinSerialize, ReadOnly]
    private int dabloons;

    [OdinSerialize, ReadOnly]
    private int adventureMaterial;

    [OdinSerialize, ReadOnly]
    private int passiveMaterial;

    [OdinSerialize, ReadOnly]
    private int evolutionEssence;

    public bool AddFurniture(string id)
    {
        if (string.IsNullOrEmpty(id) || furnitureOwned.Contains(id)) return false;
        furnitureOwned.Add(id);
        MarkDirty();
        return true;
    }

    public bool HasFurniture(string id) => furnitureOwned.Contains(id);

    public IReadOnlyList<string> FurnitureOwned => furnitureOwned;

    public void AddWorldProp(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        worldPropsStored.Add(id);
        MarkDirty();
    }

    public bool RemoveWorldProp(string id)
    {
        if (!worldPropsStored.Remove(id)) return false;
        MarkDirty();
        return true;
    }

    public IReadOnlyList<string> WorldPropsStored => worldPropsStored;

    private List<string> GridFor(EquipmentSlot slot)
    {
        if (!equipmentGrids.TryGetValue(slot, out var list))
        {
            list = new List<string>();
            equipmentGrids[slot] = list;
        }
        return list;
    }

    public void AddEquipment(EquipmentSlot slot, string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        var grid = GridFor(slot);
        for (int i = 0; i < grid.Count; i++)
        {
            if (string.IsNullOrEmpty(grid[i]))
            {
                grid[i] = id;
                MarkDirty();
                return;
            }
        }
        grid.Add(id);
        MarkDirty();
    }

    public bool RemoveEquipmentAt(EquipmentSlot slot, int index)
    {
        var grid = GridFor(slot);
        if (index < 0 || index >= grid.Count || string.IsNullOrEmpty(grid[index])) return false;
        grid[index] = null;
        TrimTrailing(grid);
        MarkDirty();
        return true;
    }

    public void MoveEquipment(EquipmentSlot slot, int from, int to)
    {
        var grid = GridFor(slot);
        if (from < 0 || from >= grid.Count || string.IsNullOrEmpty(grid[from])) return;
        if (from == to) return;
        if (to >= grid.Count)
        {
            while (grid.Count <= to) grid.Add(null);
        }
        if (string.IsNullOrEmpty(grid[to]))
        {
            grid[to] = grid[from];
            grid[from] = null;
        }
        else
        {
            (grid[from], grid[to]) = (grid[to], grid[from]);
        }
        TrimTrailing(grid);
        MarkDirty();
    }

    public IReadOnlyList<string> GetEquipment(EquipmentSlot slot) => GridFor(slot);

    private static void TrimTrailing(List<string> list)
    {
        while (list.Count > 0 && string.IsNullOrEmpty(list[list.Count - 1]))
            list.RemoveAt(list.Count - 1);
    }

    public int Dabloons => dabloons;

    public void AddDabloons(int amount)
    {
        if (amount <= 0) return;
        dabloons += amount;
        MarkDirty();
    }

    public bool SpendDabloons(int amount)
    {
        if (amount <= 0 || dabloons < amount) return false;
        dabloons -= amount;
        MarkDirty();
        return true;
    }

    public void ResetDabloons()
    {
        dabloons = 0;
        MarkDirty();
    }

    public int AdventureMaterial => adventureMaterial;

    public void AddAdventureMaterial(int amount)
    {
        if (amount <= 0) return;
        adventureMaterial += amount;
        MarkDirty();
    }

    public int PassiveMaterial => passiveMaterial;

    public int EvolutionEssence => evolutionEssence;

    public void ClearFurnitureOwned()
    {
        furnitureOwned.Clear();
        MarkDirty();
    }

    public void ClearWorldPropsStored()
    {
        worldPropsStored.Clear();
        MarkDirty();
    }

    public void ClearEquipmentOwned()
    {
        equipmentGrids.Clear();
        MarkDirty();
    }

    public void ClearHotbar()
    {
        for (int i = 0; i < hotbarSlots.Length; i++) hotbarSlots[i] = null;
        MarkDirty();
    }

    public string GetHotbarSlot(int index) =>
        (index >= 0 && index < hotbarSlots.Length) ? hotbarSlots[index] : null;

    public void SetHotbarSlot(int index, string id)
    {
        if (index < 0 || index >= hotbarSlots.Length) return;
        hotbarSlots[index] = id;
        MarkDirty();
    }

    public void ClearHotbarSlot(int index) => SetHotbarSlot(index, null);

    public string[] HotbarSlots => hotbarSlots;

    [Serializable]
    public class InventoryData
    {
        public List<string> FurnitureOwned   = new List<string>();
        public List<string> WorldPropsStored = new List<string>();
        public Dictionary<EquipmentSlot, List<string>> EquipmentGrids = new Dictionary<EquipmentSlot, List<string>>();
        public string[]      HotbarSlots      = new string[HotbarSize];
        public int           Dabloons         = 0;
        public int           AdventureMaterial = 0;
        public int           PassiveMaterial   = 0;
        public int           EvolutionEssence  = 0;
    }

    public InventoryData GetData() => new InventoryData
    {
        FurnitureOwned   = new List<string>(furnitureOwned),
        WorldPropsStored = new List<string>(worldPropsStored),
        EquipmentGrids   = equipmentGrids.ToDictionary(kv => kv.Key, kv => new List<string>(kv.Value)),
        HotbarSlots      = (string[])hotbarSlots.Clone(),
        Dabloons         = dabloons,
        AdventureMaterial = adventureMaterial,
        PassiveMaterial   = passiveMaterial,
        EvolutionEssence  = evolutionEssence,
    };

    public void LoadFrom(InventoryData data)
    {
        furnitureOwned   = data?.FurnitureOwned   ?? new List<string>();
        worldPropsStored = data?.WorldPropsStored ?? new List<string>();
        equipmentGrids   = data?.EquipmentGrids != null
            ? data.EquipmentGrids.ToDictionary(kv => kv.Key, kv => new List<string>(kv.Value))
            : new Dictionary<EquipmentSlot, List<string>>();
        hotbarSlots      = NormalizeHotbar(data?.HotbarSlots);
        dabloons         = data?.Dabloons ?? 0;
        adventureMaterial = data?.AdventureMaterial ?? 0;
        passiveMaterial   = data?.PassiveMaterial   ?? 0;
        evolutionEssence  = data?.EvolutionEssence  ?? 0;
        MarkDirty();
    }

    private static string[] NormalizeHotbar(string[] saved)
    {
        var slots = new string[HotbarSize];
        if (saved != null)
            for (int i = 0; i < HotbarSize && i < saved.Length; i++)
                slots[i] = saved[i];
        return slots;
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
