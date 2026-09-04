using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "ClashTuning", menuName = "RunRunSimulator/Expedition/Clash Tuning")]
public class ClashTuningSO : ScriptableObject
{
    public static ClashTuningSO Current { get; private set; }

    private void OnEnable()
    {
        Current = this;
    }

    [Title("Movimientos por slot")]
    public ClashMoveSO Horn;
    public ClashMoveSO Wings;
    public ClashMoveSO Back;

    [Title("Enganche")]
    [Min(0f)] public float EngageRange = 5f;
    [Range(0f, 1f)] public float MinBoldness = 0.45f;
    [Min(0f)] public float Cooldown = 8f;
    [Min(0f)] public float DiveMinDistance = 4f;
    [Min(1)] public int SweepMinRivals = 2;
    [Min(0f)] public float SweepRange = 2.5f;

    [Title("Después del golpe")]
    [Min(0f)] public float ResolveSeconds = 0.4f;
    [Min(0f)] public float DazedSeconds = 0.7f;
    [Range(0f, 1f)] public float ReengageBoldness = 0.7f;
    [Min(0f)] public float RetreatDistance = 6f;
    [Min(0f)] public float VictimGraceSeconds = 6f;
    [Min(0f)] public float ChainImmunitySeconds = 0.8f;

    public ClashMoveSO MoveFor(ClashSlot slot)
    {
        switch (slot)
        {
            case ClashSlot.Horn: return Horn;
            case ClashSlot.Wings: return Wings;
            case ClashSlot.Back: return Back;
            default: return null;
        }
    }
}
}
