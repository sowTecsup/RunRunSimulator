using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public partial class BreedingPanelUITK
{
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
}
