using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{
    public class MonchiMiningFeedback : MonoBehaviour
    {
        [Required, SerializeField] private MoriMochiAgent agent;
        [Required, SerializeField] private MMF_Player onMining;

        private bool mining;

        private void LateUpdate()
        {
            bool now = agent.Intent == CreatureIntent.Taking;
            if (now == mining) return;
            mining = now;

            if (onMining == null) return;

            if (now)
                onMining.PlayFeedbacks();
            else
                onMining.StopFeedbacks();
        }

        private void OnDisable()
        {
            if (!mining) return;
            mining = false;
            if (onMining != null) onMining.StopFeedbacks();
        }
    }
}
