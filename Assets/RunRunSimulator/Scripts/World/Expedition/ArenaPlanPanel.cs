using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

[RequireComponent(typeof(UIDocument))]
public class ArenaPlanPanel : MonoBehaviour
{
    private static readonly Occupation[] Occupations = { Occupation.Gather, Occupation.Guard, Occupation.Break, Occupation.Decoy };
    private static readonly string[] OccupationLabels = { "Recolecta", "Vigila", "Rompe", "Distrae" };
    private static readonly ArenaSite[] Sites = { ArenaSite.Center, ArenaSite.NearVein, ArenaSite.FarVein };
    private static readonly string[] SiteLabels = { "Centro", "Veta cercana", "Veta lejana" };

    [Required, SerializeField] private ArenaSandbox sandbox;
    [Required, SerializeField] private ArenaRound round;
    [Required, SerializeField] private ArenaCastPicker picker;
    [Required, SerializeField] private ArenaResultPanel resultPanel;
    [SerializeField, Min(0f)] private float resultHoldSeconds = 4f;

    private class Card
    {
        public int Index;
        public Button[] OccupationPills;
        public Button[] SitePills;
    }

    private VisualElement root;
    private VisualElement castList;
    private Label roomLabel;
    private Label rivalLabel;
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
        rivalLabel = root.Q<Label>("plan-rival");
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
    }

    private void Refresh()
    {
        lastPlannedCount = sandbox.PlannedCast.Count;
        lastSeed = sandbox.ActiveSeed;

        roomLabel.text = $"sala {sandbox.ActiveSeed}  ·  {sandbox.PaletteName}  ·  entrada {sandbox.EntryName}";
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

        var text = new VisualElement();
        var name = new Label(entry.Dna.CustomName);
        name.AddToClassList("cast-card__name");
        var dials = new Label($"osadía {entry.Dna.Boldness:0.00}  ·  sociable {entry.Dna.Sociability:0.00}");
        dials.AddToClassList("cast-card__dials");
        text.Add(name);
        text.Add(dials);
        head.Add(text);
        card.Add(head);

        var state = new Card { Index = index, OccupationPills = new Button[Occupations.Length], SitePills = new Button[Sites.Length] };

        var occupationRow = new VisualElement();
        occupationRow.AddToClassList("plan-row");
        var occupationLabel = new Label("HACE");
        occupationLabel.AddToClassList("plan-row__label");
        occupationRow.Add(occupationLabel);
        for (int k = 0; k < Occupations.Length; k++)
        {
            int choice = k;
            var pill = new Button(() => ChooseOccupation(state, choice)) { text = OccupationLabels[k] };
            pill.AddToClassList("pill");
            state.OccupationPills[k] = pill;
            occupationRow.Add(pill);
        }
        card.Add(occupationRow);

        var siteRow = new VisualElement();
        siteRow.AddToClassList("plan-row");
        var siteLabel = new Label("DÓNDE");
        siteLabel.AddToClassList("plan-row__label");
        siteRow.Add(siteLabel);
        for (int k = 0; k < Sites.Length; k++)
        {
            int choice = k;
            var pill = new Button(() => ChooseSite(state, choice)) { text = SiteLabels[k] };
            pill.AddToClassList("pill");
            pill.AddToClassList("pill--site");
            state.SitePills[k] = pill;
            siteRow.Add(pill);
        }
        card.Add(siteRow);

        cards.Add(state);
        RefreshPills(state);
        return card;
    }

    private void ChooseOccupation(Card card, int choice)
    {
        var entry = sandbox.PlannedCast[card.Index];
        sandbox.SetPlayerPlan(card.Index, Occupations[choice], entry.Site);
        RefreshPills(card);
    }

    private void ChooseSite(Card card, int choice)
    {
        var entry = sandbox.PlannedCast[card.Index];
        sandbox.SetPlayerPlan(card.Index, entry.Occupation, Sites[choice]);
        RefreshPills(card);
    }

    private void RefreshPills(Card card)
    {
        if (card.Index >= sandbox.PlannedCast.Count) return;
        var entry = sandbox.PlannedCast[card.Index];

        for (int k = 0; k < Occupations.Length; k++)
            card.OccupationPills[k].EnableInClassList("pill--on", Occupations[k] == entry.Occupation);

        bool siteMatters = entry.Occupation != Occupation.Decoy;
        for (int k = 0; k < Sites.Length; k++)
        {
            card.SitePills[k].EnableInClassList("pill--on", siteMatters && Sites[k] == entry.Site);
            card.SitePills[k].SetEnabled(siteMatters);
        }
    }

    private void RefreshRivalLine()
    {
        var names = new List<string>();
        foreach (var entry in sandbox.PlannedCast)
            if (entry.Team == ExpeditionTeam.Rival && entry.Dna != null) names.Add(entry.Dna.CustomName);

        rivalLabel.text = names.Count == 0
            ? ""
            : "Rival: " + string.Join(" · ", names) + "  ·  entra por el lado opuesto";
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
