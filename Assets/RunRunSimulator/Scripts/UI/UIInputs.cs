using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MoriMonchiSimulator
{

// Single owner of the "UI" action map — the menu-side mirror of PlayerInputs
// (which owns the "Player" map). It translates raw UI input into STATIC events
// that UIManager (the panel-stack router) routes to the focused panel. No UI
// logic here; same decoupling idea as PlayerInputs/GameEvents.
//
// Map switching: the UI map is enabled ONLY while a panel is focused. UIInputs
// listens to UIManager.OnUIFocusChanged and enables/disables its map on the 0↔1
// edge, so the Player map and the UI map never read input at the same time — this
// is what removes "E closes the panel" (in a menu the Player map is off) and kills
// the "interact toggles the panel behind" bug.
//
// Lives on the UIManager GameObject. Input map (Assets/InputSystem_Actions, "UI"):
//   Navigate → NavigatePressed (stepped) | Submit → SubmitPressed | Cancel → CancelPressed
public class UIInputs : MonoBehaviour
{
    // Stepped navigation: one event per directional press (NOT continuous), so a
    // single A/D tap moves exactly one tab. Carries the stepped direction (x/y ∈ -1..1).
    public static event Action<Vector2> NavigatePressed;
    public static event Action SubmitPressed;   // confirm (Enter / gamepad A)
    public static event Action CancelPressed;   // ESC / gamepad B — universal "back"

    private InputSystem_Actions actions;

    // Last stepped direction, to emit exactly one event per push (re-stepping needs
    // a return toward center first — the standard menu feel).
    private Vector2 lastStep;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake() => actions = new InputSystem_Actions();

    private void OnEnable()
    {
        // performed fires when Navigate changes to a direction; canceled when it
        // returns to neutral — both feed the same stepping logic.
        actions.UI.Navigate.performed += OnNavigate;
        actions.UI.Navigate.canceled  += OnNavigate;
        actions.UI.Submit.performed   += OnSubmit;
        actions.UI.Cancel.performed   += OnCancel;

        UIManager.OnUIFocusChanged += OnFocusChanged;
        // The map starts disabled; it's enabled by the first focus edge.
    }

    private void OnDisable()
    {
        actions.UI.Navigate.performed -= OnNavigate;
        actions.UI.Navigate.canceled  -= OnNavigate;
        actions.UI.Submit.performed   -= OnSubmit;
        actions.UI.Cancel.performed   -= OnCancel;

        UIManager.OnUIFocusChanged -= OnFocusChanged;
        actions.UI.Disable();
    }

    private void OnDestroy() => actions?.Dispose();

    // ── Map switching ─────────────────────────────────────────────

    private void OnFocusChanged(bool focused)
    {
        if (focused) actions.UI.Enable();
        else         { actions.UI.Disable(); lastStep = Vector2.zero; }
    }

    // ── Raw Input System → static events ──────────────────────────

    // Emits one event each time the stepped direction changes to a new non-zero
    // value, debouncing held keys/sticks into single steps.
    private void OnNavigate(InputAction.CallbackContext c)
    {
        Vector2 v = c.ReadValue<Vector2>();
        Vector2 step = new Vector2(Step(v.x), Step(v.y));
        if (step == lastStep) return;
        lastStep = step;
        if (step != Vector2.zero) NavigatePressed?.Invoke(step);
    }

    private void OnSubmit(InputAction.CallbackContext c) => SubmitPressed?.Invoke();
    private void OnCancel(InputAction.CallbackContext c) => CancelPressed?.Invoke();

    private static float Step(float a) => a > 0.5f ? 1f : a < -0.5f ? -1f : 0f;
}
}
