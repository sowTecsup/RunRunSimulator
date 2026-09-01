using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

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
        SetOpen(false);
    }

    private void OnBuildModeChanged(bool building)
    {
        if (!building) SetOpen(false);
    }

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

    private void Toggle() => SetOpen(!open);

    private void SetOpen(bool show)
    {
        if (root == null) Resolve();
        if (root == null) return;

        open = show;
        root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (show) RefreshPieces();
    }

    private void BuildTabs()
    {
        if (tabsContainer == null) return;
        tabsContainer.Clear();
        tabEls.Clear();

        for (int i = 0; i < Categories.Length; i++)
        {
            int idx = i;
            var tab = new Label(LocEnumMaps.FurnitureCategoryName(Categories[i]));
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

    private void HighlightActiveTab() => UiPanels.SetActiveIndex(tabEls, activeCategory, TabActiveClass);

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
        UiPanels.SetActiveIndex(pieceEls, selectedPiece, PieceSelClass);
        if (selectedPiece >= 0 && selectedPiece < pieceEls.Count && piecesContainer != null)
            piecesContainer.ScrollTo(pieceEls[selectedPiece]);
    }

    private void ConfirmSelection()
    {
        if (selectedPiece < 0 || selectedPiece >= pieces.Count) return;
        if (buildMode == null) { Debug.LogWarning("[BuildBrowserUITK] No BuildModeController assigned."); return; }
        buildMode.SelectPieceFromBrowser(pieces[selectedPiece]);
        SetOpen(false);
    }

    private void Resolve()
    {
        root = UiPanels.RootOf(document);
        if (root == null) return;
        tabsContainer   = root.Q<VisualElement>("tabs");
        piecesContainer = root.Q<ScrollView>("pieces");
        emptyLabel      = root.Q<Label>("empty");
        SetupPiecesGrid();
        WireStaticLabels();
    }

    private void WireStaticLabels()
    {
        var title = root.Q<Label>(className: "browser-title");
        if (title != null) title.text = Loc.Tr("ui.build.title");

        var hint = root.Q<Label>(className: "browser-hint");
        if (hint != null) hint.text = Loc.Tr("ui.build.hints");

        if (emptyLabel != null) emptyLabel.text = Loc.Tr("ui.build.empty");
    }

    private void SetupPiecesGrid()
    {
        if (piecesContainer == null) return;
        var c = piecesContainer.contentContainer;
        c.style.flexDirection = FlexDirection.Row;
        c.style.flexWrap      = Wrap.Wrap;
        c.style.alignContent  = Align.FlexStart;
    }
}
}
