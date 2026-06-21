using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// Owns every Canvas/UI panel AND is the static event hub for UI-domain panel
// events. The events live here (not in GameEvents) on purpose: GameEvents stays
// gameplay-only so it doesn't grow without bound — each domain keeps its own bus.
//
// Decoupling: a world PanelTrigger calls UIManager.RequestPanelToggle(panel)
// (static) without holding any UIManager reference; the live instance —
// subscribed in OnEnable — toggles the matching panel. Same pattern as the
// static events on PlayerInputs.
//
// Scene MonoBehaviour (NOT a ScriptableObject): panels are scene GameObjects and
// only a scene object can reference them. The enum→GameObject map is editable in
// the inspector (Odin serializes the dictionary).
//
// Two hide strategies (so panels can keep updating while hidden):
//  • UI Toolkit panels (GameObject has a UIDocument) → toggle the root's
//    `display`, leaving the UIDocument ACTIVE. Its controller (e.g.
//    CreatureGridUITK, which lives on THIS always-active object) keeps populating
//    it while hidden. NEVER SetActive(false) a UIDocument — an inactive document
//    has no rootVisualElement and stops updating entirely.
//  • uGUI panels → plain GameObject SetActive.
//
// All panels start HIDDEN (in Start), and the manager fires OnUIFocusChanged so
// the player can suspend control whenever at least one panel is open.
//
// Keyboard/gamepad routing: open panels live in an ordered STACK (last = focused
// top). The UIManager is the SINGLE subscriber to UIInputs and dispatches Navigate/
// Submit to the top panel's IUINavigable; Cancel (ESC) pops the top panel, so they
// close in reverse order. Panels register their handler via RegisterNavigable.
public class UIManager : SerializedMonoBehaviour
{
    // ── Static event hub ──────────────────────────────────────────

    // A world interactable asked to show/hide a panel. Static so any trigger can
    // fire it without referencing a UIManager instance.
    public static event Action<UIPanelType> OnPanelToggleRequested;
    public static void RequestPanelToggle(UIPanelType panel) => OnPanelToggleRequested?.Invoke(panel);

    // Explicitly show/hide a panel (no toggle). Used when the action isn't a
    // flip — e.g. clicking a card opens the detail even if it's already open.
    public static event Action<UIPanelType, bool> OnPanelSetRequested;
    public static void RequestPanelSet(UIPanelType panel, bool show) => OnPanelSetRequested?.Invoke(panel, show);

    // A grid card was clicked: carries the creature (and the registry, to resolve
    // parents) so the detail window can populate itself. The event carries the data.
    public static event Action<CreatureDNA, CreatureRegistrySO> OnCreatureSelected;
    public static void SelectCreature(CreatureDNA dna, CreatureRegistrySO registry) => OnCreatureSelected?.Invoke(dna, registry);

    // Raised when UI focus changes: true once the first panel opens, false once
    // the last one closes. The player listens to suspend/resume control; UIInputs
    // listens to enable/disable the UI action map.
    public static event Action<bool> OnUIFocusChanged;

    // A focusable panel registers the handler that should receive routed input
    // while it's the top of the stack. Static registration keeps panels decoupled
    // from the UIManager instance (same style as the rest of this hub).
    public static event Action<UIPanelType, IUINavigable> OnNavigableRegistered;
    public static void RegisterNavigable(UIPanelType panel, IUINavigable nav) => OnNavigableRegistered?.Invoke(panel, nav);
    public static event Action<UIPanelType> OnNavigableUnregistered;
    public static void UnregisterNavigable(UIPanelType panel) => OnNavigableUnregistered?.Invoke(panel);

    // ── Inspector ─────────────────────────────────────────────────

    [Tooltip("Map each panel type to its scene GameObject. Toggled on interaction.")]
    [OdinSerialize]
    private Dictionary<UIPanelType, GameObject> panels = new Dictionary<UIPanelType, GameObject>();

    // ── State ─────────────────────────────────────────────────────

    // Ordered stack of open panels — the last entry is the focused TOP. ESC pops
    // it, so panels close in reverse order (detail → grid → gameplay).
    private readonly List<UIPanelType> stack = new List<UIPanelType>();

    // Routed-input handler per panel, populated via the registration events.
    private readonly Dictionary<UIPanelType, IUINavigable> navigables = new Dictionary<UIPanelType, IUINavigable>();

