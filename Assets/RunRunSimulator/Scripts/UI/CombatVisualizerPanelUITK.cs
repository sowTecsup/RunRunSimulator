using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
public class CombatVisualizerPanelUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField, Range(1, 30)] private int maxLogLines = 6;

    private VisualElement root;
    private Label         turnLabel;
    private VisualElement logContainer;
    private int           totalTurns;

    private void OnEnable()
    {
        CombatVisualEvents.OnVisualCombatStart += HandleStart;
        CombatVisualEvents.OnVisualCombatEnd   += HandleEnd;
        CombatVisualEvents.OnTurnStart         += HandleTurnStart;
        CombatVisualEvents.OnLog               += HandleLog;
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnVisualCombatStart -= HandleStart;
        CombatVisualEvents.OnVisualCombatEnd   -= HandleEnd;
        CombatVisualEvents.OnTurnStart         -= HandleTurnStart;
        CombatVisualEvents.OnLog               -= HandleLog;
    }

    private void Start()
    {
        if (document == null) { Debug.LogWarning("[CombatVisualizerPanelUITK] No UIDocument."); return; }
        root = document.rootVisualElement;
        if (root == null) return;
        turnLabel    = root.Q<Label>("turn-label");
        logContainer = root.Q<VisualElement>("log-container");
        SetVisible(false);
    }

    private void HandleStart(CombatVisualContext ctx)
    {
        totalTurns = ctx.TotalTurns;
        SetVisible(true);
        if (turnLabel != null) turnLabel.text = totalTurns > 0 ? $"Turno 0 / {totalTurns}" : "Turno 0";
        logContainer?.Clear();
    }

    private void HandleEnd(CombatVisualSide _, bool isDraw)
    {
        if (turnLabel != null) turnLabel.text = isDraw ? "Empate" : "Final";
    }

    private void HandleTurnStart(CombatTurn turn)
    {
        if (turnLabel == null) return;
        turnLabel.text = totalTurns > 0
            ? $"Turno {turn.TurnNumber} / {totalTurns}"
            : $"Turno {turn.TurnNumber}";
    }

    private void HandleLog(string line)
    {
        if (logContainer == null) return;
        var label = new Label(line);
        label.AddToClassList("log-line");
        logContainer.Add(label);
        while (logContainer.childCount > maxLogLines)
            logContainer.RemoveAt(0);
    }

    private void SetVisible(bool v)
    {
        if (root == null) return;
        root.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
}
