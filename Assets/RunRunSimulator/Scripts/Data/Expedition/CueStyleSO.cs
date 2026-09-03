using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "CueStyle", menuName = "RunRunSimulator/Expedition/Cue Style")]
public class CueStyleSO : SerializedScriptableObject
{
    [Title("Intención")]
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Intent", ValueLabel = "Color")]
    private Dictionary<CreatureIntent, Color> intentColors = new Dictionary<CreatureIntent, Color>();

    public Color DefaultIntentColor = new Color(0.6f, 0.6f, 0.6f);

    [Title("Aparición")]
    public float AppearSeconds = 0.25f;
    public float AppearScale = 0.85f;

    [Title("Geometría")]
    public float HeightOffset = 0.03f;
    public float RingThickness = 0.06f;
    [Range(0f, 1f)] public float RingAlpha = 0.35f;
    public float PathThickness = 0.08f;
    public float HeadLength = 0.5f;
    public float HeadWidth = 0.4f;
    public float PerceptThickness = 0.03f;

    [Title("Percepción")]
    public Color FriendColor = new Color(0.35f, 0.9f, 0.35f);
    public Color FoeColor = new Color(0.9f, 0.25f, 0.25f);
    [Range(0f, 1f)] public float PerceptAlpha = 0.6f;
    [Range(0f, 180f)] public float AttentionArcDegrees = 50f;
    [Range(0f, 1f)] public float AttentionAlpha = 0.9f;
    public float PulseSeconds = 0.35f;
    public float PulseAmount = 0.05f;

    [Title("Percibidos")]
    public float PerceptDashLength = 0.2f;
    public float PerceptDashGap = 0.2f;
    public float PerceptFlowSpeed = 1f;
    [Range(0f, 1f)] public float PerceptFarAlpha = 0.1f;

    [Title("Anillo de percepción")]
    [Min(4)] public int RingDashCount = 28;
    [Range(0f, 1f)] public float RingDashRatio = 0.55f;
    public float RingSpinSpeed = 0.35f;

    [Title("Retícula")]
    public float ReticleRadius = 0.9f;
    public float ReticleThickness = 0.06f;
    [Range(0f, 180f)] public float ReticleSweepDegrees = 50f;
    public float ReticleSpinSpeed = -0.6f;
    public float ReticleAppearScale = 1.4f;

    [Title("Ruta")]
    public float PathFadeSeconds = 0.35f;
    public float PathSmoothing = 8f;
    [Range(2, 24)] public int CurveSamples = 10;
    public float StartTangent = 1.2f;
    public float PathFlowSpeed = 1.5f;
    public float PathDashLength = 0.35f;
    public float PathDashGap = 0.25f;
    [Range(0f, 1f)] public float PathTailAlpha = 0.15f;
    public float DestMarkerRadius = 0.35f;
    public float DestPulseSpeed = 2.5f;
    [Range(0f, 1f)] public float DestPulseAmount = 0.15f;

    [Title("Minerales")]
    public Color MineralColor = Color.cyan;
    public float MineralDiscRadius = 0.6f;
    [Range(0f, 1f)] public float MineralInnerAlpha = 0.35f;
    [Range(0f, 1f)] public float MineralOuterAlpha = 0f;
    public float MineralRingThickness = 0.04f;
    [Range(0f, 1f)] public float MineralRingAlpha = 0.5f;

    [Title("Social")]
    public Color SocialLinkColor = new Color(0.95f, 0.5f, 0.8f);
    public Color FightColor = new Color(0.9f, 0.15f, 0.15f);
    public float SocialLinkThickness = 0.05f;
    public float FightPulseSpeed = 6f;

    public Color ColorFor(CreatureIntent intent) =>
        intentColors != null && intentColors.TryGetValue(intent, out var color) ? color : DefaultIntentColor;

    [Button("Populate Defaults", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f)]
    public void PopulateDefaults()
    {
        if (intentColors == null) intentColors = new Dictionary<CreatureIntent, Color>();

        AddIfMissing(CreatureIntent.Idle, new Color(0.75f, 0.75f, 0.75f));
        AddIfMissing(CreatureIntent.Wandering, new Color(0.75f, 0.75f, 0.75f));
        AddIfMissing(CreatureIntent.Following, new Color(0.3f, 0.85f, 0.3f));
        AddIfMissing(CreatureIntent.Approaching, new Color(0.3f, 0.85f, 0.3f));
        AddIfMissing(CreatureIntent.Fleeing, new Color(0.9f, 0.2f, 0.2f));
        AddIfMissing(CreatureIntent.Retreating, new Color(0.9f, 0.2f, 0.2f));
        AddIfMissing(CreatureIntent.Chasing, new Color(1f, 0.55f, 0.1f));
        AddIfMissing(CreatureIntent.SeekingFood, new Color(0.95f, 0.85f, 0.15f));
        AddIfMissing(CreatureIntent.Eating, new Color(0.95f, 0.85f, 0.15f));
        AddIfMissing(CreatureIntent.SeekingRest, new Color(0.25f, 0.5f, 0.95f));
        AddIfMissing(CreatureIntent.Resting, new Color(0.25f, 0.5f, 0.95f));
        AddIfMissing(CreatureIntent.SleepingTogether, new Color(0.25f, 0.5f, 0.95f));
        AddIfMissing(CreatureIntent.SeekingPlay, new Color(0.95f, 0.5f, 0.8f));
        AddIfMissing(CreatureIntent.Playing, new Color(0.95f, 0.5f, 0.8f));
        AddIfMissing(CreatureIntent.Socializing, new Color(0.95f, 0.5f, 0.8f));
        AddIfMissing(CreatureIntent.Fighting, new Color(0.55f, 0.05f, 0.05f));
        AddIfMissing(CreatureIntent.Held, Color.white);
        AddIfMissing(CreatureIntent.Tumbling, Color.white);
        AddIfMissing(CreatureIntent.Collecting, Color.cyan);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void AddIfMissing(CreatureIntent intent, Color color)
    {
        if (!intentColors.ContainsKey(intent)) intentColors.Add(intent, color);
    }
}
}
