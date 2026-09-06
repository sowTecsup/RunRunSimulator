using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator
{
[RequireComponent(typeof(UIDocument))]
public class ArenaRoundHud : MonoBehaviour
{
    [Required, SerializeField] private ArenaRound round;
    [SerializeField, Min(0f)] private float warnSeconds = 15f;

    private class Row
    {
        public MoriMochiAgent Agent;
        public Label Sub;
        public Label Carry;
        public VisualElement Mine;
        public VisualElement MineFill;
        public string LastSub;
        public int LastCarried = -1;
        public float LastProgress = -1f;
    }

    private VisualElement root;
    private Label seedLabel;
    private Label playerScoreLabel;
    private Label timeLabel;
    private Label rivalScoreLabel;
    private VisualElement barFill;
    private VisualElement playerTeam;
    private VisualElement rivalTeam;
    private Label resultLabel;

    private readonly List<Row> rows = new();

    private string lastSeedText;
    private string lastPlayerScoreText;
    private string lastTimeText;
    private string lastRivalScoreText;
    private string lastResultText;
    private int lastRosterCount = -1;
    private int lastBarPercent = -1;
    private bool lastTimeWarn;
    private bool resultShown;
    private bool lastShown;

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement.Q("hud-root");
        if (root == null) return;

        seedLabel = root.Q<Label>("hud-seed");
        playerScoreLabel = root.Q<Label>("hud-player-score");
        timeLabel = root.Q<Label>("hud-time");
        rivalScoreLabel = root.Q<Label>("hud-rival-score");
        barFill = root.Q("hud-bar-fill");
        playerTeam = root.Q("hud-player-team");
        rivalTeam = root.Q("hud-rival-team");
        resultLabel = root.Q<Label>("hud-result");

        lastRosterCount = -1;
        lastSeedText = null;
        lastPlayerScoreText = null;
        lastTimeText = null;
        lastRivalScoreText = null;
        lastResultText = null;
        lastBarPercent = -1;
        lastTimeWarn = false;
        resultShown = false;
        lastShown = false;
    }

    private void OnDisable()
    {
        rows.Clear();
        playerTeam?.Clear();
        rivalTeam?.Clear();
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

    private void RefreshSeed()
    {
        var sandbox = round.Sandbox;
        string seedText = sandbox != null ? "sala " + sandbox.ActiveSeed : "";
        if (seedText == lastSeedText) return;
        seedLabel.text = seedText;
        lastSeedText = seedText;
    }

    private void RefreshRoster()
    {
        var sandbox = round.Sandbox;
        int count = sandbox != null ? sandbox.Spawned.Count : 0;
        if (count == lastRosterCount) return;
        lastRosterCount = count;

        playerTeam.Clear();
        rivalTeam.Clear();
        rows.Clear();

        for (int i = 0; i < count; i++)
        {
            var controller = sandbox.Spawned[i];
            var agent = controller != null ? controller.Agent : null;
            if (agent == null || agent.DNA == null) continue;
            BuildRow(agent);
        }
    }

    private void BuildRow(MoriMochiAgent agent)
    {
        bool isRival = agent.Team == ExpeditionTeam.Rival;

        var row = new VisualElement();
        row.pickingMode = PickingMode.Ignore;
        row.AddToClassList("hud-row");
        if (isRival) row.AddToClassList("hud-row--rival");

        var swatch = new VisualElement();
        swatch.pickingMode = PickingMode.Ignore;
        swatch.AddToClassList("hud-row__swatch");
        Color color = agent.DNA.BaseColor;
        color.a = 1f;
        swatch.style.backgroundColor = color;
        row.Add(swatch);

        var text = new VisualElement();
        text.pickingMode = PickingMode.Ignore;
        text.AddToClassList("hud-row__text");

        var name = new Label(agent.DNA.CustomName);
        name.pickingMode = PickingMode.Ignore;
        name.AddToClassList("hud-row__name");
        text.Add(name);

        var sub = new Label();
        sub.pickingMode = PickingMode.Ignore;
        sub.AddToClassList("hud-row__sub");
        text.Add(sub);

        var mine = new VisualElement();
        mine.pickingMode = PickingMode.Ignore;
        mine.AddToClassList("hud-row__mine");
        var mineFill = new VisualElement();
        mineFill.pickingMode = PickingMode.Ignore;
        mineFill.AddToClassList("hud-row__mine-fill");
        mine.Add(mineFill);
        text.Add(mine);

        row.Add(text);

        var carry = new Label();
        carry.pickingMode = PickingMode.Ignore;
        carry.AddToClassList("hud-row__carry");
        row.Add(carry);

        (isRival ? rivalTeam : playerTeam).Add(row);

        rows.Add(new Row
        {
            Agent = agent,
            Sub = sub,
            Carry = carry,
            Mine = mine,
            MineFill = mineFill
        });
    }

    private void Update()
    {
        if (round == null) return;

        RefreshSeed();

        bool shown = round.IsRunning || round.IsOver;
        if (shown != lastShown)
        {
            root.EnableInClassList("hud--idle", !shown);
            lastShown = shown;
            if (shown) lastRosterCount = -1;
        }

        if (!round.IsOver && resultShown)
        {
            resultLabel.EnableInClassList("hud-result--show", false);
            resultShown = false;
            lastResultText = null;
        }

        if (!shown) return;

        RefreshRoster();

        string playerScoreText = round.PlayerSecured.ToString();
        if (playerScoreText != lastPlayerScoreText)
        {
            playerScoreLabel.text = playerScoreText;
            lastPlayerScoreText = playerScoreText;
        }

        string rivalScoreText = round.RivalSecured.ToString();
        if (rivalScoreText != lastRivalScoreText)
        {
            rivalScoreLabel.text = rivalScoreText;
            lastRivalScoreText = rivalScoreText;
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

        bool timeWarn = round.Remaining <= warnSeconds;
        if (timeWarn != lastTimeWarn)
        {
            timeLabel.EnableInClassList("hud-time--warn", timeWarn);
            barFill.EnableInClassList("hud-bar__fill--warn", timeWarn);
            lastTimeWarn = timeWarn;
        }

        int barPercent = Mathf.RoundToInt(Mathf.Clamp01(round.Remaining / round.RoundSeconds) * 100f);
        if (barPercent != lastBarPercent)
        {
            barFill.style.width = Length.Percent(barPercent);
            lastBarPercent = barPercent;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var agent = row.Agent;
            if (agent == null)
            {
                lastRosterCount = -1;
                continue;
            }

            string sub = $"{Verb(agent.Occupation)}  ·  {LocEnumMaps.IntentName(agent.Intent)}";
            if (sub != row.LastSub)
            {
                row.Sub.text = sub;
                row.LastSub = sub;
            }

            int carried = agent.Carried;
            if (carried != row.LastCarried)
            {
                row.Carry.text = carried > 0 ? $"◆ {carried}" : "";
                row.Carry.EnableInClassList("hud-row__carry--empty", carried == 0);
                row.LastCarried = carried;
            }

            float progress = agent.MiningProgress;
            if (Mathf.Abs(progress - row.LastProgress) > 0.01f)
            {
                row.Mine.EnableInClassList("hud-row__mine--hidden", progress <= 0f);
                row.MineFill.style.width = Length.Percent(progress * 100f);
                row.LastProgress = progress;
            }
        }

        if (round.IsOver)
        {
            if (!resultShown)
            {
                resultLabel.EnableInClassList("hud-result--show", true);
                resultShown = true;
            }

            string resultText;
            switch (round.Winner)
            {
                case ExpeditionTeam.Player: resultText = "Gana tu equipo"; break;
                case ExpeditionTeam.Rival: resultText = "Gana el rival"; break;
                default: resultText = "Empate"; break;
            }

            if (resultText != lastResultText)
            {
                resultLabel.text = resultText;
                resultLabel.EnableInClassList("hud-result--win", round.Winner == ExpeditionTeam.Player);
                resultLabel.EnableInClassList("hud-result--lose", round.Winner == ExpeditionTeam.Rival);
                resultLabel.EnableInClassList("hud-result--draw", round.Winner == ExpeditionTeam.None);
                lastResultText = resultText;
            }
        }
    }
}
}
