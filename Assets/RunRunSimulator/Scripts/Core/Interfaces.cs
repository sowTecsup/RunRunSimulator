using UnityEngine;

// All cross-system interfaces live here (project rule: one home for interfaces).

// Something in the world the player can interact with by TAPPING E (vs. holding
// E to grab). The player only knows this contract; the implementer decides what
// interacting does (e.g. a PanelTrigger opens/closes a Canvas panel).
public interface IInteractable
{
    void Interact();
}

// A UI panel that can receive routed input while it holds focus. The UIManager
// (panel-stack router) is the SINGLE subscriber to UIInputs; it dispatches these
// only to the panel on top of the stack, so panels never compete for input.
// Cancel (ESC / gamepad B) is handled by the UIManager itself — it pops the top
// panel — so a panel doesn't implement it here.
public interface IUINavigable
{
    // Stepped directional input (one step per press). Tabs read dir.x.
    void OnUINavigate(Vector2 dir);

    // Confirm / activate the current selection (gamepad A / Enter). May be a no-op.
    void OnUISubmit();

    // ESC / gamepad B while focused. Return true if the panel CONSUMED it (e.g. it
    // stepped back inside itself — closed a sub-list, went up a focus level — and
    // wants to stay open). Return false to let the UIManager pop this panel off the
    // stack (the default for simple panels).
    bool OnUICancel();
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

    // Hit by ANOTHER throwable (not the player): go dynamic if needed and fly with
    // the given impulse. Lets thrown objects knock each other ragdoll-style. A held
    // object ignores this (it's in the player's hand).
    void Knock(Vector3 force);
}
