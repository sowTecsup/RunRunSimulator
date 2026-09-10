using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

[RequireComponent(typeof(UIDocument))]
public class ArenaPlanPanel : MonoBehaviour
{
    private static readonly OrderPillar[] Pillars = { OrderPillar.Loot, OrderPillar.Contact, OrderPillar.Posture };
    private const int ChoicesPerPillar = 2;

    [Required, SerializeField] private ArenaSandbox sandbox;
    [Required, SerializeField] private ArenaRound round;
    [Required, SerializeField] private ArenaCastPicker picker;
    [Required, SerializeField] private ArenaResultPanel resultPanel;
    [SerializeField] private MonchiTurntable turntable;
    [SerializeField, Min(0f)] private float resultHoldSeconds = 4f;

    private class Card
    {
        public int Index;
        public Button[][] Pills;
        public Label Archetype;
        public Label Description;
        public Label Hint;
    }

    private VisualElement root;
    private VisualElement castList;
    private Label roomLabel;
    private VisualElement rivalList;
    private Button castButton;
    private Button pickButton;
    private Button shuffleButton;
    private Button paletteButton;
    private Button roomButton;
    private Button playButton;

    private readonly List<Card> cards = new();
    private bool visible;
    private bool roundEndHandled;
    private float roundEndedAt;
    private ExpeditionTeam pendingWinner;
    private int pendingMine;
    private int pendingTheirs;
    private int lastPlannedCount = -1;
    private int lastSeed = int.MinValue;

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement.Q("plan-root");
        if (root == null) return;

        castList = root.Q("cast-list");
        roomLabel = root.Q<Label>("plan-room");
        rivalList = root.Q("rival-list");
        castButton = root.Q<Button>("btn-cast");
        pickButton = root.Q<Button>("btn-pick");
        shuffleButton = root.Q<Button>("btn-shuffle");
        paletteButton = root.Q<Button>("btn-palette");
        roomButton = root.Q<Button>("btn-room");
        playButton = root.Q<Button>("btn-play");

        castButton.clicked += ToggleCastMode;
        pickButton.clicked += OpenPicker;
        shuffleButton.clicked += Shuffle;
        paletteButton.clicked += CyclePalette;
        roomButton.clicked += NewRoom;
        playButton.clicked += Play;

