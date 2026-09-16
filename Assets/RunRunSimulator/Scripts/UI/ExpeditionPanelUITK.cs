using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

[DisallowMultipleComponent]
public class ExpeditionPanelUITK : MonoBehaviour, IUINavigable
{
    [SerializeField] private UIDocument document;
    [SerializeField] private UIPanelType panel = UIPanelType.Expedition;
    [SerializeField, Min(1)] private int maxPick = 3;

    private Label emptyLabel;
    private ScrollView list;
    private Button closeButton;
    private Button goButton;

    private readonly List<VisualElement> cards = new List<VisualElement>();
    private readonly List<CreatureDNA> dnas = new List<CreatureDNA>();
    private readonly List<bool> eligible = new List<bool>();
    private readonly List<bool> picked = new List<bool>();
    private int focused = -1;
    private bool wired;

    private void OnEnable()
    {
        UIManager.OnPanelSetRequested    += OnPanelSet;
        UIManager.OnPanelToggleRequested += OnPanelToggle;
    }

    private void OnDisable()
    {
        UIManager.OnPanelSetRequested    -= OnPanelSet;
        UIManager.OnPanelToggleRequested -= OnPanelToggle;
    }

    private void Start()
    {
        var root = UiPanels.RootOf(document);
        if (root == null) return;

        root.Q<Label>("exp-title").text = Loc.Tr("ui.expedition.title");
        root.Q<Label>("exp-subtitle").text = Loc.Tr("ui.expedition.subtitle", maxPick);

        emptyLabel = root.Q<Label>("exp-empty");
        list = root.Q<ScrollView>("exp-list");

        closeButton = root.Q<Button>("exp-close");
        if (closeButton != null)
        {
            closeButton.text = Loc.Tr("ui.expedition.close");
            closeButton.clicked += Close;
        }

        goButton = root.Q<Button>("exp-go");
        if (goButton != null) goButton.clicked += Depart;

        wired = true;
        UIManager.RegisterNavigable(panel, this);
        Rebuild();
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= Close;
        if (goButton != null) goButton.clicked -= Depart;
        UIManager.UnregisterNavigable(panel);
    }

    private void OnPanelSet(UIPanelType p, bool show)
    {
        if (p == panel && show && wired) Rebuild();
    }

    private void OnPanelToggle(UIPanelType p)
    {
        if (p != panel || !wired) return;
        var root = UiPanels.RootOf(document);
        root?.schedule.Execute(() => { if (root.resolvedStyle.display != DisplayStyle.None) Rebuild(); });
    }

    private void Rebuild()
    {
        list?.Clear();
        cards.Clear();
        dnas.Clear();
        eligible.Clear();
        picked.Clear();

        var gm = GameManager.Instance;
        var registry = gm != null ? gm.Registry : null;

        var entries = new List<CreatureDNA>();
        if (registry != null)
        {
            foreach (var dna in registry.GetAll().Values)
            {
                if (dna.IsDead || dna.IsSold) continue;
                entries.Add(dna);
            }
        }

        entries.Sort(CompareEntries);

        foreach (var dna in entries)
        {
            bool ok = !dna.IsBusy && dna.Needs.Health > 0f;
            var card = BuildCard(dna, ok);

            int index = cards.Count;
            card.RegisterCallback<ClickEvent>(_ => ToggleAt(index));

            cards.Add(card);
            dnas.Add(dna);
            eligible.Add(ok);
            picked.Add(false);
            list?.Add(card);
        }

        if (emptyLabel != null)
        {
            emptyLabel.text = Loc.Tr("ui.expedition.empty");
            emptyLabel.style.display = eligible.Contains(true) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        SetFocus(cards.Count == 0 ? -1 : FirstEligible());
        UpdateGoButton();
    }

    private int CompareEntries(CreatureDNA a, CreatureDNA b)
    {
        bool ea = !a.IsBusy && a.Needs.Health > 0f;
        bool eb = !b.IsBusy && b.Needs.Health > 0f;
        if (ea != eb) return ea ? -1 : 1;
        return b.Needs.Health.CompareTo(a.Needs.Health);
    }

    private int FirstEligible()
    {
        for (int i = 0; i < eligible.Count; i++)
            if (eligible[i]) return i;
        return 0;
    }

    private VisualElement BuildCard(CreatureDNA dna, bool ok)
    {
        var card = new VisualElement();
        card.AddToClassList("exp-card");
        if (!ok) card.AddToClassList("exp-card--off");

        var icon = new VisualElement();
        icon.AddToClassList("exp-card__icon");
        MonchiPortraitUI.Apply(icon, dna);
        card.Add(icon);

        var name = new Label(dna.CustomName);
        name.AddToClassList("exp-card__name");
        card.Add(name);

        var state = new Label(StateTextFor(dna, ok));
        state.AddToClassList("exp-card__state");
        card.Add(state);

        var barTrack = new VisualElement();
        barTrack.AddToClassList("exp-card__bar-track");
        var barFill = new VisualElement();
        barFill.AddToClassList("exp-card__bar-fill");
        barFill.AddToClassList(HealthColorClass(dna.Needs.Health));
        barFill.style.width = new StyleLength(new Length(Mathf.Clamp(dna.Needs.Health, 0f, 100f), LengthUnit.Percent));
        barTrack.Add(barFill);
        card.Add(barTrack);

        return card;
    }

    private string HealthColorClass(float health)
    {
        if (health >= 60f) return "exp-bar--good";
        if (health >= 30f) return "exp-bar--warn";
        return "exp-bar--crit";
    }

    private string StateTextFor(CreatureDNA dna, bool ok)
    {
        int health = Mathf.RoundToInt(dna.Needs.Health);
        if (dna.IsBusy) return Loc.Tr("ui.expedition.busy");
        if (ok) return Loc.Tr("ui.expedition.energy", health);
        return Loc.Tr("ui.expedition.tired", health);
    }

    private void ToggleAt(int index)
    {
        SetFocus(index);
        if (index < 0 || index >= eligible.Count || !eligible[index]) return;
        if (!picked[index] && CountPicked() >= maxPick) return;
        picked[index] = !picked[index];
        cards[index].EnableInClassList("exp-card--on", picked[index]);
        UpdateGoButton();
    }

    private int CountPicked()
    {
        int n = 0;
        for (int i = 0; i < picked.Count; i++)
            if (picked[i]) n++;
        return n;
    }

    private void SetFocus(int index)
    {
        focused = UiPanels.ClampSelection(cards.Count, index);
        UiPanels.SetActiveIndex(cards, focused, "exp-card--focus");
        if (focused >= 0) list?.ScrollTo(cards[focused]);
    }

    private void UpdateGoButton()
    {
        int count = CountPicked();
        if (goButton != null)
        {
            goButton.text = Loc.Tr("ui.expedition.go", count, maxPick);
            goButton.SetEnabled(count >= 1);
        }
    }

    private void Depart()
    {
        if (CountPicked() < 1) return;

        var ids = new List<string>();
        for (int i = 0; i < picked.Count; i++)
            if (picked[i]) ids.Add(dnas[i].UniqueID);

        Close();
        ExpeditionBridge.RequestDeparture(ids);
    }

    private void Close() => UIManager.RequestPanelSet(panel, false);

    public void OnUINavigate(Vector2 dir)
    {
        if (!wired) return;
        if (dir.x > 0.5f) SetFocus(focused + 1);
        else if (dir.x < -0.5f) SetFocus(focused - 1);
    }

    public void OnUISubmit()
    {
        if (!wired || focused < 0) return;
        ToggleAt(focused);
    }

    public bool OnUICancel() => false;
}
}
