using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace MoriMonchiSimulator
{
    public class MonchiLocomotionAnimator : MonoBehaviour
    {
        [Required, SerializeField] private MonchiVisualizer visualizer;
        [Required, SerializeField] private NavMeshAgent navAgent;
        [SerializeField] private DragonAnimationDriver combatDriver;
        [SerializeField] private float walkThreshold = 0.15f;
        [SerializeField] private float runThreshold = 2.6f;
        [SerializeField] private float crossFade = 0.2f;

        private string currentState = "";

        private void Update()
        {
            var anim = visualizer != null ? visualizer.Animator : null;
            if (anim == null || !anim.isActiveAndEnabled) return;

            if (combatDriver != null && combatDriver.IsBusy)
            {
                currentState = "";
                return;
            }

            string target;
            if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh)
            {
                target = "Idle";
            }
            else
            {
                float speed = navAgent.velocity.magnitude;
                if (speed >= runThreshold)
                    target = "Run";
                else if (speed >= walkThreshold)
                    target = "Walk";
                else
                    target = "Idle";
            }

            if (target != currentState)
            {
                anim.CrossFadeInFixedTime(target, crossFade);
                currentState = target;
            }
        }
    }
}
