using Sirenix.OdinInspector;
using UnityEngine;

// Catalog entry for a WORLD PROP: a tangible object the player can hold, use, drop
// and store (broom, food, medicine). Furniture is NOT modelled here — it lives in
// FurnitureDefinitionSO and is acquired straight into the inventory, never as a
// physical delivery. Keeping this WorldProp-only removes the old furniture/world-prop
// bridge: the two item worlds work differently and no longer share one definition.
//
// ID namespacing: this def's Id is "I#" (dictated by ItemDatabaseSO's dictionary key).
// Furniture uses the separate "F#" space — the two never collide. See PlayerInventorySO.
[CreateAssetMenu(fileName = "ItemDef", menuName = "RunRunSimulator/Item Definition")]
[InlineEditor]
public class ItemDefinitionSO : SerializedScriptableObject
{
    [Title("Identity")]
    // Dictated by the database slot (its dictionary key), never authored here —
    // ItemDatabaseSO.SyncIds() stamps it. ReadOnly so it can't drift from the key.
    [ReadOnly] public string Id;
    public string DisplayName = "";
    public WorldPropCategory Category = WorldPropCategory.Tool;

    [Title("World Prop")]
    // The 3D object spawned into the world (from a DeliveryBox or ejected from
    // storage). Carries a WorldPropInstance so it can be picked up / auto-collected.
    [Required, AssetsOnly]
    public GameObject Prefab;
}
