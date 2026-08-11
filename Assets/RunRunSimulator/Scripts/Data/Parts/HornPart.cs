using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "HornPart", menuName = "RunRunSimulator/Parts/Horn")]
public class HornPart : BodyPart
{
    public override PartRole GetPartRole() => PartRole.Horn;
}
}
