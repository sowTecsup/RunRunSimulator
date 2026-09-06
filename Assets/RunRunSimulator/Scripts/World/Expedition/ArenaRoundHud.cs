using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator
{
[RequireComponent(typeof(UIDocument))]
public class ArenaRoundHud : MonoBehaviour
{
    [Required, SerializeField] private ArenaRound round;
    [SerializeField] private Color playerColor = new Color(0.76f, 1f, 0.6f);
    [SerializeField] private Color rivalColor  = new Color(0.96f, 0.6f, 0.6f);
    [SerializeField] private Color timeColor   = Color.white;

    private VisualElement scoreboard;
    private VisualElement resultRoot;
    private Label playerLabel;
    private Label timeLabel;
    private Label rivalLabel;
    private Label resultLabel;
    private VisualElement rosterRoot;
    private Label playerRoster;
    private Label rivalRoster;
    private Label seedLabel;
    private string lastSeedText;
    private int lastRosterCount = -1;

    private string lastPlayerText;
    private string lastTimeText;
    private string lastRivalText;
    private string lastResultText;
    private bool resultShown;
    private bool lastShown = true;

    private void OnEnable()
    {
        VisualElement rootVisualElement = GetComponent<UIDocument>().rootVisualElement;

        scoreboard?.RemoveFromHierarchy();
        resultRoot?.RemoveFromHierarchy();

        scoreboard = new VisualElement();
        scoreboard.style.position = Position.Absolute;
        scoreboard.style.top = 14;
        scoreboard.style.left = Length.Percent(50);
        scoreboard.style.translate = new Translate(Length.Percent(-50), 0);
        scoreboard.style.flexDirection = FlexDirection.Row;
        scoreboard.style.alignItems = Align.Center;

        playerLabel = new Label();
        playerLabel.style.fontSize = 30;
        playerLabel.style.color = playerColor;
        playerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        playerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        timeLabel = new Label();
        timeLabel.style.fontSize = 36;
        timeLabel.style.color = timeColor;
        timeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        timeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        timeLabel.style.marginLeft = 28;
        timeLabel.style.marginRight = 28;

        rivalLabel = new Label();
        rivalLabel.style.fontSize = 30;
        rivalLabel.style.color = rivalColor;
        rivalLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        rivalLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        scoreboard.Add(playerLabel);
        scoreboard.Add(timeLabel);
        scoreboard.Add(rivalLabel);
        rootVisualElement.Add(scoreboard);

        rosterRoot?.RemoveFromHierarchy();
        rosterRoot = new VisualElement();
        rosterRoot.style.position = Position.Absolute;
        rosterRoot.style.top = 60;
        rosterRoot.style.left = Length.Percent(50);
        rosterRoot.style.translate = new Translate(Length.Percent(-50), 0);
        rosterRoot.style.flexDirection = FlexDirection.Row;
        rosterRoot.style.alignItems = Align.Center;

        playerRoster = new Label();
        playerRoster.style.fontSize = 19;
        playerRoster.style.color = playerColor;
        playerRoster.style.unityFontStyleAndWeight = FontStyle.Bold;
        playerRoster.style.unityTextAlign = TextAnchor.MiddleRight;
        playerRoster.style.marginRight = 22;

        rivalRoster = new Label();
        rivalRoster.style.fontSize = 19;
        rivalRoster.style.color = rivalColor;
        rivalRoster.style.unityFontStyleAndWeight = FontStyle.Bold;
        rivalRoster.style.unityTextAlign = TextAnchor.MiddleLeft;
        rivalRoster.style.marginLeft = 22;

        rosterRoot.Add(playerRoster);
        rosterRoot.Add(rivalRoster);
        rootVisualElement.Add(rosterRoot);
        lastRosterCount = -1;

        seedLabel = new Label();
        seedLabel.style.position = Position.Absolute;
        seedLabel.style.top = 14;
        seedLabel.style.left = 16;
        seedLabel.style.fontSize = 16;
        seedLabel.style.color = new Color(1f, 1f, 1f, 0.7f);
        seedLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        rootVisualElement.Add(seedLabel);
        lastSeedText = null;

        resultRoot = new VisualElement();
        resultRoot.style.position = Position.Absolute;
        resultRoot.style.top = 94;
        resultRoot.style.left = Length.Percent(50);
        resultRoot.style.translate = new Translate(Length.Percent(-50), 0);

        resultLabel = new Label();
        resultLabel.style.fontSize = 26;
        resultLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        resultLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        resultLabel.style.display = DisplayStyle.None;

        resultRoot.Add(resultLabel);
        rootVisualElement.Add(resultRoot);

        lastPlayerText = null;
        lastTimeText = null;
        lastRivalText = null;
        lastResultText = null;
        resultShown = false;
        lastShown = true;
    }

    private void OnDisable()
    {
        scoreboard?.RemoveFromHierarchy();
        resultRoot?.RemoveFromHierarchy();
        rosterRoot?.RemoveFromHierarchy();
        seedLabel?.RemoveFromHierarchy();
        scoreboard = null;
        resultRoot = null;
        rosterRoot = null;
        seedLabel = null;
    }

    private static string Verb(Occupation occupation)
    {
        switch (occupation)
        {
            case Occupation.Guard: return "vigila";
            case Occupation.Break: return "rompe";
            case Occupation.Decoy: return "distrae";
            case Occupation.Explore: return "explora";
            default: return "recolecta";
        }
    }

    private void RefreshRoster()
    {
        var sandbox = round.Sandbox;
        int count = sandbox != null ? sandbox.Spawned.Count : 0;
        if (count == lastRosterCount) return;
        lastRosterCount = count;

        var player = new System.Text.StringBuilder();
        var rival = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++)
        {
            var agent = sandbox.Spawned[i] != null ? sandbox.Spawned[i].Agent : null;
            if (agent == null || agent.DNA == null) continue;
            var target = agent.Team == ExpeditionTeam.Rival ? rival : player;
            if (target.Length > 0) target.Append("  ·  ");
            target.Append(agent.DNA.CustomName).Append(' ').Append(Verb(agent.Occupation));
        }

        playerRoster.text = player.ToString();
        rivalRoster.text = rival.ToString();
    }

    private void RefreshSeed()
    {
        var sandbox = round.Sandbox;
        string seedText = sandbox != null ? "sala " + sandbox.ActiveSeed : "";
        if (seedText == lastSeedText) return;
        seedLabel.text = seedText;
        lastSeedText = seedText;
    }

    private void Update()
    {
        if (round == null) return;

        RefreshSeed();

        bool shown = round.IsRunning || round.IsOver;
        if (shown != lastShown)
        {
            var display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            scoreboard.style.display = display;
            rosterRoot.style.display = display;
            resultRoot.style.display = display;
            lastShown = shown;
            lastRosterCount = -1;
        }

        if (!round.IsOver && resultShown)
        {
            resultLabel.style.display = DisplayStyle.None;
            resultShown = false;
            lastResultText = null;
        }

        if (!shown) return;

        RefreshRoster();

        string playerText = round.PlayerSecured.ToString();
        if (playerText != lastPlayerText)
        {
            playerLabel.text = playerText;
            lastPlayerText = playerText;
        }

        string rivalText = round.RivalSecured.ToString();
        if (rivalText != lastRivalText)
        {
            rivalLabel.text = rivalText;
            lastRivalText = rivalText;
        }

        int totalSeconds = Mathf.CeilToInt(round.Remaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        string timeText = $"{minutes:00}:{seconds:00}";
        if (timeText != lastTimeText)
        {
            timeLabel.text = timeText;
            lastTimeText = timeText;
        }

        if (round.IsOver)
        {
            if (!resultShown)
            {
                resultLabel.style.display = DisplayStyle.Flex;
                resultShown = true;
            }

            string resultText;
            Color resultColor;
            switch (round.Winner)
            {
                case ExpeditionTeam.Player:
                    resultText = "Gana tu equipo";
                    resultColor = playerColor;
                    break;
                case ExpeditionTeam.Rival:
                    resultText = "Gana el rival";
                    resultColor = rivalColor;
                    break;
                default:
                    resultText = "Empate";
                    resultColor = timeColor;
                    break;
            }

            if (resultText != lastResultText)
            {
                resultLabel.text = resultText;
                resultLabel.style.color = resultColor;
                lastResultText = resultText;
            }
        }
    }
}
}
