using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

[RequireComponent(typeof(UIDocument))]
public class ArenaCastPicker : MonoBehaviour
{
    [Required, SerializeField] private ArenaSandbox sandbox;
    [SerializeField, Min(1)] private int maxPick = 3;

    private VisualElement root;
    private Label countLabel;
    private VisualElement grid;
    private Button okButton;
    private Button cancelButton;

    private readonly List<CreatureDNA> selection = new();
    private readonly Dictionary<CreatureDNA, Button> cards = new();
    private System.Action onClosed;

    public bool IsOpen { get; private set; }

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement.Q("picker-root");
        if (root == null) return;

        countLabel = root.Q<Label>("picker-count");
        grid = root.Q("picker-grid");
        okButton = root.Q<Button>("btn-picker-ok");
        cancelButton = root.Q<Button>("btn-picker-cancel");

        okButton.clicked += Confirm;
        cancelButton.clicked += Cancel;

        root.AddToClassList("picker--hidden");
        IsOpen = false;
    }

    private void OnDisable()
    {
        if (root == null) return;
        okButton.clicked -= Confirm;
        cancelButton.clicked -= Cancel;
    }

    public void Open(System.Action closedCallback)
    {
        if (root == null) return;

        onClosed = closedCallback;
        BuildGrid();
        root.RemoveFromClassList("picker--hidden");
        IsOpen = true;
        Refresh();
    }

    private void BuildGrid()
    {
        grid.Clear();
        selection.Clear();
        cards.Clear();

        foreach (var dna in sandbox.LocalPool)
        {
            if (dna == null) continue;
            if (IsPlanned(dna)) selection.Add(dna);
            cards[dna] = BuildCard(dna);
            grid.Add(cards[dna]);
        }
    }

    private bool IsPlanned(CreatureDNA dna)
    {
        foreach (var entry in sandbox.PlannedCast)
        {
            if (entry.Team != ExpeditionTeam.Player || entry.Dna == null) continue;
            if (entry.Dna == dna) return true;
            if (entry.Dna.CustomName == dna.CustomName) return true;
        }
        return false;
    }

    private Button BuildCard(CreatureDNA dna)
    {
        var card = new Button(() => TogglePick(dna));
        card.AddToClassList("pick-card");

        var swatch = new VisualElement();
        swatch.AddToClassList("pick-card__swatch");
        Color color = dna.BaseColor;
        color.a = 1f;
        swatch.style.backgroundColor = color;
        card.Add(swatch);

        var name = new Label(dna.CustomName);
        name.AddToClassList("pick-card__name");
        card.Add(name);

        var dials = new Label($"osadía {dna.Boldness:0.00} · sociable {dna.Sociability:0.00}");
        dials.AddToClassList("pick-card__dials");
        card.Add(dials);

        return card;
    }

    private void TogglePick(CreatureDNA dna)
    {
        if (selection.Contains(dna)) selection.Remove(dna);
        else if (selection.Count < maxPick) selection.Add(dna);

        Refresh();
    }

    private void Refresh()
    {
        foreach (var pair in cards)
            pair.Value.EnableInClassList("pick-card--on", selection.Contains(pair.Key));

        countLabel.text = $"{selection.Count} / {maxPick}";
        okButton.SetEnabled(selection.Count > 0);
    }

    private void Confirm()
    {
        sandbox.SelectLocalCast(selection);
        Close();
    }

    private void Cancel()
    {
        Close();
    }

    private void Close()
    {
        root.AddToClassList("picker--hidden");
        IsOpen = false;
        var callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }
}
}
