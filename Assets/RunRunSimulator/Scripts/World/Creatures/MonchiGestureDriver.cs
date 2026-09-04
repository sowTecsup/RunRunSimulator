using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{
    public class MonchiGestureDriver : MonoBehaviour
    {
        [Required, SerializeField] private MoriMochiAgent agent;
        [Required, SerializeField] private MonchiLocomotionAnimator locomotion;
        [Required, SerializeField] private MonchiGestureSetSO set;
        [SerializeField] private DragonAnimationDriver combatDriver;

        private CreatureIntent lastIntent;
        private string currentHold = "";
        private string pendingEnter = "";
        private string lastClashGesture = "";
        private float nextFidget;

        private void OnEnable()
        {
            nextFidget = Time.time + Random.Range(0f, set.FidgetInterval.x);
            lastIntent = agent.Intent;
            currentHold = "";
            lastClashGesture = "";
        }

        private void Update()
        {
            if (combatDriver != null && combatDriver.IsBusy) return;

            if (agent.IsHeld || agent.IsAirborne || agent.IsRecovering)
            {
                if (locomotion.IsGesturing) locomotion.StopGesture();
                currentHold = "";
                pendingEnter = "";
                return;
            }

            var intent = agent.Intent;
            if (intent != lastIntent)
            {
                pendingEnter = set.TryEnterGesture(intent, out var enterState) ? enterState : "";
                lastIntent = intent;
            }

            string clashGesture = agent.ClashGesture ?? "";
            if (clashGesture != lastClashGesture)
            {
                lastClashGesture = clashGesture;
                if (clashGesture != "") pendingEnter = clashGesture;
            }

            if (pendingEnter != "" && locomotion.PlayGesture(pendingEnter))
                pendingEnter = "";

            var desiredHold = agent.Condition == CreatureCondition.Sick
                ? set.SickGesture
                : (set.TryHoldGesture(intent, out var holdState) ? holdState : "");

            if (currentHold != "" && !locomotion.IsGesturing)
                currentHold = "";

            if (desiredHold != currentHold)
            {
                if (string.IsNullOrEmpty(desiredHold))
                {
                    locomotion.StopGesture();
                    currentHold = "";
                }
                else if (locomotion.HoldGesture(desiredHold))
                {
                    currentHold = desiredHold;
                }
            }

            if (Time.time >= nextFidget
                && (intent == CreatureIntent.Idle || intent == CreatureIntent.Wandering)
                && locomotion.IsStill && !locomotion.IsGesturing && currentHold == "")
            {
                nextFidget = Time.time + Random.Range(set.FidgetInterval.x, set.FidgetInterval.y);
                var boldness = agent.DNA != null ? agent.DNA.Boldness : 0.5f;
                var fidget = set.PickFidget(boldness);
                if (fidget != null)
                    locomotion.PlayGesture(fidget);
            }
        }
    }
}
