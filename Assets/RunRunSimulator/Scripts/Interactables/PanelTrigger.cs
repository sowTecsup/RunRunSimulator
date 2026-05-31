using UnityEngine;

// A world object that, when the player taps E on it, asks the UIManager to
// show/hide a Canvas panel. Knows nothing about the UIManager — it just fires
// a UIEvents request carrying which panel it represents. Drop this on any prop
// (a shelf, a terminal, a poster) and pick its panel in the inspector.
public class PanelTrigger : MonoBehaviour, IInteractable
{
    [Tooltip("Which Canvas panel this object opens/closes.")]
    [SerializeField] private UIPanelType panel;

    public void Interact()
    {
        Debug.Log($"[PanelTrigger] '{name}' interacted → requesting toggle of '{panel}'.");
        UIEvents.RequestPanelToggle(panel);
    }
}
