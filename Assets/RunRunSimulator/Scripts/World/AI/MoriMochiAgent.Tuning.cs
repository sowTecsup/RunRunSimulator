using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
namespace MoriMonchiSimulator
{

public partial class MoriMochiAgent
{
    // ── Tuning (Odin tabs) ────────────────────────────────────────
    // Grouped to mirror the two concerns this component juggles — the NavMesh "brain"
    // (Movement) and the physics/throwable layer (Physics) — plus Presentation.

    [TabGroup("Tuning", "References"), Title("References")]
    [SerializeField] private NameTag nameTag;
    // ── Movement (NavMesh brain) ──
    [TabGroup("Tuning", "Movement"), Title("NavMesh sampling")]
    [Tooltip("Max distance to snap a desired point onto the NavMesh.")]
    [SerializeField] private float sampleRadius = 4f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("How often (s) Reacting/Roaming recomputes its destination.")]
    [SerializeField] private float repathInterval = 0.35f;

    [TabGroup("Tuning", "Movement"), Title("Player proximity")]
    [Tooltip("Max seconds a friendly reaction (Approach/Follow/Retreat) lasts before the creature resumes its own business.")]
    [SerializeField, Min(1f)] private float followDuration = 10f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("Seconds the creature waits before reacting to the player again after a reaction ends (timer or petting).")]
    [SerializeField, Min(0f)] private float reactCooldown = 15f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("Max distance (m) at which the pet hint appears and petting is valid.")]
    [SerializeField, Min(0.5f)] private float petRadius = 2.5f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("Half-angle of the camera cone (degrees) within which the player must be aiming at this creature for the pet hint to appear. 20° = ±20° from dead-center.")]
    [SerializeField, Range(5f, 60f)] private float petLookAngle = 20f;

    [TabGroup("Tuning", "Movement"), Title("Personality radii (runtime — from PersonalityProfileSO)")]
    [ShowInInspector, ReadOnly] private float ProfileProximityRadius => profile?.ProximityRadius ?? 0f;
    [TabGroup("Tuning", "Movement")]
    [ShowInInspector, ReadOnly] private float ProfileRoamRadius      => profile?.RoamRadius      ?? 0f;
    [TabGroup("Tuning", "Movement")]
    [ShowInInspector, ReadOnly] private float ProfileFollowDistance  => profile?.FollowDistance  ?? 0f;

    [TabGroup("Tuning", "Movement"), Title("Breeding pen confinement")]
    [Tooltip("NavMesh Area that breeding pens paint their floor with. Free agents EXCLUDE it (so they route around every pen); a penned creature is RESTRICTED to it. Pick the exact Area from Navigation → Areas.")]
    [ValueDropdown(nameof(EditorNavMeshAreaNames))]
    [SerializeField] private string breedingAreaName = "BreedingRoom";

    [TabGroup("Tuning", "Movement"), Title("Courtship (mientras corteja en el corral)")]
    [Tooltip("Multiplica la velocidad base del agente mientras corteja — más alegre/animoso que el merodeo normal.")]
    [SerializeField, Min(0f)] private float courtSpeedMultiplier = 1.7f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("MACHO: radio (m) al que orbita a su pareja (a su lado).")]
    [SerializeField, Min(0.1f)] private float courtOrbitRadius = 0.9f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("MACHO: velocidad angular de la órbita (grados/s). Más alto = gira más rápido alrededor de la hembra.")]
    [SerializeField, Min(0f)] private float courtAngularSpeed = 140f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("MACHO: cuántos grados adelanta el punto-objetivo sobre el círculo para que la órbita sea fluida (no a tirones).")]
    [SerializeField, Range(5f, 90f)] private float courtLookahead = 35f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("MACHO: cada cuánto (s) refresca el destino de la órbita.")]
    [SerializeField, Min(0.02f)] private float courtRepath = 0.12f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("HEMBRA: radio (m) de sus movimientos cortos alrededor del slot — chico = casi en el lugar.")]
    [SerializeField, Min(0.05f)] private float courtTendRadius = 0.35f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("HEMBRA: cada cuánto (s) elige un nuevo punto cerca del slot (más bajo = darts más frecuentes).")]
    [SerializeField, Min(0.05f)] private float courtTendInterval = 0.5f;

    // ── Needs (decay + thresholds) ──
    // Live readout of this creature's current needs (the values in dna.Needs, mutated each frame).
    // Editor-only window into runtime state — drives nothing.
    [TabGroup("Tuning", "Needs"), Title("Live values (play mode)")]
    [ShowInInspector, ProgressBar(0f, 100f, 0.3f, 0.9f, 0.4f)]
    private float Health => dna != null ? dna.Needs.Health : 0f;
    [TabGroup("Tuning", "Needs")]
    [ShowInInspector, ProgressBar(0f, 100f, 0.3f, 0.6f, 1f)]
    private float Energy => dna != null ? dna.Needs.Energy : 0f;
    [TabGroup("Tuning", "Needs")]
    [ShowInInspector, ProgressBar(-100f, 100f, 1f, 0.5f, 0.7f)]
    private float Affect => dna != null ? dna.Needs.Affect : 0f;

    // Overall wellbeing, DERIVED from the needs against the critical thresholds below (never stored —
    // always in sync). Sick = Health critical (survival emergency); InNeed = Energy/Affect critical;
    // Healthy = none. Gates whether it can afford to react to the player (see ReactIfPlayerNear).
    [TabGroup("Tuning", "Needs"), ShowInInspector, EnumToggleButtons, ReadOnly]
    public CreatureCondition Condition
    {
        get
        {
            if (dna == null) return CreatureCondition.Healthy;
            if (dna.Needs.Health <= criticalHealth) return CreatureCondition.Sick;
            if (dna.Needs.Energy <= criticalEnergy || dna.Needs.Affect <= criticalAffect) return CreatureCondition.InNeed;
            return CreatureCondition.Healthy;
        }
    }

    [TabGroup("Tuning", "Needs"), Title("Decay per second (only while spawned)")]
    [Tooltip("Health lost per second — passive hunger.")]
    [SerializeField, Min(0f)] private float healthDecayPerSecond = 0.5f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Energy lost per second WHILE MOVING (active life).")]
    [SerializeField, Min(0f)] private float energyDecayPerSecond = 1f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Affect lost per second — drifts toward stress (negative) when neglected.")]
    [SerializeField, Min(0f)] private float affectDecayPerSecond = 0.5f;

    [TabGroup("Tuning", "Needs"), Title("Critical thresholds (seek a station, else degrade)")]
    [Tooltip("Health at/below this → seek a Feeder.")]
    [SerializeField, Range(0f, 100f)] private float criticalHealth = 25f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Energy at/below this → seek a RestZone.")]
    [SerializeField, Range(0f, 100f)] private float criticalEnergy = 25f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Affect AT OR BELOW this → stressed: seek a PlayZone (and flee the player if none).")]
    [SerializeField, Range(-100f, 100f)] private float criticalAffect = -75f;

    [TabGroup("Tuning", "Needs"), Title("Stress events (affect penalties)")]
    [Tooltip("Affect lost each time the player throws or knocks it.")]
    [SerializeField, Min(0f)] private float affectOnThrow = 8f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Affect lost on a hard collision (impact speed ≥ threshold).")]
    [SerializeField, Min(0f)] private float affectOnHardCollision = 5f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Impact speed (m/s) at/above which a collision counts as 'hard' for stress.")]
    [SerializeField, Min(0f)] private float hardImpactThreshold = 4f;

    [TabGroup("Tuning", "Needs"), Title("Player interaction")]
    [Tooltip("Affect boost granted when the player pets this creature from the front.")]
    [SerializeField, Min(0f)] private float affectOnPet = 20f;

    // ── Stats (live readout) ──
    [TabGroup("Tuning", "Stats"), Title("Base (con partes) → Final (con equipo) — play mode")]
    [ShowInInspector, ReadOnly, LabelText("CON")] private string StatCon => StatLine(StatType.Constitution);
    [TabGroup("Tuning", "Stats")]
    [ShowInInspector, ReadOnly, LabelText("ATK")] private string StatAtk => StatLine(StatType.Attack);
    [TabGroup("Tuning", "Stats")]
    [ShowInInspector, ReadOnly, LabelText("SPD")] private string StatSpd => StatLine(StatType.Speed);
    [TabGroup("Tuning", "Stats")]
    [ShowInInspector, ReadOnly, LabelText("DEF")] private string StatDef => StatLine(StatType.Defense);
    [TabGroup("Tuning", "Stats")]
    [ShowInInspector, ReadOnly, LabelText("LCK")] private string StatLck => StatLine(StatType.Luck);
    [TabGroup("Tuning", "Stats")]
    [ShowInInspector, ReadOnly, LabelText("EVA")] private string StatEva => StatLine(StatType.Evasion);

    private EffectiveStats StatsBase()
    {
        if (dna == null) return default;
        var db = GameManager.Instance != null ? GameManager.Instance.Database : null;
        return db != null
            ? CombatStats.GetEffectiveStats(dna, db)
            : new EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);
    }

    private EffectiveStats StatsFinal()
    {
        if (dna == null) return default;
        var equip = GameManager.Instance != null ? GameManager.Instance.EquipmentDatabase : null;
        return EquipmentStats.Apply(StatsBase(), dna, equip);
    }

    private string StatLine(StatType t)
    {
        if (dna == null) return "—";
        float b = StatValue(StatsBase(), t);
        float f = StatValue(StatsFinal(), t);
        float d = f - b;
        return Mathf.Approximately(d, 0f)
            ? $"{b:0.#}"
            : $"{b:0.#} → {f:0.#} ({(d > 0 ? "+" : "")}{d:0.#})";
    }

    private static float StatValue(EffectiveStats s, StatType t) => t switch
    {
        StatType.Constitution => s.Constitution,
        StatType.Attack       => s.Attack,
        StatType.Speed        => s.Speed,
        StatType.Defense      => s.Defense,
        StatType.Luck         => s.Luck,
        StatType.Evasion      => s.Evasion,
        _                     => 0f,
    };

    // ── Physics (throwable layer) ──
    [TabGroup("Tuning", "Physics"), Title("Hold feel (while carried)")]
    [Tooltip("How snappily the body chases the hold anchor while carried.")]
    [SerializeField] private float followSpeed = 15f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Below this speed (and grounded) after a throw, the agent re-joins the NavMesh.")]
    [SerializeField] private float settleSpeed = 0.15f;
    [TabGroup("Tuning", "Physics")]
    [SerializeField] private float settleDelay = 0.4f;

    [TabGroup("Tuning", "Physics"), Title("Throw physics (after release)")]
    [Tooltip("Linear drag applied while airborne/sliding after a throw, so it slows to a stop instead of gliding forever.")]
    [SerializeField] private float thrownLinearDamping = 1.2f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Angular drag applied after a throw so it stops spinning.")]
    [SerializeField] private float thrownAngularDamping = 2f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Extra ray length below the body used to confirm it's resting on the floor before it settles.")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Safety net: it recovers no matter what this many seconds after a throw, even if still sliding.")]
    [SerializeField] private float maxThrownTime = 6f;

    [TabGroup("Tuning", "Physics")]
    [Tooltip("Red de seguridad de carga en frío: si queda kinematic pero FUERA del NavMesh (handoff fallido) este tiempo (s), se recupera — el penned se re-ancla a su corral, el libre cae a física. Sube si en la 1ª carga aparece flotando un rato.")]
    [SerializeField, Min(0.1f)] private float offMeshRecoverDelay = 1.5f;

    [TabGroup("Tuning", "Physics"), Title("Bounce (plushie throw)")]
    [Tooltip("Fraction of speed kept on each bounce (0 = dead drop, 1 = no energy loss). Lower settles sooner.")]
    [Range(0f, 1f)]
    [SerializeField] private float bounciness = 0.55f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("How many reflections it gets before it's allowed to stop bouncing and settle.")]
    [SerializeField] private int maxBounces = 3;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Impacts slower than this don't count as a bounce — it just settles (avoids endless micro-bounces).")]
    [SerializeField] private float minBounceSpeed = 1.5f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Random tumble added on each bounce so it reads as a soft plushie, not a billiard ball. 0 = none.")]
    [SerializeField] private float bounceSpin = 4f;

    [TabGroup("Tuning", "Physics"), Title("Knock (throwable vs throwable)")]
    [Tooltip("Fraction of impact speed transferred to a creature you slam into. ~1 = full, higher = explosive ragdoll.")]
    [SerializeField] private float knockTransfer = 1.2f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Upward pop blended into the knock so the creature you hit launches a bit instead of just sliding.")]
    [Range(0f, 1f)]
    [SerializeField] private float knockUpBias = 0.35f;

    [TabGroup("Tuning", "Physics"), Title("Recovery (after being thrown)")]
    [Tooltip("Seconds it stays down/dazed where it landed before standing up. Scaled per-personality by RecoverySpeed.")]
    [SerializeField] private float downedDelay = 0.6f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("How long the get-up takes — it rotates from its tumbled pose back upright before the agent resumes. Scaled per-personality by RecoverySpeed.")]
    [SerializeField] private float getUpDuration = 0.5f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Random ±jitter (fraction) on the get-up timing so even same-personality creatures don't rise in lockstep.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float getUpJitter = 0.15f;

    // ── Presentation (visuals + juice) ──
    // Juice hook points. They fire UnityEvents now (compiles without Feel installed).
    // When Feel lands: drop an MMF_Player on the prefab and wire its PlayFeedbacks()
    // into the matching event in the inspector — zero code coupling. These are the
    // template every future "has visual juice" script should follow.
    [TabGroup("Tuning", "Presentation"), Title("Feedbacks (Feel-ready — wire MMF_Player.PlayFeedbacks here)")]
    [SerializeField] private UnityEvent onGrab;     // player picked it up
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] private UnityEvent onThrow;    // player threw it
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] private UnityEvent onBounce;   // each reflection off a surface mid-flight
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] private UnityEvent onLand;     // settled on the ground (before the get-up beat)
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] private UnityEvent onGetUp;    // finished standing up, resumes roaming
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] private UnityEvent onPet;      // player petted it from the front

    [TabGroup("Tuning", "Dev"), Title("Live State (play mode)")]
    [ShowInInspector, ReadOnly, EnumToggleButtons]
    private AgentState CurrentState => state;

    [TabGroup("Tuning", "Dev"), ShowInInspector, ReadOnly]
    private string NavStatus =>
        !Application.isPlaying || agent == null ? "---"
        : $"enabled={agent.enabled}  onMesh={agent.enabled && agent.isOnNavMesh}  y={transform.position.y:0.00}";

    [TabGroup("Tuning", "Dev"), ShowInInspector, ReadOnly]
    private string CourtInfo =>
        state != AgentState.Courting ? "—"
        : $"{courtRole} ↔ {(courtPartner != null && courtPartner.DNA != null ? courtPartner.DNA.CustomName : "?")}";

    [TabGroup("Tuning", "Dev"), Title("Debug toggles")]
    [Tooltip("Fuerza al agente a quedarse en ragdoll: nunca rejoina el NavMesh (aísla el handoff que lo pinea al piso).")]
    [SerializeField] private bool forceRagdoll;

    [TabGroup("Tuning", "Dev")]
    [Tooltip("Loguea cada transición de estado y avisa si la 'y' salta de golpe (snap) estando NavMesh-driven.")]
    [SerializeField] private bool logStateTransitions;

    [TabGroup("Tuning", "Dev")]
    [Tooltip("Umbral (m) de salto vertical en un frame para reportar un snap al piso.")]
    [SerializeField, Min(0.05f)] private float snapWarnThreshold = 0.75f;

    private AgentState devPrevState;
    private float      devLastY;
    private bool       devTrackingInit;

    private void DevTrackState()
    {
        if (!devTrackingInit)
        {
            devPrevState    = state;
            devLastY        = transform.position.y;
            devTrackingInit = true;
            return;
        }

        if (logStateTransitions && state != devPrevState)
            Debug.Log($"[MoriMochiAgent:{name}] {devPrevState} → {state}  (f{Time.frameCount}, y={transform.position.y:0.00})");

        if (logStateTransitions && IsNavMeshControlled() &&
            Mathf.Abs(transform.position.y - devLastY) > snapWarnThreshold)
            Debug.LogWarning($"[MoriMochiAgent:{name}] SNAP Δy={transform.position.y - devLastY:0.00} en {state} (f{Time.frameCount})");

        devPrevState = state;
        devLastY     = transform.position.y;
    }

    [TabGroup("Tuning", "Dev"), Title("Dev Tools (Play mode only)")]
    [Button("Force Ragdoll")]
    private void DevForceRagdoll()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[MoriMochiAgent] Enter Play mode first."); return; }
        ReleaseStation();
        EnterRagdoll();
        rb.AddForce(Vector3.up * 4f, ForceMode.VelocityChange);
    }

    [TabGroup("Tuning", "Dev")]
    [Button("Force Roam")]
    private void DevForceRoam()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[MoriMochiAgent] Enter Play mode first."); return; }
        EnterRoaming();
    }
}
}
