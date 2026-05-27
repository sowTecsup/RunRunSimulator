using UnityEngine;

[CreateAssetMenu(fileName = "BodyShapeDatabase", menuName = "RunRunSimulator/Databases/Body Shape Database")]
public class BodyShapeDatabaseSO : PartDatabaseSO<BodyShapePart>
{
    protected override string IDPrefix => "BS";
}
