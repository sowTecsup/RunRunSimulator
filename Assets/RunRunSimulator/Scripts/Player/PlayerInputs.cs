using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Single source of player intent and the ONLY script that touches the Input
// System. No gameplay logic — it just translates raw Input System callbacks into
// STATIC events, the same decoupling idea as GameEvents: listeners (PlayerController,
// PlayerAnimator) subscribe WITHOUT ever referencing this component.
//
// The event carries the data: MoveChanged delivers the Vector2 so a listener
// caches it locally instead of reaching back into PlayerInputs.
//
// Look/aim is NOT here: Cinemachine (Pan Tilt + Input Axis Controller) reads the
// Look action directly, so the camera is fully owned by Cinemachine.
//
// Input map (Assets/InputSystem_Actions, "Player" map):
//   Move → MoveChanged | Jump → Jumped | Interact → GrabReleaseToggled | Attack → ThrowPressed
public class PlayerInputs : MonoBehaviour
{
    // Continuous — fired on every change (carries the value).
    public static event Action<Vector2> MoveChanged;

    // Discrete intents.
    public static event Action Jumped;            // Jump pressed
    public static event Action InteractPressed;   // Interact key DOWN (raw — hold/tap meaning is decided in PlayerController)
    public static event Action InteractReleased;  // Interact key UP
    public static event Action ThrowPressed;      // Attack — throw the held object

    private InputSystem_Actions actions;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake() => actions = new InputSystem_Actions();

    private void OnEnable()
    {
        actions.Enable();

        actions.Player.Move.performed += OnMove;
        actions.Player.Move.canceled  += OnMove;

        actions.Player.Jump.performed     += OnJump;
        actions.Player.Interact.performed += OnInteractPerformed;  // key down (disable the action's Hold interaction so this fires on press)
        actions.Player.Interact.canceled  += OnInteractCanceled;   // key up
        actions.Player.Attack.performed   += OnAttack;
    }

    private void OnDisable()
    {
        actions.Player.Move.performed -= OnMove;
        actions.Player.Move.canceled  -= OnMove;

        actions.Player.Jump.performed     -= OnJump;
        actions.Player.Interact.performed -= OnInteractPerformed;
        actions.Player.Interact.canceled  -= OnInteractCanceled;
        actions.Player.Attack.performed   -= OnAttack;

        actions.Disable();
    }

    private void OnDestroy() => actions?.Dispose();

    // ── Raw Input System → static events ──────────────────────────

    private void OnMove(InputAction.CallbackContext c)     => MoveChanged?.Invoke(c.ReadValue<Vector2>());
    private void OnJump(InputAction.CallbackContext c)      => Jumped?.Invoke();

    private void OnInteractPerformed(InputAction.CallbackContext c)
    {
        Debug.Log("[PlayerInputs] Interact (E) DOWN → InteractPressed.");
        InteractPressed?.Invoke();
    }

    private void OnInteractCanceled(InputAction.CallbackContext c)
    {
        Debug.Log("[PlayerInputs] Interact (E) UP → InteractReleased.");
        InteractReleased?.Invoke();
    }

    private void OnAttack(InputAction.CallbackContext c)
    {
        int subs = ThrowPressed?.GetInvocationList().Length ?? 0;
        Debug.Log($"[PlayerInputs] Attack fired → invoking ThrowPressed ({subs} listener(s)).");
        ThrowPressed?.Invoke();
    }
}
