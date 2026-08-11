using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "BackDatabase", menuName = "RunRunSimulator/Databases/Back Database")]
public class BackDatabaseSO : PartDatabaseSO<BackPart>
{
    protected override string IDPrefix => "BK";
}
}
