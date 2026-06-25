using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

[RequireComponent(typeof(MoriMonchiVisualizer))]
public class MoriMonchiProceduralAnimator : MonoBehaviour
{
    private const string G = "Animator";

    [TabGroup(G, "General"), Required, SerializeField]
    private MoriMonchiVisualizer visualizer;

    [TabGroup(G, "General"), Tooltip("Estado de loop con el que arranca (Idle o Walk) al habilitarse el componente."), SerializeField]
    private MMAnimationType defaultLoop = MMAnimationType.Idle;

    [TabGroup(G, "General"), Tooltip("Al habilitarse, fuerza el loop a 'Default Loop'."), SerializeField]
    private bool playOnEnable = true;

    [TabGroup(G, "General"), Tooltip("Cambia solo entre Idle y Walk según la velocidad del NavMeshAgent. Apagar para controlar el loop únicamente con PlayMMAnimation (ej: escena de combate)."), SerializeField]
    private bool autoLoopFromMovement = true;

    [TabGroup(G, "General"), Tooltip("Velocidad (m/s) a partir de la cual pasa a Walk. Vuelve a Idle por debajo de la mitad de este valor (histéresis anti-parpadeo)."), Range(0.01f, 1f), SerializeField]
    private float walkSpeedThreshold = 0.08f;

    [TabGroup(G, "General"), ShowInInspector, ReadOnly]
    private string Status =>
        visualizer == null ? "NO VISUALIZER"
        : visualizer.BodyTransform == null ? "esperando Assemble (BodyTransform null)"
        : captured ? $"OK · root={(root != null ? root.name : "?")} body={(body != null ? body.name : "?")}"
        : "capturando…";

    [TabGroup(G, "General"), ShowInInspector, ReadOnly]
    private MMAnimationType loop = MMAnimationType.Idle;

    [TabGroup(G, "General"), ShowInInspector, ReadOnly]
    private bool dead;

    [TabGroup(G, "General"), Button("Test ▶")]
    private void TestPlay(MMAnimationType type) => PlayMMAnimation(type);

    [TabGroup(G, "Idle"), Tooltip("Profundidad de la respiración: fracción que se estira/encoge el cuerpo (0.10 = ±10%)."), Range(0f, 0.4f), SerializeField] private float breatheAmp = 0.10f;
    [TabGroup(G, "Idle"), Tooltip("Velocidad de la respiración (oscilaciones por segundo aprox)."), Range(0f, 8f),   SerializeField] private float breatheSpeed = 2.2f;
    [TabGroup(G, "Idle"), Tooltip("Altura del cabeceo vertical de toda la criatura, en metros."), Range(0f, 0.3f), SerializeField] private float bobAmp = 0.03f;
    [TabGroup(G, "Idle"), Tooltip("Velocidad del cabeceo vertical."), Range(0f, 8f),   SerializeField] private float bobSpeed = 1.6f;
    [TabGroup(G, "Idle"), Tooltip("Ángulo máximo de apertura/cierre de los brazos (grados). Ambos sincronizados."), Range(0f, 45f),  SerializeField] private float armOpenAngle = 14f;
    [TabGroup(G, "Idle"), Tooltip("Velocidad de apertura/cierre de los brazos."), Range(0f, 8f),   SerializeField] private float armOpenSpeed = 1.5f;
    [TabGroup(G, "Idle"), Tooltip("Tiempo mínimo (s) entre parpadeos."), Range(0.5f, 8f), SerializeField] private float blinkMinInterval = 2.5f;
    [TabGroup(G, "Idle"), Tooltip("Tiempo máximo (s) entre parpadeos. Cada parpadeo elige un intervalo al azar entre min y max."), Range(0.5f, 12f),SerializeField] private float blinkMaxInterval = 6f;
    [TabGroup(G, "Idle"), Tooltip("Duración (s) de un parpadeo (cierre + apertura del ojo)."), Range(0.02f, 0.4f), SerializeField] private float blinkDuration = 0.12f;
    [TabGroup(G, "Idle"), Tooltip("Escala vertical del ojo con el párpado cerrado (0 = cerrado total, 1 = sin parpadeo)."), Range(0f, 1f),   SerializeField] private float blinkClosedScale = 0.1f;

