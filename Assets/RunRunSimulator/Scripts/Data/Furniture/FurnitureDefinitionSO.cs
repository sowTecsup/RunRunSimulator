using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "FurnitureDef", menuName = "RunRunSimulator/Furniture/Furniture Definition")]
[InlineEditor]
public class FurnitureDefinitionSO : SerializedScriptableObject
{
    [Title("Identity")]
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
