using UnityEngine;

namespace MoriMonchiSimulator.Prototype
{
    public class SpiderLegStepper : MonoBehaviour
    {
        [SerializeField] private SpiderTuningSO tuning;
        [SerializeField] private Transform hip;
        [SerializeField] private Vector3 homeOffset = new Vector3(0.45f, -1.16f, 0f);
        [SerializeField] private float stepDistance = 0.55f;
        [SerializeField] private float stepHeight = 0.3f;
        [SerializeField] private float stepDuration = 0.18f;
        [SerializeField] private LayerMask groundMask = 1;
        [SerializeField] private float rayUp = 1.5f;
        [SerializeField] private float rayLength = 5f;
        [SerializeField] private bool drawGizmos = true;

        private Vector3 footPosition;
        private bool isStepping;
        private float stepT;
        private Vector3 stepFrom;
        private Vector3 stepTo;
        private Vector3 lastHome;
        private Vector3 lastHipPos;
        private Vector3 hipVelocity;
        private float restTimer;
        private float currentStepDuration;
        private bool moving;
        private float lastUrgencyScale = 1f;

        public bool IsStepping { get { return isStepping; } }
        public Vector3 FootPosition { get { return footPosition; } }
        public Vector3 Home { get { return lastHome; } }
        public float Drag { get { return (footPosition - lastHome).magnitude; } }
        public bool WantsStep { get { return !isStepping && restTimer <= 0f && (Drag > (moving ? StepDistance : StepDistance * 1.6f) || Twist > MaxTwist); } }

        public float Twist
        {
            get
            {
                if (hip == null) return 0f;
                Vector3 a = footPosition - hip.position;
                Vector3 b = lastHome - hip.position;
                a.y = 0f;
                b.y = 0f;
                if (a.sqrMagnitude < 0.0004f || b.sqrMagnitude < 0.0004f) return 0f;
                return Vector3.Angle(a, b);
            }
        }

        private float StepDistance { get { return tuning != null ? tuning.stepDistance : stepDistance; } }
        private float StepHeight { get { return tuning != null ? tuning.stepHeight : stepHeight; } }
        private float StepDuration { get { return tuning != null ? tuning.stepDuration : stepDuration; } }
        private float FootSplay { get { return tuning != null ? tuning.footSplay : 1f; } }
        private float StepOvershoot { get { return tuning != null ? tuning.stepOvershoot : 0f; } }
        private float Anticipation { get { return tuning != null ? tuning.anticipation : 0f; } }
        private float MaxTwist { get { return tuning != null ? tuning.maxTwist : 999f; } }

        private Vector3 GroundedHome()
        {
            Vector3 off = homeOffset;
            float splay = FootSplay;
            off.x *= splay;
            off.z *= splay;
            Vector3 home = hip.TransformPoint(off) + hipVelocity * Anticipation;
            RaycastHit hit;
            if (Physics.Raycast(home + Vector3.up * rayUp, Vector3.down, out hit, rayLength, groundMask))
            {
                return hit.point;
            }
            return home;
        }

        private void Start()
        {
            footPosition = GroundedHome();
            lastHome = footPosition;
            isStepping = false;
            lastHipPos = hip.position;
        }

        public void Tick(bool mayStep)
        {
            if (hip == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            if (dt > 0.0001f)
            {
                Vector3 rawVel = (hip.position - lastHipPos) / dt;
                rawVel.y = 0f;
                if (rawVel.sqrMagnitude > 25f) rawVel = Vector3.zero;
                hipVelocity = Vector3.Lerp(hipVelocity, rawVel, 1f - Mathf.Exp(-10f * dt));
            }
            lastHipPos = hip.position;

            moving = hipVelocity.magnitude > 0.05f;
            if (restTimer > 0f) restTimer -= dt;

            Vector3 home = GroundedHome();
            lastHome = home;

            if (isStepping)
            {
                stepT += Time.deltaTime / Mathf.Max(currentStepDuration, 0.0001f);
                if (stepT >= 1f)
                {
                    footPosition = stepTo;
                    isStepping = false;
                    restTimer = 0.09f * lastUrgencyScale;
                }
                else
                {
                    float ts = stepT * stepT * (3f - 2f * stepT);
                    footPosition = Vector3.Lerp(stepFrom, stepTo, ts) + Vector3.up * (StepHeight * Mathf.Sin(stepT * Mathf.PI));
                }
            }
            else
            {
                if (mayStep && WantsStep)
                {
                    isStepping = true;
                    stepT = 0f;
                    float threshold = moving ? StepDistance : StepDistance * 1.6f;
                    float urgency = Mathf.Max(Drag / Mathf.Max(threshold, 0.01f), Twist / Mathf.Max(MaxTwist, 1f));
                    lastUrgencyScale = Mathf.Clamp(1f / Mathf.Sqrt(Mathf.Max(urgency, 1f)), 0.5f, 1f);
                    currentStepDuration = (moving ? StepDuration : StepDuration * 1.8f) * lastUrgencyScale;
                    stepFrom = footPosition;
                    Vector3 catchUp = home - footPosition;
                    catchUp.y = 0f;
                    stepTo = moving && catchUp.sqrMagnitude > 0.0001f ? home + catchUp.normalized * StepOvershoot : home;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || hip == null) return;

            Gizmos.color = isStepping ? Color.red : Color.green;
            Gizmos.DrawSphere(footPosition, 0.05f);

            Vector3 home = Application.isPlaying ? GroundedHome() : hip.TransformPoint(homeOffset);

            Gizmos.color = new Color(1f, 0.85f, 0f, 0.9f);
            Gizmos.DrawWireSphere(home, 0.05f);
            Gizmos.DrawLine(home, footPosition);

            Gizmos.color = new Color(1f, 0.85f, 0f, 0.25f);
            Gizmos.DrawWireSphere(home, StepDistance);

            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.6f);
            Vector3 rayFrom = hip.TransformPoint(homeOffset) + Vector3.up * rayUp;
            Gizmos.DrawLine(rayFrom, rayFrom + Vector3.down * rayLength);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(hip.position, footPosition);

            if (isStepping)
            {
                Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.8f);
                Vector3 prev = stepFrom;
                for (int i = 1; i <= 12; i++)
                {
                    float t = i / 12f;
                    Vector3 p = Vector3.Lerp(stepFrom, stepTo, t) + Vector3.up * (StepHeight * Mathf.Sin(t * Mathf.PI));
                    Gizmos.DrawLine(prev, p);
                    prev = p;
                }
            }

            if (Application.isPlaying && WantsStep)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(footPosition + Vector3.up * 0.02f, lastHome + Vector3.up * 0.02f);
                Gizmos.DrawWireSphere(footPosition, 0.09f);
            }

            if (Application.isPlaying && hipVelocity.sqrMagnitude > 0.001f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(hip.position, hip.position + hipVelocity * Mathf.Max(Anticipation, 0.05f));
            }
        }
    }
}
