using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "CutieMark", menuName = "RunRunSimulator/Cutie Marks/Cutie Mark")]
public class CutieMarkSO : SerializedScriptableObject
{
    [ReadOnly] public string Id;
    public string DisplayName = "";
    [TextArea] public string Description = "";
    public Sprite Icon;
    public int CostAdventureMaterial = 0;
    public int CostPassiveMaterial = 0;
}
}
