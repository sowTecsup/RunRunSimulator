using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "BackPart", menuName = "RunRunSimulator/Parts/Back")]
public class BackPart : BodyPart
{
    public override PartRole GetPartRole() => PartRole.Back;
}
}
