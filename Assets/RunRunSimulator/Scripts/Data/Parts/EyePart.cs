using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "EyePart", menuName = "RunRunSimulator/Parts/Eye")]
public class EyePart : BodyPart
{
    [Title("Eye Properties")]
    public bool IsCompound;
    [Range(1, 8)] public int EyeCount = 2;
}
