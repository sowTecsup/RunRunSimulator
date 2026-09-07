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
    [SerializeField, Min(0f)] private float warnSeconds = 15f;

    private static readonly OrderPillar[] PillarOrder = { OrderPillar.Loot, OrderPillar.Contact, OrderPillar.Posture };
    private static readonly string[] PillarChipClass = { "hud-chip--loot", "hud-chip--contact", "hud-chip--team" };

    private class Card
    {
        public MoriMochiAgent Agent;
        public Occupation Occupation;
        public VisualElement Root;
        public VisualElement Carry;
        public Label Action;
        public string LastAction;
        public List<VisualElement> CarrySlots = new();
        public VisualElement CarryFill;
        public int LastCapacity = -1;
        public int LastCarried = -1;
        public bool LastMining;
        public float LastMiningProgress = -1f;
        public VisualElement Move1Cool;
        public float LastMove1Value = -1f;
        public VisualElement Move2Box;
        public VisualElement Move2Cool;
        public float LastMove2Value = -1f;
        public bool LastChasing;
        public int LastKnocked;
        public float HitUntil;
    }

    private class RivalChip
    {
        public MoriMochiAgent Agent;
        public Label Action;
        public string LastAction;
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

    private readonly List<Card> cards = new();
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
            else BuildCard(agent);
        }
    }

    private void BuildChip(MoriMochiAgent agent)
    {
        var chip = new VisualElement();
        chip.pickingMode = PickingMode.Ignore;
        chip.AddToClassList("hud-chip-rival");

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

        chips.Add(new RivalChip { Agent = agent, Action = action });
    }

    private void BuildCard(MoriMochiAgent agent)
    {
        var card = new Card { Agent = agent, Occupation = agent.Occupation };

        var root2 = new VisualElement();
        root2.pickingMode = PickingMode.Ignore;
        root2.AddToClassList("hud-card");
        Color color = agent.DNA.BaseColor;
        color.a = 1f;
        root2.style.borderTopColor = color;
        card.Root = root2;

        var header = new VisualElement();
        header.pickingMode = PickingMode.Ignore;
        header.AddToClassList("hud-card__header");

        var swatch = new VisualElement();
        swatch.pickingMode = PickingMode.Ignore;
        swatch.AddToClassList("hud-card__swatch");
        swatch.style.backgroundColor = color;
        header.Add(swatch);

        var text = new VisualElement();
        text.pickingMode = PickingMode.Ignore;
        text.AddToClassList("hud-card__text");

        var name = new Label(agent.DNA.CustomName);
        name.pickingMode = PickingMode.Ignore;
        name.AddToClassList("hud-card__name");
        text.Add(name);

        var posture = new Label(ArenaOrderCatalog.ArchetypeShort(agent.Orders));
        posture.pickingMode = PickingMode.Ignore;
        posture.AddToClassList("hud-card__posture");
        text.Add(posture);

        header.Add(text);
        root2.Add(header);

        var pillars = new VisualElement();
        pillars.pickingMode = PickingMode.Ignore;
        pillars.AddToClassList("hud-card__pillars");
        for (int i = 0; i < PillarOrder.Length; i++)
        {
            var pillar = PillarOrder[i];
            var chipLabel = new Label(ArenaOrderCatalog.ChoiceLabel(pillar, ArenaOrderRules.Choice(agent.Orders, pillar)));
            chipLabel.pickingMode = PickingMode.Ignore;
            chipLabel.AddToClassList("hud-chip");
            chipLabel.AddToClassList(PillarChipClass[i]);
            pillars.Add(chipLabel);
        }
        root2.Add(pillars);

        var action = new Label();
        action.pickingMode = PickingMode.Ignore;
        action.AddToClassList("hud-card__action");
        root2.Add(action);
        card.Action = action;

        var carry = new VisualElement();
        carry.pickingMode = PickingMode.Ignore;
        carry.AddToClassList("hud-card__carry");
        root2.Add(carry);
        card.Carry = carry;

        var moves = new VisualElement();
        moves.pickingMode = PickingMode.Ignore;
        moves.AddToClassList("hud-card__moves");

        string move1Label, move2Label;
        switch (card.Occupation)
        {
            case Occupation.Guard: move1Label = "Embestida"; move2Label = "Persigue"; break;
            case Occupation.Break: move1Label = "Embestida"; move2Label = "Retirada"; break;
            case Occupation.Decoy: move1Label = "Provocar"; move2Label = "Huir"; break;
            default: move1Label = "Minar"; move2Label = "Huir"; break;
        }

        BuildMove(moves, move1Label, out _, out card.Move1Cool);
        BuildMove(moves, move2Label, out card.Move2Box, out card.Move2Cool);

        root2.Add(moves);

        playerTeam.Add(root2);
        card.LastKnocked = agent.ClashTimesKnocked;
        cards.Add(card);
    }

    private void BuildMove(VisualElement parent, string label, out VisualElement box, out VisualElement cool)
    {
        var moveBox = new VisualElement();
        moveBox.pickingMode = PickingMode.Ignore;
        moveBox.AddToClassList("hud-move");

        var coolElement = new VisualElement();
        coolElement.pickingMode = PickingMode.Ignore;
        coolElement.AddToClassList("hud-move__cool");
        moveBox.Add(coolElement);

        var text = new Label(label);
        text.pickingMode = PickingMode.Ignore;
        text.AddToClassList("hud-move__label");
        moveBox.Add(text);

        parent.Add(moveBox);
        box = moveBox;
        cool = coolElement;
    }

    private static void SetMoveValue(VisualElement cool, ref float last, float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Abs(value - last) <= 0.01f) return;
        cool.style.width = Length.Percent(value * 100f);
        last = value;
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
        }

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            var agent = card.Agent;
            if (agent == null)
            {
                lastRosterCount = -1;
                continue;
            }

            string actionText = LocEnumMaps.IntentName(agent.Intent) + (agent.TrustedGuardian != null ? " · custodiado" : "");
            if (actionText != card.LastAction)
            {
                card.Action.text = actionText;
                card.LastAction = actionText;
            }

            RefreshCarry(card, agent);
            RefreshMoves(card, agent);

            int knocked = agent.ClashTimesKnocked;
            if (knocked > card.LastKnocked)
            {
                card.LastKnocked = knocked;
                card.Root.AddToClassList("hud-card--hit");
                card.HitUntil = Time.time + 0.6f;
            }
            if (card.HitUntil > 0f && Time.time >= card.HitUntil)
            {
                card.Root.RemoveFromClassList("hud-card--hit");
                card.HitUntil = 0f;
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

    private void RefreshCarry(Card card, MoriMochiAgent agent)
    {
        int capacity = agent.CarryCapacity;
        if (capacity != card.LastCapacity)
        {
            card.LastCapacity = capacity;
            card.CarrySlots.Clear();
            card.Carry.Clear();
            for (int i = 0; i < capacity; i++)
            {
                var slot = new VisualElement();
                slot.pickingMode = PickingMode.Ignore;
                slot.AddToClassList("hud-slot");
                card.Carry.Add(slot);
                card.CarrySlots.Add(slot);
            }
            card.LastCarried = -1;
            card.LastMining = false;
            card.LastMiningProgress = -1f;
        }

        int carried = agent.Carried;
        bool mining = agent.Intent == CreatureIntent.Taking && carried < capacity;
        if (carried != card.LastCarried || mining != card.LastMining)
        {
            for (int i = 0; i < card.CarrySlots.Count; i++)
            {
                card.CarrySlots[i].EnableInClassList("hud-slot--full", i < carried);
                if (card.CarryFill != null && card.CarryFill.parent == card.CarrySlots[i]) card.CarrySlots[i].Remove(card.CarryFill);
            }

            if (mining)
            {
                card.CarryFill ??= new VisualElement { pickingMode = PickingMode.Ignore };
                card.CarryFill.AddToClassList("hud-slot__fill");
                card.CarrySlots[carried].Add(card.CarryFill);
            }

            card.LastCarried = carried;
            card.LastMining = mining;
            card.LastMiningProgress = -1f;
        }

        if (mining)
        {
            float progress = agent.MiningProgress;
            if (Mathf.Abs(progress - card.LastMiningProgress) > 0.01f)
            {
                card.CarryFill.style.width = Length.Percent(progress * 100f);
                card.LastMiningProgress = progress;
            }
        }
    }

    private void RefreshMoves(Card card, MoriMochiAgent agent)
    {
        switch (card.Occupation)
        {
            case Occupation.Guard:
                SetMoveValue(card.Move1Cool, ref card.LastMove1Value, agent.ClashCooldown01);
                bool chasing = agent.IsChasing;
                if (chasing != card.LastChasing)
                {
                    card.Move2Box.EnableInClassList("hud-move--on", chasing);
                    card.LastChasing = chasing;
                }
                break;
            case Occupation.Break:
                SetMoveValue(card.Move1Cool, ref card.LastMove1Value, agent.ClashCooldown01);
                SetMoveValue(card.Move2Cool, ref card.LastMove2Value, agent.Retreat01);
                break;
            case Occupation.Decoy:
                SetMoveValue(card.Move1Cool, ref card.LastMove1Value, agent.DecoyCooldown01);
                SetMoveValue(card.Move2Cool, ref card.LastMove2Value, agent.FleeCooldown01);
                break;
            default:
                float mine = agent.Intent == CreatureIntent.Taking ? 1f - agent.MiningProgress : 0f;
                SetMoveValue(card.Move1Cool, ref card.LastMove1Value, mine);
                SetMoveValue(card.Move2Cool, ref card.LastMove2Value, agent.FleeCooldown01);
                break;
        }
    }
}
}
