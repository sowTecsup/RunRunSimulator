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
        [SerializeField] private float rivalMaxDistance = 12f;
        [SerializeField] private float scanYaw = 55f;
        [SerializeField] private float scanPeriod = 6f;
        [SerializeField] private float glanceInterval = 4f;
        [SerializeField] private float glanceHold = 0.8f;
        [SerializeField] private float huntPitch = 12f;
        [SerializeField] private float facePitch = 10f;
        [SerializeField] private float pitchSpeed = 90f;

        private float currentYaw;
        private float currentPitch;
        private float scanPhase;
        private float nextGlanceAt;
        private float glanceUntil;

        private void OnEnable()
        {
            scanPhase = Random.Range(0f, scanPeriod);
            nextGlanceAt = Time.time + Random.Range(0f, glanceInterval);
        }

        private void LateUpdate()
        {
            float desiredYaw = 0f;
            float desiredPitch = 0f;

            bool bodyFree = visualizer.ModelRoot != null
                && (combatDriver == null || !combatDriver.IsBusy)
                && !agent.IsHeld && !agent.IsAirborne && !agent.IsRecovering;

            bool still = navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh || navAgent.velocity.magnitude < stillSpeed;

            if (bodyFree)
            {
                var rival = FindRival();
                var intent = agent.Intent;
                bool hunting = intent == CreatureIntent.Hunting || intent == CreatureIntent.Chasing;
                bool guarding = intent == CreatureIntent.Guarding;
                bool fightOrder = agent.Orders.Contact == ContactChoice.Fight;
                bool fleeOrder = agent.Orders.Contact == ContactChoice.Flee;

                if (hunting)
                    desiredPitch = huntPitch;

                bool yawSet = false;

                if (fleeOrder && rival != null)
                {
                    if (Time.time >= nextGlanceAt)
                    {
                        glanceUntil = Time.time + glanceHold;
                        nextGlanceAt = Time.time + glanceInterval;
                    }

                    if (Time.time < glanceUntil)
                    {
                        desiredYaw = YawTo(rival.position);
                        yawSet = true;
                    }
                }

                if (still && !yawSet)
                {
                    if (guarding)
                    {
                        desiredYaw = rival != null
                            ? YawTo(rival.position)
                            : scanYaw * Mathf.Sin((Time.time + scanPhase) * 2f * Mathf.PI / Mathf.Max(0.1f, scanPeriod));
                    }
                    else if (fightOrder && rival != null)
                    {
                        desiredYaw = YawTo(rival.position);
                        desiredPitch = facePitch;
                    }
                    else
                    {
                        Transform target = agent.ExpeditionTarget;
                        if (target == null) target = agent.SocialPartner != null ? agent.SocialPartner.transform : null;
                        if (target == null) target = FindPerceptTarget();

                        if (target != null)
                            desiredYaw = YawTo(target.position);
                    }
                }
            }

            float dt = Time.deltaTime;
            currentYaw = Mathf.MoveTowardsAngle(currentYaw, desiredYaw, turnSpeed * dt);
            currentPitch = Mathf.MoveTowards(currentPitch, desiredPitch, pitchSpeed * dt);
            if (visualizer.ModelRoot != null)
                visualizer.ModelRoot.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        }

        private float YawTo(Vector3 worldPos)
        {
            Vector3 to = worldPos - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude <= 0.01f) return 0f;
            return Mathf.Clamp(Vector3.SignedAngle(transform.forward, to.normalized, Vector3.up), -maxYaw, maxYaw);
        }

        private Transform FindRival()
        {
            float maxSqr = rivalMaxDistance * rivalMaxDistance;
            var percepts = agent.Percepts;
            for (int i = 0; i < percepts.Count; i++)
            {
                var percept = percepts[i];
                if (percept.Kind != PerceivableKind.Monchi) continue;
                if (percept.Source == null || percept.Source.Monchi == null) continue;
                if (percept.SqrDistance > maxSqr) continue;
                if (!ExpeditionTeams.AreRivals(agent.Team, percept.Team)) continue;
                return percept.Source.transform;
            }
            return null;
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
            currentPitch = 0f;
            if (visualizer != null && visualizer.ModelRoot != null)
                visualizer.ModelRoot.localRotation = Quaternion.identity;
        }
    }
}
