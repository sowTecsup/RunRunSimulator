using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

[DisallowMultipleComponent]
public class MorimonchiDetailInfoUITK : MonoBehaviour, IUINavigable
{
    [Header("UI Toolkit setup")]
    [SerializeField] private UIDocument document;
    [SerializeField] private UIPanelType panel = UIPanelType.MorimonchiDetail;

    [Header("Data")]
    [Tooltip("Resolves part names/sets/rarity and effective stats. Shared SO asset.")]
    [SerializeField] private CreatureDatabaseSO database;

    [Tooltip("Umbral de necesidades para poder bajar (marca de apta en la ficha).")]
    [SerializeField] private CareGateSO careGate;

    [Tooltip("Draw order; higher keeps this modal above the grid panel.")]
    [SerializeField] private int sortingOrder = 100;

    private Label titleLabel;
    private VisualElement portrait;
    private TabView tabs;
    private Button closeButton;
    private bool wired;

    private DetailInfoTabPresenter info;
    private DetailTreesPresenter trees;
    private DetailRelationsPresenter relations;

    private CreatureRegistrySO registry;

    private CreatureDNA current;

    private void Awake()
    {
        if (document != null) document.sortingOrder = sortingOrder;
    }

    private void OnEnable()
    {
        UIManager.OnCreatureSelected += Show;
        GameEvents.OnRegistryChanged += OnRegistryChanged;
    }

    private void OnDisable()
    {
        UIManager.OnCreatureSelected -= Show;
        GameEvents.OnRegistryChanged -= OnRegistryChanged;
    }

    private void Start()
    {
        Wire();
        UIManager.RegisterNavigable(panel, this);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= OnClose;
        UIManager.UnregisterNavigable(panel);
    }

    public void OnUINavigate(Vector2 dir)
    {
        if (tabs == null) return;
        int last = tabs.Query<Tab>().ToList().Count - 1;
        if (last < 0) return;

        if (dir.x > 0.5f)       tabs.selectedTabIndex = Mathf.Min(tabs.selectedTabIndex + 1, last);
        else if (dir.x < -0.5f) tabs.selectedTabIndex = Mathf.Max(tabs.selectedTabIndex - 1, 0);
    }

    public void OnUISubmit() { }

    public bool OnUICancel() => false;

    private void Wire()
    {
        if (wired) return;
        var root = UiPanels.RootOf(document);
        if (root == null) return;

        titleLabel = root.Q<Label>("title");
        portrait   = root.Q<VisualElement>("portrait");
        tabs       = root.Q<TabView>("tabs");

        closeButton = root.Q<Button>("close-button");
        if (closeButton != null) closeButton.clicked += OnClose;

        WireStaticLabels(root);

        info   = new DetailInfoTabPresenter(root, database, careGate);
        trees  = new DetailTreesPresenter(root, database, () => registry);
        relations = new DetailRelationsPresenter(root, () => registry);

        wired = true;
    }

    private static void WireStaticLabels(VisualElement root)
    {
        SetTabLabel(root, "tab-info", "ui.detail.tab.info");
        SetTabLabel(root, "tab-breed", "ui.detail.tab.breed");
        SetTabLabel(root, "tab-lineage", "ui.detail.tab.lineage");
        SetTabLabel(root, "tab-relations", "ui.detail.tab.relations");

        SetSectionTitles(root, "tab-info", "ui.detail.section.identity", "ui.detail.section.role_element",
            "ui.detail.section.parts", "ui.detail.section.progression");
        SetSectionTitles(root, "tab-relations", "ui.detail.section.friends", "ui.detail.section.foes");

        SetLabelText(root, "care-title", "ui.detail.section.care");
        SetLabelText(root, "need-health-label", "ui.detail.need.health");
        SetLabelText(root, "need-energy-label", "ui.detail.need.energy");
        SetLabelText(root, "need-affect-label", "ui.detail.need.affect");
        SetLabelText(root, "breed-empty", "ui.detail.placeholder.breed_empty");
        SetLabelText(root, "lineage-empty", "ui.detail.placeholder.lineage_empty");
        SetLabelText(root, "relations-empty", "ui.detail.placeholder.relations_empty");
    }

    private static void SetTabLabel(VisualElement root, string tabName, string key)
    {
        var tab = root.Q<Tab>(tabName);
        if (tab != null) tab.label = Loc.Tr(key);
    }

    private static void SetSectionTitles(VisualElement root, string tabName, params string[] keys)
    {
        var tab = root.Q<Tab>(tabName);
        if (tab == null) return;
        var labels = tab.Query<Label>(className: "section-title").ToList();
        for (int i = 0; i < keys.Length && i < labels.Count; i++)
            labels[i].text = Loc.Tr(keys[i]);
    }

    private static void SetLabelText(VisualElement root, string name, string key)
    {
        var label = root.Q<Label>(name);
        if (label != null) label.text = Loc.Tr(key);
    }

    private void Show(CreatureDNA dna, CreatureRegistrySO registry)
    {
        this.registry = registry;
        current = dna;
        Wire();
        Populate(dna);
        if (tabs != null) tabs.selectedTabIndex = 0;
        UIManager.RequestPanelSet(panel, true);
    }

    private void OnClose()
    {
        UIManager.RequestPanelSet(panel, false);
    }

    private void OnRegistryChanged(CreatureRegistrySO _)
    {
        if (current == null || !wired) return;
        var root = UiPanels.RootOf(document);
        if (root == null || root.resolvedStyle.display == DisplayStyle.None) return;
        Populate(current);
    }

    private void Populate(CreatureDNA dna)
    {
        if (dna == null) return;

        if (titleLabel != null)
            titleLabel.text = string.IsNullOrEmpty(dna.CustomName) ? dna.ToStringID() : dna.CustomName;

        if (portrait != null)
            MonchiPortraitUI.ApplyLive(portrait, dna);

        info.Rebuild(dna);
        trees.Rebuild(dna);
        relations.Rebuild(dna);
    }
}
}
