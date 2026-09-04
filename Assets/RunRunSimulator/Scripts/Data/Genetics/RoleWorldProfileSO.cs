using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "RoleWorldProfileTable", menuName = "RunRunSimulator/Genetics/Role World Profile Table")]
public class RoleWorldProfileSO : SerializedScriptableObject
{
    [Title("Per-Role Profiles")]
    [InfoBox("Una entrada por Role. Si falta alguna, GetProfile devuelve un perfil neutral seguro.")]
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Role", ValueLabel = "Profile")]
    private Dictionary<Role, RoleWorldProfile> profiles = new Dictionary<Role, RoleWorldProfile>();

    public RoleWorldProfile GetProfile(Role r) =>
        profiles != null && profiles.TryGetValue(r, out var prof) && prof != null
            ? prof : RoleWorldProfile.Neutral();

    [Button("Populate Defaults", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f)]
    private void PopulateDefaults()
    {
        profiles = new Dictionary<Role, RoleWorldProfile>
        {
            { Role.Protector, Make(1.8f, 0.55f, 1.5f, 4f, 6f, ProximityReaction.Ignore,  3.0f, WorldArea.Storage,       0.75f, 0.8f, new Color(0.45f, 0.65f, 1.00f)) },
            { Role.Agresivo,  Make(2.6f, 0.25f, 0.6f, 4f, 7f, ProximityReaction.Approach, 1.5f, WorldArea.ShopFrontDesk, 0.60f, 1.4f, new Color(1.00f, 0.30f, 0.25f)) },
            { Role.Empatico,  Make(2.8f, 0.25f, 0.7f, 5f, 9f, ProximityReaction.Follow,   2.2f, WorldArea.ShopFrontDesk, 0.50f, 1.3f, new Color(1.00f, 0.50f, 0.85f)) },
        };

        profiles[Role.Protector].RoamSpeedFactor = 0.35f;
        profiles[Role.Agresivo].RoamSpeedFactor  = 0.35f;
        profiles[Role.Empatico].RoamSpeedFactor  = 0.35f;

        profiles[Role.Protector].Reactions = new List<ReactionRuleBase>
        {
            new ApproachFriendRule { MinAffinity = 0.3f },
            new AvoidDislikedRule { MaxAffinity = -0.35f },
        };
        profiles[Role.Agresivo].Reactions = new List<ReactionRuleBase>
        {
            new PlayChaseRule { MinAffinity = 0.25f, Cooldown = 20f },
            new AvoidDislikedRule { MaxAffinity = -0.5f },
        };
        profiles[Role.Empatico].Reactions = new List<ReactionRuleBase>
        {
            new ApproachFriendRule { MinAffinity = 0.15f },
            new PlayChaseRule { MinAffinity = 0.35f },
            new AvoidDislikedRule { MaxAffinity = -0.6f },
        };
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private static RoleWorldProfile Make(
        float moveSpeed, float idleChance, float idleSeconds, float roamRadius,
        float proximityRadius, ProximityReaction reaction, float followDistance,
        WorldArea area, float areaPreference, float recoverySpeed, Color tint) => new RoleWorldProfile
    {
        MoveSpeed       = moveSpeed,
        IdleChance      = idleChance,
        IdleMin         = idleSeconds,
        IdleMax         = idleSeconds + 1f,
        RoamRadius      = roamRadius,
        ProximityRadius = proximityRadius,
        Reaction        = reaction,
        FollowDistance  = followDistance,
        PreferredArea   = area,
        AreaPreference  = areaPreference,
        RecoverySpeed   = recoverySpeed,
        Tint            = tint,
    };
}

[System.Serializable]
public class RoleWorldProfile
{
    [LabelWidth(150)] public float             MoveSpeed       = 2.5f;
    [LabelWidth(150)] [Range(0.2f, 1f)]
    public float                               RoamSpeedFactor = 1f;
    [LabelWidth(150)] public float             IdleChance      = 0.3f;
    [LabelWidth(150)] public float             IdleMin         = 0.5f;
    [LabelWidth(150)] public float             IdleMax         = 1.5f;
    [LabelWidth(150)] public float             RoamRadius      = 4f;
    [LabelWidth(150)] public float             ProximityRadius = 6f;
    [LabelWidth(150)] public ProximityReaction Reaction        = ProximityReaction.Ignore;
    [LabelWidth(150)] public float             FollowDistance  = 2f;
    [LabelWidth(150)] public WorldArea         PreferredArea   = WorldArea.ShopBackroom;
    [LabelWidth(150)] [Range(0f, 1f)]
    public float                               AreaPreference  = 0.5f;
    [LabelWidth(150)] public float             RecoverySpeed   = 1f;
    [LabelWidth(150)] public Color             Tint            = Color.white;
    [LabelWidth(150)] public List<ReactionRuleBase> Reactions   = new List<ReactionRuleBase>();

    public static RoleWorldProfile Neutral() => new RoleWorldProfile();
}
}
