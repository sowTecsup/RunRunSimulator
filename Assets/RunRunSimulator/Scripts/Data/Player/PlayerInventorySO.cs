using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

// The player's single inventory, split into the two id namespaces (one SO, two
// categories — see ItemType). Source of truth for "what the player owns"; mutate
// only through here and fire GameEvents.InventoryChanged so GameManager persists.
//
//  furnitureOwned   — "F#" ids (FurnitureDefinitionSO). The build browser filters
//                     the furniture DB against this list. A piece you own can be
//                     placed any number of times, so ownership is a SET (no dupes).
//  worldPropsStored — "I#" ids (ItemDefinitionSO) of props sitting in the storage
//                     box. World props are UNIQUE instances, so this is a LIST:
//                     two brooms = the id "I3" appears twice.
//  equipmentOwned   — "EQ#" ids (EquipmentSO). Items don't stack, so this is a LIST
//                     (dupes = multiple instances); the ORDER is significant — it's
//                     the backpack grid order shown in UI.
//  hotbarSlots      — "I#" ids the player put on the play-mode hotbar; persists so
//                     the bar survives a reload. null = empty slot.
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

    [Title("Equipment owned (EQ# ids — list, dupes = multiple instances, order = backpack grid order)")]
    [OdinSerialize, ReadOnly]
    private List<string> equipmentOwned = new List<string>();

    [Title("Hotbar (I# ids, 6 slots — persists)")]
    [OdinSerialize, ReadOnly]
    private string[] hotbarSlots = new string[HotbarSize];

    [Title("Dabloons (currency)")]
    [OdinSerialize, ReadOnly]
    private int dabloons;

    // ── Furniture ─────────────────────────────────────────────────

    // Ownership is a set — owning a piece lets you place it any number of times.
    public bool AddFurniture(string id)
    {
        if (string.IsNullOrEmpty(id) || furnitureOwned.Contains(id)) return false;
        furnitureOwned.Add(id);
        MarkDirty();
        return true;
    }

    public bool RemoveFurniture(string id)
    {
        if (!furnitureOwned.Remove(id)) return false;
        MarkDirty();
        return true;
    }

    public bool HasFurniture(string id) => furnitureOwned.Contains(id);

    public IReadOnlyList<string> FurnitureOwned => furnitureOwned;

    // ── World props ───────────────────────────────────────────────

    // A list (not a set): each call adds one more physical instance.
    public void AddWorldProp(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        worldPropsStored.Add(id);
        MarkDirty();
    }

    // Removes ONE instance of the id (one physical object leaves the box).
    public bool RemoveWorldProp(string id)
    {
        if (!worldPropsStored.Remove(id)) return false;
        MarkDirty();
        return true;
    }

    public IReadOnlyList<string> WorldPropsStored => worldPropsStored;

    // ── Equipment ─────────────────────────────────────────────────

    // A list (not a set): each call adds one more physical instance. Order matters —
    // it's the backpack grid order shown in UI.
    public void AddEquipment(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        equipmentOwned.Add(id);
        MarkDirty();
    }

    // Removes the instance at the given grid slot (not by id — items don't stack).
    public bool RemoveEquipmentAt(int index)
    {
        if (index < 0 || index >= equipmentOwned.Count) return false;
        equipmentOwned.RemoveAt(index);
        MarkDirty();
        return true;
    }

    // Reorders the backpack grid (drag-drop between slots).
    public void MoveEquipment(int from, int to)
    {
        if (from < 0 || from >= equipmentOwned.Count) return;
        if (from == to) return;
        var id = equipmentOwned[from];
        equipmentOwned.RemoveAt(from);
        equipmentOwned.Insert(Mathf.Clamp(to, 0, equipmentOwned.Count), id);
        MarkDirty();
    }

    public IReadOnlyList<string> EquipmentOwned => equipmentOwned;

    // ── Dabloons ──────────────────────────────────────────────────

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

    // ── Clear helpers (DEV / reset flows) ────────────────────────

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
        equipmentOwned.Clear();
        MarkDirty();
    }

    public void ClearHotbar()
    {
        for (int i = 0; i < hotbarSlots.Length; i++) hotbarSlots[i] = null;
        MarkDirty();
    }

    // ── Hotbar ────────────────────────────────────────────────────

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

    // ── Persistence (mirror of CreatureRegistrySO.GetAll / LoadFrom) ──

    // Flat DTO the SaveSystem (de)serializes to JSON. The SO is the runtime truth;
    // this is the on-disk shape.
    [Serializable]
    public class InventoryData
    {
        public List<string> FurnitureOwned   = new List<string>();
        public List<string> WorldPropsStored = new List<string>();
        public List<string> EquipmentOwned   = new List<string>();
        public string[]      HotbarSlots      = new string[HotbarSize];
        public int           Dabloons         = 0;
    }

    public InventoryData GetData() => new InventoryData
    {
        FurnitureOwned   = new List<string>(furnitureOwned),
        WorldPropsStored = new List<string>(worldPropsStored),
        EquipmentOwned   = new List<string>(equipmentOwned),
        HotbarSlots      = (string[])hotbarSlots.Clone(),
        Dabloons         = dabloons,
    };

    public void LoadFrom(InventoryData data)
    {
        furnitureOwned   = data?.FurnitureOwned   ?? new List<string>();
        worldPropsStored = data?.WorldPropsStored ?? new List<string>();
        equipmentOwned   = data?.EquipmentOwned   ?? new List<string>();
        hotbarSlots      = NormalizeHotbar(data?.HotbarSlots);
        dabloons         = data?.Dabloons ?? 0;
        MarkDirty();
    }

    // Guards against a save written when HotbarSize differed (length mismatch).
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
