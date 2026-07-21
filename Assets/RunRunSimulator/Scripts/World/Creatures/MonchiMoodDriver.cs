using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{
    public class MonchiMoodDriver : MonoBehaviour
    {
        [Required, SerializeField] private MoriMochiAgent agent;
        [Required, SerializeField] private MonchiVisualizer visualizer;
        [SerializeField] private DragonAnimationDriver combatDriver;
        [SerializeField] private Vector2 tickSeconds = new Vector2(2.5f, 5f);

        private float nextTick;

        private void OnEnable()
        {
            nextTick = Time.time + Random.Range(0f, tickSeconds.x);
        }

        private void Update()
        {
            if (Time.time < nextTick) return;
            nextTick = Time.time + Random.Range(tickSeconds.x, tickSeconds.y);

            if (combatDriver != null && combatDriver.IsBusy) return;
            if (agent == null || visualizer == null) return;

            visualizer.SetMood(ResolveMood());
        }

        private MonchiMood ResolveMood()
        {
            if (agent.Condition == CreatureCondition.Sick) return MonchiMood.Enfermo;
            if (agent.Condition == CreatureCondition.InNeed) return MonchiMood.Triste;

            switch (agent.Intent)
            {
                case CreatureIntent.Resting: return MonchiMood.Dormido;
                case CreatureIntent.Eating: return MonchiMood.Feliz;
                case CreatureIntent.Playing: return MonchiMood.Emocionado;
                case CreatureIntent.Held: return MonchiMood.Asustado;
                case CreatureIntent.Tumbling: return MonchiMood.Mareado;
                case CreatureIntent.Fleeing: return MonchiMood.Asustado;
                default: return Random.value < 0.35f ? MonchiMood.Feliz : MonchiMood.Neutral;
            }
        }
    }
}
