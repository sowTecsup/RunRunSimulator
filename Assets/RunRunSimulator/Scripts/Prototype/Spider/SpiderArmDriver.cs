using System;
using System.Collections;
using UnityEngine;

namespace MoriMonchiSimulator.Prototype
{
    public class SpiderArmDriver : MonoBehaviour
    {
        [SerializeField] private ConfigurableJoint armL;
        [SerializeField] private ConfigurableJoint armR;
        [SerializeField] private Vector3 armsUpEuler = new Vector3(-70f, 0f, 0f);
        [SerializeField] private Vector3 windupEuler = new Vector3(50f, 0f, -30f);
        [SerializeField] private Vector3 strikeEuler = new Vector3(-60f, 0f, 20f);

        private Quaternion restL;
        private Quaternion restR;

        private void Awake()
        {
            if (armL != null) restL = armL.targetRotation;
            if (armR != null) restR = armR.targetRotation;
        }

        public void PoseRest()
        {
            if (armL != null) armL.targetRotation = restL;
            if (armR != null) armR.targetRotation = restR;
        }

        public void PoseArmsUp()
        {
            if (armL != null) armL.targetRotation = Quaternion.Euler(armsUpEuler) * restL;
            if (armR != null) armR.targetRotation = Quaternion.Euler(armsUpEuler) * restR;
        }

        public Coroutine Swipe(float windupSeconds, float strikeSeconds, Action onImpact)
        {
            return StartCoroutine(SwipeRoutine(windupSeconds, strikeSeconds, onImpact));
        }

        private IEnumerator SwipeRoutine(float windupSeconds, float strikeSeconds, Action onImpact)
        {
            if (armR != null) armR.targetRotation = Quaternion.Euler(windupEuler) * restR;
            yield return new WaitForSeconds(windupSeconds);

            if (armR != null) armR.targetRotation = Quaternion.Euler(strikeEuler) * restR;
            yield return new WaitForSeconds(strikeSeconds * 0.5f);

            onImpact?.Invoke();
            yield return new WaitForSeconds(strikeSeconds * 0.5f);

            if (armR != null) armR.targetRotation = restR;
        }
    }
}
