using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "WingDatabase", menuName = "RunRunSimulator/Databases/Wing Database")]
public class WingDatabaseSO : PartDatabaseSO<WingPart>
{
    protected override string IDPrefix => "W";
}
}
