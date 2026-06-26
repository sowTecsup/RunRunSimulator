using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public partial class BreedingPanelUITK
{
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
            : new CombatService.EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);

        var l = new Label($"{dna.CustomName}  ·  CON {eff.Constitution:0} ATK {eff.Attack:0} SPD {eff.Speed:0} DEF {eff.Defense:0} LCK {eff.Luck:0} EVA {eff.Evasion:0}  ·  {dna.BreedCount}/{BreedingService.MaxBreedCount}");
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

        int mins = (BreedingController.Instance != null && BreedingController.Instance.InheritanceOdds != null) ? BreedingController.Instance.InheritanceOdds.BreedDurationMinutes : 30;
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
            : new CombatService.EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);
        var stats = new Label($"CON {eff.Constitution:0}   ATK {eff.Attack:0}   SPD {eff.Speed:0}   DEF {eff.Defense:0}   LCK {eff.Luck:0}   EVA {eff.Evasion:0}");
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
}
}
