using UnityEngine;

[CreateAssetMenu(fileName = "MouthPart", menuName = "RunRunSimulator/Parts/Mouth")]
public class MouthPart : BodyPart
{
    public override PartRole GetPartRole() => PartRole.Mouth;
}
