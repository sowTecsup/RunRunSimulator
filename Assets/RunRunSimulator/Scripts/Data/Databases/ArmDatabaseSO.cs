using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "ArmDatabase", menuName = "RunRunSimulator/Databases/Arm Database")]
public class ArmDatabaseSO : PartDatabaseSO<ArmPart>
{
    protected override string IDPrefix => "A";
}
}
