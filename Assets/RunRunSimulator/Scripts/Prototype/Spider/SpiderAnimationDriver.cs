using System;
using System.Collections;
using UnityEngine;

namespace MoriMonchiSimulator.Prototype
{
    public class SpiderAnimationDriver : MonchiAnimationDriver
    {
        [SerializeField] private SpiderBodyController controller;
        [SerializeField] private SpiderJump jump;
        [SerializeField] private SpiderElasticBody elastic;
        [SerializeField] private SpiderRagdollMode ragdollMode;
        [SerializeField] private SpiderArmDriver arms;
        [SerializeField] private SpiderBodyMotion motion;
        [SerializeField] private Rigidbody rootBody;

        [SerializeField] private float arriveRadius = 0.25f;
        [SerializeField] private float faceAngle = 35f;
        [SerializeField] private float attackWindupSeconds = 0.35f;
        [SerializeField] private float attackStrikeSeconds = 0.25f;
        [SerializeField] private float attackPunch = 1.5f;
        [SerializeField] private float hitSquash = 2.5f;
        [SerializeField] private float victoryBounceDelay = 0.25f;
        [SerializeField] private float turnGain = 0.04f;
        [SerializeField] private float attackRange = 1.1f;
        [SerializeField] private float approachTimeout = 6f;
        [SerializeField] private float buffWiggleSeconds = 0.15f;
        [SerializeField] private int buffCycles = 3;
        [SerializeField] private float attackLeanDegrees = 28f;
        [SerializeField] private float hitLeanDegrees = 22f;
        [SerializeField] private float defeatUpImpulse = 5f;

        private Coroutine current;

        public override bool IsBusy { get { return current != null; } }

        private void Interrupt()
        {
            if (current != null)
            {
                StopCoroutine(current);
                current = null;
            }
            controller?.ClearExternalDrive();
        }

        public override void MoveTo(Vector3 destination, Action onArrived)
        {
            Interrupt();
            if (controller == null)
            {
                onArrived?.Invoke();
                return;
            }
            current = StartCoroutine(MoveToRoutine(destination, onArrived));
        }

        private IEnumerator MoveToRoutine(Vector3 destination, Action onArrived)
        {
            while (true)
            {
                Vector3 origin = controller.transform.position;
                Vector3 toTarget = destination - origin;
                toTarget.y = 0f;

                if (toTarget.magnitude < arriveRadius)
                {
                    break;
                }

                Vector3 forwardFlat = controller.transform.forward;
                forwardFlat.y = 0f;

                float angle = Vector3.SignedAngle(forwardFlat, toTarget, Vector3.up);
                float turn = Mathf.Clamp(angle * turnGain, -1f, 1f);
                float forward = Mathf.Abs(angle) < faceAngle ? 1f : 0f;

                controller.SetExternalDrive(forward, turn);

                yield return null;
            }

            controller.ClearExternalDrive();
            current = null;
            onArrived?.Invoke();
        }

        public override void PlayAttack(Vector3 targetPosition, Action onImpact, Action onFinished)
        {
            Interrupt();
            current = StartCoroutine(PlayAttackRoutine(targetPosition, onImpact, onFinished));
        }

