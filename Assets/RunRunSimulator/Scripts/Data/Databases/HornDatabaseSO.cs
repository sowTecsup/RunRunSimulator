using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "HornDatabase", menuName = "RunRunSimulator/Databases/Horn Database")]
public class HornDatabaseSO : PartDatabaseSO<HornPart>
{
    protected override string IDPrefix => "H";
}
}
