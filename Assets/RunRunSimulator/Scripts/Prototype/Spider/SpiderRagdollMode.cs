using UnityEngine;

namespace MoriMonchiSimulator.Prototype
{
    public class SpiderRagdollMode : MonoBehaviour
    {
        [SerializeField] private bool ragdoll;
        [SerializeField] private SpiderBodyController controller;
        [SerializeField] private SpiderLegIK[] legIks;
        [SerializeField] private Rigidbody[] bodies;
        [SerializeField] private bool drawGizmos = true;

        private bool applied;
        private bool hasApplied;

        public bool IsRagdoll { get { return ragdoll; } }

        public void SetRagdoll(bool value)
        {
            ragdoll = value;
            Apply();
        }

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            if (!hasApplied || applied != ragdoll) Apply();
        }

        private void Apply()
        {
            applied = ragdoll;
            hasApplied = true;

            if (controller != null) controller.enabled = !ragdoll;

            if (legIks != null)
            {
                for (int i = 0; i < legIks.Length; i++)
                {
                    if (legIks[i] != null) legIks[i].enabled = !ragdoll;
                }
            }

            if (bodies != null)
            {
                for (int i = 0; i < bodies.Length; i++)
                {
                    var rb = bodies[i];
                    if (rb == null) continue;
                    if (ragdoll)
                    {
                        rb.position = rb.transform.position;
                        rb.rotation = rb.transform.rotation;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                        rb.isKinematic = false;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    else
                    {
                        rb.isKinematic = true;
                        rb.interpolation = RigidbodyInterpolation.None;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            Gizmos.color = ragdoll ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2.2f, Vector3.one * 0.18f);
        }
    }
}
