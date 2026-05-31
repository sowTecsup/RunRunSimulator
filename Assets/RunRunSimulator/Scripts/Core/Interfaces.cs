using UnityEngine;

// All cross-system interfaces live here (project rule: one home for interfaces).

// Something in the world the player can interact with by TAPPING E (vs. holding
// E to grab). The player only knows this contract; the implementer decides what
// interacting does (e.g. a PanelTrigger opens/closes a Canvas panel).
public interface IInteractable
{
    void Interact();
}

// A physics object the player can pick up, hold in front of them, and throw.
// The player only knows this contract — never the concrete object. Each
// implementer owns its own "hold feel" (how it follows the anchor, mass, etc.).
public interface IThrowable
{
    // True while the player is holding it — lets the player avoid re-grabbing.
    bool IsHeld { get; }

    // Player grabbed it: follow this anchor (a point in front of the camera).
    void OnGrab(Transform holdAnchor);

    // Player dropped it with no throw force — just hands physics back.
    void OnRelease();

    // Player threw it: hands physics back and applies an impulse.
    void OnThrow(Vector3 force);
}