    private bool focused;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void OnEnable()
    {
        OnPanelToggleRequested  += TogglePanel;
        OnPanelSetRequested     += SetPanel;
        OnNavigableRegistered   += AddNavigable;
        OnNavigableUnregistered += RemoveNavigable;

        // Single subscriber to the UI input bus — dispatches to the focused panel.
        UIInputs.NavigatePressed += RouteNavigate;
        UIInputs.SubmitPressed   += RouteSubmit;
        UIInputs.CancelPressed   += RouteCancel;
    }

    private void OnDisable()
    {
        OnPanelToggleRequested  -= TogglePanel;
        OnPanelSetRequested     -= SetPanel;
        OnNavigableRegistered   -= AddNavigable;
        OnNavigableUnregistered -= RemoveNavigable;

        UIInputs.NavigatePressed -= RouteNavigate;
        UIInputs.SubmitPressed   -= RouteSubmit;
        UIInputs.CancelPressed   -= RouteCancel;
    }

    // Hide every panel once the documents have built their trees (OnEnable runs
    // before Start, so UITK rootVisualElements exist here). Hidden via `display`
    // for UITK so they keep updating; SetActive(false) for uGUI.
    private void Start()
    {
        foreach (var go in panels.Values)
            if (go != null) SetPanelShown(go, false);

        stack.Clear();
        UpdateFocus();
    }

    // ── Private Methods ───────────────────────────────────────────

    private void TogglePanel(UIPanelType panel)
    {
        if (!panels.TryGetValue(panel, out var go) || go == null)
        {
            Debug.LogWarning($"[UIManager] No GameObject mapped for panel '{panel}'.");
            return;
        }

        bool show = !IsPanelShown(go);
        SetPanelShown(go, show);

        if (show) Push(panel);
        else      stack.Remove(panel);

        Debug.Log($"[UIManager] Panel '{panel}' → {(show ? "shown" : "hidden")}.");
        UpdateFocus();
    }

    // Explicit show/hide (idempotent) — same bookkeeping as the toggle.
    private void SetPanel(UIPanelType panel, bool show)
    {
        if (!panels.TryGetValue(panel, out var go) || go == null)
        {
            Debug.LogWarning($"[UIManager] No GameObject mapped for panel '{panel}'.");
            return;
        }

        SetPanelShown(go, show);
        if (show) Push(panel);
        else      stack.Remove(panel);

        Debug.Log($"[UIManager] Panel '{panel}' set → {(show ? "shown" : "hidden")}.");
        UpdateFocus();
    }

    // Moves a panel to the top of the stack (re-pushing an open panel re-focuses it).
    private void Push(UIPanelType panel)
    {
        stack.Remove(panel);
        stack.Add(panel);
    }

    // ── Routed input (single subscriber to UIInputs) ──────────────

    private void AddNavigable(UIPanelType panel, IUINavigable nav) => navigables[panel] = nav;
    private void RemoveNavigable(UIPanelType panel) => navigables.Remove(panel);

    private void RouteNavigate(Vector2 dir) { if (TopNavigable(out var nav)) nav.OnUINavigate(dir); }
    private void RouteSubmit() { if (TopNavigable(out var nav)) nav.OnUISubmit(); }

    // ESC = back: first let the top panel handle it internally (close a sub-view,
    // step up a focus level); only if it doesn't consume it do we pop the panel.
    private void RouteCancel()
    {
        if (stack.Count == 0) return;
        if (TopNavigable(out var nav) && nav.OnUICancel()) return;
        SetPanel(stack[stack.Count - 1], false);
    }

    private bool TopNavigable(out IUINavigable nav)
    {
        nav = null;
        if (stack.Count == 0) return false;
        return navigables.TryGetValue(stack[stack.Count - 1], out nav) && nav != null;
    }

    // Shows/hides a panel using the strategy that fits its kind.
    private static void SetPanelShown(GameObject go, bool show)
    {
        if (go.TryGetComponent<UIDocument>(out var doc) && doc.rootVisualElement != null)
            doc.rootVisualElement.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        else
            go.SetActive(show);
    }

    private static bool IsPanelShown(GameObject go)
    {
        if (go.TryGetComponent<UIDocument>(out var doc) && doc.rootVisualElement != null)
            return doc.rootVisualElement.resolvedStyle.display != DisplayStyle.None;
        return go.activeSelf;
    }

    // Fires OnUIFocusChanged only on the 0↔1 edge, not on every toggle.
    private void UpdateFocus()
    {
        bool nowFocused = stack.Count > 0;
        if (nowFocused == focused) return;
        focused = nowFocused;
        OnUIFocusChanged?.Invoke(focused);
    }
}
}