        lastPlannedCount = -1;
        lastSeed = int.MinValue;
        SetVisible(true);
    }

    private void OnDisable()
    {
        if (turntable != null) turntable.HideAll();
        if (root == null) return;
        castButton.clicked -= ToggleCastMode;
        pickButton.clicked -= OpenPicker;
        shuffleButton.clicked -= Shuffle;
        paletteButton.clicked -= CyclePalette;
        roomButton.clicked -= NewRoom;
        playButton.clicked -= Play;
    }

    private void Update()
    {
        if (root == null || sandbox == null || round == null) return;

        if (round.IsRunning && visible)
        {
            roundEndHandled = false;
            SetVisible(false);
        }

        if (round.IsOver && !roundEndHandled)
        {
            roundEndHandled = true;
            roundEndedAt = Time.time;
            pendingWinner = round.Winner;
            pendingMine = round.PlayerSecured;
            pendingTheirs = round.RivalSecured;
        }

        if (roundEndHandled && !visible && Time.time - roundEndedAt >= resultHoldSeconds)
        {
            round.Reset(false);
            resultPanel.Show(pendingWinner, pendingMine, pendingTheirs, round.Summary);
            SetVisible(true);
        }

        if (!visible) return;
        if (picker.IsOpen) return;

        if (sandbox.PlannedCast.Count != lastPlannedCount || sandbox.ActiveSeed != lastSeed)
            Refresh();
    }

    private void SetVisible(bool value)
    {
        visible = value;
        root.EnableInClassList("plan--hidden", !value);
        if (value) Refresh();
        else if (turntable != null) turntable.HideAll();
    }

    private void Refresh()
    {
        lastPlannedCount = sandbox.PlannedCast.Count;
        lastSeed = sandbox.ActiveSeed;

        RefreshTeamLine();
        castButton.text = sandbox.CastMode == ArenaCastMode.LocalSave
            ? (sandbox.LocalCastAvailable ? "Mis MoriMonchis" : "Mis MoriMonchis (sin save)")
            : "Elenco básico";
        paletteButton.text = "Paleta ▸";
        pickButton.SetEnabled(sandbox.CastMode == ArenaCastMode.LocalSave && sandbox.LocalCastAvailable);
        shuffleButton.SetEnabled(sandbox.CastMode == ArenaCastMode.LocalSave && sandbox.LocalCastAvailable);

        BuildCards();
        RefreshRivalLine();
    }

    private void BuildCards()
    {
        castList.Clear();
        cards.Clear();

        var cast = sandbox.PlannedCast;
        for (int i = 0; i < cast.Count; i++)
        {
            var entry = cast[i];
            if (entry.Team != ExpeditionTeam.Player || entry.Dna == null) continue;
            castList.Add(BuildCard(i, entry));
        }
    }

    private VisualElement BuildCard(int index, ArenaCastEntry entry)
    {
        var card = new VisualElement();
        card.AddToClassList("cast-card");

        var head = new VisualElement();
        head.AddToClassList("cast-card__head");

        var swatch = new VisualElement();
        swatch.AddToClassList("cast-card__swatch");
        Color color = entry.Dna.BaseColor;
        color.a = 1f;
        swatch.style.backgroundColor = color;
        head.Add(swatch);

        var rules = ExpeditionRulesSO.Current;

        var text = new VisualElement();
        var name = new Label(entry.Dna.CustomName);
        name.AddToClassList("cast-card__name");
        var dials = new Label($"{ArenaOrderCatalog.PersonalityName(entry.Dna, rules)} → {ArenaOrderCatalog.UnlockRead(entry.Dna, rules)}  ·  osadía {entry.Dna.Boldness:0.00}  ·  sociable {entry.Dna.Sociability:0.00}");
        dials.AddToClassList("cast-card__dials");
        text.Add(name);
        text.Add(dials);
        head.Add(text);
        card.Add(head);

        var state = new Card { Index = index, Pills = new Button[Pillars.Length][] };

        for (int p = 0; p < Pillars.Length; p++)
        {
            OrderPillar pillar = Pillars[p];
            state.Pills[p] = new Button[ChoicesPerPillar];

            var row = new VisualElement();
            row.AddToClassList("plan-row");
            var rowLabel = new Label(ArenaOrderCatalog.PillarLabels[p]);
            rowLabel.AddToClassList("plan-row__label");
            row.Add(rowLabel);

            for (int k = 0; k < ChoicesPerPillar; k++)
            {
                int choice = k;
                var pill = new Button(() => ChoosePillar(state, pillar, choice)) { text = ArenaOrderCatalog.ChoiceLabel(pillar, k) };
                pill.AddToClassList("pill");
                if (pillar == OrderPillar.Loot) pill.AddToClassList("pill--site");
                state.Pills[p][k] = pill;
                row.Add(pill);
            }

            if (ArenaOrderRules.IsLocked(entry.Dna, rules, pillar, out int forced))
            {
                for (int k = 0; k < ChoicesPerPillar; k++)
                    state.Pills[p][k].SetEnabled(false);
                state.Pills[p][forced].AddToClassList("pill--locked");

                var lockLabel = new Label(ArenaOrderCatalog.LockReason(pillar, forced));
                lockLabel.AddToClassList("plan-row__lock");
                row.Add(lockLabel);
            }

            card.Add(row);
        }

        state.Archetype = new Label();
        state.Archetype.AddToClassList("cast-card__arch");
        card.Add(state.Archetype);

        state.Description = new Label();
        state.Description.AddToClassList("cast-card__desc");
        card.Add(state.Description);

        state.Hint = new Label();
        state.Hint.AddToClassList("cast-card__hint");
        card.Add(state.Hint);

        cards.Add(state);
        RefreshPills(state);
        return card;
    }

    private void ChoosePillar(Card card, OrderPillar pillar, int choice)
    {
        var entry = sandbox.PlannedCast[card.Index];
        sandbox.SetPlayerOrders(card.Index, ArenaOrderRules.With(entry.Orders, pillar, choice));
        RefreshPills(card);
    }

    private void RefreshPills(Card card)
    {
        if (card.Index >= sandbox.PlannedCast.Count) return;
        var entry = sandbox.PlannedCast[card.Index];

        for (int p = 0; p < Pillars.Length; p++)
        {
            int active = ArenaOrderRules.Choice(entry.Orders, Pillars[p]);
            for (int k = 0; k < ChoicesPerPillar; k++)
                card.Pills[p][k].EnableInClassList("pill--on", k == active);
        }

        card.Archetype.text = "→ " + ArenaOrderCatalog.ArchetypeName(entry.Orders);
        card.Description.text = ArenaOrderCatalog.ArchetypeDescription(entry.Orders);
        card.Hint.text = ArenaOrderCatalog.CounterHint(entry.Orders);
        RefreshTeamLine();
    }

    private void RefreshTeamLine()
    {
        var orders = new List<ArenaOrders>();
        foreach (var entry in sandbox.PlannedCast)
            if (entry.Team == ExpeditionTeam.Player && entry.Dna != null) orders.Add(entry.Orders);

        string plan = ArenaOrderCatalog.TeamPlanName(orders);
        string shape = string.IsNullOrEmpty(sandbox.ShapeName) ? "" : $"  ·  {sandbox.ShapeName}";
        string room = $"sala {sandbox.ActiveSeed}{shape}  ·  {sandbox.PaletteName}  ·  entrada {sandbox.EntryName}";
        string read = ArenaOrderCatalog.RoomText(sandbox.ReadRoom(ExpeditionTeam.Player));
        roomLabel.text = room + System.Environment.NewLine + read + (plan.Length > 0 ? System.Environment.NewLine + "Tu plan: " + plan : "");
    }

    private void RefreshRivalLine()
    {
        var rules = ExpeditionRulesSO.Current;
        rivalList.Clear();
        if (turntable != null) turntable.HideAll();

        int k = 0;
        foreach (var entry in sandbox.PlannedCast)
        {
            if (entry.Team != ExpeditionTeam.Rival || entry.Dna == null) continue;

            var card = new VisualElement();
            card.AddToClassList("rival-card");

            var portrait = new VisualElement();
            portrait.AddToClassList("rival-card__portrait");
            card.Add(portrait);

            var text = new VisualElement();
            text.AddToClassList("rival-card__text");

            var name = new Label(entry.Dna.CustomName);
            name.AddToClassList("rival-card__name");
            text.Add(name);

            var nature = new Label(ArenaOrderCatalog.PersonalityName(entry.Dna, rules));
            nature.AddToClassList("rival-card__nature");
            text.Add(nature);

            var read = new Label("→ " + ArenaOrderCatalog.RivalRead(entry.Dna, rules));
            read.AddToClassList("rival-card__read");
            text.Add(read);

            card.Add(text);
            rivalList.Add(card);

            if (turntable != null && k < 3) turntable.Show(k, entry.Dna, portrait);
            k++;
        }
    }

    private void ToggleCastMode()
    {
        sandbox.SetCastMode(sandbox.CastMode == ArenaCastMode.Roster ? ArenaCastMode.LocalSave : ArenaCastMode.Roster);
        Refresh();
    }

    private void OpenPicker()
    {
        picker.Open(Refresh);
    }

    private void Shuffle()
    {
        sandbox.ShuffleCast();
        Refresh();
    }

    private void CyclePalette()
    {
        sandbox.CyclePalette();
        Refresh();
    }

    private void NewRoom()
    {
        round.Reset(true);
        resultPanel.Hide();
        Refresh();
    }

    private void Play()
    {
        resultPanel.Hide();
        round.Launch();
        roundEndHandled = false;
        SetVisible(false);
    }
}
}
