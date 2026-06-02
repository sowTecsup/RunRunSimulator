using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

// Data-driven tuning for each Personality. The MoriMochiAgent reads its profile
// here instead of hardcoding behavior per archetype — this SO is the single place
// to balance how every personality moves and reacts, and the reserved "endpoint"
// for making personalities matter to other systems later (combat, breeding).
//
// Singleton (Current) set in OnEnable, same pattern as CombatManagerSO /
// InheritanceOddsTableSO. Press "Populate Defaults" once to fill all six.
[CreateAssetMenu(fileName = "PersonalityProfileTable", menuName = "RunRunSimulator/Personality Profile Table")]
public class PersonalityProfileSO : SerializedScriptableObject
{
    public static PersonalityProfileSO Current { get; private set; }
    private void OnEnable() => Current = this;

    [Title("Per-Personality Profiles")]
    [InfoBox("Una entrada por Personality. Si falta alguna, GetProfile devuelve un perfil neutral seguro.")]
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Personality", ValueLabel = "Profile")]
    private Dictionary<Personality, PersonalityProfile> profiles = new Dictionary<Personality, PersonalityProfile>();

    // Returns the tuning for a personality, or a neutral fallback if unconfigured.
    public PersonalityProfile GetProfile(Personality p) =>
        profiles != null && profiles.TryGetValue(p, out var prof) && prof != null
            ? prof : PersonalityProfile.Neutral();

    [Button("Populate Defaults", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f)]
    private void PopulateDefaults()
    {
        profiles = new Dictionary<Personality, PersonalityProfile>
        {
            { Personality.Skittish,   Make(3.6f, 0.35f, 0.4f, 6f, 9f, ProximityReaction.Flee,     5.0f, WorldArea.Storage,       0.80f, 1.8f, new Color(0.55f, 0.85f, 1.00f)) },
            { Personality.Aggressive, Make(2.6f, 0.25f, 0.6f, 4f, 7f, ProximityReaction.Approach, 1.5f, WorldArea.ShopFrontDesk, 0.60f, 1.4f, new Color(1.00f, 0.30f, 0.25f)) },
            { Personality.Lazy,       Make(1.1f, 0.80f, 2.0f, 5f, 4f, ProximityReaction.Ignore,   3.0f, WorldArea.ShopBackroom,  0.70f, 0.5f, new Color(0.80f, 0.75f, 0.30f)) },
            { Personality.Curious,    Make(2.4f, 0.20f, 0.8f, 8f, 9f, ProximityReaction.Approach, 2.0f, WorldArea.ShopBackroom,  0.20f, 1.2f, new Color(0.40f, 0.90f, 0.50f)) },
            { Personality.Social,     Make(2.8f, 0.25f, 0.7f, 4f, 9f, ProximityReaction.Follow,   2.2f, WorldArea.ShopFrontDesk, 0.50f, 1.3f, new Color(1.00f, 0.50f, 0.85f)) },
            { Personality.Grumpy,     Make(1.8f, 0.55f, 1.5f, 3f, 6f, ProximityReaction.Retreat,  4.0f, WorldArea.Storage,       0.75f, 0.8f, new Color(0.55f, 0.45f, 0.70f)) },
        };
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private static PersonalityProfile Make(
        float moveSpeed, float idleChance, float idleSeconds, float roamRadius,
        float proximityRadius, ProximityReaction reaction, float followDistance,
        WorldArea area, float areaPreference, float recoverySpeed, Color tint) => new PersonalityProfile
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

// Plain serializable tuning block — one per personality. Kept as fields (not
// properties) so Unity/Odin serializes it inline in the dictionary.
[System.Serializable]
public class PersonalityProfile
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
    [LabelWidth(150)] public Color             Tint            = Color.white;  // debug/visible body color per personality

    public static PersonalityProfile Neutral() => new PersonalityProfile();
}
