using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Furniture browser shown in build mode: the player opens it (Tab), picks an owned
// piece, and the build mode starts placing it. It REPLACES the 1-4 hotbar as the way
// to choose what to build.
//
// Deliberately NOT a UIManager panel: routing it through UIManager would raise
// OnUIFocusChanged, and BuildModeController exits build mode on UI focus. So this is a
// standalone overlay that toggles its own root display and reads its own navigation
// (keyboard, console-first D-pad style) directly while open — no cursor needed, which
// matters because build mode keeps the Cinemachine mouselook active.
//
// Lives on an always-active object referencing its UIDocument (same split as the
// other UITK controllers). Calls BuildModeController.SelectPieceFromBrowser on pick.
[DisallowMultipleComponent]
public class BuildBrowserUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private FurnitureDatabaseSO database;
    [SerializeField] private BuildModeController buildMode;

    private const string TabClass        = "browser-tab";
    private const string TabActiveClass  = "browser-tab--active";
    private const string PieceClass      = "browser-piece";
    private const string PieceSelClass   = "browser-piece--selected";

    private static readonly FurnitureCategory[] Categories =
        (FurnitureCategory[])Enum.GetValues(typeof(FurnitureCategory));

    private VisualElement root;
    private VisualElement tabsContainer;
    private ScrollView piecesContainer;
    private Label emptyLabel;

    private readonly List<VisualElement> tabEls = new List<VisualElement>();
    private readonly List<VisualElement> pieceEls = new List<VisualElement>();
    private readonly List<FurnitureDefinitionSO> pieces = new List<FurnitureDefinitionSO>();

    private int activeCategory;
    private int selectedPiece = -1;
    private bool open;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void OnEnable()
    {
        BuildingInputs.BrowseToggled           += Toggle;
        BuildModeController.OnBuildModeChanged  += OnBuildModeChanged;
    }

    private void OnDisable()
    {
        BuildingInputs.BrowseToggled           -= Toggle;
        BuildModeController.OnBuildModeChanged  -= OnBuildModeChanged;
    }

    private void Start()
    {
        Resolve();
        BuildTabs();
        SetOpen(false);   // starts hidden (not a UIManager panel, so it hides itself)
    }

    // Leaving build mode closes the browser.
    private void OnBuildModeChanged(bool building)
    {
        if (!building) SetOpen(false);
    }

    // ── Navigation (read directly while open) ─────────────────────

    private void Update()
    {
        if (!open) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if      (kb.downArrowKey.wasPressedThisFrame)  StepCategory(1);
        else if (kb.upArrowKey.wasPressedThisFrame)    StepCategory(-1);
        if      (kb.rightArrowKey.wasPressedThisFrame) StepPiece(1);
        else if (kb.leftArrowKey.wasPressedThisFrame)  StepPiece(-1);
        if      (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) ConfirmSelection();
    }

    // ── Open / close ──────────────────────────────────────────────

    private void Toggle() => SetOpen(!open);

    private void SetOpen(bool show)
    {
        if (root == null) Resolve();
        if (root == null) return;

        open = show;
        root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (show) RefreshPieces();
    }

    // ── Tabs / pieces ─────────────────────────────────────────────

    private void BuildTabs()
    {
        if (tabsContainer == null) return;
        tabsContainer.Clear();
        tabEls.Clear();

        for (int i = 0; i < Categories.Length; i++)
        {
            int idx = i;
            var tab = new Label(Categories[i].ToString());
            tab.AddToClassList(TabClass);
            tab.RegisterCallback<ClickEvent>(_ => SetCategory(idx));
            tabsContainer.Add(tab);
            tabEls.Add(tab);
        }
        HighlightActiveTab();
    }

    private void StepCategory(int dir) => SetCategory(((activeCategory + dir) % Categories.Length + Categories.Length) % Categories.Length);

    private void SetCategory(int idx)
    {
        activeCategory = Mathf.Clamp(idx, 0, Categories.Length - 1);
        HighlightActiveTab();
        RefreshPieces();
    }

    private void HighlightActiveTab()
    {
        for (int i = 0; i < tabEls.Count; i++)
            tabEls[i].EnableInClassList(TabActiveClass, i == activeCategory);
    }

    // Pieces in the active category. For testing this lists the FULL furniture catalog;
    // once economy/ownership exists it will filter by PlayerInventorySO.furnitureOwned.
    private void RefreshPieces()
    {
        if (piecesContainer == null) return;
        piecesContainer.Clear();
        pieceEls.Clear();
        pieces.Clear();

        var cat = Categories[activeCategory];
        if (database != null)
        {
            foreach (var def in database.All)
            {
                if (def == null || def.Category != cat) continue;
                pieces.Add(def);
            }
        }

        for (int i = 0; i < pieces.Count; i++)
        {
            int idx = i;
            var def = pieces[i];
            var card = new VisualElement();
            card.AddToClassList(PieceClass);

            var name = new Label(string.IsNullOrEmpty(def.DisplayName) ? def.Id : def.DisplayName);
            name.AddToClassList("browser-piece__name");
            card.Add(name);

            card.RegisterCallback<ClickEvent>(_ => { selectedPiece = idx; HighlightSelected(); ConfirmSelection(); });
            piecesContainer.Add(card);
            pieceEls.Add(card);
        }

        if (emptyLabel != null)
            emptyLabel.style.display = pieces.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

        selectedPiece = pieces.Count == 0 ? -1 : 0;
        HighlightSelected();
    }

    private void StepPiece(int dir)
    {
        if (pieces.Count == 0) return;
        selectedPiece = ((selectedPiece + dir) % pieces.Count + pieces.Count) % pieces.Count;
        HighlightSelected();
    }

    private void HighlightSelected()
    {
        for (int i = 0; i < pieceEls.Count; i++)
            pieceEls[i].EnableInClassList(PieceSelClass, i == selectedPiece);
        if (selectedPiece >= 0 && selectedPiece < pieceEls.Count && piecesContainer != null)
            piecesContainer.ScrollTo(pieceEls[selectedPiece]);
    }

    // Enter / click → hand the piece to build mode and close (back to placing).
    private void ConfirmSelection()
    {
        if (selectedPiece < 0 || selectedPiece >= pieces.Count) return;
        if (buildMode == null) { Debug.LogWarning("[BuildBrowserUITK] No BuildModeController assigned."); return; }
        buildMode.SelectPieceFromBrowser(pieces[selectedPiece]);
        SetOpen(false);
    }

    // ── Plumbing ──────────────────────────────────────────────────

    private void Resolve()
    {
        root = document != null ? document.rootVisualElement : null;
        if (root == null) return;
        tabsContainer   = root.Q<VisualElement>("tabs");
        piecesContainer = root.Q<ScrollView>("pieces");
        emptyLabel      = root.Q<Label>("empty");
    }
}
