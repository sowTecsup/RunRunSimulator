using Sirenix.OdinInspector;
using UnityEngine;

// Catalog entry for anything the player can acquire (the shop lists these). One
// definition is EITHER furniture or a world prop (ItemType): a furniture item is a
// thin bridge to its FurnitureDefinitionSO (the placement system owns its data),
// while a world prop owns its own 3D prefab directly.
//
// ID namespacing: this def's own Id is "I#" (dictated by ItemDatabaseSO). For a
// furniture item the player's inventory stores the *FurnitureDef's* "F#" id, not
// this one — FurnitureDef is the bridge to that namespace. See PlayerInventorySO.
[CreateAssetMenu(fileName = "ItemDef", menuName = "RunRunSimulator/Item Definition")]
[InlineEditor]
public class ItemDefinitionSO : SerializedScriptableObject
{
    [Title("Identity")]
    // Dictated by the database slot (its dictionary key), never authored here —
    // ItemDatabaseSO.SyncIds() stamps it. ReadOnly so it can't drift from the key.
    [ReadOnly] public string Id;
    public string DisplayName = "";
    public ItemType Type = ItemType.WorldProp;

    [Title("Furniture")]
    // The bridge to the placement system: buying this puts FurnitureDef.Id ("F#")
    // into the inventory's furnitureOwned, and the build browser resolves it there.
    [ShowIf("@Type == ItemType.Furniture")]
    [Required, AssetsOnly]
    public FurnitureDefinitionSO FurnitureDef;

    [Title("World Prop")]
    [ShowIf("@Type == ItemType.WorldProp")]
    public WorldPropCategory Category = WorldPropCategory.Tool;

    // The 3D object spawned into the world (from a DeliveryBox or ejected from
    // storage). Carries a WorldPropInstance so it can be picked up / auto-collected.
    [ShowIf("@Type == ItemType.WorldProp")]
    [Required, AssetsOnly]
    public GameObject Prefab;
}
