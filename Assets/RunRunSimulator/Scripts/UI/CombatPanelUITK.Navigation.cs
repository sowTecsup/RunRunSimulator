using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public partial class CombatPanelUITK
{
    // ── IUINavigable ──────────────────────────────────────────────

    public void OnUINavigate(Vector2 dir)
    {
        int h = dir.x >  0.5f ? 1 : dir.x < -0.5f ? -1 : 0;
        int v = dir.y < -0.5f ? 1 : dir.y >  0.5f ? -1 : 0;   // down = +1
        if (h == 0 && v == 0) return;

        switch (region)
        {
            case Region.TabBar:
                if (h != 0 && tabs != null) tabs.selectedTabIndex = Mathf.Clamp(tabs.selectedTabIndex + h, 0, 3);
                else if (v > 0) EnterContent();
                break;

            case Region.T1List:
                MoveCards(onlineCards, ref onlineIndex, h + v, onlineList, GoTabBar);
                break;

            case Region.T1Actions:
                if (h != 0) { t1ActionIndex = Mathf.Clamp(t1ActionIndex + h, 0, 1); ApplyT1ActionsFocus(); }
                else if (v < 0) { ClearT1ActionsFocus(); region = Region.T1List; HighlightCards(onlineCards, onlineIndex); }
                break;

            case Region.T3List:   MoveT3(h + v); break;
            case Region.T4List:   MoveT4(h + v); break;
        }
    }

    public void OnUISubmit()
    {
        switch (region)
        {
            case Region.TabBar: EnterContent(); break;

            case Region.T1List:
                if (InRange(onlineCards, onlineIndex))
                {
                    SelectOnline((string)onlineCards[onlineIndex].userData);
                    ClearFocus(onlineCards);
                    region = Region.T1Actions; t1ActionIndex = 0; ApplyT1ActionsFocus();
                }
                break;

            case Region.T1Actions: EnqueueOnline(t1ActionIndex == 0); break;

            case Region.T3List:
                if (t3Index == 0) DoRefresh();
                break;

            case Region.T4List:
                if (t4Index >= 0 && t4Index < historyCards.Count && historyCards[t4Index].userData is int hi)
                    ShowHistoryByRenderIndex(hi);
                break;
        }
    }

    public bool OnUICancel()
    {
        switch (region)
        {
            case Region.T1Actions: ClearT1ActionsFocus(); region = Region.T1List; HighlightCards(onlineCards, onlineIndex); return true;
            case Region.T1List:
            case Region.T3List:
            case Region.T4List:
                ClearAllFocus(); region = Region.TabBar; SetTabBarFocus(true); return true;
            default: return false;
        }
    }

    // ── Navigation helpers ────────────────────────────────────────

    private void GoTabBar() { region = Region.TabBar; SetTabBarFocus(true); }

    private void EnterContent()
    {
        int t = tabs != null ? tabs.selectedTabIndex : 0;
        if (t == 3) { SetTabBarFocus(true); return; }   // Equipo 3v3: mouse-driven by CombatLineupUITK

        SetTabBarFocus(false);
        if (t == 0)
        {
            region = Region.T1List; onlineIndex = 0;
            if (onlineCards.Count > 0) { HighlightCards(onlineCards, 0); onlineList.ScrollTo(onlineCards[0]); }
        }
        else if (t == 1)
        {
            region = Region.T3List; t3Index = 0; HighlightT3();
        }
        else
        {
            region = Region.T4List; t4Index = 0; HighlightT4();
            if (historyCards.Count > 0) historyList.ScrollTo(historyCards[0]);
        }
    }

    private void MoveCards(List<VisualElement> cards, ref int idx, int delta, ScrollView scroll, Action exitUp)
    {
        if (cards.Count == 0) { if (delta < 0 && exitUp != null) exitUp(); return; }
        int next = idx + delta;
        if (next < 0) { if (exitUp != null) { ClearFocus(cards); exitUp(); } return; }
        idx = Mathf.Clamp(next, 0, cards.Count - 1);
        HighlightCards(cards, idx);
        scroll?.ScrollTo(cards[idx]);
    }

    private void MoveT3(int delta)
    {
        if (t3Cards.Count == 0) return;
        int next = t3Index + delta;
        if (next < 0) { ClearT3Focus(); region = Region.TabBar; SetTabBarFocus(true); return; }
        t3Index = Mathf.Clamp(next, 0, t3Cards.Count - 1);
        HighlightT3();
        var el = t3Cards[t3Index];
        if (t3Index > 0) resultsList?.ScrollTo(el);
    }

    // ── Focus visuals ─────────────────────────────────────────────

    private static void HighlightCards(List<VisualElement> cards, int idx)
    {
        for (int i = 0; i < cards.Count; i++) cards[i].EnableInClassList(Focus, i == idx);
    }
    private static void ClearFocus(List<VisualElement> cards)
    {
        foreach (var c in cards) c.RemoveFromClassList(Focus);
    }

    private void ApplyT1ActionsFocus()
    {
        btnInstant?.EnableInClassList(Focus, t1ActionIndex == 0);
        btnTimer?.EnableInClassList(Focus, t1ActionIndex == 1);
    }
    private void ClearT1ActionsFocus() { btnInstant?.RemoveFromClassList(Focus); btnTimer?.RemoveFromClassList(Focus); }

    private void HighlightT3()
    {
        for (int i = 0; i < t3Cards.Count; i++) t3Cards[i].EnableInClassList(Focus, i == t3Index);
    }
    private void ClearT3Focus() { foreach (var c in t3Cards) c.RemoveFromClassList(Focus); }

    private void MoveT4(int delta)
    {
        if (historyCards.Count == 0) { if (delta < 0) { region = Region.TabBar; SetTabBarFocus(true); } return; }
        int next = t4Index + delta;
        if (next < 0) { ClearT4Focus(); region = Region.TabBar; SetTabBarFocus(true); return; }
        t4Index = Mathf.Clamp(next, 0, historyCards.Count - 1);
        HighlightT4();
        historyList?.ScrollTo(historyCards[t4Index]);
        ShowHistoryByRenderIndex(t4Index);
    }

    private void HighlightT4()
    {
        for (int i = 0; i < historyCards.Count; i++) historyCards[i].EnableInClassList(Focus, i == t4Index);
    }
    private void ClearT4Focus() { foreach (var c in historyCards) c.RemoveFromClassList(Focus); }

    private void SetTabBarFocus(bool on) { tabs?.EnableInClassList("tabbar-focused", on); }

    private void ClearAllFocus()
    {
        ClearFocus(onlineCards);
        ClearT1ActionsFocus(); ClearT3Focus(); ClearT4Focus();
        SetTabBarFocus(false);
    }

    private void ResetFocus()
    {
        if (!wired) return;
        if (tabs != null) tabs.selectedTabIndex = 0;
        ClearAllFocus();
        region = Region.TabBar;
        SetTabBarFocus(true);
    }

    private static bool InRange(List<VisualElement> list, int i) => i >= 0 && i < list.Count;
}
}
