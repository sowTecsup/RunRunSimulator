using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Data-driven tuning for each Role. The MoriMochiAgent reads its profile
// here instead of hardcoding behavior per archetype — this SO is the single place
// to balance how every role moves and reacts in the world, and the reserved "endpoint"
// for making roles matter to other systems later (combat, breeding).
//
// Singleton (Current) set in OnEnable, same pattern as CombatManagerSO /
// InheritanceOddsTableSO. Press "Populate Defaults" once to fill all three.
[CreateAssetMenu(fileName = "RoleWorldProfileTable", menuName = "RunRunSimulator/Genetics/Role World Profile Table")]
public class RoleWorldProfileSO : SerializedScriptableObject
{
    [Title("Per-Role Profiles")]
    [InfoBox("Una entrada por Role. Si falta alguna, GetProfile devuelve un perfil neutral seguro.")]
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Role", ValueLabel = "Profile")]
    private Dictionary<Role, RoleWorldProfile> profiles = new Dictionary<Role, RoleWorldProfile>();

    // Returns the tuning for a role, or a neutral fallback if unconfigured.
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

// Plain serializable tuning block — one per role. Kept as fields (not
// properties) so Unity/Odin serializes it inline in the dictionary.
[System.Serializable]
public class RoleWorldProfile
{
    [LabelWidth(150)] public float             MoveSpeed       = 2.5f;   // NavMeshAgent.speed
    [LabelWidth(150)] public float             IdleChance      = 0.3f;   // chance to pause when a waypoint is reached
    [LabelWidth(150)] public float             IdleMin         = 0.5f;   // idle seconds (min)
    [LabelWidth(150)] public float             IdleMax         = 1.5f;   // idle seconds (max)
    [LabelWidth(150)] public float             RoamRadius      = 4f;     // how far the next roam point is sampled
    [LabelWidth(150)] public float             ProximityRadius = 6f;     // player-detection radius
    [LabelWidth(150)] public ProximityReaction Reaction        = ProximityReaction.Ignore;
    [LabelWidth(150)] public float             FollowDistance  = 2f;     // stop distance for Approach/Follow
    [LabelWidth(150)] public WorldArea         PreferredArea   = WorldArea.ShopBackroom;
    [LabelWidth(150)] [Range(0f, 1f)]
    public float                               AreaPreference  = 0.5f;   // odds a roam point heads to PreferredArea (0 = ignores it, fully free; 1 = always homes)
    [LabelWidth(150)] public float             RecoverySpeed   = 1f;     // get-up pace after a throw (>1 faster, <1 groggier)
    [LabelWidth(150)] public Color             Tint            = Color.white;  // debug/visible body color per role
    [LabelWidth(150)] public List<ReactionRuleBase> Reactions   = new List<ReactionRuleBase>();

    public static RoleWorldProfile Neutral() => new RoleWorldProfile();
}
}
