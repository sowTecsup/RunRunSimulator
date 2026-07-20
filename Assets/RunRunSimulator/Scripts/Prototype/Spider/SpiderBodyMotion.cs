using UnityEngine;

namespace MoriMonchiSimulator.Prototype
{
    public class SpiderBodyMotion : MonoBehaviour
    {
        [SerializeField] private SpiderTuningSO tuning;
        [SerializeField] private Transform visualPivot;
        [SerializeField] private SpiderRagdollMode ragdollMode;
        [SerializeField] private float breatheFrequency = 1.4f;
        [SerializeField] private float bobFrequency = 9f;
        [SerializeField] private float maxSpeedReference = 1.5f;
        [SerializeField] private float actionFrequency = 2.5f;
        [SerializeField] private float actionDamping = 0.35f;

        private Vector3 basePos;
        private Quaternion baseRot;
        private Vector3 lastPos;
        private float lastYaw;
        private Vector3 vel;
        private float yawRate;
        private float bobPhase;
        private float pitch;
        private float roll;
        private float actionPitch;
        private float actionPitchVelocity;

        private void Awake()
        {
            if (visualPivot != null)
            {
                basePos = visualPivot.localPosition;
                baseRot = visualPivot.localRotation;
            }
            lastPos = transform.position;
            lastYaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            if (visualPivot == null || tuning == null) return;
            if (ragdollMode != null && ragdollMode.IsRagdoll)
            {
                visualPivot.localPosition = basePos;
                visualPivot.localRotation = baseRot;
                actionPitch = 0f;
                actionPitchVelocity = 0f;
                return;
            }

            float dt = Time.deltaTime;
            if (dt < 0.0001f) return;

            Vector3 rawVel = (transform.position - lastPos) / dt;
            rawVel.y = 0f;
            if (rawVel.sqrMagnitude > 25f) rawVel = Vector3.zero;
            vel = Vector3.Lerp(vel, rawVel, 1f - Mathf.Exp(-8f * dt));
            lastPos = transform.position;

            float yaw = transform.eulerAngles.y;
            float rawYawRate = Mathf.DeltaAngle(lastYaw, yaw) / dt;
            if (Mathf.Abs(rawYawRate) > 720f) rawYawRate = 0f;
            yawRate = Mathf.Lerp(yawRate, rawYawRate, 1f - Mathf.Exp(-8f * dt));
            lastYaw = yaw;

            float speed01 = Mathf.Clamp01(vel.magnitude / Mathf.Max(maxSpeedReference, 0.01f));

            float idle = tuning.idleAmount;
            float t = Time.time;
            float breatheY = 0.014f * idle * Mathf.Sin(t * breatheFrequency * 2f * Mathf.PI);
            float idlePitch = 1.2f * idle * (Mathf.PerlinNoise(t * 0.35f, 0.2f) - 0.5f) * 2f;
            float idleRoll = 1.2f * idle * (Mathf.PerlinNoise(0.7f, t * 0.3f) - 0.5f) * 2f;

            bobPhase += dt * Mathf.Lerp(0f, bobFrequency, speed01);
            float bobY = tuning.bobAmount * speed01 * Mathf.Sin(bobPhase);

            float lean = tuning.leanAmount;
            float fwdSpeed = Vector3.Dot(vel, transform.forward);
            float targetPitch = lean * 5f * Mathf.Clamp(fwdSpeed / Mathf.Max(maxSpeedReference, 0.01f), -1f, 1f);
            float targetRoll = lean * -6f * Mathf.Clamp(yawRate / 120f, -1f, 1f);
            pitch = Mathf.Lerp(pitch, targetPitch, 1f - Mathf.Exp(-6f * dt));
            roll = Mathf.Lerp(roll, targetRoll, 1f - Mathf.Exp(-6f * dt));

            float actionOmega = 2f * Mathf.PI * actionFrequency;
            actionPitchVelocity += (0f - actionPitch) * actionOmega * actionOmega * dt;
            actionPitchVelocity -= actionPitchVelocity * 2f * actionDamping * actionOmega * dt;
            actionPitch += actionPitchVelocity * dt;
            actionPitch = Mathf.Clamp(actionPitch, -45f, 45f);

            visualPivot.localPosition = basePos + Vector3.up * (breatheY + bobY);
            visualPivot.localRotation = baseRot * Quaternion.Euler(pitch + idlePitch + actionPitch, 0f, roll + idleRoll);
        }

        public void AddPitchImpulse(float degrees)
        {
            actionPitchVelocity += degrees * 2f * Mathf.PI * actionFrequency;
        }
    }
}
