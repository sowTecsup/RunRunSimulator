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
    private readonly AsyncBreedingService asyncBreedingService;

    private readonly ScrollView eggListView;

    private readonly List<EggView> eggs = new List<EggView>();
    private int eggIndex;

    private int lastTickSecond = -1;

    private class EggView
    {
        public string MotherId;
        public long ReadyAt;
        public VisualElement Row;
        public Label Time;
        public Button Hatch;
    }

    public BreedingEggsTabPresenter(VisualElement root, Func<CreatureRegistrySO> getRegistry,
        AsyncBreedingService asyncBreedingService)
    {
        this.getRegistry = getRegistry;
        this.asyncBreedingService = asyncBreedingService;

        eggListView = root.Q<ScrollView>("egg-list");
    }

    // ── ITabPresenter ────────────────────────────────────────────

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

    // ── Tab 1: eggs ───────────────────────────────────────────────

    // Build the "Incubando" cards: one per breeding female + her partner.
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

            var hatch = new Button { text = Loc.Tr("ui.breeding.hatch.action") };
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
                e.Time.text = Loc.Tr("ui.breeding.eggs.ready");
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

    // Gray the button to "Hatching..." while the server is consulted. On success
    // RebuildEggs replaces the row (the button is orphaned → we skip restoring it);
    // on not_ready the row stays, so we restore the button for a retry.
    private async void DoHatch(string motherId, Button btn)
    {
        if (asyncBreedingService == null) { Debug.LogError("[BreedingPanel] AsyncBreedingService not assigned."); return; }
        var registry = getRegistry();
        if (registry == null || !registry.TryGet(motherId, out var mother)) return;

        if (btn != null)
        {
            btn.SetEnabled(false);
            btn.text = Loc.Tr("ui.breeding.hatch.busy");
            btn.AddToClassList("egg-hatch--busy");
        }

        await asyncBreedingService.HatchAsync(motherId, mother.BreedPartnerID);

        if (btn != null && btn.panel != null)   // still attached → the egg didn't hatch (not_ready)
        {
            btn.SetEnabled(true);
            btn.text = Loc.Tr("ui.breeding.hatch.action");
            btn.RemoveFromClassList("egg-hatch--busy");
        }
    }

    private void HatchFocusedEgg()
    {
        if (!InRange2(eggs, eggIndex)) return;
        var e = eggs[eggIndex];
        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= e.ReadyAt) DoHatch(e.MotherId, e.Hatch);
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
}
}
