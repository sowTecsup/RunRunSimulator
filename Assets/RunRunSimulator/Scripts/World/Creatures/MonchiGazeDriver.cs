using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace MoriMonchiSimulator
{
    public class MonchiGazeDriver : MonoBehaviour
    {
        [Required, SerializeField] private MoriMochiAgent agent;
        [Required, SerializeField] private MonchiVisualizer visualizer;
        [Required, SerializeField] private NavMeshAgent navAgent;
        [SerializeField] private DragonAnimationDriver combatDriver;
        [SerializeField] private float maxYaw = 70f;
        [SerializeField] private float turnSpeed = 240f;
        [SerializeField] private float stillSpeed = 0.15f;
        [SerializeField] private float maxDistance = 8f;

        private float currentYaw;

        private void LateUpdate()
        {
            float desired = 0f;

            bool canGaze = visualizer.ModelRoot != null
                && (combatDriver == null || !combatDriver.IsBusy)
                && !agent.IsHeld && !agent.IsAirborne && !agent.IsRecovering
                && (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh || navAgent.velocity.magnitude < stillSpeed);

            if (canGaze)
            {
                Transform target = agent.ExpeditionTarget;
                if (target == null) target = agent.SocialPartner != null ? agent.SocialPartner.transform : null;
                if (target == null) target = FindPerceptTarget();

                if (target != null)
                {
                    Vector3 to = target.position - transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude > 0.01f)
                        desired = Mathf.Clamp(Vector3.SignedAngle(transform.forward, to.normalized, Vector3.up), -maxYaw, maxYaw);
                }
            }

            currentYaw = Mathf.MoveTowardsAngle(currentYaw, desired, turnSpeed * Time.deltaTime);
            if (visualizer.ModelRoot != null)
                visualizer.ModelRoot.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
        }

        private Transform FindPerceptTarget()
        {
            float maxSqr = maxDistance * maxDistance;
            var percepts = agent.Percepts;
            for (int i = 0; i < percepts.Count; i++)
            {
                var percept = percepts[i];
                if (percept.Source == null) continue;
                if (percept.SqrDistance > maxSqr) continue;
                if (percept.Kind != PerceivableKind.Monchi && percept.Kind != PerceivableKind.Player && percept.Kind != PerceivableKind.Material) continue;
                return percept.Source.transform;
            }
            return null;
        }

        private void OnDisable()
        {
            currentYaw = 0f;
            if (visualizer != null && visualizer.ModelRoot != null)
                visualizer.ModelRoot.localRotation = Quaternion.identity;
        }
    }
}
