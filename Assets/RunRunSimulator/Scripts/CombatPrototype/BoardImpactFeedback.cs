using UnityEngine;
using MoreMountains.Feedbacks;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class BoardImpactFeedback : MonoBehaviour
    {
        [SerializeField] private CombatBoardBuilder builder;
        [SerializeField] private MMF_Player extraFeedback;
        [SerializeField] private float wiggleDuration = 0.3f;

        public void ShakeAt(Vector2Int cell)
        {
            if (builder == null) return;

            Transform block = builder.GetBlock(cell);
            if (block != null)
            {
                MMWiggle wiggle = block.GetComponent<MMWiggle>();
                if (wiggle != null)
                {
                    wiggle.WigglePosition(wiggleDuration);
                }
            }

            if (extraFeedback != null && builder.Board != null)
            {
                extraFeedback.transform.position = builder.Board.CellToWorld(cell);
                extraFeedback.PlayFeedbacks();
            }
        }
    }
}
