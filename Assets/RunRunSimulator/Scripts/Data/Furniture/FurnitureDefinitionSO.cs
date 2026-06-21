using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Catalog entry: one kind of furniture the player can own and place — the furniture
// analogue of a BodyPart in the creature DB. The shop lists these; placement reads
// the footprint; the spawner instantiates the prefab.
[CreateAssetMenu(fileName = "FurnitureDef", menuName = "RunRunSimulator/Furniture/Furniture Definition")]
[InlineEditor]
public class FurnitureDefinitionSO : SerializedScriptableObject
{
    [Title("Identity")]
    // The id is dictated by the database SLOT this def lives in (its dictionary key),
    // never authored here — FurnitureDatabaseSO.SyncIds() stamps it. ReadOnly so it
    // can't drift from the key by hand (mirror of BodyPart.ID).
    [ReadOnly] public string Id;
    public string DisplayName = "";
    [Required, AssetsOnly] public GameObject Prefab;

    [Title("Placement")]
    [Tooltip("Size in grid cells, before rotation (e.g. 1x1 cube, 2x1 shelf).")]
    [MinValue(1)] public Vector2Int Footprint = Vector2Int.one;

    [Title("Shop")]
    [MinValue(0)] public int Price = 0;
    public FurnitureCategory Category = FurnitureCategory.Decoration;
}
}
