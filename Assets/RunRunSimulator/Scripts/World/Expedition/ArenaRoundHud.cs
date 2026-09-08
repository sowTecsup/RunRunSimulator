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
    [SerializeField] private ArenaClockControl clock;
    [SerializeField] private ArenaCameraDirector director;
    [SerializeField, Min(0f)] private float warnSeconds = 15f;

    private class RivalChip
    {
        public MoriMochiAgent Agent;
        public Label Action;
        public string LastAction;
        public VisualElement Root;
        public bool LastSelected;
        public bool LastChase;
    }

    private VisualElement root;
    private Button pauseButton;
    private Button speedButton;
    private Label seedLabel;
    private Label playerScoreLabel;
    private Label timeLabel;
    private Label rivalScoreLabel;
    private VisualElement barFill;
    private VisualElement playerTeam;
    private VisualElement rivalTeam;
    private Label resultLabel;
    private Label playerCarryLabel;
    private Label rivalCarryLabel;

    private readonly List<ArenaHudCard> cards = new();
    private readonly List<RivalChip> chips = new();

    private string lastSeedText;
    private string lastPlayerScoreText;
    private string lastTimeText;
    private string lastRivalScoreText;
    private string lastResultText;
    private string lastPauseText;
    private string lastSpeedText;
    private bool lastSpeedDim;
    private int lastRosterCount = -1;
    private int lastBarPercent = -1;
    private bool lastTimeWarn;
    private bool resultShown;
    private bool lastShown;
    private string lastPlayerCarryText;
    private string lastRivalCarryText;
    private bool lastPlayerCarrySome;
    private bool lastRivalCarrySome;

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement.Q("hud-root");
        if (root == null) return;

        pauseButton = root.Q<Button>("hud-pause");
        speedButton = root.Q<Button>("hud-speed");
        seedLabel = root.Q<Label>("hud-seed");
        playerScoreLabel = root.Q<Label>("hud-player-score");
        timeLabel = root.Q<Label>("hud-time");
        rivalScoreLabel = root.Q<Label>("hud-rival-score");
        barFill = root.Q("hud-bar-fill");
        playerTeam = root.Q("hud-player-team");
        rivalTeam = root.Q("hud-rival-team");
        resultLabel = root.Q<Label>("hud-result");
        playerCarryLabel = root.Q<Label>("hud-player-carry");
        rivalCarryLabel = root.Q<Label>("hud-rival-carry");

        if (clock != null)
        {
            pauseButton.clicked += clock.TogglePause;
            speedButton.clicked += clock.CycleSpeed;
        }
        else
        {
            pauseButton.style.display = DisplayStyle.None;
            speedButton.style.display = DisplayStyle.None;
        }

        lastRosterCount = -1;
        lastSeedText = null;
        lastPlayerScoreText = null;
        lastTimeText = null;
        lastRivalScoreText = null;
        lastResultText = null;
        lastPauseText = null;
        lastSpeedText = null;
        lastSpeedDim = false;
        lastBarPercent = -1;
        lastTimeWarn = false;
        resultShown = false;
        lastShown = false;
        lastPlayerCarryText = null;
        lastRivalCarryText = null;
    }

    private void OnDisable()
    {
        if (clock != null && pauseButton != null && speedButton != null)
        {
            pauseButton.clicked -= clock.TogglePause;
            speedButton.clicked -= clock.CycleSpeed;
        }
        cards.Clear();
        chips.Clear();
        playerTeam?.Clear();
        rivalTeam?.Clear();
    }

    private void RefreshSeed()
    {
        var sandbox = round.Sandbox;
        string seedText = sandbox != null ? "sala " + sandbox.ActiveSeed + (ArenaClockControl.Speed != 1f ? "  ·  " + ArenaClockControl.Speed + "×" : "") : "";
        if (seedText == lastSeedText) return;
        seedLabel.text = seedText;
        lastSeedText = seedText;
    }

    private void RefreshClockButtons()
    {
        if (clock == null) return;

        bool paused = ArenaClockControl.Paused;
        string pauseText = paused ? "▶" : "II";
        if (pauseText != lastPauseText)
        {
            pauseButton.text = pauseText;
            lastPauseText = pauseText;
        }

        string speedText = "▶ " + Mathf.RoundToInt(ArenaClockControl.Speed) + "×";
        if (speedText != lastSpeedText)
        {
            speedButton.text = speedText;
            lastSpeedText = speedText;
        }

        if (paused != lastSpeedDim)
        {
            speedButton.EnableInClassList("hud-btn--dim", paused);
            lastSpeedDim = paused;
        }
    }

    private void RefreshRoster()
    {
        var sandbox = round.Sandbox;
        int count = sandbox != null ? sandbox.Spawned.Count : 0;
        if (count == lastRosterCount) return;
        lastRosterCount = count;

        playerTeam.Clear();
        rivalTeam.Clear();
        cards.Clear();
        chips.Clear();

        for (int i = 0; i < count; i++)
        {
            var controller = sandbox.Spawned[i];
            var agent = controller != null ? controller.Agent : null;
            if (agent == null || agent.DNA == null) continue;

            if (agent.Team == ExpeditionTeam.Rival) BuildChip(agent);
            else
            {
                var card = new ArenaHudCard(agent, OnCardTapped);
                playerTeam.Add(card.Root);
                cards.Add(card);
            }
        }
    }

    private void OnCardTapped(MoriMochiAgent agent)
    {
        if (director != null) director.TogglePin(agent);
    }

    private void BuildChip(MoriMochiAgent agent)
    {
        var chip = new VisualElement();
        chip.pickingMode = PickingMode.Position;
        chip.AddToClassList("hud-chip-rival");
        chip.RegisterCallback<ClickEvent>(_ => OnCardTapped(agent));

        var swatch = new VisualElement();
        swatch.pickingMode = PickingMode.Ignore;
        swatch.AddToClassList("hud-chip-rival__swatch");
        Color color = agent.DNA.BaseColor;
        color.a = 1f;
        swatch.style.backgroundColor = color;
        chip.Add(swatch);

        var text = new VisualElement();
        text.pickingMode = PickingMode.Ignore;
        text.AddToClassList("hud-chip-rival__text");

        var name = new Label(agent.DNA.CustomName);
        name.pickingMode = PickingMode.Ignore;
        name.AddToClassList("hud-chip-rival__name");
        text.Add(name);

        var archetype = new Label(ArenaOrderCatalog.ArchetypeShort(agent.Orders));
        archetype.pickingMode = PickingMode.Ignore;
        archetype.AddToClassList("hud-chip-rival__archetype");
        text.Add(archetype);

        var action = new Label();
        action.pickingMode = PickingMode.Ignore;
        action.AddToClassList("hud-chip-rival__action");
        text.Add(action);

        chip.Add(text);
        rivalTeam.Add(chip);

        chips.Add(new RivalChip { Agent = agent, Action = action, Root = chip });
    }

    private void Update()
    {
        if (round == null) return;

        RefreshSeed();
        RefreshClockButtons();

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

        var sandboxForCarry = round.Sandbox;
        int playerCarried = 0;
        int rivalCarried = 0;
        if (sandboxForCarry != null)
        {
            for (int i = 0; i < sandboxForCarry.Spawned.Count; i++)
            {
                var controller = sandboxForCarry.Spawned[i];
                var agent = controller != null ? controller.Agent : null;
                if (agent == null) continue;
                if (agent.Team == ExpeditionTeam.Player) playerCarried += agent.Carried;
                else if (agent.Team == ExpeditionTeam.Rival) rivalCarried += agent.Carried;
            }
        }

        RefreshCarryLabel(playerCarryLabel, playerCarried, ref lastPlayerCarryText, ref lastPlayerCarrySome);
        RefreshCarryLabel(rivalCarryLabel, rivalCarried, ref lastRivalCarryText, ref lastRivalCarrySome);

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

        for (int i = 0; i < chips.Count; i++)
        {
            var chip = chips[i];
            var agent = chip.Agent;
            if (agent == null)
            {
                lastRosterCount = -1;
                continue;
            }

            string action = LocEnumMaps.IntentName(agent.Intent);
            if (action != chip.LastAction)
            {
                chip.Action.text = action;
                chip.LastAction = action;
            }

            bool chipSelected = director != null && director.Pinned == agent;
            if (chipSelected != chip.LastSelected)
            {
                chip.Root.EnableInClassList("hud-chip-rival--selected", chipSelected);
                chip.LastSelected = chipSelected;
            }

            bool chipChase = agent.IsChasing;
            if (chipChase != chip.LastChase)
            {
                chip.Root.EnableInClassList("hud-chip-rival--chase", chipChase);
                chip.LastChase = chipChase;
            }
        }

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card.Agent == null)
            {
                lastRosterCount = -1;
                continue;
            }

            card.Refresh(director != null && director.Pinned == card.Agent);
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

    private void RefreshCarryLabel(Label label, int carried, ref string lastText, ref bool lastSome)
    {
        if (label == null) return;
        string text = carried + " en manos";
        if (text != lastText)
        {
            label.text = text;
            lastText = text;
        }
        bool some = carried > 0;
        if (some != lastSome)
        {
            label.EnableInClassList("hud-carry--some", some);
            lastSome = some;
        }
    }
}
}
