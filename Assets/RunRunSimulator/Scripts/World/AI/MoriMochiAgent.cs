using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
namespace MoriMonchiSimulator
{

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class MoriMochiAgent : MonoBehaviour, IThrowable, IInteractable
{
    private AgentContext     ctx;
    private AgentBrain       brain;
    private AgentPhysics     physics;
    private AgentConfinement confinement;
    private AgentSenses      senses;
    private AgentSocial      social;
    private AgentExpedition  expedition;
    private AgentClash       clash;
    private Perceivable perceivable;

    private void OnEnable()
    {
        GameEvents.OnNavMeshWillRebake += confinement.OnNavMeshWillRebake;
        GameEvents.OnNavMeshRebaked    += confinement.OnNavMeshRebaked;
    }

    private void OnDisable()
    {
        GameEvents.OnNavMeshWillRebake -= confinement.OnNavMeshWillRebake;
        GameEvents.OnNavMeshRebaked    -= confinement.OnNavMeshRebaked;
    }

    private void Awake()
    {
        var agent = GetComponent<NavMeshAgent>();
        var rb    = GetComponent<Rigidbody>();
        var col   = GetComponent<Collider>();

        ctx = new AgentContext(this, agent, rb, col);
        ctx.BaseSpeed = agent.speed;

        brain       = new AgentBrain(this, ctx);
        physics     = new AgentPhysics(this, ctx);
        confinement = new AgentConfinement(this, ctx);
        senses      = new AgentSenses(this, ctx);
        social      = new AgentSocial(this, ctx);
        expedition  = new AgentExpedition(this, ctx);
        clash       = new AgentClash(this, ctx);
        perceivable = GetComponent<Perceivable>();

        ctx.Rb.isKinematic = true;
        ctx.Rb.useGravity  = false;

        ctx.SetColliderTrigger(true);
    }

    public void Initialize(CreatureDNA creature, RoleWorldProfileSO profileTable, Transform playerTransform)
    {
        ctx.Dna     = creature;
        ctx.Profile = profileTable?.GetProfile(creature.Role)
                      ?? RoleWorldProfile.Neutral();
        ctx.Player  = playerTransform;

        RestoreNavMeshControl();

        if (nameTag != null) nameTag.Bind(creature, this);

        int breeding         = NavMesh.GetAreaFromName(breedingAreaName);
        ctx.ConfinedAreaMask = breeding >= 0 ? 1 << breeding : NavMesh.AllAreas;
        ctx.FreeAreaMask     = breeding >= 0 ? NavMesh.AllAreas & ~(1 << breeding) : NavMesh.AllAreas;
        ctx.Agent.areaMask   = ctx.FreeAreaMask;

        brain.EnterRoaming();
        physics.CaptureNavAnchor(transform.position);
    }

    public void Rebind(CreatureDNA creature, RoleWorldProfileSO profileTable)
    {
        ctx.Dna     = creature;
        ctx.Profile = profileTable?.GetProfile(creature.Role) ?? RoleWorldProfile.Neutral();
        if (nameTag != null) nameTag.Bind(creature, this);
    }

    private void RestoreNavMeshControl()
    {
        brain.ReleaseStation();
        confinement.DetachForReuse();

        if (!ctx.Rb.isKinematic)
        {
            ctx.Rb.linearVelocity  = Vector3.zero;
            ctx.Rb.angularVelocity = Vector3.zero;
        }
        ctx.Rb.isKinematic = true;
        ctx.Rb.useGravity  = false;

        if (!ctx.Agent.enabled)
        {
            Vector3 keep = ctx.Body.position;
            ctx.Agent.enabled = true;
            if (ctx.Agent.isOnNavMesh && NavMesh.SamplePosition(keep, out var hit, sampleRadius * 2f, NavMesh.AllAreas))
                ctx.Agent.Warp(hit.position);
        }
        ctx.Agent.updateRotation = true;
        ctx.SetColliderTrigger(true);
        ctx.HoldAnchor = null;

        brain.ResetForReuse();
        physics.ResetForReuse();
        social.ResetForReuse();
        senses.ResetForReuse();
        expedition.ResetForReuse();
        clash.ResetForReuse();
        ctx.RebakeInProgress = false;
        ctx.State            = AgentState.Idle;
    }

    public void PrepareForPool()
    {
        brain.ReleaseStation();
        confinement.DetachForReuse();
        social.ResetForReuse();
        if (ctx.Agent.enabled && ctx.Agent.isOnNavMesh) ctx.Agent.ResetPath();
    }

    private void Update()
    {
        DevTrackState();
        if (forceRagdoll && ctx.IsNavMeshControlled()) { brain.ReleaseStation(); physics.EnterRagdoll(); }
        physics.RecoverIfStuckOffMesh();

        brain.TickAlways(Time.deltaTime);
        senses.Tick();
        ctx.ApplyGaitSpeed();
        switch (ctx.State)
        {
            case AgentState.Idle:         brain.TickIdle();    if (ctx.State == AgentState.Idle    && !clash.TryEngage() && !expedition.TryEngage()) social.TryEngage(); break;
            case AgentState.Roaming:      brain.TickRoaming(); if (ctx.State == AgentState.Roaming && !clash.TryEngage() && !expedition.TryEngage()) social.TryEngage(); break;
            case AgentState.Reacting:     brain.TickReacting();      break;
            case AgentState.Thrown:       clash.TickAirborne(); physics.TickThrown(); break;
            case AgentState.Recovering:   physics.TickRecovering();  break;
            case AgentState.SeekingNeed:  brain.TickSeekingNeed();   break;
            case AgentState.UsingStation: brain.TickUsingStation();  break;
            case AgentState.Courting:     confinement.TickCourting(); break;
            case AgentState.Socializing:  social.TickSocializing();  break;
            case AgentState.HandFeed:     brain.TickHandFeed();      break;
            case AgentState.Expedition:   if (clash.TryEngage()) expedition.ResetForReuse(); else expedition.TickExpedition(); break;
            case AgentState.Clashing:     clash.TickClashing();      break;
        }
    }

    private void FixedUpdate()
    {
        physics.FixedTick();
    }

    private void OnCollisionEnter(Collision collision) => physics.HandleCollisionEnter(collision);
    private void OnTriggerEnter(Collider other) => physics.HandleTriggerEnter(other);

    public bool IsHeld => ctx.State == AgentState.Carried;
    public bool IsAirborne => ctx.State == AgentState.Thrown;
    public CreatureDNA DNA => ctx.Dna;

    public bool IsPenned => ctx.CurrentContainer != null;

    public bool IsForSale => ctx.CurrentContainer is StoreContainer;
    public bool IsCourting => ctx.State == AgentState.Courting;
    public bool IsSocializing => ctx.State == AgentState.Socializing;
    public bool IsRecovering => ctx.State == AgentState.Recovering;

    public bool IsInFriendlyReaction => brain.IsInFriendlyReaction;

    public bool IsBeingPetted => brain.IsBeingPetted;

    public bool CanBePetted => brain.CanBePetted;

    public CreatureIntent Intent =>
        ctx.State == AgentState.Clashing    ? clash.Intent :
        ctx.State == AgentState.Socializing ? social.Intent :
        ctx.State == AgentState.Expedition  ? expedition.Intent :
        brain.Intent;

    public IReadOnlyList<Percept> Percepts => ctx.Percepts;
    public ExpeditionTeam Team => perceivable != null ? perceivable.Team : ExpeditionTeam.None;
    public Occupation Occupation => ctx.Occupation;
    public int Carried => expedition.Carried;
    public float MiningProgress => expedition.MiningProgress;
    public void SetOccupation(Occupation occupation) => ctx.Occupation = occupation == Occupation.None ? Occupation.Gather : occupation;
    public void SetHomeExit(ExitZone exit) => ctx.HomeExit = exit;
    public void SetGuardPost(Transform post) => ctx.GuardPost = post;

    public int CollectedMaterial => expedition.Collected;
    public Transform ExpeditionTarget => expedition.TargetTransform;
    public MoriMochiAgent SocialPartner => ctx.State == AgentState.Socializing ? social.Partner : null;
    public MoriMochiAgent ClashTarget => clash.Target;
    public string ClashGesture => clash.Gesture;
    public bool IsClashTargetable => clash.IsTargetable;
    public bool ForceClash(ClashMoveSO move, MoriMochiAgent rival) => clash.ForceMove(move, rival);

    public event System.Action<EmoteKind> OnEmote;
    internal void EmitEmote(EmoteKind kind) => OnEmote?.Invoke(kind);

    public bool IsPlayerFacingMe() => brain.IsPlayerFacingMe();

    public void Interact() => brain.Interact();
    public bool BeginPetting() => brain.BeginPetSession();
    public void EndPetting() => brain.EndPetSession();

    public void Knock(Vector3 force) => physics.Knock(force);

    public void Launch(Vector3 launchPos, Vector3 launchVelocity) => physics.Launch(launchPos, launchVelocity);

    public void OnGrab(Transform anchor) => physics.OnGrab(anchor);
    public void OnRelease() => physics.OnRelease();
    public void OnThrow(Vector3 force) => physics.OnThrow(force);

    public bool EnterConfinement(MoriMochiContainer pen) => confinement.EnterConfinement(pen);
    public void EnterCourtship(MoriMochiAgent partner, Vector3 anchor) => confinement.EnterCourtship(partner, anchor);
    public void ExitCourtship() => confinement.ExitCourtship();

    internal bool TryJoinSocialPlay(MoriMochiAgent initiator) => social.TryJoinSocialPlay(initiator);
    internal void CompleteSocialPlayFromPartner() => social.CompleteFromPartner();
    internal bool TryJoinSocialSleep(MoriMochiAgent initiator, NeedStation station, Vector3 fallbackSpot) => social.TryJoinSleep(initiator, station, fallbackSpot);
    internal bool TryJoinSocialFight(MoriMochiAgent initiator) => social.TryJoinFight(initiator);

    internal void RequestRoam() => brain.EnterRoaming();
    internal void RequestReleaseStation() => brain.ReleaseStation();
    internal void RequestEnterRagdoll() => physics.EnterRagdoll();
    internal void RequestDetachToPhysics() => physics.DetachToPhysics();
    internal bool RequestRejoinNavMesh(Vector3 desired, int mask) => physics.RejoinNavMesh(desired, mask);
    internal void RequestReleaseFromPen() => confinement.ReleaseFromPen();
    internal Vector3 AdjustRoamForAvoidance(Vector3 candidate) => social.AdjustRoamForAvoidance(candidate);
    internal void RequestPlayfulKnock(Vector3 force) => physics.Knock(force, false);
    internal void ReceiveClashHit(MoriMochiAgent attacker, Vector3 force) { clash.ReceiveHit(attacker); physics.Knock(force, false); }
    internal void NotifyKnocked() { clash.Cancel(); expedition.OnKnocked(); }
    internal void NotifyRecovered() => clash.OnRecovered();
    internal bool IgnoresChainKnock(MoriMochiAgent other) => clash.IgnoresChainKnock(other);

    [TabGroup("Tuning", "References"), Title("References")]
    [SerializeField] private NameTag nameTag;
    [TabGroup("Tuning", "Movement"), Title("NavMesh sampling")]
    [Tooltip("Max distance to snap a desired point onto the NavMesh.")]
    [SerializeField] internal float sampleRadius = 4f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("How often (s) Reacting/Roaming recomputes its destination.")]
    [SerializeField] internal float repathInterval = 0.35f;

