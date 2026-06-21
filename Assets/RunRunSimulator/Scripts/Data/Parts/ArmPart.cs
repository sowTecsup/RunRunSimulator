using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "ArmPart", menuName = "RunRunSimulator/Parts/Arm")]
public class ArmPart : BodyPart
{
    public override PartRole GetPartRole() => PartRole.Arm;
}
}
