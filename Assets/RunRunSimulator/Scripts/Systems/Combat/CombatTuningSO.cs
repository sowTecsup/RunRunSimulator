using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "CombatTuning", menuName = "RunRunSimulator/Combat/Tuning")]
public class CombatTuningSO : ScriptableObject
{
    public int CooldownMinutes = 20;
    public int MaterialPerWin = 3;
    public int BudgetTolerance = 1;
    public float MinEnergyToFight = 0f;
}
}
