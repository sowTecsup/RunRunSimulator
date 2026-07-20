using UnityEngine;

namespace MoriMonchiSimulator.Prototype
{
    public class SpiderElasticBody : MonoBehaviour
    {
        [SerializeField] private SpiderTuningSO tuning;
        [SerializeField] private Transform visualPivot;
        [SerializeField] private SpiderRagdollMode ragdollMode;
        [SerializeField] private float frequency = 3f;
        [SerializeField] private float dampingRatio = 0.25f;
        [SerializeField] private float speedReference = 3f;

        private Vector3 baseScale;
        private float scaleValue = 1f;
        private float scaleVelocity;
        private float lastY;

        private void Awake()
        {
            if (visualPivot != null) baseScale = visualPivot.localScale;
            lastY = transform.position.y;
        }

        public void AddImpulse(float velocity)
        {
            scaleVelocity += velocity;
        }

        private void Update()
        {
            if (visualPivot == null || tuning == null) return;

            if (ragdollMode != null && ragdollMode.IsRagdoll)
            {
                visualPivot.localScale = baseScale;
                scaleValue = 1f;
                scaleVelocity = 0f;
                lastY = transform.position.y;
                return;
            }

            float dt = Time.deltaTime;
            if (dt < 0.0001f) return;

            float vy = (transform.position.y - lastY) / dt;
            lastY = transform.position.y;

            if (Mathf.Abs(vy) > 12f) vy = 0f;

            float target = 1f + tuning.elasticAmount * 0.35f * Mathf.Clamp(vy / speedReference, -1f, 1f);

            float omega = 2f * Mathf.PI * frequency;
            scaleVelocity += (target - scaleValue) * omega * omega * dt;
            scaleVelocity -= scaleVelocity * 2f * dampingRatio * omega * dt;
            scaleValue += scaleVelocity * dt;
            scaleValue = Mathf.Clamp(scaleValue, 0.55f, 1.6f);

            float xz = 1f / Mathf.Sqrt(scaleValue);
            visualPivot.localScale = new Vector3(baseScale.x * xz, baseScale.y * scaleValue, baseScale.z * xz);
        }
    }
}
