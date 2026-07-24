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
        [Tooltip("Probabilidad de que un tramo de movimiento se haga volando.")]
        [Range(0f, 1f)] [SerializeField] private float flyChance = 0.25f;
        [Tooltip("Crossfade del aterrizaje (FlyDown) al terminar un tramo volando.")]
        [SerializeField] private float flyLandCrossFade = 0.25f;

        private string currentState = "";
        private bool flying;

        private void Update()
        {
            var anim = visualizer != null ? visualizer.Animator : null;
            if (anim == null || !anim.isActiveAndEnabled) return;

            if (combatDriver != null && combatDriver.IsBusy)
            {
                currentState = "";
                flying = false;
                return;
            }

            string rawTarget;
            if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh)
            {
                rawTarget = "Idle";
                flying = false;
            }
            else
            {
                float speed = navAgent.velocity.magnitude;
                if (speed >= runThreshold)
                    rawTarget = "Run";
                else if (speed >= walkThreshold)
                    rawTarget = "Walk";
                else
                    rawTarget = "Idle";
            }

            bool isMoving = rawTarget == "Walk" || rawTarget == "Run";
            bool wasIdle = currentState == "Idle" || currentState == "";

            if (isMoving && wasIdle)
            {
                flying = Random.value < flyChance;
            }

            if (!isMoving && flying)
            {
                flying = false;
                anim.CrossFadeInFixedTime("FlyDown", flyLandCrossFade);
                currentState = "Idle";
                return;
            }

            string target = isMoving && flying ? "Fly" : rawTarget;

            if (target != currentState)
            {
                anim.CrossFadeInFixedTime(target, crossFade);
                currentState = target;
            }
        }
    }
}