    [TabGroup(G, "Walk"), Tooltip("Profundidad de la respiración al caminar (más agitada que en Idle)."), Range(0f, 0.4f), SerializeField] private float walkBreatheAmp = 0.14f;
    [TabGroup(G, "Walk"), Tooltip("Velocidad de la respiración al caminar."), Range(0f, 16f),  SerializeField] private float walkBreatheSpeed = 5f;
    [TabGroup(G, "Walk"), Tooltip("Altura del rebote de paso, en metros."), Range(0f, 0.3f), SerializeField] private float walkBobAmp = 0.05f;
    [TabGroup(G, "Walk"), Tooltip("Cadencia/velocidad del rebote de paso."), Range(0f, 20f),  SerializeField] private float walkBobSpeed = 7f;
    [TabGroup(G, "Walk"), Tooltip("Ángulo máximo del braceo adelante-atrás (grados). Brazos en oposición."), Range(0f, 60f),  SerializeField] private float armSwingAngle = 30f;
    [TabGroup(G, "Walk"), Tooltip("Cadencia/velocidad del braceo."), Range(0f, 20f),  SerializeField] private float armSwingSpeed = 7f;

    [TabGroup(G, "Reacciones"), Tooltip("Attack — Distancia (m) que la criatura se lanza hacia adelante y vuelve."), Range(0f, 1.5f),  SerializeField] private float attackLungeDist = 0.45f;
    [TabGroup(G, "Reacciones"), Tooltip("Attack — Inclinación hacia adelante (grados) durante el lance."), Range(0f, 30f),   SerializeField] private float attackPitch = 14f;
    [TabGroup(G, "Reacciones"), Tooltip("Attack — Duración total (s) del lance ida y vuelta."), Range(0.1f, 1f),  SerializeField] private float attackDuration = 0.3f;
    [TabGroup(G, "Reacciones"), Tooltip("Hit — Distancia (m) de retroceso al recibir un golpe."), Range(0f, 1f),    SerializeField] private float hitRecoilDist = 0.25f;
    [TabGroup(G, "Reacciones"), Tooltip("Hit — Amplitud (grados) de la sacudida amortiguada al recibir el golpe."), Range(0f, 40f),   SerializeField] private float hitShakeAngle = 12f;
    [TabGroup(G, "Reacciones"), Tooltip("Hit — Duración total (s) de la reacción de golpe."), Range(0.1f, 1f),  SerializeField] private float hitDuration = 0.3f;
    [TabGroup(G, "Reacciones"), Tooltip("Death — Ángulo de vuelco hacia atrás (grados). Queda tumbado al final."), Range(0f, 90f),   SerializeField] private float deathToppleAngle = 85f;
    [TabGroup(G, "Reacciones"), Tooltip("Death — Escala final al morir (0.7 = encoge al 70%)."), Range(0f, 1f),    SerializeField] private float deathShrink = 0.7f;
    [TabGroup(G, "Reacciones"), Tooltip("Death — Duración (s) de la caída hasta quedar tumbado."), Range(0.1f, 2f),  SerializeField] private float deathDuration = 0.5f;
    [TabGroup(G, "Reacciones"), Tooltip("Victory — Altura (m) de los saltitos de celebración."), Range(0f, 1f),    SerializeField] private float victoryHopHeight = 0.25f;
    [TabGroup(G, "Reacciones"), Tooltip("Victory — Cantidad de saltitos durante la celebración."), Range(1, 6),      SerializeField] private int   victoryHops = 3;
    [TabGroup(G, "Reacciones"), Tooltip("Victory — Duración total (s) de la celebración."), Range(0.2f, 2f),  SerializeField] private float victoryDuration = 0.7f;

    private bool captured;
    private Transform root, body;
    private Transform[] arms, eyes;
    private Vector3 rootPos0, rootScale0, bodyScale0;
    private Quaternion rootRot0;
    private Quaternion[] armRot0;
    private Vector3[] eyeScale0;

    private float time;
    private bool oneShotActive;
    private MMAnimationType oneShot;
    private float oneShotElapsed;

