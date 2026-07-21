using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{
    public class DragonAnimationDriver : MonchiAnimationDriver
    {
        [Required, SerializeField] private MonchiVisualizer visualizer;
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private float arriveDistance = 0.06f;
        [SerializeField] private float attackImpactFraction = 0.45f;

        [SerializeField] private float anticipationSeconds = 0.3f;
        [SerializeField] private float anticipationPullback = 0.35f;
        [SerializeField] private float takeoffSeconds = 0.3f;
        [SerializeField] private float flightHeight = 1.1f;
        [SerializeField] private float flightSpeed = 5.5f;
        [SerializeField] private float strikeDistance = 1.1f;
        [SerializeField] private float hitStopSeconds = 0.12f;
        [SerializeField] private float landSeconds = 0.28f;

        private float timeScale = 1f;
        private Coroutine current;

        public override bool IsBusy => current != null;

        private Animator Anim => visualizer != null ? visualizer.Animator : null;

        private void Begin(IEnumerator routine)
        {
            if (current != null)
                StopCoroutine(current);

            current = StartCoroutine(routine);
        }

        private void Finish()
        {
            current = null;
        }

        private float Scaled(float seconds) => seconds / timeScale;

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        private static float EaseIn(float t) => t * t;

        public override void SetTimeScale(float value)
        {
            timeScale = Mathf.Clamp(value, 0.25f, 4f);

            if (Anim != null)
                Anim.speed = timeScale;
        }

        private float ClipLength(string stateName)
        {
            if (Anim == null || Anim.runtimeAnimatorController == null)
                return 1f;

            AnimationClip best = null;
            foreach (var clip in Anim.runtimeAnimatorController.animationClips)
            {
                if (clip == null)
                    continue;

                string compactName = clip.name.Replace("_", string.Empty);
                bool matches = clip.name.IndexOf(stateName, StringComparison.OrdinalIgnoreCase) >= 0
                    || compactName.IndexOf(stateName, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!matches)
                    continue;

                if (best == null || clip.name.Length < best.name.Length)
                    best = clip;
            }

            return best != null ? best.length : 1f;
        }

        private void Fade(string state, float blend = 0.12f)
        {
            if (Anim != null && Anim.isActiveAndEnabled)
                Anim.CrossFadeInFixedTime(state, blend);
        }

        private IEnumerator FaceTowards(Vector3 worldPos)
        {
            Vector3 flatDirection = worldPos - transform.position;
            flatDirection.y = 0f;

            if (flatDirection.sqrMagnitude < 0.0001f)
                yield break;

            Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);

            while (Quaternion.Angle(transform.rotation, targetRotation) > 2f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * timeScale * Time.deltaTime);
                yield return null;
            }

            transform.rotation = targetRotation;
        }

        private IEnumerator MoveLinear(Vector3 from, Vector3 to, float duration, Func<float, float> ease)
        {
            float scaledDuration = Scaled(duration);

            if (scaledDuration <= 0.0001f)
            {
                transform.position = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < scaledDuration)
            {
                elapsed += Time.deltaTime;
                float t = ease(Mathf.Clamp01(elapsed / scaledDuration));
                transform.position = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            transform.position = to;
        }

        private IEnumerator MoveLinearAndRotate(Vector3 fromPos, Vector3 toPos, Quaternion fromRot, Quaternion toRot, float duration, Func<float, float> ease)
        {
            float scaledDuration = Scaled(duration);

            if (scaledDuration <= 0.0001f)
            {
                transform.position = toPos;
                transform.rotation = toRot;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < scaledDuration)
            {
                elapsed += Time.deltaTime;
                float t = ease(Mathf.Clamp01(elapsed / scaledDuration));
                transform.position = Vector3.LerpUnclamped(fromPos, toPos, t);
                transform.rotation = Quaternion.Slerp(fromRot, toRot, t);
                yield return null;
            }

            transform.position = toPos;
            transform.rotation = toRot;
        }

        private IEnumerator MoveAtSpeed(Vector3 from, Vector3 to, float speed, Vector3 faceTarget)
        {
            float scaledSpeed = speed * timeScale;

            if (scaledSpeed <= 0.0001f)
            {
                transform.position = to;
                yield break;
            }

            Vector3 pos = from;
            transform.position = pos;

            while (Vector3.Distance(pos, to) > 0.02f)
            {
                pos = Vector3.MoveTowards(pos, to, scaledSpeed * Time.deltaTime);
                transform.position = pos;
                yield return FaceTowards(faceTarget);
            }

            transform.position = to;
        }

        public override void MoveTo(Vector3 destination, Action onArrived)
        {
            if (visualizer == null || Anim == null)
            {
                onArrived?.Invoke();
                return;
            }

            Begin(MoveToRoutine(destination, onArrived));
        }

        private IEnumerator MoveToRoutine(Vector3 destination, Action onArrived)
        {
            yield return FaceTowards(destination);
            Fade("Walk");

            Vector3 flatDestination = destination;
            flatDestination.y = transform.position.y;

            while (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(flatDestination.x, 0f, flatDestination.z)) > arriveDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, flatDestination, moveSpeed * timeScale * Time.deltaTime);
                yield return FaceTowards(destination);
            }

            Fade("Idle");
            Finish();
            onArrived?.Invoke();
        }

        public override void PlayAttack(Vector3 targetPosition, Action onImpact, Action onFinished)
        {
            if (visualizer == null || Anim == null)
            {
                onImpact?.Invoke();
                onFinished?.Invoke();
                return;
            }

            Begin(PlayAttackRoutine(targetPosition, onImpact, onFinished));
        }

        private IEnumerator PlayAttackRoutine(Vector3 targetPosition, Action onImpact, Action onFinished)
        {
            Vector3 homePos = transform.position;
            Quaternion homeRot = transform.rotation;

            yield return FaceTowards(targetPosition);
            visualizer?.SetMood(MonchiMood.Enojado);

            Vector3 pullbackPos = homePos - transform.forward * anticipationPullback;
            yield return MoveLinear(homePos, pullbackPos, anticipationSeconds * 0.6f, EaseOut);
            yield return new WaitForSeconds(Scaled(anticipationSeconds * 0.4f));

            Fade("FlyUp", 0.1f);
            Vector3 riseStart = transform.position;
            Vector3 riseEnd = new Vector3(riseStart.x, homePos.y + flightHeight, riseStart.z);
            yield return MoveLinear(riseStart, riseEnd, takeoffSeconds, EaseOut);

            Fade("Fly", 0.15f);
            Vector3 flatTarget = new Vector3(targetPosition.x, homePos.y, targetPosition.z);
            Vector3 flatHome = new Vector3(homePos.x, homePos.y, homePos.z);
            Vector3 attackDir = flatTarget - flatHome;
            attackDir.y = 0f;

            if (attackDir.sqrMagnitude < 0.0001f)
                attackDir = transform.forward;

            attackDir.Normalize();

            Vector3 strikeGround = flatTarget - attackDir * strikeDistance;
            Vector3 strikePos = new Vector3(strikeGround.x, homePos.y + flightHeight, strikeGround.z);

            yield return MoveAtSpeed(transform.position, strikePos, flightSpeed, targetPosition);

            Fade("FlyFire", 0.08f);
            float len = ClipLength("FlyFire");
            yield return new WaitForSeconds(Scaled(len * attackImpactFraction));
            onImpact?.Invoke();

            if (Anim != null)
                Anim.speed = 0f;

            yield return new WaitForSeconds(Scaled(hitStopSeconds));

            if (Anim != null)
                Anim.speed = timeScale;

            yield return new WaitForSeconds(Scaled(len * (1f - attackImpactFraction)));

            Fade("Fly", 0.15f);
            Vector3 returnPos = new Vector3(homePos.x, homePos.y + flightHeight, homePos.z);
            yield return MoveAtSpeed(transform.position, returnPos, flightSpeed, homePos);

            Fade("FlyDown", 0.1f);
            Vector3 landStart = transform.position;
            Quaternion rotStart = transform.rotation;
            yield return MoveLinearAndRotate(landStart, homePos, rotStart, homeRot, landSeconds, EaseIn);

            visualizer?.SetMood(MonchiMood.Neutral);
            Fade("Idle");
            Finish();
            onFinished?.Invoke();
        }

        public override void PlayHit(float intensity)
        {
            if (visualizer == null || Anim == null)
                return;

            Begin(PlayHitRoutine());
        }

        private IEnumerator PlayHitRoutine()
        {
            visualizer?.SetMood(MonchiMood.Dolor);
            Fade("Damage", 0.06f);
            yield return new WaitForSeconds(Scaled(ClipLength("Damage")));
            visualizer?.SetMood(MonchiMood.Neutral);
            Fade("Idle");
            Finish();
        }

        public override void PlayBuff(Action onFinished)
        {
            if (visualizer == null || Anim == null)
            {
                onFinished?.Invoke();
                return;
            }

            Begin(PlayBuffRoutine(onFinished));
        }

        private IEnumerator PlayBuffRoutine(Action onFinished)
        {
            visualizer?.SetMood(MonchiMood.Amoroso);
            yield return new WaitForSeconds(Scaled(anticipationSeconds * 0.5f));
            Fade("Yes");
            yield return new WaitForSeconds(Scaled(ClipLength("Yes")));
            visualizer?.SetMood(MonchiMood.Neutral);
            Fade("Idle");
            Finish();
            onFinished?.Invoke();
        }

        public override void PlayDefeat()
        {
            if (current != null)
            {
                StopCoroutine(current);
                current = null;
            }

            if (Anim != null)
                Anim.speed = timeScale;

            visualizer?.SetMood(MonchiMood.KO);
            Fade("Die", 0.1f);
        }

        public override void PlayVictory()
        {
            if (visualizer == null || Anim == null)
                return;

            Begin(PlayVictoryRoutine());
        }

        private IEnumerator PlayVictoryRoutine()
        {
            visualizer?.SetMood(MonchiMood.Emocionado);
            Fade("Roar");
            yield return new WaitForSeconds(Scaled(ClipLength("Roar")));

            while (true)
            {
                Fade("Jump", 0.1f);
                yield return new WaitForSeconds(Scaled(ClipLength("Jump")));
            }
        }

        public override void PlayIdle()
        {
            if (current != null)
            {
                StopCoroutine(current);
                current = null;
            }

            if (Anim != null)
                Anim.speed = timeScale;

            visualizer?.SetMood(MonchiMood.Neutral);
            Fade("Idle", 0.2f);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            current = null;
        }
    }
}
