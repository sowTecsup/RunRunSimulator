using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "MouthPart", menuName = "RunRunSimulator/Parts/Mouth")]
public class MouthPart : BodyPart
{
    [Title("Mouth Properties")]
    public TeethType TeethType;
    public bool HasTentacles;
}
