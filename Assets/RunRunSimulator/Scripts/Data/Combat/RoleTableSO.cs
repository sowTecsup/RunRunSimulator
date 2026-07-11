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
    public float ShieldPerTurn;
    public float BacklineHitChance;
    public float HealPercentOfDamage;
    public float PriceModifier;
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

    [Button("Poblar v1 (Protector/Agresivo/Empático)")]
    private void PopulateV1()
    {
        Profiles ??= new Dictionary<Role, RoleProfile>();

        Profiles[Role.Protector] = new RoleProfile
        {
            ConMod = 4f,
            AtkMod = -2f,
            SpdMod = -2f,
            ShieldPerTurn = 1f,
            BacklineHitChance = 0f,
            HealPercentOfDamage = 0f,
            PriceModifier = 0f,
        };

        Profiles[Role.Agresivo] = new RoleProfile
        {
            ConMod = -3f,
            AtkMod = 2f,
            SpdMod = 1f,
            ShieldPerTurn = 0f,
            BacklineHitChance = 0.5f,
            HealPercentOfDamage = 0f,
            PriceModifier = -0.10f,
        };

        Profiles[Role.Empatico] = new RoleProfile
        {
            ConMod = 1f,
            AtkMod = -3f,
            SpdMod = 2f,
            ShieldPerTurn = 0f,
            BacklineHitChance = 0f,
            HealPercentOfDamage = 0.5f,
            PriceModifier = 0.10f,
        };

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
