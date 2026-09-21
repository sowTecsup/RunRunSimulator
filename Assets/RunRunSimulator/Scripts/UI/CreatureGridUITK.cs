using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

[DisallowMultipleComponent]
public class CreatureGridUITK : MonoBehaviour, IUINavigable
{
    [Header("UI Toolkit setup")]

    [SerializeField] private UIDocument document;

    [SerializeField] private UIPanelType panel = UIPanelType.CreatureGrid;

    [SerializeField] private VisualTreeAsset cardTemplate;

    [Header("Layout")]

    [SerializeField] private Vector2 cardSize = new Vector2(120f, 150f);

    private const string SelectedClass = "card--selected";

    private ScrollView scroll;

    private Button closeButton;

    private readonly List<VisualElement> cards = new List<VisualElement>();
    private CreatureRegistrySO currentRegistry;
    private int selectedIndex = -1;

    private void OnEnable()
    {
        GameEvents.OnRegistryChanged  += Rebuild;
        GameEvents.OnRegistryReloaded += Rebuild;
    }

    private void OnDisable()
    {
        GameEvents.OnRegistryChanged  -= Rebuild;
        GameEvents.OnRegistryReloaded -= Rebuild;
    }

    private void Start()
    {
        WireCloseButton();
        WireTitle();
        UIManager.RegisterNavigable(panel, this);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= OnCloseClicked;
        UIManager.UnregisterNavigable(panel);
    }

    public void OnUINavigate(Vector2 dir)
    {
        if (cards.Count == 0) return;
        int idx  = selectedIndex < 0 ? 0 : selectedIndex;
        int cols = ColumnsPerRow();

        if      (dir.x >  0.5f) idx += 1;
        else if (dir.x < -0.5f) idx -= 1;
        else if (dir.y < -0.5f) idx += cols;
        else if (dir.y >  0.5f) idx -= cols;

        Select(idx);
    }

    public void OnUISubmit()
    {
        if (selectedIndex < 0 || selectedIndex >= cards.Count) return;
        if (cards[selectedIndex].userData is CreatureDNA dna)
            UIManager.SelectCreature(dna, currentRegistry);
    }

    public bool OnUICancel() => false;

    private void Rebuild(CreatureRegistrySO registry)
    {
        var container = ResolveContainer();
        if (container == null || registry == null || cardTemplate == null) return;

        currentRegistry = registry;
        container.Clear();
        cards.Clear();

        foreach (var dna in registry.GetAll().Values.OrderByDescending(d => d.BirthDate))
        {
            var card = cardTemplate.Instantiate().Q<VisualElement>("card");
            if (card == null) continue;
            BindCard(card, dna);
            card.userData = dna;

            card.RegisterCallback<ClickEvent>(_ =>
            {
                Select(cards.IndexOf(card));
                UIManager.SelectCreature(dna, registry);
            });

            cards.Add(card);
            container.Add(card);
        }

        Select(selectedIndex < 0 ? 0 : selectedIndex, scrollIntoView: false);
    }

    private void BindCard(VisualElement card, CreatureDNA dna)
    {
        card.style.width  = cardSize.x;
        card.style.height = cardSize.y;

        var nameLabel = card.Q<Label>("name-label");
        if (nameLabel != null)
            nameLabel.text = string.IsNullOrEmpty(dna.CustomName) ? dna.ToStringID() : dna.CustomName;

        var icon = card.Q<VisualElement>("icon");
        if (icon != null)
            MonchiPortraitUI.Apply(icon, dna);

        var stateLabel = card.Q<Label>("state-label");
        if (stateLabel != null)
            stateLabel.text = CreatureDisplay.StateOf(dna);
    }

    private void Select(int idx, bool scrollIntoView = true)
    {
        selectedIndex = UiPanels.ClampSelection(cards.Count, idx);
        if (selectedIndex < 0) return;

        UiPanels.SetActiveIndex(cards, selectedIndex, SelectedClass);

        if (scrollIntoView && scroll != null)
            scroll.ScrollTo(cards[selectedIndex]);
    }

    private int ColumnsPerRow()
    {
        if (cards.Count <= 1) return Mathf.Max(1, cards.Count);

        float firstTop = cards[0].layout.y;
        int cols = 0;
        foreach (var c in cards)
        {
            if (Mathf.Abs(c.layout.y - firstTop) < 1f) cols++;
            else break;
        }
        return Mathf.Max(1, cols);
    }

    private void WireCloseButton()
    {
        var root = UiPanels.RootOf(document);
        if (root == null) return;

        closeButton = root.Q<Button>("close-button");
        if (closeButton != null) closeButton.clicked += OnCloseClicked;
    }

    private void OnCloseClicked() => UIManager.RequestPanelToggle(panel);

    private void WireTitle()
    {
        var root = UiPanels.RootOf(document);
        if (root == null) return;

        var title = root.Q<Label>(className: "grid-title");
        if (title != null) title.text = Loc.Tr("ui.grid.title");
    }

    private ScrollView ResolveContainer()
    {
        if (scroll != null && scroll.panel != null) return scroll;
        var root = UiPanels.RootOf(document);
        if (root == null) return null;
        scroll = root.Q<ScrollView>("grid-container");
        return scroll;
    }
}
}
