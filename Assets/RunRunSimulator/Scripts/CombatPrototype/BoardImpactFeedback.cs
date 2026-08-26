using UnityEngine;
using MoreMountains.Feedbacks;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class BoardImpactFeedback : MonoBehaviour
    {
        [SerializeField] private CombatBoardBuilder builder;
        [SerializeField] private MMF_Player extraFeedback;
        [SerializeField] private int radius = 1;
        [SerializeField] private float wiggleDuration = 0.3f;

        public void ShakeAt(Vector2Int cell)
        {
            if (builder == null) return;

            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector2Int neighbor = new Vector2Int(cell.x + x, cell.y + z);
                    Transform block = builder.GetBlock(neighbor);
                    if (block == null) continue;

                    MMWiggle wiggle = block.GetComponent<MMWiggle>();
                    if (wiggle != null)
                    {
                        wiggle.WigglePosition(wiggleDuration);
                    }
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
