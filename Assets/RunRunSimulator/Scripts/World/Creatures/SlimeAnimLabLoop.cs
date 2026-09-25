using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{
    public class SlimeAnimLabLoop : MonoBehaviour
    {
        [Required, SerializeField] private Animator animator;
        [SerializeField] private string state;
        [SerializeField] private float pause = 0.6f;
        [SerializeField] private float crossFade = 0.1f;

        private Coroutine routine;
        private bool replaying;

        public string Label => name.StartsWith("Slime_") ? name.Substring("Slime_".Length) : name;

        private void OnEnable()
        {
            routine = StartCoroutine(LoopRoutine());
        }

        private void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        public void Replay()
        {
            if (routine != null) StopCoroutine(routine);
            replaying = true;
            routine = StartCoroutine(LoopRoutine());
        }

        private IEnumerator LoopRoutine()
        {
            while (true)
            {
                if (replaying)
                {
                    animator.Play(state, 0, 0f);
                    replaying = false;
                }
                else
                {
                    animator.CrossFadeInFixedTime(state, crossFade);
                }
                int hash = Animator.StringToHash(state);
                for (int i = 0; i < 30 && !Entered(hash); i++) yield return null;
                var clip = ResolveClip();
                if (clip != null && clip.isLooping) yield break;

                float length = clip != null ? clip.length : 1f;
                yield return new WaitForSeconds(length);
                animator.CrossFadeInFixedTime("Idle", crossFade);
                yield return new WaitForSeconds(pause);
            }
        }

        private bool Entered(int hash)
        {
            if (animator.IsInTransition(0)) return animator.GetNextAnimatorStateInfo(0).shortNameHash == hash;
            return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash;
        }

        private AnimationClip ResolveClip()
        {
            if (animator.IsInTransition(0))
            {
                var next = animator.GetNextAnimatorClipInfo(0);
                if (next.Length > 0) return next[0].clip;
            }

            var current = animator.GetCurrentAnimatorClipInfo(0);
            return current.Length > 0 ? current[0].clip : null;
        }
    }
}
