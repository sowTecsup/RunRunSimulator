using UnityEngine;

[CreateAssetMenu(fileName = "EyeDatabase", menuName = "RunRunSimulator/Databases/Eye Database")]
public class EyeDatabaseSO : PartDatabaseSO<EyePart>
{
    protected override string IDPrefix => "E";
}
