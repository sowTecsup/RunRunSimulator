using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "FacePart", menuName = "RunRunSimulator/Parts/Face")]
public class FacePart : BodyPart
{
    public override PartRole GetPartRole() => PartRole.Face;
}
}
