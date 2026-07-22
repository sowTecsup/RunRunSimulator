using UnityEngine;
namespace MoriMonchiSimulator
{

public class CombatCameraDirector : MonoBehaviour
{
    [SerializeField] private Unity.Cinemachine.CinemachineCamera sceneCamera;
    [SerializeField] private Unity.Cinemachine.CinemachineCamera allyCamera;
    [SerializeField] private Unity.Cinemachine.CinemachineCamera enemyCamera;
    [SerializeField] private int scenePriority = 10;
    [SerializeField] private int phasePriority = 30;

    private void OnEnable()
    {
        CombatVisualEvents.OnVisualCombatStart += HandleStart;
        CombatVisualEvents.OnVisualCombatEnd += HandleEnd;
        CombatVisualEvents.OnPhase += HandlePhase;
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnVisualCombatStart -= HandleStart;
        CombatVisualEvents.OnVisualCombatEnd -= HandleEnd;
        CombatVisualEvents.OnPhase -= HandlePhase;
    }

    private void HandleStart(CombatVisualContext ctx)
    {
        if (sceneCamera != null) sceneCamera.Priority = scenePriority;
        if (allyCamera != null) allyCamera.Priority = 0;
        if (enemyCamera != null) enemyCamera.Priority = 0;
    }

    private void HandleEnd(CombatVisualSide winner, bool isDraw)
    {
        if (sceneCamera != null) sceneCamera.Priority = scenePriority;
        if (allyCamera != null) allyCamera.Priority = 0;
        if (enemyCamera != null) enemyCamera.Priority = 0;
    }

    private void HandlePhase(CombatTurnPhase phase, CombatVisualSide actorSide)
    {
        if (phase == CombatTurnPhase.Passives)
        {
            if (actorSide == CombatVisualSide.A)
            {
                if (allyCamera != null) allyCamera.Priority = phasePriority;
                if (enemyCamera != null) enemyCamera.Priority = 0;
            }
            else
            {
                if (enemyCamera != null) enemyCamera.Priority = phasePriority;
                if (allyCamera != null) allyCamera.Priority = 0;
            }
        }
        else if (phase == CombatTurnPhase.Attack)
        {
            if (actorSide == CombatVisualSide.A)
            {
                if (enemyCamera != null) enemyCamera.Priority = phasePriority;
                if (allyCamera != null) allyCamera.Priority = 0;
            }
            else
            {
                if (allyCamera != null) allyCamera.Priority = phasePriority;
                if (enemyCamera != null) enemyCamera.Priority = 0;
            }
        }
        else
        {
            if (allyCamera != null) allyCamera.Priority = 0;
            if (enemyCamera != null) enemyCamera.Priority = 0;
        }
    }
}
}