        private IEnumerator PlayAttackRoutine(Vector3 targetPosition, Action onImpact, Action onFinished)
        {
            bool hopApproach = UnityEngine.Random.value < 0.5f;

            if (controller != null)
            {
                float elapsed = 0f;
                while (elapsed < approachTimeout)
                {
                    Vector3 origin = controller.transform.position;
                    Vector3 toTarget = targetPosition - origin;
                    toTarget.y = 0f;

                    if (toTarget.magnitude <= attackRange)
                    {
                        break;
                    }

                    Vector3 forwardFlat = controller.transform.forward;
                    forwardFlat.y = 0f;

                    float angle = Vector3.SignedAngle(forwardFlat, toTarget, Vector3.up);
                    float turn = Mathf.Clamp(angle * turnGain, -1f, 1f);
                    float forward = Mathf.Abs(angle) < faceAngle ? 1f : 0f;

                    controller.SetExternalDrive(forward, turn);

                    if (hopApproach && jump != null && !jump.IsAirborne)
                    {
                        jump.Jump();
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                controller.ClearExternalDrive();

                elapsed = 0f;
                while (elapsed < 1.5f)
                {
                    Vector3 origin = controller.transform.position;
                    Vector3 toTarget = targetPosition - origin;
                    toTarget.y = 0f;

                    Vector3 forwardFlat = controller.transform.forward;
                    forwardFlat.y = 0f;

                    float angle = Vector3.SignedAngle(forwardFlat, toTarget, Vector3.up);
                    if (Mathf.Abs(angle) < 10f)
                    {
                        break;
                    }

                    float turn = Mathf.Clamp(angle * turnGain, -1f, 1f);
                    controller.SetExternalDrive(0f, turn);

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            controller?.ClearExternalDrive();
            elastic?.AddImpulse(attackPunch);
            motion?.AddPitchImpulse(attackLeanDegrees);

            if (arms != null)
            {
                yield return arms.Swipe(attackWindupSeconds, attackStrikeSeconds, onImpact);
            }
            else
            {
                yield return new WaitForSeconds(attackWindupSeconds);
                onImpact?.Invoke();
                yield return new WaitForSeconds(attackStrikeSeconds);
            }

            current = null;
            onFinished?.Invoke();
        }

        public override void PlayHit(float intensity)
        {
            elastic?.AddImpulse(-hitSquash * Mathf.Max(0.2f, intensity));
            motion?.AddPitchImpulse(-hitLeanDegrees * Mathf.Max(0.2f, intensity));
        }

        public override void PlayBuff(Action onFinished)
        {
            Interrupt();
            current = StartCoroutine(PlayBuffRoutine(onFinished));
        }

        private IEnumerator PlayBuffRoutine(Action onFinished)
        {
            arms?.PoseArmsUp();

            for (int i = 0; i < buffCycles; i++)
            {
                controller?.SetExternalDrive(0f, 1f);
                yield return new WaitForSeconds(buffWiggleSeconds);

                controller?.SetExternalDrive(0f, -1f);
                yield return new WaitForSeconds(buffWiggleSeconds * 2f);

                controller?.SetExternalDrive(0f, 1f);
                yield return new WaitForSeconds(buffWiggleSeconds);
            }

            controller?.ClearExternalDrive();
            jump?.Jump();
            yield return new WaitForSeconds(0.4f);

            arms?.PoseRest();
            current = null;
            onFinished?.Invoke();
        }

        public override void PlayDefeat()
        {
            Interrupt();
            arms?.PoseRest();
            ragdollMode?.SetRagdoll(true);

            if (rootBody != null)
            {
                rootBody.linearVelocity = Vector3.up * defeatUpImpulse;
                rootBody.angularVelocity = new Vector3(UnityEngine.Random.Range(-3f, -1.5f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
            }
        }

        public override void PlayVictory()
        {
            Interrupt();
            if (ragdollMode != null && ragdollMode.IsRagdoll)
            {
                ragdollMode.SetRagdoll(false);
            }
            arms?.PoseArmsUp();
            current = StartCoroutine(PlayVictoryRoutine());
        }

        private IEnumerator PlayVictoryRoutine()
        {
            while (true)
            {
                if (jump != null && !jump.IsAirborne)
                {
                    jump.Jump();
                    yield return new WaitForSeconds(victoryBounceDelay);
                }
                else
                {
                    yield return null;
                }
            }
        }

        public override void PlayIdle()
        {
            Interrupt();
            if (ragdollMode != null && ragdollMode.IsRagdoll)
            {
                ragdollMode.SetRagdoll(false);
            }
            arms?.PoseRest();
        }

        private void OnDisable()
        {
            Interrupt();
        }
    }
}
