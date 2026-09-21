using System;
using System.Collections.Generic;
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

    [Title("Hotbar (I# ids, 6 slots — persists)")]
    [OdinSerialize, ReadOnly]
    private string[] hotbarSlots = new string[HotbarSize];

    [Title("Dabloons (currency)")]
    [OdinSerialize, ReadOnly]
    private int dabloons;

    [OdinSerialize, ReadOnly, PreviouslySerializedAs("adventureMaterial")]
    private int minerita;

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

    public int Balance(Currency c)
    {
        switch (c)
        {
            case Currency.Dabloons: return dabloons;
            case Currency.Minerita: return minerita;
            default: return 0;
        }
    }

    public void Add(Currency c, int amount)
    {
        if (amount <= 0) return;
        switch (c)
        {
            case Currency.Dabloons: dabloons += amount; break;
            case Currency.Minerita: minerita += amount; break;
        }
        MarkDirty();
    }

    public bool TrySpend(Currency c, int amount)
    {
        if (amount <= 0 || Balance(c) < amount) return false;
        switch (c)
        {
            case Currency.Dabloons: dabloons -= amount; break;
            case Currency.Minerita: minerita -= amount; break;
        }
        MarkDirty();
        return true;
    }

    public void ResetCurrency(Currency c)
    {
        switch (c)
        {
            case Currency.Dabloons: dabloons = 0; break;
            case Currency.Minerita: minerita = 0; break;
        }
        MarkDirty();
    }

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
        public string[]      HotbarSlots      = new string[HotbarSize];
        public int           Dabloons         = 0;
        public int           Minerita         = 0;
    }

    public InventoryData GetData() => new InventoryData
    {
        FurnitureOwned   = new List<string>(furnitureOwned),
        WorldPropsStored = new List<string>(worldPropsStored),
        HotbarSlots      = (string[])hotbarSlots.Clone(),
        Dabloons         = dabloons,
        Minerita         = minerita,
    };

    public void LoadFrom(InventoryData data)
    {
        furnitureOwned   = data?.FurnitureOwned   ?? new List<string>();
        worldPropsStored = data?.WorldPropsStored ?? new List<string>();
        hotbarSlots      = NormalizeHotbar(data?.HotbarSlots);
        dabloons         = data?.Dabloons ?? 0;
        minerita         = data?.Minerita ?? 0;
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
