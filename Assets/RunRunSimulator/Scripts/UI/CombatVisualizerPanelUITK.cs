using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
public class CombatVisualizerPanelUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    private VisualElement root;
    private Label         turnLabel;
    private ScrollView    logScroll;
    private VisualElement logContainer;
    private Button        backButton;
    private Button        playButton;
    private Button        nextButton;
    private Slider        speedSlider;
    private Label         speedLabel;
    private Button        logToggleButton;

    private bool logExpanded = true;
    private CombatVisualContext context;

    private CombatVisualizerService Service => CombatVisualizerService.Instance;

    private void OnEnable()
    {
        CombatVisualEvents.OnVisualCombatStart += HandleStart;
        CombatVisualEvents.OnPanelState        += HandleState;
        CombatVisualEvents.OnLogAppend         += HandleLogAppend;
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnVisualCombatStart -= HandleStart;
        CombatVisualEvents.OnPanelState        -= HandleState;
        CombatVisualEvents.OnLogAppend         -= HandleLogAppend;
    }

    private void Start()
    {
        if (document == null) { Debug.LogWarning("[CombatVisualizerPanelUITK] No UIDocument."); return; }
        root = document.rootVisualElement;
        if (root == null) return;

        turnLabel    = root.Q<Label>("turn-label");
        logScroll    = root.Q<ScrollView>("log-scroll");
        logContainer = root.Q<VisualElement>("log-container");
        backButton   = root.Q<Button>("btn-back");
        playButton   = root.Q<Button>("btn-play");
        nextButton   = root.Q<Button>("btn-next");
        speedSlider     = root.Q<Slider>("speed-slider");
        speedLabel      = root.Q<Label>("speed-label");
        logToggleButton = root.Q<Button>("btn-log-toggle");

        if (backButton  != null) backButton.clicked += () => Service?.Back();
        if (playButton  != null) playButton.clicked += () => Service?.TogglePlay();
        if (nextButton  != null) nextButton.clicked += () => Service?.Next();
        if (speedSlider != null) speedSlider.RegisterValueChangedCallback(e => Service?.SetSpeed(e.newValue));
        if (logToggleButton != null) logToggleButton.clicked += ToggleLog;

        ApplyLogExpanded();
        SetVisible(false);
    }

    private void HandleStart(CombatVisualContext ctx)
    {
        context = ctx;
        SetVisible(true);
    }

    private void HandleState(CombatVisualPanelState st)
    {
        SetVisible(true);

        if (turnLabel != null)
            turnLabel.text = st.Ended
                ? (st.IsDraw ? "Empate" : "Final")
                : (st.TotalTurns > 0 ? $"Turno {st.TurnNumber} / {st.TotalTurns}" : $"Turno {st.TurnNumber}");

        RebuildLog(st.Log);

        backButton?.SetEnabled(st.CanBack);
        nextButton?.SetEnabled(st.CanForward);
        if (playButton != null) playButton.text = st.IsAuto ? "❚❚" : "▶";
        if (speedLabel != null) speedLabel.text = $"Velocidad x{st.Speed:0.0}";
        if (speedSlider != null && !Mathf.Approximately(speedSlider.value, st.Speed))
            speedSlider.SetValueWithoutNotify(st.Speed);
    }

    private void RebuildLog(CombatVisualLogLine[] lines)
    {
        if (logContainer == null) return;
        logContainer.Clear();
        if (lines == null) return;

        foreach (var line in lines)
            AddCard(line);

        ScrollLogToEnd();
    }

    private void HandleLogAppend(CombatVisualLogLine line)
    {
        if (logContainer == null) return;
        AddCard(line);
        ScrollLogToEnd();
    }

    private void AddCard(CombatVisualLogLine line)
    {
        if (!line.HasUnit && line.Kind != CombatVisualLogKind.Result) return;

        var card = new VisualElement();
        card.AddToClassList("log-card");
        card.AddToClassList(KindClass(line.Kind));

        if (line.HasUnit)
        {
            var headshot = new VisualElement();
            headshot.AddToClassList("log-headshot");
            MonchiPortraitUI.ApplyHeadshot(headshot, ResolveDna(line.UnitSide, line.UnitIndex));
            card.Add(headshot);
        }

        var label = new Label(line.Text);
        label.AddToClassList("log-text");
        card.Add(label);
        logContainer.Add(card);
    }

    private void ScrollLogToEnd()
    {
        if (logScroll != null)
            logScroll.schedule.Execute(() => logScroll.scrollOffset = new Vector2(0f, float.MaxValue)).ExecuteLater(1);
    }

    private CreatureDNA ResolveDna(CombatVisualSide side, int index)
    {
        var team = side == CombatVisualSide.A ? context.TeamA : context.TeamB;
        return team != null && index >= 0 && index < team.Count ? team[index] : null;
    }

    private void ToggleLog()
    {
        logExpanded = !logExpanded;
        ApplyLogExpanded();
    }

    private void ApplyLogExpanded()
    {
        if (logScroll != null)
            logScroll.style.display = logExpanded ? DisplayStyle.Flex : DisplayStyle.None;
        if (logToggleButton != null)
            logToggleButton.text = logExpanded ? "▼" : "▲";
    }

    private static string KindClass(CombatVisualLogKind kind) => kind switch
    {
        CombatVisualLogKind.Versus => "log-versus",
        CombatVisualLogKind.Hit    => "log-hit",
        CombatVisualLogKind.Crit   => "log-crit",
        CombatVisualLogKind.Death  => "log-death",
        CombatVisualLogKind.Result => "log-result",
        _                          => "log-hit",
    };

    private void SetVisible(bool v)
    {
        if (root == null) return;
        root.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
}
