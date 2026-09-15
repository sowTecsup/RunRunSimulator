using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public enum AbilityKind { Damage = 0, Mobility = 1, Passive = 2 }

public enum AbilityRole { Basic, Super }

public enum AbilityChargeSource { Hit, Mined, Secured }

[System.Flags]
public enum AbilityTrigger { None = 0, RivalInReach = 1, Fleeing = 2, Chasing = 4 }

[CreateAssetMenu(fileName = "Ability", menuName = "RunRunSimulator/Expedition/Ability")]
public class AbilitySO : ScriptableObject
{
    public string Name = "";
    [TextArea] public string Description = "";
    public ClashSlot Slot = ClashSlot.Horn;
    public AbilityKind Kind = AbilityKind.Damage;
    public AbilityRole Role = AbilityRole.Basic;
    [Min(0f)] public float Cooldown = 8f;
    [EnumToggleButtons] public AbilityTrigger Trigger = AbilityTrigger.RivalInReach;
    public Color Color = new Color(1f, 0.6f, 0.2f);

    [Title("Daño")]
    public ClashMoveSO Move;
    [Min(0f)] public float MinDistance = 0f;
    [Min(0)] public int MinRivalsNearby = 0;

    [Title("Carga (Super)")]
    [Range(0f, 1f)] public float ChargeOnHit = 0.34f;
    [Range(0f, 1f)] public float ChargeOnMined = 0.08f;
    [Range(0f, 1f)] public float ChargeOnSecured = 0.25f;

    [Title("Movilidad")]
    [Min(1f)] public float SpeedMultiplier = 1.35f;
    [Min(0f)] public float BoostSeconds = 3f;

    [Title("Pasiva / costo")]
    [Min(0)] public int CarryCapacity = 0;
    [Min(0f)] public float LoadedSpeedFactor = 1f;
    public bool KeepCarryOnKnock = false;
    [Min(0f)] public float GuardRadius = 0f;
    [Min(0f)] public float VisibleFrom = 0f;

    [Title("Partes que la otorgan")]
    public List<string> PartIds = new List<string>();

    public bool Triggers(AbilityTrigger t) => (Trigger & t) != 0;

    public float ChargeFor(AbilityChargeSource s)
    {
        switch (s)
        {
            case AbilityChargeSource.Hit: return ChargeOnHit;
            case AbilityChargeSource.Mined: return ChargeOnMined;
            case AbilityChargeSource.Secured: return ChargeOnSecured;
            default: return 0f;
        }
    }
}
}
