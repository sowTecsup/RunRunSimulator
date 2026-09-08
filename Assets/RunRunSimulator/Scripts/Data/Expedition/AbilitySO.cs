using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public enum AbilityKind { Damage = 0, Mobility = 1 }

[System.Flags]
public enum AbilityTrigger { None = 0, RivalInReach = 1, Fleeing = 2, Chasing = 4 }

[CreateAssetMenu(fileName = "Ability", menuName = "RunRunSimulator/Expedition/Ability")]
public class AbilitySO : ScriptableObject
{
    public string Name = "";
    [TextArea] public string Description = "";
    public ClashSlot Slot = ClashSlot.Horn;
    public AbilityKind Kind = AbilityKind.Damage;
    [Min(0f)] public float Cooldown = 8f;
    [EnumToggleButtons] public AbilityTrigger Trigger = AbilityTrigger.RivalInReach;
    public Color Color = new Color(1f, 0.6f, 0.2f);

    [Title("Daño")]
    public ClashMoveSO Move;
    [Min(0f)] public float MinDistance = 0f;
    [Min(0)] public int MinRivalsNearby = 0;

    [Title("Movilidad")]
    [Min(1f)] public float SpeedMultiplier = 1.35f;
    [Min(0f)] public float BoostSeconds = 3f;

    public bool Triggers(AbilityTrigger t) => (Trigger & t) != 0;
}
}
