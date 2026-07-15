using UnityEngine;

namespace MoriMonchiSimulator.Prototype
{
    public class SpiderLegIK : MonoBehaviour
    {
        [SerializeField] private Transform upperBone;
        [SerializeField] private Transform lowerBone;
        [SerializeField] private float upperLength = 0.35f;
        [SerializeField] private float lowerLength = 0.4f;
        [SerializeField] private Transform poleTarget;
        [SerializeField] private Vector3 poleFallbackOffset = new Vector3(0f, 1f, 0.5f);
        [SerializeField] private bool drawGizmos = true;

        private Vector3 kneePosition;
        private Vector3 polePosition;

        public Vector3 KneePosition => kneePosition;
        public Vector3 PolePosition => polePosition;

        public void SolveTo(Vector3 target)
        {
            if (upperBone == null || lowerBone == null)
            {
                return;
            }

            polePosition = poleTarget != null ? poleTarget.position : upperBone.TransformPoint(poleFallbackOffset);

            Vector3 H = upperBone.position;
            float a = Mathf.Max(upperLength, 0.0001f);
            float b = Mathf.Max(lowerLength, 0.0001f);
            Vector3 toTarget = target - H;
            float d = toTarget.magnitude;
            Vector3 dirT = d < 0.0001f ? Vector3.down : toTarget / d;
            d = Mathf.Clamp(d, Mathf.Abs(a - b) + 0.001f, a + b - 0.001f);

            Vector3 poleDir = polePosition - H;
            Vector3 axis = Vector3.Cross(dirT, poleDir);
            if (axis.sqrMagnitude < 0.000001f) axis = Vector3.Cross(dirT, Vector3.forward);
            if (axis.sqrMagnitude < 0.000001f) axis = Vector3.Cross(dirT, Vector3.right);
            axis.Normalize();

            float cosA = Mathf.Clamp((a * a + d * d - b * b) / (2f * a * d), -1f, 1f);
            float alpha = Mathf.Acos(cosA) * Mathf.Rad2Deg;
            Vector3 upperDir = Quaternion.AngleAxis(alpha, axis) * dirT;
            kneePosition = H + upperDir * a;
            Vector3 toFoot = target - kneePosition;
            Vector3 lowerDir = toFoot.sqrMagnitude < 0.000001f ? upperDir : toFoot.normalized;

            upperBone.rotation = Quaternion.LookRotation(Vector3.Cross(axis, upperDir), upperDir);
            lowerBone.rotation = Quaternion.LookRotation(Vector3.Cross(axis, lowerDir), lowerDir);
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || upperBone == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(upperBone.position, kneePosition);
            Gizmos.DrawLine(kneePosition, kneePosition + lowerBone.up * lowerLength);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(kneePosition, 0.04f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(polePosition, 0.05f);
            Gizmos.DrawLine(upperBone.position, polePosition);

            Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
            Gizmos.DrawWireSphere(upperBone.position, upperLength + lowerLength);
        }
    }
}
