using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// Detailed MoriMochi summary window (UI Toolkit), FireRed-summary inspired.
// Lives on the always-active UIManager object and fills its own UIDocument
// (kept active, hidden via display). Opened when a grid card is clicked.
//
// Modal: its full-screen backdrop sits above the grid (higher sortingOrder) and
// captures clicks, so the panel behind can't be touched until the X closes it.
//
// Event-driven: it never references the grid. It listens to UIManager's static
// OnCreatureSelected — the event carries the creature AND the registry (for
// parent names). This is a thin core: each tab's content is built by its own
// presenter (Info/Combate/Linaje+Descendencia/Equipo), all sharing the same root.
[DisallowMultipleComponent]
public class MorimonchiDetailInfoUITK : MonoBehaviour, IUINavigable
{
    [Header("UI Toolkit setup")]
    [SerializeField] private UIDocument document;
    [SerializeField] private UIPanelType panel = UIPanelType.MorimonchiDetail;

    [Header("Data")]
    [Tooltip("Resolves part names/sets/rarity and effective stats. Shared SO asset.")]
    [SerializeField] private CreatureDatabaseSO database;

    [Tooltip("Resolves equipped item IDs to their EquipmentSO (icon, rarity, effects) for the Equipo tab.")]
    [SerializeField] private EquipmentDatabaseSO equipmentDatabase;

    [Tooltip("Colores por rareza (pastel, nombre del ítem) y por slot (acento de la card).")]
    [SerializeField] private EquipmentPaletteSO equipmentPalette;

    [Tooltip("Popup mochila para equipar desde la tab Equipo.")]
    [SerializeField] private EquipmentBackpackUITK backpack;

    [Tooltip("Draw order; higher keeps this modal above the grid panel.")]
    [SerializeField] private int sortingOrder = 100;

    // Queried once the document tree is built.
    private Label titleLabel;
    private VisualElement portrait;
    private TabView tabs;
    private Button closeButton;
    private bool wired;

    // One presenter per tab (Linaje + Descendencia share DetailTreesPresenter).
    private DetailInfoTabPresenter info;
    private DetailCombatTabPresenter combat;
    private DetailTreesPresenter trees;
    private DetailEquipTabPresenter equip;
    private DetailRelationsPresenter relations;

    // Kept from the latest Show() so the tab presenters can resolve opponents
    // and ancestors by ID (the event carries it — the panel never touches the grid).
    private CreatureRegistrySO registry;

    // The creature currently on display, so a registry change (e.g. equipping
    // from the mochila) can re-run Populate and refresh the Equipo tab in place.
    private CreatureDNA current;

    // ── Lifecycle ─────────────────────────────────────────────────

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

    // Register as the focused-input handler in Start: UIManager subscribes to the
    // registration events in OnEnable, which always runs before any Start.
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

    // ── IUINavigable (routed only while this panel is the focused top) ──

    // A/D (or stick/dpad) steps through the tabs.
    public void OnUINavigate(Vector2 dir)
    {
        if (tabs == null) return;
        int last = tabs.Query<Tab>().ToList().Count - 1;
        if (last < 0) return;

        if (dir.x > 0.5f)       tabs.selectedTabIndex = Mathf.Min(tabs.selectedTabIndex + 1, last);
        else if (dir.x < -0.5f) tabs.selectedTabIndex = Mathf.Max(tabs.selectedTabIndex - 1, 0);
    }

    // Nothing to confirm on the summary page yet.
    public void OnUISubmit() { }

    // No internal back state — let the UIManager close the panel on ESC.
    public bool OnUICancel() => false;

    // ── Private Methods ───────────────────────────────────────────

    private void Wire()
    {
        if (wired) return;
        var root = document != null ? document.rootVisualElement : null;
        if (root == null) return;

        titleLabel = root.Q<Label>("title");
        portrait   = root.Q<VisualElement>("portrait");
        tabs       = root.Q<TabView>("tabs");

        closeButton = root.Q<Button>("close-button");
        if (closeButton != null) closeButton.clicked += OnClose;

        WireStaticLabels(root);

        info   = new DetailInfoTabPresenter(root, database, equipmentDatabase);
        combat = new DetailCombatTabPresenter(root, () => registry);
        trees  = new DetailTreesPresenter(root, database, () => registry);
        equip  = new DetailEquipTabPresenter(root, database, equipmentDatabase, equipmentPalette, backpack, () => registry);
        relations = new DetailRelationsPresenter(root, () => registry);

        wired = true;
    }

    private static void WireStaticLabels(VisualElement root)
    {
        SetTabLabel(root, "tab-info", "ui.detail.tab.info");
        SetTabLabel(root, "tab-combat", "ui.detail.tab.combat");
        SetTabLabel(root, "tab-breed", "ui.detail.tab.breed");
        SetTabLabel(root, "tab-lineage", "ui.detail.tab.lineage");
        SetTabLabel(root, "tab-team", "ui.detail.tab.equipment");
        SetTabLabel(root, "tab-relations", "ui.detail.tab.relations");

        SetSectionTitles(root, "tab-info", "ui.detail.section.identity", "ui.detail.section.role_element",
            "ui.detail.section.parts", "ui.detail.section.progression");
        SetSectionTitles(root, "tab-team", "ui.detail.section.stats");
        SetSectionTitles(root, "tab-relations", "ui.detail.section.friends", "ui.detail.section.foes");

        SetLabelText(root, "combat-empty", "ui.detail.placeholder.combat_empty");
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

    // Populate then show. Repopulates if already open (clicking another card).
    // The registry rides along in the event so the Linaje tab can resolve ancestors
    // by ID (kept in a field for the tab presenters above).
    private void Show(CreatureDNA dna, CreatureRegistrySO registry)
    {
        this.registry = registry;
        current = dna;
        Wire();
        Populate(dna);
        if (tabs != null) tabs.selectedTabIndex = 0; // always open on the Info tab
        UIManager.RequestPanelSet(panel, true);
    }

    private void OnClose()
    {
        UIManager.RequestPanelSet(panel, false);
        backpack?.Close();
    }

    // Re-populates in place after any registry mutation (e.g. equipping an item
    // from the mochila), so the Equipo tab's cards and Base→Final stats stay
    // current. Populate never touches the selected tab, so the user stays put.
    // Only repopulates while the panel is visible: Show() always calls Populate
    // on open, so skipping mutations while hidden loses no freshness and avoids
    // waking a hidden panel's live camera on every background registry change.
    private void OnRegistryChanged(CreatureRegistrySO _)
    {
        if (current == null || !wired) return;
        var root = document != null ? document.rootVisualElement : null;
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
        combat.Rebuild(dna);
        trees.Rebuild(dna);
        equip.Rebuild(dna);
        relations.Rebuild(dna);
    }
}
}
