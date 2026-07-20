using UnityEngine;
using UnityEngine.InputSystem;

namespace MoriMonchiSimulator.Prototype
{
    public class SpiderBodyController : MonoBehaviour
    {
        [SerializeField] private SpiderTuningSO tuning;
        [SerializeField] private SpiderJump jump;
        [SerializeField] private SpiderLegStepper[] legs;
        [SerializeField] private SpiderLegIK[] legIks;
        [SerializeField] private int[] gaitGroup = new int[] { 0, 1, 2 };
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private float turnSpeed = 90f;
        [SerializeField] private float rideHeight = 0.55f;
        [SerializeField] private LayerMask groundMask = 1;
        [SerializeField] private float rayUp = 2f;
        [SerializeField] private float rayLength = 8f;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private bool autoWalk;
        [SerializeField] private float autoTurn;
        [SerializeField] private bool externalMotion;

        private bool useExternalDrive;
        private float externalForward;
        private float externalTurn;
        private float externalLastYaw;

        public bool AutoWalk { get { return autoWalk; } set { autoWalk = value; } }
        public float AutoTurn { get { return autoTurn; } set { autoTurn = value; } }
        public bool ExternalMotion { get { return externalMotion; } set { externalMotion = value; } }

        public void SetExternalDrive(float forward, float turn)
        {
            useExternalDrive = true;
            externalForward = Mathf.Clamp(forward, -1f, 1f);
            externalTurn = Mathf.Clamp(turn, -1f, 1f);
        }

        public void ClearExternalDrive()
        {
            useExternalDrive = false;
            externalForward = 0f;
            externalTurn = 0f;
        }

        private void Awake()
        {
            externalLastYaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            if (legs == null || legIks == null || legs.Length != legIks.Length)
            {
                return;
            }

            bool turning;

            if (externalMotion)
            {
                float yaw = transform.eulerAngles.y;
                float yawRate = Mathf.DeltaAngle(externalLastYaw, yaw) / Mathf.Max(Time.deltaTime, 0.0001f);
                externalLastYaw = yaw;
                turning = Mathf.Abs(yawRate) > 25f;
            }
            else
            {
                var kb = Keyboard.current;
                float turn = 0f;
                float fwd = 0f;
                if (kb != null)
                {
                    turn = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
                    fwd = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
                }
                if (autoWalk) fwd = 1f;
                if (autoTurn != 0f) turn = Mathf.Clamp(autoTurn, -1f, 1f);

                if (useExternalDrive)
                {
                    fwd = externalForward;
                    turn = externalTurn;
                }

                turning = Mathf.Abs(turn) > 0.01f;

                float spd = tuning != null ? tuning.moveSpeed : moveSpeed;
                float turnSpd = tuning != null ? tuning.turnSpeed : turnSpeed;
                float ride = tuning != null ? tuning.rideHeight : rideHeight;

                transform.Rotate(0f, turn * turnSpd * Time.deltaTime, 0f);
                transform.position += transform.forward * (fwd * spd * Time.deltaTime);

                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up * rayUp, Vector3.down, out hit, rayLength, groundMask))
                {
                    Vector3 p = transform.position;
                    p.y = hit.point.y + ride + (jump != null ? jump.HeightOffset : 0f);
                    transform.position = p;
                }

                externalLastYaw = transform.eulerAngles.y;
            }

            int best = -1;
            float bestDrag = 0f;
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null || !legs[i].WantsStep) continue;
                int gi = (gaitGroup != null && i < gaitGroup.Length) ? gaitGroup[i] : 0;
                if (gi < 0) continue;
                bool blocked = false;
                for (int j = 0; j < legs.Length; j++)
                {
                    if (j == i || legs[j] == null) continue;
                    int gj = (gaitGroup != null && j < gaitGroup.Length) ? gaitGroup[j] : 0;
                    if (gj < 0) continue;
                    if (gj != gi && legs[j].IsStepping) { blocked = true; break; }
                }
                if (blocked) continue;
                if (legs[i].Drag > bestDrag)
                {
                    bestDrag = legs[i].Drag;
                    best = i;
                }
            }
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                int gi = (gaitGroup != null && i < gaitGroup.Length) ? gaitGroup[i] : 0;
                legs[i].Tick(gi < 0 || i == best, turning);
            }

            for (int i = 0; i < legIks.Length; i++)
            {
                if (legIks[i] == null || legs[i] == null) continue;
                legIks[i].SolveTo(legs[i].FootPosition);
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.9f);
            Vector3 rayFrom = transform.position + Vector3.up * rayUp;
            Gizmos.DrawLine(rayFrom, rayFrom + Vector3.down * rayLength);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.8f);

            Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, 0.08f);

            if (legs == null) return;
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                int g = (gaitGroup != null && i < gaitGroup.Length) ? gaitGroup[i] : 0;
                Gizmos.color = g == 0 ? Color.cyan : (g == 1 ? Color.magenta : Color.yellow);
                Gizmos.DrawLine(transform.position, legs[i].FootPosition);
            }
        }
    }
}
