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

    private CombatVisualizerService Service => CombatVisualizerService.Instance;

    private void OnEnable()
    {
        CombatVisualEvents.OnVisualCombatStart += HandleStart;
        CombatVisualEvents.OnPanelState        += HandleState;
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnVisualCombatStart -= HandleStart;
        CombatVisualEvents.OnPanelState        -= HandleState;
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
        speedSlider  = root.Q<Slider>("speed-slider");
        speedLabel   = root.Q<Label>("speed-label");

        if (backButton  != null) backButton.clicked += () => Service?.Back();
        if (playButton  != null) playButton.clicked += () => Service?.TogglePlay();
        if (nextButton  != null) nextButton.clicked += () => Service?.Next();
        if (speedSlider != null) speedSlider.RegisterValueChangedCallback(e => Service?.SetSpeed(e.newValue));

        SetVisible(false);
    }

    private void HandleStart(CombatVisualContext ctx) => SetVisible(true);

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
        {
            var card = new VisualElement();
            card.AddToClassList("log-card");
            card.AddToClassList(KindClass(line.Kind));
            var label = new Label(line.Text);
            label.AddToClassList("log-text");
            card.Add(label);
            logContainer.Add(card);
        }

        if (logScroll != null)
            logScroll.schedule.Execute(() => logScroll.scrollOffset = new Vector2(0f, float.MaxValue)).ExecuteLater(1);
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
