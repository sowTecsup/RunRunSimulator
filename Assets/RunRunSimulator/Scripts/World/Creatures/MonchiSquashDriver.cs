using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class MonchiSquashDriver : MonoBehaviour
{
    [Required, SerializeField] private MoriMochiAgent agent;
    [Required, SerializeField] private Transform pivot;
    [Required, SerializeField] private Transform counter;

    [Title("Stretch by speed")]
    [Tooltip("Below this speed the body keeps its rest shape.")]
    [Min(0f)] [SerializeField] private float minSpeed = 3f;
    [Tooltip("Extra stretch per unit of speed above minSpeed.")]
    [Min(0f)] [SerializeField] private float stretchPerSpeed = 0.045f;
    [Range(1f, 2.5f)] [SerializeField] private float maxStretch = 1.6f;
    [Min(0f)] [SerializeField] private float stretchSmoothing = 14f;
    [Min(0f)] [SerializeField] private float axisSmoothing = 18f;

    [Title("Impact spring")]
    [Min(0f)] [SerializeField] private float springStiffness = 260f;
    [Min(0f)] [SerializeField] private float springDamping = 10f;
    [Tooltip("Positive = stretch along the axis, negative = squash. Each pulse is an instant offset the spring then settles.")]
    [SerializeField] private float throwPulse = 0.28f;
    [SerializeField] private float bouncePulse = -0.3f;
    [SerializeField] private float landPulse = -0.38f;
    [SerializeField] private float getUpPulse = 0.18f;
    [SerializeField] private float tellPulse = -0.14f;
    [SerializeField] private float hitPulse = 0.32f;

    private const float MinScale = 0.45f;
    private const float MaxScale = 1.9f;
    private const float SpringStep = 1f / 120f;
    private const float MaxSpringDt = 0.034f;

    private float stretch = 1f;
    private float pulse;
    private float pulseVelocity;
    private Vector3 axis = Vector3.up;
    private bool posed;

    private void OnEnable()
    {
        if (agent == null) return;
        agent.onThrow.AddListener(OnThrow);
        agent.onBounce.AddListener(OnBounce);
        agent.onLand.AddListener(OnLand);
        agent.onGetUp.AddListener(OnGetUp);
        agent.onClashTell.AddListener(OnClashTell);
        agent.onClashHit.AddListener(OnClashHit);
    }

    private void OnDisable()
    {
        if (agent != null)
        {
            agent.onThrow.RemoveListener(OnThrow);
            agent.onBounce.RemoveListener(OnBounce);
            agent.onLand.RemoveListener(OnLand);
            agent.onGetUp.RemoveListener(OnGetUp);
            agent.onClashTell.RemoveListener(OnClashTell);
            agent.onClashHit.RemoveListener(OnClashHit);
        }

        stretch = 1f;
        pulse = 0f;
        pulseVelocity = 0f;
        axis = Vector3.up;
        ClearPose();
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || agent == null || pivot == null || counter == null) return;

        Vector3 velocity = agent.Velocity;
        float speed = velocity.magnitude;
        float targetStretch = 1f;
        float axisBlend = 1f - Mathf.Exp(-axisSmoothing * dt);

        if (speed > minSpeed && !agent.IsHeld)
        {
            targetStretch = Mathf.Min(maxStretch, 1f + (speed - minSpeed) * stretchPerSpeed);
            axis = Vector3.Slerp(axis, velocity / speed, axisBlend);
        }
        else if (Mathf.Abs(pulse) < 0.01f)
        {
            axis = Vector3.Slerp(axis, Vector3.up, axisBlend);
        }

        stretch = Mathf.Lerp(stretch, targetStretch, 1f - Mathf.Exp(-stretchSmoothing * dt));

        float remaining = Mathf.Min(dt, MaxSpringDt);
        while (remaining > 0f)
        {
            float h = Mathf.Min(remaining, SpringStep);
            pulseVelocity += (-springStiffness * pulse - springDamping * pulseVelocity) * h;
            pulse += pulseVelocity * h;
            remaining -= h;
        }

        float s = Mathf.Clamp(stretch * (1f + pulse), MinScale, MaxScale);
        if (Mathf.Abs(s - 1f) < 0.004f && Mathf.Abs(pulseVelocity) < 0.02f)
        {
            if (posed) ClearPose();
            return;
        }

        Apply(s);
    }

    private void Apply(float s)
    {
        pivot.rotation = Quaternion.FromToRotation(Vector3.up, axis);
        float side = 1f / Mathf.Sqrt(s);
        pivot.localScale = new Vector3(side, s, side);
        counter.localRotation = Quaternion.Inverse(pivot.localRotation);
        posed = true;
    }

    private void ClearPose()
    {
        if (pivot != null)
        {
            pivot.localRotation = Quaternion.identity;
            pivot.localScale = Vector3.one;
        }
        if (counter != null) counter.localRotation = Quaternion.identity;
        posed = false;
    }

    private void Kick(float amount)
    {
        pulse = amount;
        pulseVelocity = 0f;
    }

    private void OnThrow()
    {
        Vector3 v = agent.Velocity;
        if (v.sqrMagnitude > 0.01f) axis = v.normalized;
        Kick(throwPulse);
    }

    private void OnBounce() => Kick(bouncePulse);

    private void OnLand()
    {
        axis = Vector3.up;
        stretch = 1f;
        Kick(landPulse);
    }

    private void OnGetUp()
    {
        axis = Vector3.up;
        Kick(getUpPulse);
    }

    private void OnClashTell()
    {
        axis = Vector3.up;
        Kick(tellPulse);
    }

    private void OnClashHit()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        axis = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.up;
        Kick(hitPulse);
    }
}
}