    private float blinkCountdown;
    private float blinkProgress;

    private NavMeshAgent navAgent;

    private void Awake()
    {
        if (visualizer == null) visualizer = GetComponent<MoriMonchiVisualizer>();
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null) navAgent = GetComponentInParent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        if (playOnEnable) loop = defaultLoop;
        blinkCountdown = Random.Range(blinkMinInterval, blinkMaxInterval);
    }

    public void PlayMMAnimation(MMAnimationType type)
    {
        switch (type)
        {
            case MMAnimationType.Idle:
            case MMAnimationType.Walk:
                loop = type;
                dead = false;
                if (oneShotActive && oneShot == MMAnimationType.Death) oneShotActive = false;
                break;
            default:
                oneShot = type;
                oneShotActive = true;
                oneShotElapsed = 0f;
                if (type != MMAnimationType.Death) dead = false;
                break;
        }
    }

    public void PlayIdle()    => PlayMMAnimation(MMAnimationType.Idle);
    public void PlayWalk()    => PlayMMAnimation(MMAnimationType.Walk);
    public void PlayAttack()  => PlayMMAnimation(MMAnimationType.Attack);
    public void PlayHit()     => PlayMMAnimation(MMAnimationType.Hit);
    public void PlayDeath()   => PlayMMAnimation(MMAnimationType.Death);
    public void PlayVictory() => PlayMMAnimation(MMAnimationType.Victory);

    private void LateUpdate()
    {
        if (!EnsureRestPose()) return;

        float dt = Time.deltaTime;
        time += dt;

        if (autoLoopFromMovement && navAgent != null && !dead)//->Remover eventualmente el procedural animator no deberia estar contanimado de esta maenra
        {
            float v = navAgent.velocity.magnitude;
            if (loop == MMAnimationType.Walk) { if (v < walkSpeedThreshold * 0.5f) loop = MMAnimationType.Idle; }
            else if (v > walkSpeedThreshold) loop = MMAnimationType.Walk;
        }

        if (oneShotActive && oneShot == MMAnimationType.Death)
        {
            oneShotElapsed += dt;
            float p = deathDuration <= 0f ? 1f : Mathf.Clamp01(oneShotElapsed / deathDuration);
            ApplyDeath(p);
            if (p >= 1f) { oneShotActive = false; dead = true; }
            return;
        }
        if (dead) { ApplyDeath(1f); return; }

        Vector3 pos = rootPos0;
        Quaternion rot = rootRot0;
        Vector3 scale = rootScale0;

        bool walking = loop == MMAnimationType.Walk;
        pos.y += (walking
            ? Mathf.Abs(Mathf.Sin(time * walkBobSpeed)) * walkBobAmp
            : Mathf.Sin(time * bobSpeed) * bobAmp);

        if (oneShotActive) ApplyTransient(ref pos, ref rot, dt);

        root.SetLocalPositionAndRotation(pos, rot);
        root.localScale = scale;

        ApplyBreathe(walking);
        ApplyArms(walking);
        ApplyBlink(dt);
    }

    private void ApplyTransient(ref Vector3 pos, ref Quaternion rot, float dt)
    {
        float dur = oneShot == MMAnimationType.Attack ? attackDuration
                  : oneShot == MMAnimationType.Hit ? hitDuration
                  : victoryDuration;
        oneShotElapsed += dt;
        float p = dur <= 0f ? 1f : Mathf.Clamp01(oneShotElapsed / dur);
        float e = Mathf.Sin(p * Mathf.PI);

        switch (oneShot)
        {
            case MMAnimationType.Attack:
                pos += Vector3.forward * (attackLungeDist * e);
                rot *= Quaternion.Euler(attackPitch * e, 0f, 0f);
                break;
            case MMAnimationType.Hit:
                pos += Vector3.back * (hitRecoilDist * e);
                rot *= Quaternion.Euler(0f, 0f, hitShakeAngle * Mathf.Sin(p * Mathf.PI * 8f) * (1f - p));
                break;
            case MMAnimationType.Victory:
                pos += Vector3.up * (victoryHopHeight * Mathf.Abs(Mathf.Sin(p * Mathf.PI * victoryHops)));
                break;
        }

        if (p >= 1f) oneShotActive = false;
    }

    private void ApplyDeath(float p)
    {
        float e = 1f - (1f - p) * (1f - p);
        root.SetLocalPositionAndRotation(rootPos0, rootRot0 * Quaternion.Euler(deathToppleAngle * e, 0f, 0f));
        root.localScale = Vector3.Lerp(rootScale0, rootScale0 * deathShrink, e);

        if (body != null) body.localScale = bodyScale0;
        ResetArms();
        SetEyes(blinkClosedScale);
    }

    private void ApplyBreathe(bool walking)
    {
        if (body == null) return;
        float amp = walking ? walkBreatheAmp : breatheAmp;
        float spd = walking ? walkBreatheSpeed : breatheSpeed;
        float br = Mathf.Sin(time * spd) * amp;
        body.localScale = new Vector3(
            bodyScale0.x * (1f - br * 0.5f),
            bodyScale0.y * (1f + br),
            bodyScale0.z * (1f - br * 0.5f));
    }

    private void ApplyArms(bool walking)
    {
        if (arms == null) return;
        if (walking)
        {
            float s = Mathf.Sin(time * armSwingSpeed) * armSwingAngle;
            SetArm(0, Quaternion.Euler(s, 0f, 0f));
            SetArm(1, Quaternion.Euler(-s, 0f, 0f));
        }
        else
        {
            float a = Mathf.Sin(time * armOpenSpeed) * armOpenAngle;
            SetArm(0, Quaternion.Euler(0f, 0f, a));
            SetArm(1, Quaternion.Euler(0f, 0f, -a));
        }
    }

    private void SetArm(int i, Quaternion offset)
    {
        if (arms == null || i >= arms.Length || arms[i] == null) return;
        arms[i].localRotation = armRot0[i] * offset;
    }

    private void ResetArms()
    {
        if (arms == null) return;
        for (int i = 0; i < arms.Length; i++)
            if (arms[i] != null) arms[i].localRotation = armRot0[i];
    }

    private void ApplyBlink(float dt)
    {
        if (eyes == null) return;
        float closed = 0f;
        blinkCountdown -= dt;
        if (blinkCountdown <= 0f)
        {
            blinkProgress += dt / Mathf.Max(0.01f, blinkDuration);
            closed = Mathf.Sin(Mathf.Clamp01(blinkProgress) * Mathf.PI);
            if (blinkProgress >= 1f)
            {
                blinkProgress = 0f;
                blinkCountdown = Random.Range(blinkMinInterval, blinkMaxInterval);
            }
        }
        SetEyes(Mathf.Lerp(1f, blinkClosedScale, closed));
    }

    private void SetEyes(float f)
    {
        if (eyes == null) return;
        for (int i = 0; i < eyes.Length; i++)
        {
            if (eyes[i] == null) continue;
            var sc = eyeScale0[i];
            eyes[i].localScale = new Vector3(sc.x, sc.y * f, sc.z);
        }
    }

    private bool EnsureRestPose()
    {
        if (visualizer == null) return false;
        var b = visualizer.BodyTransform;
        if (b == null) { captured = false; return false; }
        if (captured && b == body) return true;

        body = b;
        root = visualizer.ModelRoot != null ? visualizer.ModelRoot
             : b.parent != null ? b.parent.parent : b.parent;
        if (root == null) return false;

        rootPos0 = root.localPosition;
        rootRot0 = root.localRotation;
        rootScale0 = root.localScale;
        bodyScale0 = b.localScale;

        arms = visualizer.ArmTransforms;
        armRot0 = new Quaternion[arms != null ? arms.Length : 0];
        for (int i = 0; i < armRot0.Length; i++)
            if (arms[i] != null) armRot0[i] = arms[i].localRotation;

        eyes = visualizer.EyeTransforms;
        eyeScale0 = new Vector3[eyes != null ? eyes.Length : 0];
        for (int i = 0; i < eyeScale0.Length; i++)
            if (eyes[i] != null) eyeScale0[i] = eyes[i].localScale;

        captured = true;
        return true;
    }
}
}
