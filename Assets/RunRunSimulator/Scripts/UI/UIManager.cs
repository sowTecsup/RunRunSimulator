using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

// Owns every Canvas panel and shows/hides them on request. It's a scene
// MonoBehaviour (NOT a ScriptableObject) on purpose: panels are scene
// GameObjects, and only a scene object can hold references to them.
//
// Fully decoupled: it never references the triggers. World PanelTriggers fire
// UIEvents.RequestPanelToggle(panel) and this manager — the sole listener —
// toggles the matching GameObject. The enum→GameObject map is editable in the
// inspector (Odin serializes the dictionary).
public class UIManager : SerializedMonoBehaviour
{
    [Tooltip("Map each panel type to its scene GameObject. Toggled on/off on interaction.")]
    [OdinSerialize]
    private Dictionary<UIPanelType, GameObject> panels = new Dictionary<UIPanelType, GameObject>();

    private void OnEnable()  => UIEvents.OnPanelToggleRequested += TogglePanel;
    private void OnDisable() => UIEvents.OnPanelToggleRequested -= TogglePanel;

    private void TogglePanel(UIPanelType panel)
    {
        if (!panels.TryGetValue(panel, out var go) || go == null)
        {
            Debug.LogWarning($"[UIManager] No GameObject mapped for panel '{panel}'.");
            return;
        }

        go.SetActive(!go.activeSelf);
        Debug.Log($"[UIManager] Panel '{panel}' → {(go.activeSelf ? "shown" : "hidden")}.");
    }
}
