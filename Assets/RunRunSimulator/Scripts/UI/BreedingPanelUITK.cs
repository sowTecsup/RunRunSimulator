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
public class BreedingPanelUITK : MonoBehaviour, IUINavigable
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

    // Populate the two side lists from the registry (kept fresh even while hidden).
    private void RebuildCandidates()
    {
        if (fatherList == null || motherList == null) return;
        fatherList.Clear(); motherList.Clear();
        fatherCards.Clear(); motherCards.Clear();
        if (registry == null) return;

        var all = registry.GetAll().Values;
        foreach (var dna in Eligible(all, CreatureGender.Male))
            fatherList.Add(MakeCandidate(dna, fatherCards, isFather: true));
        foreach (var dna in Eligible(all, CreatureGender.Female))
            motherList.Add(MakeCandidate(dna, motherCards, isFather: false));
    }

    private static IEnumerable<CreatureDNA> Eligible(IEnumerable<CreatureDNA> all, CreatureGender gender) =>
        all.Where(d => !d.IsDead && !d.IsBusy && d.Gender == gender && d.BreedCount < BreedingService.MaxBreedCount)
           .OrderBy(d => d.CustomName);

    private VisualElement MakeCandidate(CreatureDNA dna, List<VisualElement> bucket, bool isFather)
    {
        var row = new VisualElement();
        row.AddToClassList("breed-candidate");
        row.userData = dna.UniqueID;

        var eff = database != null
            ? CombatService.GetEffectiveStats(dna, database)
            : new CombatService.EffectiveStats(dna.BaseHP, dna.BaseAttack, dna.BaseSpeed);

        var l = new Label($"{dna.CustomName}  ·  HP {eff.HP:0} ATK {eff.Attack:0} SPD {eff.Speed:0}  ·  {dna.BreedCount}/{BreedingService.MaxBreedCount}");
        l.AddToClassList("breed-candidate-text");
        row.Add(l);

        string id = dna.UniqueID;
        row.RegisterCallback<ClickEvent>(_ => { if (isFather) SelectFather(id); else SelectMother(id); });
        bucket.Add(row);
        return row;
    }

    // Build the "Incubando" cards: one per breeding female + her partner.
    private void RebuildEggs()
    {
        if (eggListView == null) return;
        eggListView.Clear();
        eggs.Clear();
        if (registry == null) return;

        var mothers = registry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.Breeding && d.Gender == CreatureGender.Female && d.BreedReadyAt > 0)
            .OrderBy(d => d.BreedReadyAt);

        foreach (var mother in mothers)
        {
            string fatherName = registry.TryGet(mother.BreedPartnerID, out var father) ? father.CustomName : "???";

            var row = new VisualElement();
            row.AddToClassList("egg-row");
            row.userData = mother.UniqueID;

            var pair = new Label($"{mother.CustomName}  💗  {fatherName}");
            pair.AddToClassList("egg-pair");

            var time = new Label();
            time.AddToClassList("egg-time");

            var hatch = new Button { text = "Hatch" };
            hatch.AddToClassList("egg-hatch");
            hatch.style.display = DisplayStyle.None;
            string motherId = mother.UniqueID;
            hatch.clicked += () => DoHatch(motherId, hatch);

            row.Add(pair); row.Add(time); row.Add(hatch);
            eggListView.Add(row);
            eggs.Add(new EggView { MotherId = motherId, ReadyAt = mother.BreedReadyAt, Row = row, Time = time, Hatch = hatch });
        }

        eggIndex = Mathf.Clamp(eggIndex, 0, Mathf.Max(0, eggs.Count - 1));
        RefreshEggTimers();
    }

    private void RefreshEggTimers()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var e in eggs)
        {
            long rem = e.ReadyAt - now;
            if (rem <= 0)
            {
                e.Time.text = "¡Listo!";
                e.Hatch.style.display = DisplayStyle.Flex;
            }
            else
            {
                var t = TimeSpan.FromMilliseconds(rem);
                e.Time.text = rem >= 3600000 ? t.ToString(@"hh\:mm\:ss") : t.ToString(@"mm\:ss");
                e.Hatch.style.display = DisplayStyle.None;
            }
        }
    }

    // ── Slots / preview ───────────────────────────────────────────

    private void RefreshSlots()
    {
        SetSlot(fatherSlotName, fatherSlotImg, selectedFatherId);
        SetSlot(motherSlotName, motherSlotImg, selectedMotherId);
        BuildPreview();
    }

    // Empty placeholder (gray) until a parent is chosen, then name + BaseColor tint.
    private void SetSlot(Label nameLabel, VisualElement img, string id)
    {
        CreatureDNA dna = null;
        if (!string.IsNullOrEmpty(id) && registry != null) registry.TryGet(id, out dna);

        if (nameLabel != null) nameLabel.text = dna != null ? dna.CustomName : "Vacío";
        if (img != null) img.style.backgroundColor = dna != null ? dna.BaseColor : new Color(0.24f, 0.24f, 0.28f);
    }

    private void BuildPreview()
    {
        if (preview == null) return;
        preview.Clear();

        // TryGet must sit in the early-return condition (not a separate bool) so the
        // compiler tracks father/mother as definitely assigned in the fall-through.
        if (registry == null
            || !registry.TryGet(selectedFatherId, out var father)
            || !registry.TryGet(selectedMotherId, out var mother))
        {
            if (timeLabel != null) timeLabel.text = "";
            return;
        }

        preview.Add(ParentSummary(father));
        preview.Add(ParentSummary(mother));

        int mins = InheritanceOddsTableSO.Current != null ? InheritanceOddsTableSO.Current.BreedDurationMinutes : 30;
        if (timeLabel != null) timeLabel.text = $"≈ {mins} min";
    }

    private VisualElement ParentSummary(CreatureDNA dna)
    {
        var col = new VisualElement();
        col.AddToClassList("preview-parent");

        var name = new Label(dna.CustomName);
        name.AddToClassList("preview-name");
        col.Add(name);

        var eff = database != null
            ? CombatService.GetEffectiveStats(dna, database)
            : new CombatService.EffectiveStats(dna.BaseHP, dna.BaseAttack, dna.BaseSpeed);
        var stats = new Label($"HP {eff.HP:0}   ATK {eff.Attack:0}   SPD {eff.Speed:0}");
        stats.AddToClassList("preview-stats");
        col.Add(stats);

        if (database != null)
        {
            AddPartRow(col, database.GetBodyShape(dna.BodyShapeID));
            AddPartRow(col, database.GetArm(dna.ArmID));
            AddPartRow(col, database.GetEye(dna.EyeID));
            AddPartRow(col, database.GetMouth(dna.MouthID));
        }
        return col;
    }

    private static void AddPartRow(VisualElement parent, BodyPart part)
    {
        var row = new VisualElement();
        row.AddToClassList("preview-part-row");

        var swatch = new VisualElement();
        swatch.AddToClassList("preview-swatch");
        swatch.style.backgroundColor = part != null ? BodyPart.SetColor(part.Set) : Color.gray;
        row.Add(swatch);

        var text = new Label(part != null ? $"{part.Name} · {part.Set}" : "—");
        text.AddToClassList("preview-part-text");
        row.Add(text);

        parent.Add(row);
    }

    // ── Selection ─────────────────────────────────────────────────

    private void SelectFather(string id) { if (breedBusy) return; selectedFatherId = id; AfterSelect(0); }
    private void SelectMother(string id) { if (breedBusy) return; selectedMotherId = id; AfterSelect(1); }

    private void AfterSelect(int slot)
    {
        ClearListFocus();
        region = Region.Criar;
        criarIndex = slot;
        RefreshSlots();
        ApplyCriarFocus();
    }

    private async void TryBreed()
    {
        if (breedBusy) return;
        if (string.IsNullOrEmpty(selectedFatherId) || string.IsNullOrEmpty(selectedMotherId))
        {
            Debug.LogWarning("[BreedingPanel] Select a Father and a Mother first.");
            return;
        }
        if (asyncBreedingService == null)
        {
            Debug.LogError("[BreedingPanel] AsyncBreedingService not assigned.");
            return;
        }

        string motherId = selectedMotherId, fatherId = selectedFatherId;

        // Freeze inputs + gray the button until the async update lands.
        SetBreedBusy(true);
        await asyncBreedingService.StartBreedingAsync(motherId, fatherId);
        SetBreedBusy(false);

        // Only clear + jump to Incubando if it actually started (parents now Breeding).
        if (registry != null && registry.TryGet(motherId, out var mother) && mother.BusyState == BusyReason.Breeding)
        {
            selectedFatherId = selectedMotherId = "";
            RefreshSlots();
            if (tabs != null) tabs.selectedTabIndex = 1;
            region = Region.TabBar;
            ClearAllFocus();
            SetTabBarFocus(true);
        }
    }

    private void SetBreedBusy(bool busy)
    {
        breedBusy = busy;
        if (breedButton == null) return;
        breedButton.SetEnabled(!busy);
        breedButton.text = busy ? "Breeding..." : "Breed";
        breedButton.EnableInClassList("breed-action--busy", busy);
    }

    // Gray the button to "Hatching..." while the server is consulted. On success
    // RebuildEggs replaces the row (the button is orphaned → we skip restoring it);
    // on not_ready the row stays, so we restore the button for a retry.
    private async void DoHatch(string motherId, Button btn)
    {
        if (asyncBreedingService == null) { Debug.LogError("[BreedingPanel] AsyncBreedingService not assigned."); return; }
        if (registry == null || !registry.TryGet(motherId, out var mother)) return;

        if (btn != null)
        {
            btn.SetEnabled(false);
            btn.text = "Hatching...";
            btn.AddToClassList("egg-hatch--busy");
        }

        await asyncBreedingService.HatchAsync(motherId, mother.BreedPartnerID);

        if (btn != null && btn.panel != null)   // still attached → the egg didn't hatch (not_ready)
        {
            btn.SetEnabled(true);
            btn.text = "Hatch";
            btn.RemoveFromClassList("egg-hatch--busy");
        }
    }

    // ── IUINavigable ──────────────────────────────────────────────

    public void OnUINavigate(Vector2 dir)
    {
        if (breedBusy) return;   // inputs frozen while a breed is in flight
        int h = dir.x >  0.5f ? 1 : dir.x < -0.5f ? -1 : 0;
        int v = dir.y < -0.5f ? 1 : dir.y >  0.5f ? -1 : 0;   // down = +1
        if (h == 0 && v == 0) return;

        switch (region)
        {
            case Region.TabBar:
                if (h != 0 && tabs != null)
                {
                    tabs.selectedTabIndex = Mathf.Clamp(tabs.selectedTabIndex + h, 0, 1);
                }
                else if (v > 0) EnterContent();
                break;

            case Region.Criar:      MoveCriar(h + v); break;
            case Region.FatherList: MoveList(fatherCards, ref fatherIndex, h + v, fatherList); break;
            case Region.MotherList: MoveList(motherCards, ref motherIndex, h + v, motherList); break;
            case Region.Incubando:  MoveEggs(h + v); break;
        }
    }

    public void OnUISubmit()
    {
        if (breedBusy) return;
        switch (region)
        {
            case Region.TabBar: EnterContent(); break;
            case Region.Criar:
                if      (criarIndex == 0) OpenList(Region.FatherList);
                else if (criarIndex == 1) OpenList(Region.MotherList);
                else                      TryBreed();
                break;
            case Region.FatherList:
                if (InRange(fatherCards, fatherIndex)) SelectFather((string)fatherCards[fatherIndex].userData);
                break;
            case Region.MotherList:
                if (InRange(motherCards, motherIndex)) SelectMother((string)motherCards[motherIndex].userData);
                break;
            case Region.Incubando: HatchFocusedEgg(); break;
        }
    }

    public bool OnUICancel()
    {
        if (breedBusy) return true;   // consume ESC (don't close) while breeding
        switch (region)
        {
            case Region.FatherList:
            case Region.MotherList:
                ClearListFocus();
                region = Region.Criar;
                ApplyCriarFocus();
                return true;
            case Region.Criar:
            case Region.Incubando:
                ClearAllFocus();
                region = Region.TabBar;
                SetTabBarFocus(true);
                return true;
            default:
                return false;   // already at the TabBar → let the UIManager close us
        }
    }

    // ── Navigation helpers ────────────────────────────────────────

    private void EnterContent()
    {
        SetTabBarFocus(false);
        if (tabs != null && tabs.selectedTabIndex == 1)
        {
            region = Region.Incubando;
            eggIndex = 0;
            HighlightEggs();
            if (eggs.Count > 0) eggListView.ScrollTo(eggs[0].Row);
        }
        else
        {
            region = Region.Criar;
            criarIndex = 0;
            ApplyCriarFocus();
        }
    }

    private void MoveCriar(int delta)
    {
        int next = criarIndex + delta;
        if (next < 0) { ClearCriarFocus(); region = Region.TabBar; SetTabBarFocus(true); return; }
        criarIndex = Mathf.Clamp(next, 0, 2);
        ApplyCriarFocus();
    }

    private void MoveList(List<VisualElement> cards, ref int idx, int delta, ScrollView scroll)
    {
        if (cards.Count == 0) return;
        idx = Mathf.Clamp(idx + delta, 0, cards.Count - 1);
        for (int i = 0; i < cards.Count; i++) cards[i].EnableInClassList(Focus, i == idx);
        scroll?.ScrollTo(cards[idx]);
    }

    private void MoveEggs(int delta)
    {
        int next = eggIndex + delta;
        if (next < 0) { ClearEggFocus(); region = Region.TabBar; SetTabBarFocus(true); return; }
        if (eggs.Count == 0) return;
        eggIndex = Mathf.Clamp(next, 0, eggs.Count - 1);
        HighlightEggs();
        eggListView?.ScrollTo(eggs[eggIndex].Row);
    }

    // Lists are always visible; "opening" one just moves the focus into it.
    private void OpenList(Region which)
    {
        if (breedBusy) return;
        ClearListFocus();
        ClearCriarFocus();
        region = which;
        if (which == Region.FatherList)
        {
            fatherIndex = 0;
            for (int i = 0; i < fatherCards.Count; i++) fatherCards[i].EnableInClassList(Focus, i == 0);
            if (fatherCards.Count > 0) fatherList?.ScrollTo(fatherCards[0]);
        }
        else
        {
            motherIndex = 0;
            for (int i = 0; i < motherCards.Count; i++) motherCards[i].EnableInClassList(Focus, i == 0);
            if (motherCards.Count > 0) motherList?.ScrollTo(motherCards[0]);
        }
    }

    private void HatchFocusedEgg()
    {
        if (!InRange2(eggs, eggIndex)) return;
        var e = eggs[eggIndex];
        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= e.ReadyAt) DoHatch(e.MotherId, e.Hatch);
    }

    // ── Focus visuals ─────────────────────────────────────────────

    private void ResetFocus()
    {
        if (!wired) return;
        if (tabs != null) tabs.selectedTabIndex = 0;
        ClearAllFocus();
        region = Region.TabBar;
        SetTabBarFocus(true);
    }

    private void ApplyCriarFocus()
    {
        fatherSlot?.EnableInClassList(Focus, criarIndex == 0);
        motherSlot?.EnableInClassList(Focus, criarIndex == 1);
        breedButton?.EnableInClassList(Focus, criarIndex == 2);
    }

    private void ClearCriarFocus()
    {
        fatherSlot?.RemoveFromClassList(Focus);
        motherSlot?.RemoveFromClassList(Focus);
        breedButton?.RemoveFromClassList(Focus);
    }

    private void HighlightEggs()
    {
        for (int i = 0; i < eggs.Count; i++) eggs[i].Row.EnableInClassList(Focus, i == eggIndex);
    }

    private void ClearEggFocus()
    {
        foreach (var e in eggs) e.Row.RemoveFromClassList(Focus);
    }

    private void SetTabBarFocus(bool on)
    {
        if (tabs == null) return;
        tabs.EnableInClassList("tabbar-focused", on);
    }

    private void ClearAllFocus()
    {
        ClearCriarFocus();
        ClearEggFocus();
        ClearListFocus();
        SetTabBarFocus(false);
    }

    // Lists stay visible — this only drops the focus ring from the candidates.
    private void ClearListFocus()
    {
        foreach (var c in fatherCards) c.RemoveFromClassList(Focus);
        foreach (var c in motherCards) c.RemoveFromClassList(Focus);
    }

    private static bool InRange(List<VisualElement> list, int i) => i >= 0 && i < list.Count;
    private static bool InRange2(List<EggView> list, int i) => i >= 0 && i < list.Count;
}
