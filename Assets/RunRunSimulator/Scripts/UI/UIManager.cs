using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class UIManager : SerializedMonoBehaviour
{
    public static event Action<UIPanelType> OnPanelToggleRequested;
    public static void RequestPanelToggle(UIPanelType panel) => OnPanelToggleRequested?.Invoke(panel);

    public static event Action<UIPanelType, bool> OnPanelSetRequested;
    public static void RequestPanelSet(UIPanelType panel, bool show) => OnPanelSetRequested?.Invoke(panel, show);

    public static event Action<CreatureDNA, CreatureRegistrySO> OnCreatureSelected;
    public static void SelectCreature(CreatureDNA dna, CreatureRegistrySO registry) => OnCreatureSelected?.Invoke(dna, registry);

    public static event Action<bool> OnUIFocusChanged;

    public static event Action<UIPanelType, IUINavigable> OnNavigableRegistered;
    public static void RegisterNavigable(UIPanelType panel, IUINavigable nav) => OnNavigableRegistered?.Invoke(panel, nav);
    public static event Action<UIPanelType> OnNavigableUnregistered;
    public static void UnregisterNavigable(UIPanelType panel) => OnNavigableUnregistered?.Invoke(panel);

    [Tooltip("Map each panel type to its scene GameObject. Toggled on interaction.")]
    [OdinSerialize]
    private Dictionary<UIPanelType, GameObject> panels = new Dictionary<UIPanelType, GameObject>();

    private readonly List<UIPanelType> stack = new List<UIPanelType>();

    private readonly Dictionary<UIPanelType, IUINavigable> navigables = new Dictionary<UIPanelType, IUINavigable>();

    private bool focused;

    private void OnEnable()
    {
        OnPanelToggleRequested  += TogglePanel;
        OnPanelSetRequested     += SetPanel;
        OnNavigableRegistered   += AddNavigable;
        OnNavigableUnregistered += RemoveNavigable;

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

    private void Start()
    {
        foreach (var go in panels.Values)
            if (go != null) SetPanelShown(go, false);

        stack.Clear();
        UpdateFocus();
    }

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

    private void Push(UIPanelType panel)
    {
        stack.Remove(panel);
        stack.Add(panel);
    }

    private void AddNavigable(UIPanelType panel, IUINavigable nav) => navigables[panel] = nav;
    private void RemoveNavigable(UIPanelType panel) => navigables.Remove(panel);

    private void RouteNavigate(Vector2 dir) { if (TopNavigable(out var nav)) nav.OnUINavigate(dir); }
    private void RouteSubmit() { if (TopNavigable(out var nav)) nav.OnUISubmit(); }

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

    private void UpdateFocus()
    {
        bool nowFocused = stack.Count > 0;
        if (nowFocused == focused) return;
        focused = nowFocused;
        OnUIFocusChanged?.Invoke(focused);
    }
}
}
