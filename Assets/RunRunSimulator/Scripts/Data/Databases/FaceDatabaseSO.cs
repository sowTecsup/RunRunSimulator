using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "FaceDatabase", menuName = "RunRunSimulator/Databases/Face Database")]
public class FaceDatabaseSO : PartDatabaseSO<FacePart>
{
    protected override string IDPrefix => "FC";
}
}
