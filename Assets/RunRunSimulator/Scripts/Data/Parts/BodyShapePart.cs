using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "BodyShapePart", menuName = "RunRunSimulator/Parts/Body Shape")]
public class BodyShapePart : BodyPart
{
    [Title("Body Properties")]
    public BodyType BodyType;
    [Range(0.5f, 3f)] public float SizeScale = 1f;
}
