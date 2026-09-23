using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class BreedingEggsTabPresenter : ITabPresenter
{
    private const string Focus = "breed-focus";

    private readonly Func<CreatureRegistrySO> getRegistry;
    private readonly IncubationService incubation;

    private readonly ScrollView eggListView;

    private readonly List<EggView> eggs = new List<EggView>();
    private int eggIndex;

    private int lastTickSecond = -1;

    private class EggView
    {
        public string MotherId;
        public string FatherId;
        public long ReadyAt;
        public VisualElement Row;
        public Label Time;
        public Button Hatch;
    }

    public BreedingEggsTabPresenter(VisualElement root, Func<CreatureRegistrySO> getRegistry,
        IncubationService incubation)
    {
        this.getRegistry = getRegistry;
        this.incubation = incubation;

        eggListView = root.Q<ScrollView>("egg-list");
    }

    public void Enter()
    {
        eggIndex = 0;
        HighlightEggs();
        if (eggs.Count > 0) eggListView?.ScrollTo(eggs[0].Row);
    }

    public bool Navigate(int h, int v)
    {
        int delta = h + v;
        int next = eggIndex + delta;
        if (next < 0) { ClearEggFocus(); return false; }
        if (eggs.Count == 0) return true;
        eggIndex = Mathf.Clamp(next, 0, eggs.Count - 1);
        HighlightEggs();
        eggListView?.ScrollTo(eggs[eggIndex].Row);
        return true;
    }

    public void Submit() => HatchFocusedEgg();

    public bool Cancel() => false;

    public void ClearFocus() => ClearEggFocus();

    public void Rebuild()
    {
        RebuildEggs();
        lastTickSecond = -1;
    }

    public void Teardown()
    {
    }

    public void Tick()
    {
        if (eggs.Count == 0) return;
        var now = DateTime.UtcNow;
        if (now.Second == lastTickSecond) return;
        lastTickSecond = now.Second;
        RefreshEggTimers();
    }

    private void RebuildEggs()
    {
        if (eggListView == null) return;
        eggListView.Clear();
        eggs.Clear();
        var registry = getRegistry();
        if (registry == null) return;

        var mothers = registry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.Breeding && d.Gender == CreatureGender.Female && d.BreedReadyAt > 0)
            .OrderBy(d => d.BreedReadyAt);

        foreach (var mother in mothers)
        {
            string fatherName = registry.TryGet(mother.BreedPartnerID, out var father) ? father.CustomName : Loc.Tr("ui.breeding.eggs.unknownfather");

            var row = new VisualElement();
            row.AddToClassList("egg-row");
            row.userData = mother.UniqueID;

            var pair = new Label($"{mother.CustomName}  💗  {fatherName}");
            pair.AddToClassList("egg-pair");

            var time = new Label();
            time.AddToClassList("egg-time");

            var hatch = new Button();
            hatch.AddToClassList("egg-hatch");
            hatch.AddToClassList("egg-hatch--minerita");
            string motherId = mother.UniqueID;
            string fatherId = mother.BreedPartnerID;
            hatch.clicked += () => DoHatch(motherId, fatherId);

            row.Add(pair); row.Add(time); row.Add(hatch);
            eggListView.Add(row);
            eggs.Add(new EggView { MotherId = motherId, FatherId = fatherId, ReadyAt = mother.BreedReadyAt, Row = row, Time = time, Hatch = hatch });
        }

        eggIndex = Mathf.Clamp(eggIndex, 0, Mathf.Max(0, eggs.Count - 1));
        RefreshEggTimers();
    }

    private void RefreshEggTimers()
    {
        long now = GameClock.Instance != null ? GameClock.Instance.TotalMinutes : 0;
        foreach (var e in eggs)
        {
            bool ready = e.ReadyAt <= now;
            e.Time.text = ready ? Loc.Tr("ui.breeding.eggs.ready") : FormatGameDuration(e.ReadyAt - now);
            RefreshHatchButton(e, ready);
        }
    }

    private void RefreshHatchButton(EggView e, bool ready)
    {
        int cost = incubation != null ? incubation.HatchCostFor(e.MotherId, e.FatherId) : 0;
        bool canAfford = Wallet.Balance(Currency.Minerita) >= cost;
        bool enabled = ready && canAfford;
        e.Hatch.text = Loc.Tr("ui.breeding.hatch.cost", cost);
        e.Hatch.SetEnabled(enabled);
        e.Hatch.EnableInClassList("egg-hatch--busy", !enabled);
    }

    private void DoHatch(string motherId, string fatherId)
    {
        if (incubation == null) { Debug.LogError("[BreedingPanel] IncubationService not assigned."); return; }

        var result = incubation.TryHatch(motherId, fatherId);
        switch (result)
        {
            case HatchResult.Hatched:
                RebuildEggs();
                break;
            case HatchResult.NotReady:
                Debug.Log("[BreedingPanel] Egg not ready yet.");
                break;
            case HatchResult.InsufficientMinerita:
                Debug.Log("[BreedingPanel] Not enough Minerita to hatch.");
                break;
            default:
                Debug.LogWarning("[BreedingPanel] Hatch failed.");
                break;
        }
    }

    private void HatchFocusedEgg()
    {
        if (!InRange2(eggs, eggIndex)) return;
        var e = eggs[eggIndex];
        DoHatch(e.MotherId, e.FatherId);
    }

    private void HighlightEggs()
    {
        for (int i = 0; i < eggs.Count; i++) eggs[i].Row.EnableInClassList(Focus, i == eggIndex);
    }

    private void ClearEggFocus()
    {
        foreach (var e in eggs) e.Row.RemoveFromClassList(Focus);
    }

    private static bool InRange2(List<EggView> list, int i) => i >= 0 && i < list.Count;

    private static string FormatGameDuration(long minutes)
    {
        minutes = Math.Max(0, minutes);
        long h = minutes / 60;
        long m = minutes % 60;
        return $"{h}h {m:00}m";
    }
}
}
