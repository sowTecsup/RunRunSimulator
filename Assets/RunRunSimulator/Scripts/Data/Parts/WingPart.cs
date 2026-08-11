using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "WingPart", menuName = "RunRunSimulator/Parts/Wing")]
public class WingPart : BodyPart
{
    public override PartRole GetPartRole() => PartRole.Wing;
}
}
