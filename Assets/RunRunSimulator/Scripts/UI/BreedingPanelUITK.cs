using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// In-game breeding screen (UI Toolkit), modal, with two tabs:
//   • "Criar"     — pick a Father + Mother, preview both (stats + parts), see the
//                   estimated time, and start the breed (server-timed).
//   • "Incubando" — the eggs currently breeding as cards (Mother 💗 Father → time
//                   left); when an egg's timer hits 0 a Hatch button appears.
//
// Lives on the always-active UIManager object (like the grid/detail controllers)
// and fills its own UIDocument. It's an action controller (like BreedingController):
// it reaches the registry via GameManager and starts/hatches through AsyncBreedingService.
//
// Keyboard/gamepad: implements IUINavigable with a small hierarchical focus model
// (TabBar ⇄ content ⇄ side-list). ESC steps one level up (OnUICancel consumes it)
// and only closes the panel when already at the TabBar.
[DisallowMultipleComponent]
public partial class BreedingPanelUITK : MonoBehaviour, IUINavigable
{
    [Header("UI Toolkit setup")]
    [SerializeField] private UIDocument document;
    [SerializeField] private UIPanelType panel = UIPanelType.Breeding;
    [SerializeField] private int sortingOrder = 100;

    [Header("Data / services")]
    [Tooltip("Resolves part names/sets and effective stats. Shared SO asset.")]
    [SerializeField] private CreatureDatabaseSO database;
    [Tooltip("Starts the server-timed breed and hatches ready eggs.")]
    [SerializeField] private AsyncBreedingService asyncBreedingService;

    // Hierarchical focus: which region currently receives navigation.
    private enum Region { TabBar, Criar, FatherList, MotherList, Incubando }
    private Region region = Region.TabBar;

    // Criar content cursor: 0 = Father slot, 1 = Mother slot, 2 = Breed button.
    private int criarIndex;

    private const string Focus = "breed-focus";

    // ── UI refs (queried once the tree is built) ──
    private TabView tabs;
    private VisualElement fatherSlot, motherSlot, preview, fatherSlotImg, motherSlotImg;
    private Label fatherSlotName, motherSlotName, timeLabel;
    private Button breedButton, closeButton;
    private ScrollView fatherList, motherList, eggListView;

    // ── State ──
    private CreatureRegistrySO registry;
    private string selectedFatherId = "", selectedMotherId = "";
    private readonly List<VisualElement> fatherCards = new List<VisualElement>();
    private readonly List<VisualElement> motherCards = new List<VisualElement>();
    private int fatherIndex, motherIndex, eggIndex;
    private readonly List<EggView> eggs = new List<EggView>();
    private float countdownTick;
    private bool wired;
    private bool breedBusy;   // a StartBreedingAsync is in flight → inputs frozen

    private sealed class EggView
    {
        public string MotherId;
        public long   ReadyAt;
        public VisualElement Row;
        public Label  Time;
        public Button Hatch;
    }

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        if (document != null) document.sortingOrder = sortingOrder;
        if (GameManager.Instance != null) registry = GameManager.Instance.Registry;
    }

    private void OnEnable()
    {
        GameEvents.OnRegistryChanged  += OnRegistry;
        GameEvents.OnRegistryReloaded += OnRegistry;
        UIManager.OnPanelToggleRequested += OnPanelToggle;
        UIManager.OnPanelSetRequested    += OnPanelSet;
    }

    private void OnDisable()
    {
        GameEvents.OnRegistryChanged  -= OnRegistry;
        GameEvents.OnRegistryReloaded -= OnRegistry;
        UIManager.OnPanelToggleRequested -= OnPanelToggle;
        UIManager.OnPanelSetRequested    -= OnPanelSet;
    }

    private void Start()
    {
        Wire();
        UIManager.RegisterNavigable(panel, this);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= OnClose;
        if (breedButton != null) breedButton.clicked -= TryBreed;
        UIManager.UnregisterNavigable(panel);
    }

    // Ticks the egg countdowns once a second (the controller is always active).
    private void Update()
    {
        if (!wired || eggs.Count == 0) return;
        countdownTick += Time.deltaTime;
        if (countdownTick < 1f) return;
        countdownTick = 0f;
        RefreshEggTimers();
    }

    // ── Wiring ────────────────────────────────────────────────────

    private void Wire()
    {
        if (wired) return;
        var root = document != null ? document.rootVisualElement : null;
        if (root == null) return;

        tabs           = root.Q<TabView>("tabs");
        fatherSlot     = root.Q<VisualElement>("father-slot");
        motherSlot     = root.Q<VisualElement>("mother-slot");
        fatherSlotImg  = root.Q<VisualElement>("father-slot-img");
        motherSlotImg  = root.Q<VisualElement>("mother-slot-img");
        preview        = root.Q<VisualElement>("preview");
        fatherSlotName = root.Q<Label>("father-slot-name");
        motherSlotName = root.Q<Label>("mother-slot-name");
        timeLabel      = root.Q<Label>("time-label");
        breedButton    = root.Q<Button>("breed-button");
        closeButton    = root.Q<Button>("close-button");
        fatherList     = root.Q<ScrollView>("father-list");
        motherList     = root.Q<ScrollView>("mother-list");
        eggListView    = root.Q<ScrollView>("egg-list");

        if (closeButton != null) closeButton.clicked += OnClose;
        if (breedButton != null) breedButton.clicked += TryBreed;
        fatherSlot?.RegisterCallback<ClickEvent>(_ => OpenList(Region.FatherList));
        motherSlot?.RegisterCallback<ClickEvent>(_ => OpenList(Region.MotherList));

        wired = true;
        RebuildAll();
        ResetFocus();
    }

    private void OnClose() => UIManager.RequestPanelToggle(panel);

    // ── Data / events ─────────────────────────────────────────────

    private void OnRegistry(CreatureRegistrySO reg)
    {
        registry = reg;
        if (!wired) { Wire(); return; }   // Wire() already rebuilds
        RebuildAll();
    }

    private void OnPanelToggle(UIPanelType p) { if (p == panel) ResetFocus(); }
    private void OnPanelSet(UIPanelType p, bool show) { if (p == panel && show) ResetFocus(); }

    private void RebuildAll()
    {
        RebuildCandidates();
        RebuildEggs();
        RefreshSlots();
    }
}
