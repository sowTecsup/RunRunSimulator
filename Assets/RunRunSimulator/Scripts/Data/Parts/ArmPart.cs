using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "ArmPart", menuName = "RunRunSimulator/Parts/Arm")]
public class ArmPart : BodyPart
{
    [Title("Arm Properties")]
    [Range(1, 10)] public int NumberOfFingers = 4;
    public bool HasClaws;
}
