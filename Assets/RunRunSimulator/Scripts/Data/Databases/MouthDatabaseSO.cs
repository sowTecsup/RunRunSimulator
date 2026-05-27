using UnityEngine;

[CreateAssetMenu(fileName = "MouthDatabase", menuName = "RunRunSimulator/Databases/Mouth Database")]
public class MouthDatabaseSO : PartDatabaseSO<MouthPart>
{
    protected override string IDPrefix => "M";
}
