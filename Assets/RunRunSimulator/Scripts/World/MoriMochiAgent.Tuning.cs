using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

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
