using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{
    public class MonchiTeamRim : MonoBehaviour
    {
        [Required, SerializeField] private MoriMochiAgent agent;
        [Required, SerializeField] private MonchiVisualizer visualizer;
        [SerializeField] private Color rivalRimColor = new Color(1f, 0.3f, 0.22f);
        [SerializeField, Range(0f, 1f)] private float rivalRimPower = 0.5f;
        [SerializeField, Range(0f, 1f)] private float rivalRimInsideMask = 0.55f;

        private ExpeditionTeam appliedTeam;
        private bool applied;

        private void LateUpdate()
        {
            var team = agent.Team;
            if (applied && team == appliedTeam) return;

            appliedTeam = team;
            applied = true;

            if (team == ExpeditionTeam.Rival)
                visualizer.SetRimOverride(rivalRimColor, rivalRimPower, rivalRimInsideMask);
            else
                visualizer.ClearRimOverride();
        }

        private void OnDisable()
        {
            applied = false;
            if (visualizer != null) visualizer.ClearRimOverride();
        }
    }
}
