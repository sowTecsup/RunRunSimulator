using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(menuName = "RunRunSimulator/Expedition/Care Gate")]
public class CareGateSO : ScriptableObject
{
    [SerializeField] private float minHealth = 60f;
    [SerializeField] private float minEnergy = 60f;
    [SerializeField] private float minAffect = 0f;

    public float MinHealth => minHealth;
    public float MinEnergy => minEnergy;
    public float MinAffect => minAffect;
}
}
