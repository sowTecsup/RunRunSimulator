using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[Serializable]
public class RoleProfile
{
    public float ConMod;
    public float AtkMod;
    public float SpdMod;
    public float PriceModifier;
    public List<RolePassiveBase> Passives = new List<RolePassiveBase>();
    public List<RoleActiveBase> Actives = new List<RoleActiveBase>();
}

[CreateAssetMenu(fileName = "RoleTable", menuName = "RunRunSimulator/Combat/Role Table")]
public class RoleTableSO : SerializedScriptableObject
{
    [Title("Role Profiles")]
    [OdinSerialize]
    public Dictionary<Role, RoleProfile> Profiles = new Dictionary<Role, RoleProfile>();

    public RoleProfile GetProfile(Role role)
    {
        if (Profiles != null && Profiles.TryGetValue(role, out var profile) && profile != null)
            return profile;

        return new RoleProfile();
    }

    [Button("Poblar v2 (stats + pasivas/activas)")]
    private void PopulateV2()
    {
        Profiles ??= new Dictionary<Role, RoleProfile>();

        Profiles[Role.Protector] = new RoleProfile
        {
            ConMod = 4f,
            AtkMod = -2f,
            SpdMod = -2f,
            PriceModifier = 0f,
            Passives = new List<RolePassiveBase> { new ShieldAllyPassive { AmountPerTurn = 1f } },
            Actives = new List<RoleActiveBase>(),
        };

        Profiles[Role.Agresivo] = new RoleProfile
        {
            ConMod = -3f,
            AtkMod = 2f,
            SpdMod = 1f,
            PriceModifier = -0.10f,
            Passives = new List<RolePassiveBase> { new MarkRandomAllyPassive() },
            Actives = new List<RoleActiveBase> { new BacklineHunterActive { Chance = 0.5f } },
        };

        Profiles[Role.Empatico] = new RoleProfile
        {
            ConMod = 1f,
            AtkMod = -3f,
            SpdMod = 2f,
            PriceModifier = 0.10f,
            Passives = new List<RolePassiveBase> { new HealLowestAllyOnHitPassive { PercentOfDamage = 0.5f } },
            Actives = new List<RoleActiveBase>(),
        };

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
