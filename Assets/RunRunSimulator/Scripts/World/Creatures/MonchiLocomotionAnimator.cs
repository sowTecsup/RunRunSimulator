using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

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
        [SerializeField] private UnityEvent onTakeOff;
        [SerializeField] private UnityEvent onFlyLand;
        [SerializeField] private float gestureCrossFade = 0.15f;
        [SerializeField] private bool turnClips = true;
        [SerializeField] private float turnThreshold = 70f;
        [SerializeField] private float turnSmoothing = 8f;

        private string currentState = "";
        private bool flying;
        private string gestureState = "";
        private float gestureUntil;
        private bool gestureHeld;
        private float lastYaw;
        private float yawRate;
        private bool turning;
        private readonly Dictionary<string, bool> hasStateCache = new();
        private readonly Dictionary<string, float> clipLengthCache = new();

        public bool IsGesturing => gestureState != "" && (gestureHeld || Time.time < gestureUntil);
        public bool IsStill { get; private set; }

        private void Awake()
        {
            lastYaw = transform.eulerAngles.y;
        }

        private Animator GetAnimator() => visualizer != null ? visualizer.Animator : null;
        private bool CanGesture() => (combatDriver == null || !combatDriver.IsBusy) && IsStill;

        public bool PlayGesture(string state)
        {
            var anim = GetAnimator();
            if (anim == null || !CanGesture() || !HasState(anim, state)) return false;
            anim.CrossFadeInFixedTime(state, gestureCrossFade);
            gestureState = state;
            gestureUntil = Time.time + ClipLength(state);
            gestureHeld = false;
            return true;
        }

        public bool HoldGesture(string state)
        {
            if (gestureHeld && gestureState == state) return true;
            var anim = GetAnimator();
            if (anim == null || !CanGesture() || !HasState(anim, state)) return false;
            anim.CrossFadeInFixedTime(state, gestureCrossFade);
            gestureState = state;
            gestureHeld = true;
            return true;
        }

        public void StopGesture()
        {
            gestureState = "";
            gestureHeld = false;
            currentState = "";
        }

        private void Update()
        {
            var anim = GetAnimator();
            if (anim == null || !anim.isActiveAndEnabled) return;

            if (combatDriver != null && combatDriver.IsBusy)
            {
                currentState = "";
                flying = false;
                gestureState = "";
                gestureHeld = false;
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
                rawTarget = speed >= runThreshold ? "Run" : speed >= walkThreshold ? "Walk" : "Idle";
            }

            IsStill = rawTarget == "Idle";
            bool isMoving = !IsStill;

            if (gestureState != "")
            {
                if (isMoving)
                {
                    gestureState = "";
                    gestureHeld = false;
                    currentState = "";
                }
                else if (!gestureHeld && Time.time >= gestureUntil)
                {
                    gestureState = "";
                    currentState = "";
                }
                else
                {
                    return;
                }
            }

            float dt = Time.deltaTime;
            float currentYaw = transform.eulerAngles.y;
            float rawYawRate = dt > 0f ? Mathf.DeltaAngle(lastYaw, currentYaw) / dt : 0f;
            yawRate = Mathf.Lerp(yawRate, rawYawRate, 1f - Mathf.Exp(-turnSmoothing * dt));
            lastYaw = currentYaw;

            if (turnClips && isMoving)
            {
                float absYaw = Mathf.Abs(yawRate);
                turning = turning ? absYaw >= turnThreshold * 0.5f : absYaw >= turnThreshold;
            }
            else
            {
                turning = false;
            }

            bool wasIdle = currentState == "Idle" || currentState == "";

            if (isMoving && wasIdle)
            {
                flying = Random.value < flyChance;
                if (flying) onTakeOff?.Invoke();
            }

            if (!isMoving && flying)
            {
                flying = false;
                anim.CrossFadeInFixedTime("FlyDown", flyLandCrossFade);
                onFlyLand?.Invoke();
                currentState = "Idle";
                return;
            }

            string target = isMoving && flying ? "Fly" : rawTarget;

            if (turning)
            {
                string lateral = target + (yawRate > 0f ? "_R" : "_L");
                if (HasState(anim, lateral)) target = lateral;
            }

            if (target != currentState)
            {
                anim.CrossFadeInFixedTime(target, crossFade);
                currentState = target;
            }
        }

        private bool HasState(Animator anim, string state)
        {
            if (hasStateCache.TryGetValue(state, out bool cached)) return cached;
            bool has = anim.HasState(0, Animator.StringToHash(state));
            hasStateCache[state] = has;
            return has;
        }

        private float ClipLength(string state)
        {
            if (clipLengthCache.TryGetValue(state, out float cached)) return cached;

            float length = 1f;
            var anim = GetAnimator();
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                string target = state.Replace("_", "").ToLowerInvariant();
                AnimationClip best = null;
                foreach (var clip in anim.runtimeAnimatorController.animationClips)
                {
                    string name = clip.name.Replace("_", "").ToLowerInvariant();
                    if (!name.EndsWith(target)) continue;
                    if (best == null || clip.name.Length < best.name.Length) best = clip;
                }
                if (best != null) length = best.length;
            }

            clipLengthCache[state] = length;
            return length;
        }
    }
}
