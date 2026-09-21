using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "CreatureBox", menuName = "RunRunSimulator/Store/Creature Box")]
public class CreatureBoxSO : SerializedScriptableObject
{
    [Title("Identity")]
    public string Id;
    public string DisplayName;

    [TextArea]
    public string Description;

    [Title("Contents")]
    [MinValue(1)] public int Count = 5;
}
}
