using UnityEngine;

[CreateAssetMenu(fileName = "EyePart", menuName = "RunRunSimulator/Parts/Eye")]
public class EyePart : BodyPart
{
    public override PartRole GetPartRole() => PartRole.Eye;
}
