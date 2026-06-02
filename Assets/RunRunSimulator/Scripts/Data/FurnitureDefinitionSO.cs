using Sirenix.OdinInspector;
using UnityEngine;

// Catalog entry: one kind of furniture the player can own and place — the furniture
// analogue of a BodyPart in the creature DB. The shop lists these; placement reads
// the footprint; the spawner instantiates the prefab.
[CreateAssetMenu(fileName = "FurnitureDef", menuName = "RunRunSimulator/Furniture Definition")]
public class FurnitureDefinitionSO : ScriptableObject
{
    [Title("Identity")]
    [Tooltip("Stable id used in saves/network. Must NOT contain '-'.")]
    public string Id = "";
    public string DisplayName = "";
    [Required, AssetsOnly] public GameObject Prefab;

    [Title("Placement")]
    [Tooltip("Size in grid cells, before rotation (e.g. 1x1 cube, 2x1 shelf).")]
    [MinValue(1)] public Vector2Int Footprint = Vector2Int.one;

    [Title("Shop")]
    [MinValue(0)] public int Price = 0;
    public FurnitureCategory Category = FurnitureCategory.Decoration;
}
