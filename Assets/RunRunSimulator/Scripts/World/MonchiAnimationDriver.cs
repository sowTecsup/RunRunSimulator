using System;
using UnityEngine;

namespace MoriMonchiSimulator
{
    public abstract class MonchiAnimationDriver : MonoBehaviour
    {
        public abstract bool IsBusy { get; }
        public abstract void MoveTo(Vector3 destination, Action onArrived);
        public abstract void PlayAttack(Vector3 targetPosition, Action onImpact, Action onFinished);
        public abstract void PlayHit(float intensity);
        public abstract void PlayBuff(Action onFinished);
        public abstract void PlayDefeat();
        public abstract void PlayVictory();
        public abstract void PlayIdle();
        public virtual void SetTimeScale(float value) { }
    }
}
