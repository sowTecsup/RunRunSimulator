using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class BreedingBreedTabPresenter : ITabPresenter
{
    private const string Focus = "breed-focus";

    private enum SubFocus { Slots, FatherList, MotherList }

    private readonly Func<CreatureRegistrySO> getRegistry;
    private readonly CreatureDatabaseSO database;
    private readonly IncubationService incubation;
    private readonly Action onBred;

    private readonly VisualElement fatherSlot, motherSlot, preview, fatherSlotImg, motherSlotImg;
    private readonly Label fatherSlotName, motherSlotName, timeLabel;
    private readonly Button breedButton;
    private readonly ScrollView fatherList, motherList;

    private string selectedFatherId = "", selectedMotherId = "";
    private readonly List<VisualElement> fatherCards = new List<VisualElement>();
    private readonly List<VisualElement> motherCards = new List<VisualElement>();
    private int criarIndex, fatherIndex, motherIndex;
    private SubFocus focus = SubFocus.Slots;

    public BreedingBreedTabPresenter(VisualElement root, Func<CreatureRegistrySO> getRegistry,
        CreatureDatabaseSO database, IncubationService incubation, Action onBred)
    {
        this.getRegistry = getRegistry;
        this.database = database;
        this.incubation = incubation;
        this.onBred = onBred;

        fatherSlot     = root.Q<VisualElement>("father-slot");
        motherSlot     = root.Q<VisualElement>("mother-slot");
        fatherSlotImg  = root.Q<VisualElement>("father-slot-img");
        motherSlotImg  = root.Q<VisualElement>("mother-slot-img");
        preview        = root.Q<VisualElement>("preview");
        fatherSlotName = root.Q<Label>("father-slot-name");
        motherSlotName = root.Q<Label>("mother-slot-name");
        timeLabel      = root.Q<Label>("time-label");
        breedButton    = root.Q<Button>("breed-button");
        fatherList     = root.Q<ScrollView>("father-list");
        motherList     = root.Q<ScrollView>("mother-list");

        if (breedButton != null) breedButton.clicked += TryBreed;
        fatherSlot?.RegisterCallback<ClickEvent>(_ => OpenList(SubFocus.FatherList));
        motherSlot?.RegisterCallback<ClickEvent>(_ => OpenList(SubFocus.MotherList));
    }

    public void Enter()
    {
        focus = SubFocus.Slots;
        criarIndex = 0;
        ApplyCriarFocus();
    }

    public bool Navigate(int h, int v)
    {
        int delta = h + v;

        switch (focus)
        {
            case SubFocus.Slots:
                int next = criarIndex + delta;
                if (next < 0) { ClearCriarFocus(); return false; }
                criarIndex = Mathf.Clamp(next, 0, 2);
                ApplyCriarFocus();
                return true;

            case SubFocus.FatherList:
                MoveList(fatherCards, ref fatherIndex, delta, fatherList);
                return true;

            default:
                MoveList(motherCards, ref motherIndex, delta, motherList);
                return true;
        }
    }

    public void Submit()
    {
        switch (focus)
        {
            case SubFocus.Slots:
                if      (criarIndex == 0) OpenList(SubFocus.FatherList);
                else if (criarIndex == 1) OpenList(SubFocus.MotherList);
                else                      TryBreed();
                break;
            case SubFocus.FatherList:
                if (InRange(fatherCards, fatherIndex)) SelectFather((string)fatherCards[fatherIndex].userData);
                break;
            case SubFocus.MotherList:
                if (InRange(motherCards, motherIndex)) SelectMother((string)motherCards[motherIndex].userData);
                break;
        }
    }

    public bool Cancel()
    {
        if (focus == SubFocus.FatherList || focus == SubFocus.MotherList)
        {
            ClearListFocus();
            focus = SubFocus.Slots;
            ApplyCriarFocus();
            return true;
        }
        return false;
    }

    public void ClearFocus()
    {
        ClearCriarFocus();
        ClearListFocus();
        focus = SubFocus.Slots;
    }

    public void Rebuild()
    {
        RebuildCandidates();
        RefreshSlots();
    }

    public void Teardown()
    {
        if (breedButton != null) breedButton.clicked -= TryBreed;
    }

    private void RebuildCandidates()
    {
        if (fatherList == null || motherList == null) return;
        fatherList.Clear(); motherList.Clear();
        fatherCards.Clear(); motherCards.Clear();
        var registry = getRegistry();
        if (registry == null) return;

        var all = registry.GetAll().Values;
        foreach (var dna in Eligible(all, CreatureGender.Male))
            fatherList.Add(MakeCandidate(dna, fatherCards, isFather: true));
        foreach (var dna in Eligible(all, CreatureGender.Female))
            motherList.Add(MakeCandidate(dna, motherCards, isFather: false));
    }

    private static IEnumerable<CreatureDNA> Eligible(IEnumerable<CreatureDNA> all, CreatureGender gender) =>
        all.Where(d => CreatureAvailability.IsFree(d) && d.Gender == gender && d.BreedCount < BreedingService.MaxBreedCount)
           .OrderBy(d => d.CustomName);

    private VisualElement MakeCandidate(CreatureDNA dna, List<VisualElement> bucket, bool isFather)
    {
        var row = new VisualElement();
        row.AddToClassList("breed-candidate");
        row.userData = dna.UniqueID;

        var l = new Label($"{dna.CustomName}  ·  {dna.BreedCount}/{BreedingService.MaxBreedCount}");
        l.AddToClassList("breed-candidate-text");
        row.Add(l);

        string id = dna.UniqueID;
        row.RegisterCallback<ClickEvent>(_ => { if (isFather) SelectFather(id); else SelectMother(id); });
        bucket.Add(row);
        return row;
    }

    private void RefreshSlots()
    {
        SetSlot(fatherSlotName, fatherSlotImg, selectedFatherId);
        SetSlot(motherSlotName, motherSlotImg, selectedMotherId);
        BuildPreview();
    }

    private void SetSlot(Label nameLabel, VisualElement img, string id)
    {
        var registry = getRegistry();
        CreatureDNA dna = null;
        if (!string.IsNullOrEmpty(id) && registry != null) registry.TryGet(id, out dna);

        if (nameLabel != null) nameLabel.text = dna != null ? dna.CustomName : Loc.Tr("ui.breeding.slot.empty");
        if (img != null) MonchiPortraitUI.Apply(img, dna);
    }

    private void BuildPreview()
    {
        if (preview == null) return;
        preview.Clear();

        var registry = getRegistry();
        if (registry == null
            || !registry.TryGet(selectedFatherId, out var father)
            || !registry.TryGet(selectedMotherId, out var mother))
        {
            if (timeLabel != null) timeLabel.text = "";
            return;
        }

        preview.Add(ParentSummary(father));
        preview.Add(ParentSummary(mother));

        int mins = (BreedingController.Instance != null && BreedingController.Instance.InheritanceOdds != null) ? BreedingController.Instance.InheritanceOdds.BreedDurationMinutes : 360;
        if (timeLabel != null) timeLabel.text = Loc.Tr("ui.breeding.time.estimate.hours", FormatGameDuration(mins));
    }

    private static string FormatGameDuration(int minutes)
    {
        minutes = Mathf.Max(0, minutes);
        int h = minutes / 60;
        int m = minutes % 60;
        return $"{h}h {m:00}m";
    }

    private VisualElement ParentSummary(CreatureDNA dna)
    {
        var col = new VisualElement();
        col.AddToClassList("preview-parent");

        var name = new Label(dna.CustomName);
        name.AddToClassList("preview-name");
        col.Add(name);

        if (database != null)
        {
            AddPartRow(col, database.GetBodyShape(dna.BodyShapeID));
            AddPartRow(col, database.GetHorn(dna.HornID));
            AddPartRow(col, database.GetBack(dna.BackID));
            AddPartRow(col, database.GetWing(dna.WingID));
            AddPartRow(col, database.GetFace(dna.FaceID));
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

    private void SelectFather(string id) { selectedFatherId = id; AfterSelect(0); }
    private void SelectMother(string id) { selectedMotherId = id; AfterSelect(1); }

    private void AfterSelect(int slot)
    {
        ClearListFocus();
        focus = SubFocus.Slots;
        criarIndex = slot;
        RefreshSlots();
        ApplyCriarFocus();
    }

    private void TryBreed()
    {
        if (string.IsNullOrEmpty(selectedFatherId) || string.IsNullOrEmpty(selectedMotherId))
        {
            Debug.LogWarning("[BreedingPanel] Select a Father and a Mother first.");
            return;
        }
        if (incubation == null)
        {
            Debug.LogError("[BreedingPanel] IncubationService not assigned.");
            return;
        }

        string motherId = selectedMotherId, fatherId = selectedFatherId;

        bool started = incubation.StartBreeding(motherId, fatherId);

        if (started)
        {
            selectedFatherId = selectedMotherId = "";
            RefreshSlots();
            onBred();
        }
    }

    private void MoveList(List<VisualElement> cards, ref int idx, int delta, ScrollView scroll)
    {
        if (cards.Count == 0) return;
        idx = Mathf.Clamp(idx + delta, 0, cards.Count - 1);
        UiPanels.SetActiveIndex(cards, idx, Focus);
        scroll?.ScrollTo(cards[idx]);
    }

    private void OpenList(SubFocus which)
    {
        ClearListFocus();
        ClearCriarFocus();
        focus = which;
        if (which == SubFocus.FatherList)
        {
            fatherIndex = 0;
            UiPanels.SetActiveIndex(fatherCards, 0, Focus);
            if (fatherCards.Count > 0) fatherList?.ScrollTo(fatherCards[0]);
        }
        else
        {
            motherIndex = 0;
            UiPanels.SetActiveIndex(motherCards, 0, Focus);
            if (motherCards.Count > 0) motherList?.ScrollTo(motherCards[0]);
        }
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

    private void ClearListFocus()
    {
        foreach (var c in fatherCards) c.RemoveFromClassList(Focus);
        foreach (var c in motherCards) c.RemoveFromClassList(Focus);
    }

    private static bool InRange(List<VisualElement> list, int i) => i >= 0 && i < list.Count;
}
}