    [TabGroup("Tuning", "Movement"), Title("Player proximity")]
    [Tooltip("Max seconds a friendly reaction (Approach/Follow/Retreat) lasts before the creature resumes its own business.")]
    [SerializeField, Min(1f)] internal float followDuration = 10f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("Seconds the creature waits before reacting to the player again after a reaction ends (timer or petting).")]
    [SerializeField, Min(0f)] internal float reactCooldown = 15f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("Max distance (m) at which the pet hint appears and petting is valid.")]
    [SerializeField, Min(0.5f)] internal float petRadius = 2.5f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("Half-angle of the camera cone (degrees) within which the player must be aiming at this creature for the pet hint to appear. 20° = ±20° from dead-center.")]
    [SerializeField, Range(5f, 60f)] internal float petLookAngle = 20f;

    [TabGroup("Tuning", "Movement"), Title("Role radii (runtime — from RoleWorldProfileSO)")]
    [ShowInInspector, ReadOnly] private float ProfileProximityRadius => ctx?.Profile?.ProximityRadius ?? 0f;
    [TabGroup("Tuning", "Movement")]
    [ShowInInspector, ReadOnly] private float ProfileRoamRadius      => ctx?.Profile?.RoamRadius      ?? 0f;
    [TabGroup("Tuning", "Movement")]
    [ShowInInspector, ReadOnly] private float ProfileFollowDistance  => ctx?.Profile?.FollowDistance  ?? 0f;

    [TabGroup("Tuning", "Movement"), Title("Breeding pen confinement")]
    [Tooltip("NavMesh Area that breeding pens paint their floor with. Free agents EXCLUDE it (so they route around every pen); a penned creature is RESTRICTED to it. Pick the exact Area from Navigation → Areas.")]
    [ValueDropdown(nameof(EditorNavMeshAreaNames))]
    [SerializeField] internal string breedingAreaName = "BreedingRoom";

    [TabGroup("Tuning", "Movement"), Title("Courtship (mientras corteja en el corral)")]
    [Tooltip("Multiplica la velocidad base del agente mientras corteja — más alegre/animoso que el merodeo normal.")]
    [SerializeField, Min(0f)] internal float courtSpeedMultiplier = 1.7f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("MACHO: radio (m) al que orbita a su pareja (a su lado).")]
    [SerializeField, Min(0.1f)] internal float courtOrbitRadius = 0.9f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("MACHO: velocidad angular de la órbita (grados/s). Más alto = gira más rápido alrededor de la hembra.")]
    [SerializeField, Min(0f)] internal float courtAngularSpeed = 140f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("MACHO: cuántos grados adelanta el punto-objetivo sobre el círculo para que la órbita sea fluida (no a tirones).")]
    [SerializeField, Range(5f, 90f)] internal float courtLookahead = 35f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("MACHO: cada cuánto (s) refresca el destino de la órbita.")]
    [SerializeField, Min(0.02f)] internal float courtRepath = 0.12f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("HEMBRA: radio (m) de sus movimientos cortos alrededor del slot — chico = casi en el lugar.")]
    [SerializeField, Min(0.05f)] internal float courtTendRadius = 0.35f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("HEMBRA: cada cuánto (s) elige un nuevo punto cerca del slot (más bajo = darts más frecuentes).")]
    [SerializeField, Min(0.05f)] internal float courtTendInterval = 0.5f;

    [TabGroup("Tuning", "Needs"), Title("Live values (play mode)")]
    [ShowInInspector, ProgressBar(0f, 100f, 0.3f, 0.9f, 0.4f)]
    private float Health => ctx?.Dna != null ? ctx.Dna.Needs.Health : 0f;
    [TabGroup("Tuning", "Needs")]
    [ShowInInspector, ProgressBar(0f, 100f, 0.3f, 0.6f, 1f)]
    private float Energy => ctx?.Dna != null ? ctx.Dna.Needs.Energy : 0f;
    [TabGroup("Tuning", "Needs")]
    [ShowInInspector, ProgressBar(-100f, 100f, 1f, 0.5f, 0.7f)]
    private float Affect => ctx?.Dna != null ? ctx.Dna.Needs.Affect : 0f;

    [TabGroup("Tuning", "Needs"), ShowInInspector, EnumToggleButtons, ReadOnly]
    public CreatureCondition Condition
    {
        get
        {
            var dna = ctx?.Dna;
            if (dna == null) return CreatureCondition.Healthy;
            if (dna.Needs.Health <= criticalHealth) return CreatureCondition.Sick;
            if (dna.Needs.Energy <= criticalEnergy || dna.Needs.Affect <= criticalAffect) return CreatureCondition.InNeed;
            return CreatureCondition.Healthy;
        }
    }

    [TabGroup("Tuning", "Needs"), Title("Decay per second (only while spawned)")]
    [Tooltip("Health lost per second — passive hunger.")]
    [SerializeField, Min(0f)] internal float healthDecayPerSecond = 0.5f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Energy lost per second WHILE MOVING (active life).")]
    [SerializeField, Min(0f)] internal float energyDecayPerSecond = 1f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Affect lost per second — drifts toward stress (negative) when neglected.")]
    [SerializeField, Min(0f)] internal float affectDecayPerSecond = 0.5f;

    [TabGroup("Tuning", "Needs"), Title("Critical thresholds (seek a station, else degrade)")]
    [Tooltip("Health at/below this → seek a Feeder.")]
    [SerializeField, Range(0f, 100f)] internal float criticalHealth = 25f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Energy at/below this → seek a RestZone.")]
    [SerializeField, Range(0f, 100f)] internal float criticalEnergy = 25f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Affect AT OR BELOW this → stressed: seek a PlayZone (and flee the player if none).")]
    [SerializeField, Range(-100f, 100f)] internal float criticalAffect = -75f;

    [TabGroup("Tuning", "Needs"), Title("Stress events (affect penalties)")]
    [Tooltip("Affect lost each time the player throws or knocks it.")]
    [SerializeField, Min(0f)] internal float affectOnThrow = 8f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Affect lost on a hard collision (impact speed ≥ threshold).")]
    [SerializeField, Min(0f)] internal float affectOnHardCollision = 5f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Impact speed (m/s) at/above which a collision counts as 'hard' for stress.")]
    [SerializeField, Min(0f)] internal float hardImpactThreshold = 4f;

    [TabGroup("Tuning", "Needs"), Title("Player interaction")]
    [Tooltip("Affect por segundo mientras el jugador mantiene la caricia.")]
    [SerializeField, Min(0f)] internal float petAffectPerSecond = 6f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Cuánto crece la tasa de caricia por segundo sostenido (0.3 = +30% por segundo).")]
    [SerializeField, Min(0f)] internal float petRampPerSecond = 0.3f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Duración máxima de una sesión de caricia (s).")]
    [SerializeField, Min(1f)] internal float petMaxDuration = 6f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Cada cuántos segundos emite un corazón durante la caricia.")]
    [SerializeField, Min(0.2f)] internal float petEmoteInterval = 1.5f;

    [TabGroup("Tuning", "Needs"), Title("Comer de la mano")]
    [Tooltip("Radio (m) en el que un monchi con hambre nota la comida ofrecida en la mano.")]
    [SerializeField, Min(1f)] internal float feedNoticeRadius = 6f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Distancia (m) a la que se detiene a comer de la mano.")]
    [SerializeField, Min(0.5f)] internal float feedDistance = 1.2f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Sociabilidad por debajo de la cual el monchi duda antes de acercarse del todo.")]
    [SerializeField, Range(0f, 1f)] internal float feedShyBelow = 0.35f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Distancia (m) a la que el tímido se frena a dudar antes de animarse.")]
    [SerializeField, Min(1f)] internal float feedShyDistance = 3f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Segundos que el tímido duda antes de animarse a acercarse.")]
    [SerializeField, Min(0f)] internal float feedHesitateSeconds = 2f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Segundos que tarda el bocado una vez al lado de la mano.")]
    [SerializeField, Min(0.2f)] internal float feedEatSeconds = 1.5f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Solo viene a comer de la mano si su Health está por debajo de este valor.")]
    [SerializeField, Range(0f, 100f)] internal float feedHungerThreshold = 70f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Health que restaura el bocado de la mano.")]
    [SerializeField, Min(0f)] internal float feedHealthBoost = 35f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Affect extra por comer de la mano del jugador.")]
    [SerializeField, Min(0f)] internal float feedAffectBoost = 5f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Segundos antes de volver a buscar comida de la mano tras un bocado.")]
    [SerializeField, Min(0f)] internal float feedCooldown = 20f;

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
        if (ctx?.Dna == null) return default;
        var db = GameManager.Instance != null ? GameManager.Instance.Database : null;
        return db != null
            ? CreatureStats.GetEffectiveStats(ctx.Dna, db)
            : new EffectiveStats(ctx.Dna.BaseConstitution, ctx.Dna.BaseAttack, ctx.Dna.BaseSpeed, ctx.Dna.BaseDefense, ctx.Dna.BaseLuck, ctx.Dna.BaseEvasion);
    }

    private EffectiveStats StatsFinal()
    {
        if (ctx?.Dna == null) return default;
        var equip = GameManager.Instance != null ? GameManager.Instance.EquipmentDatabase : null;
        return EquipmentStats.Apply(StatsBase(), ctx.Dna, equip);
    }

    private string StatLine(StatType t)
    {
        if (ctx?.Dna == null) return "—";
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

    [TabGroup("Tuning", "Physics"), Title("Hold feel (while carried)")]
    [Tooltip("How snappily the body chases the hold anchor while carried.")]
    [SerializeField] internal float followSpeed = 15f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Below this speed (and grounded) after a throw, the agent re-joins the NavMesh.")]
    [SerializeField] internal float settleSpeed = 0.15f;
    [TabGroup("Tuning", "Physics")]
    [SerializeField] internal float settleDelay = 0.4f;

    [TabGroup("Tuning", "Physics"), Title("Throw physics (after release)")]
    [Tooltip("Linear drag applied while airborne/sliding after a throw, so it slows to a stop instead of gliding forever.")]
    [SerializeField] internal float thrownLinearDamping = 1.2f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Angular drag applied after a throw so it stops spinning.")]
    [SerializeField] internal float thrownAngularDamping = 2f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Extra ray length below the body used to confirm it's resting on the floor before it settles.")]
    [SerializeField] internal float groundCheckDistance = 0.2f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Safety net: it recovers no matter what this many seconds after a throw, even if still sliding.")]
    [SerializeField] internal float maxThrownTime = 6f;

    [TabGroup("Tuning", "Physics")]
    [Tooltip("Red de seguridad de carga en frío: si queda kinematic pero FUERA del NavMesh (handoff fallido) este tiempo (s), se recupera — el penned se re-ancla a su corral, el libre cae a física. Sube si en la 1ª carga aparece flotando un rato.")]
    [SerializeField, Min(0.1f)] internal float offMeshRecoverDelay = 1.5f;

    [TabGroup("Tuning", "Physics")]
    [Tooltip("Red anti-vacío: si cae más de estos metros por debajo de su último punto válido de NavMesh, se lo rescata teleportándolo de vuelta.")]
    [SerializeField, Min(1f)] internal float voidFallDrop = 20f;

    [TabGroup("Tuning", "Physics"), Title("Bounce (plushie throw)")]
    [Tooltip("Fraction of speed kept on each bounce (0 = dead drop, 1 = no energy loss). Lower settles sooner.")]
    [Range(0f, 1f)]
    [SerializeField] internal float bounciness = 0.55f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("How many reflections it gets before it's allowed to stop bouncing and settle.")]
    [SerializeField] internal int maxBounces = 3;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Impacts slower than this don't count as a bounce — it just settles (avoids endless micro-bounces).")]
    [SerializeField] internal float minBounceSpeed = 1.5f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Random tumble added on each bounce so it reads as a soft plushie, not a billiard ball. 0 = none.")]
    [SerializeField] internal float bounceSpin = 4f;

    [TabGroup("Tuning", "Physics"), Title("Knock (throwable vs throwable)")]
    [Tooltip("Fraction of impact speed transferred to a creature you slam into. ~1 = full, higher = explosive ragdoll.")]
    [SerializeField] internal float knockTransfer = 1.2f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Upward pop blended into the knock so the creature you hit launches a bit instead of just sliding.")]
    [Range(0f, 1f)]
    [SerializeField] internal float knockUpBias = 0.35f;

    [TabGroup("Tuning", "Physics"), Title("Recovery (after being thrown)")]
    [Tooltip("Seconds it stays down/dazed where it landed before standing up. Scaled per-personality by RecoverySpeed.")]
    [SerializeField] internal float downedDelay = 0.6f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("How long the get-up takes — it rotates from its tumbled pose back upright before the agent resumes. Scaled per-personality by RecoverySpeed.")]
    [SerializeField] internal float getUpDuration = 0.5f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Random ±jitter (fraction) on the get-up timing so even same-personality creatures don't rise in lockstep.")]
    [Range(0f, 0.5f)]
    [SerializeField] internal float getUpJitter = 0.15f;

    [TabGroup("Tuning", "Presentation"), Title("Feedbacks (Feel-ready — wire MMF_Player.PlayFeedbacks here)")]
    [SerializeField] internal UnityEvent onGrab;
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] internal UnityEvent onThrow;
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] internal UnityEvent onBounce;
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] internal UnityEvent onLand;
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] internal UnityEvent onGetUp;
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] internal UnityEvent onPet;
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] internal UnityEvent onPickup;
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] internal UnityEvent onClashTell;
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] internal UnityEvent onClashHit;
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] internal UnityEvent onKnocked;

    [TabGroup("Tuning", "Dev"), Title("Live State (play mode)")]
    [ShowInInspector, ReadOnly, EnumToggleButtons]
    private AgentState CurrentState => ctx != null ? ctx.State : AgentState.Idle;

    [TabGroup("Tuning", "Dev"), ShowInInspector, ReadOnly]
    private string NavStatus =>
        !Application.isPlaying || ctx == null || ctx.Agent == null ? "---"
        : $"enabled={ctx.Agent.enabled}  onMesh={ctx.Agent.enabled && ctx.Agent.isOnNavMesh}  y={transform.position.y:0.00}";

    [TabGroup("Tuning", "Dev"), ShowInInspector, ReadOnly]
    private string CourtInfo => confinement != null ? confinement.DescribeCourtship() : "—";

    [TabGroup("Tuning", "Dev"), ShowInInspector, ReadOnly]
    private string SocialInfo => social != null ? social.Describe() : "—";

    [TabGroup("Tuning", "Dev"), ShowInInspector, ReadOnly]
    private int PerceptCount => ctx != null ? ctx.Percepts.Count : 0;

    [TabGroup("Tuning", "Dev"), ShowInInspector, ReadOnly]
    private string Dials => ctx?.Dna != null ? $"Sociabilidad {ctx.Dna.Sociability:0.00} · Osadía {ctx.Dna.Boldness:0.00}" : "—";

    [TabGroup("Tuning", "Dev"), Title("Debug toggles")]
    [Tooltip("Fuerza al agente a quedarse en ragdoll: nunca rejoina el NavMesh (aísla el handoff que lo pinea al piso).")]
    [SerializeField] internal bool forceRagdoll;

    [TabGroup("Tuning", "Dev")]
    [Tooltip("Loguea cada transición de estado y avisa si la 'y' salta de golpe (snap) estando NavMesh-driven.")]
    [SerializeField] internal bool logStateTransitions;

    [TabGroup("Tuning", "Dev")]
    [Tooltip("Umbral (m) de salto vertical en un frame para reportar un snap al piso.")]
    [SerializeField, Min(0.05f)] internal float snapWarnThreshold = 0.75f;

    private AgentState devPrevState;
    private float      devLastY;
    private bool       devTrackingInit;

    private void DevTrackState()
    {
        if (!devTrackingInit)
        {
            devPrevState    = ctx.State;
            devLastY        = transform.position.y;
            devTrackingInit = true;
            return;
        }

        if (logStateTransitions && ctx.State != devPrevState)
            Debug.Log($"[MoriMochiAgent:{name}] {devPrevState} → {ctx.State}  (f{Time.frameCount}, y={transform.position.y:0.00})");

        if (logStateTransitions && ctx.IsNavMeshControlled() &&
            Mathf.Abs(transform.position.y - devLastY) > snapWarnThreshold)
            Debug.LogWarning($"[MoriMochiAgent:{name}] SNAP Δy={transform.position.y - devLastY:0.00} en {ctx.State} (f{Time.frameCount})");

        devPrevState = ctx.State;
        devLastY     = transform.position.y;
    }

    [TabGroup("Tuning", "Dev"), Title("Dev Tools (Play mode only)")]
    [Button("Force Ragdoll")]
    private void DevForceRagdoll()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[MoriMochiAgent] Enter Play mode first."); return; }
        brain.ReleaseStation();
        physics.EnterRagdoll();
        ctx.Rb.AddForce(Vector3.up * 4f, ForceMode.VelocityChange);
    }

    [TabGroup("Tuning", "Dev")]
    [Button("Force Roam")]
    private void DevForceRoam()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[MoriMochiAgent] Enter Play mode first."); return; }
        brain.EnterRoaming();
    }

    private static IEnumerable<string> EditorNavMeshAreaNames()
    {
#if UNITY_EDITOR
        return NavMesh.GetAreaNames();
#else
        return System.Array.Empty<string>();
#endif
    }

    private void OnDrawGizmos()
    {
        if (ctx == null || ctx.Profile == null) return;
        Vector3 c = transform.position;

        Gizmos.color = new Color(1f, 0.9f, 0.2f);
        Gizmos.DrawWireSphere(c, ctx.Profile.ProximityRadius);
        Gizmos.color = new Color(0.3f, 0.8f, 1f);
        Gizmos.DrawWireSphere(c, ctx.Profile.RoamRadius);
        Gizmos.color = Color.purple;
        Gizmos.DrawWireSphere(c, petRadius);
        if (ctx.Profile.Reaction != ProximityReaction.Ignore)
        {
            Gizmos.color = new Color(0.4f, 1f, 0.5f);
            Gizmos.DrawWireSphere(c, ctx.Profile.FollowDistance);
        }

        Gizmos.color = ctx.Profile.Tint;
        Gizmos.DrawSphere(c + Vector3.up * 1.2f, 0.12f);

        if (ctx.Agent != null && ctx.Agent.enabled && ctx.Agent.isOnNavMesh && ctx.Agent.hasPath)
        {
            Gizmos.color = new Color(1f, 0.4f, 0.85f);
            Gizmos.DrawLine(c, ctx.Agent.destination);
        }
    }
}
}
