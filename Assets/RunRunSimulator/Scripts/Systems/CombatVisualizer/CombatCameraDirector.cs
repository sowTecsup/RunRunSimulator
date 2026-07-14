using UnityEngine;
namespace MoriMonchiSimulator
{

public class CombatCameraDirector : MonoBehaviour
{
    [SerializeField] private Unity.Cinemachine.CinemachineCamera sceneCamera;
    [SerializeField] private int scenePriority = 10;
    [SerializeField] private int activePriority = 20;

    private Unity.Cinemachine.CinemachineCamera lastActive;

    private void OnEnable()
    {
        CombatVisualEvents.OnActiveUnit += HandleActiveUnit;
        CombatVisualEvents.OnVisualCombatStart += HandleStart;
        CombatVisualEvents.OnVisualCombatEnd += HandleEnd;
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnActiveUnit -= HandleActiveUnit;
        CombatVisualEvents.OnVisualCombatStart -= HandleStart;
        CombatVisualEvents.OnVisualCombatEnd -= HandleEnd;
    }

    private void HandleStart(CombatVisualContext ctx)
    {
        if (sceneCamera != null) sceneCamera.Priority = scenePriority;
        if (lastActive != null) lastActive.Priority = 0;
        lastActive = null;
    }

    private void HandleActiveUnit(CombatVisualSide side, int index)
    {
        var vcam = CombatVisualizerService.Instance != null ? CombatVisualizerService.Instance.VCamOf(side, index) : null;
        if (lastActive != null && lastActive != vcam) lastActive.Priority = 0;
        if (vcam != null) vcam.Priority = activePriority;
        lastActive = vcam;
    }

    private void HandleEnd(CombatVisualSide winner, bool isDraw)
    {
        if (lastActive != null) lastActive.Priority = 0;
        lastActive = null;
    }
}
}
