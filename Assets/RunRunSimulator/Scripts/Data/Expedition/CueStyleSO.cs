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
    [Range(0f, 1f)] public float GuideAlpha = 0.5f;

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

    [Title("Cono de visión")]
    [Range(0f, 1f)] public float VisionFillInnerAlpha = 0.09f;
    [Range(0f, 1f)] public float VisionFillOuterAlpha = 0f;
    [Range(0f, 1f)] public float VisionEdgeAlpha = 0.5f;
    [Range(0f, 1f)] public float VisionSideAlpha = 0.3f;
    [Range(0f, 1f)] public float NearRingAlpha = 0.22f;
    public float VisionTurnSmoothing = 9f;

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

    [Title("Salidas y minado")]
    [Range(0f, 1f)] public float ExitAlpha = 0.22f;
    public float ExitRingThickness = 0.08f;
    public float MiningArcRadius = 1.1f;
    public float MiningArcThickness = 0.1f;
    [Range(0f, 1f)] public float MiningArcAlpha = 0.9f;
    [Range(0f, 1f)] public float DestPulseAmount = 0.15f;

    [Title("Minerales")]
    public Color MineralColor = Color.cyan;
    public float MineralDiscRadius = 0.6f;
    [Range(0f, 1f)] public float MineralInnerAlpha = 0.35f;
    [Range(0f, 1f)] public float MineralOuterAlpha = 0f;
    public float MineralRingThickness = 0.04f;
    [Range(0f, 1f)] public float MineralRingAlpha = 0.5f;

    [Title("Pizarrón")]
    [Range(0f, 1f)] public float KnownVeinRingAlpha = 0.45f;
    public float KnownVeinRingThickness = 0.05f;
    public float KnownVeinRingOffset = 0.35f;
    public float PingSeconds = 1.4f;
    public float PingRadius = 2.6f;
    [Range(0f, 1f)] public float PingAlpha = 0.8f;
    public float PingThickness = 0.08f;

    [Title("Órdenes")]
    public Color FleeColor = new Color(1f, 0.85f, 0.2f);
    [Range(0f, 1f)] public float ContactFillAlpha = 0.12f;
    [Range(0f, 1f)] public float ContactEdgeAlpha = 0.9f;
    public float FleeRingRadius = 1.3f;
    public float FleeRingThickness = 0.08f;
    public float FleePulseSpeed = 8f;

    [Title("Base y descubrimiento")]
    public float BaseRadius = 0.9f;
    [Range(0f, 1f)] public float BaseInnerAlpha = 0.35f;
    public float BaseRingThickness = 0.05f;
    [Range(0f, 1f)] public float BaseRingAlpha = 0.6f;
    public float RevealSeconds = 0.3f;
    [Min(4)] public int ConeDashCount = 48;
    [Range(0f, 1f)] public float ConeDashRatio = 0.55f;
    public float ConeDashSpinSpeed = 0.15f;

    [Title("Habilidades")]
    public float AbilityBurstSeconds = 0.6f;
    public float AbilityBurstRadiusFrom = 0.8f;
    public float AbilityBurstRadiusTo = 2.2f;
    public float AbilityBurstThickness = 0.1f;
    [Range(0f, 1f)] public float AbilityBurstAlpha = 0.9f;

    [Title("Telegrafía")]
    [Range(0f, 1f)] public float TelegraphEdgeAlpha = 0.85f;
    public float TelegraphEdgeThickness = 0.08f;
    [Range(0f, 1f)] public float TelegraphTrackAlpha = 0.1f;
    [Range(0f, 1f)] public float TelegraphFillAlpha = 0.3f;
    [Range(0f, 1f)] public float TelegraphFillOuterAlpha = 0.08f;
    [Min(1f)] public float TelegraphRingScale = 2f;
    public float TelegraphPulseSpeed = 6f;
    [Range(0f, 1f)] public float TelegraphPulseAmount = 0.06f;
    public float TelegraphFadeSeconds = 0.15f;
    public float TelegraphBlinkSpeed = 2.5f;
    public float TelegraphBlinkSpeedEnd = 8f;
    [Range(0f, 1f)] public float TelegraphBlinkMin = 0.3f;

    [Title("Flecha de la picada")]
    public float DiveArcWidth = 0.1f;
    [Range(0f, 1f)] public float DiveArcTailScale = 0.35f;
    public float DiveArcHeadWidth = 0.34f;
    public float DiveArcHeadLength = 0.55f;
    [Range(4, 64)] public int DiveArcSamples = 28;
    public float DiveArcDashLength = 0.45f;
    public float DiveArcDashGap = 0.22f;
    public float DiveArcFlowSpeed = 2.5f;
    [Range(0f, 1f)] public float DiveArcTailAlpha = 0.08f;
    public float DiveArcStartHeight = 0.55f;
    [Min(0.1f)] public float DiveArcHeightScale = 1.3f;

    [Title("Selección")]
    public Color SelectColor = new Color(1f, 0.78f, 0.3f);
    public float SelectRadius = 1.5f;
    public float SelectThickness = 0.08f;
    [Min(4)] public int SelectDashCount = 12;
    [Range(0f, 1f)] public float SelectDashRatio = 0.6f;
    public float SelectSpinSpeed = 0.5f;
    public float SelectAppearScale = 1.4f;
    [Range(0f, 1f)] public float SelectGlowAlpha = 0.08f;
    public float SelectPulseSpeed = 3f;
    [Range(0f, 1f)] public float SelectPulseAmount = 0.06f;

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
        AddIfMissing(CreatureIntent.Taking, new Color(0.4f, 1f, 0.9f));
        AddIfMissing(CreatureIntent.Losing, new Color(0.6f, 0.62f, 0.72f));
        AddIfMissing(CreatureIntent.Clashing, new Color(1f, 0.45f, 0.15f));
        AddIfMissing(CreatureIntent.Dazed, new Color(0.75f, 0.6f, 0.95f));
        AddIfMissing(CreatureIntent.Carrying, new Color(1f, 0.8f, 0.25f));
        AddIfMissing(CreatureIntent.Securing, new Color(1f, 0.92f, 0.45f));
        AddIfMissing(CreatureIntent.Guarding, new Color(0.45f, 0.65f, 0.95f));
        AddIfMissing(CreatureIntent.Hunting, new Color(0.9f, 0.3f, 0.1f));
        AddIfMissing(CreatureIntent.Taunting, new Color(0.95f, 0.3f, 0.75f));
        AddIfMissing(CreatureIntent.Exploring, new Color(0.55f, 0.9f, 0.6f));
        AddIfMissing(CreatureIntent.Reporting, new Color(0.75f, 1f, 0.45f));

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
