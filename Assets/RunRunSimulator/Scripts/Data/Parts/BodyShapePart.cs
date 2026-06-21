using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "BodyShapePart", menuName = "RunRunSimulator/Parts/Body Shape")]
public class BodyShapePart : BodyPart
{
    public override PartRole GetPartRole() => PartRole.Body;
}
}
