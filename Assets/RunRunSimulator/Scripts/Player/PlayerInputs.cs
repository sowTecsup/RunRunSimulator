using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MoriMonchiSimulator
{

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
// Map switching: this enables ONLY the "Player" map (UIInputs owns "UI"). While a
// panel is focused the Player map is disabled — gameplay input stops, and the world
// interact (E) can't fire, so it can't toggle the panel behind a menu. Driven by
// UIManager.OnUIFocusChanged, the mirror of UIInputs' own gating.
//
// Input map (Assets/InputSystem_Actions, "Player" map):
//   Move → MoveChanged | Jump → Jumped | Interact → GrabReleaseToggled | Attack → ThrowPressed | Build → BuildToggled
public class PlayerInputs : MonoBehaviour
{
    // Continuous — fired on every change (carries the value).
    public static event Action<Vector2> MoveChanged;

    // Discrete intents.
    public static event Action Jumped;            // Jump pressed
    public static event Action InteractPressed;   // Interact key DOWN (raw — hold/tap meaning is decided in PlayerController)
    public static event Action InteractReleased;  // Interact key UP
    public static event Action ThrowPressed;      // Attack — throw the held object
    public static event Action BuildToggled;      // Build — enter/exit construction mode (BuildModeController owns it)
    public static event Action<int> HotbarScrolled; // Mouse wheel — step the hotbar (+1 / -1)
    public static event Action DropPressed;          // Q — drop the active hotbar item

    private InputSystem_Actions actions;

    // Mirrors whether the Player map is live (gated by UI focus) so the wheel read in
    // Update is suspended in menus, just like the action-driven inputs.
    private bool playerActive = true;

    [Tooltip("Minimum |scroll delta| to count as one hotbar step.")]
    [SerializeField] private float scrollThreshold = 0.1f;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake() => actions = new InputSystem_Actions();

    private void OnEnable()
    {
        actions.Player.Enable();   // only the Player map; UIInputs owns the UI map

        actions.Player.Move.performed += OnMove;
        actions.Player.Move.canceled  += OnMove;

        actions.Player.Jump.performed     += OnJump;
        actions.Player.Interact.performed += OnInteractPerformed;  // key down (disable the action's Hold interaction so this fires on press)
        actions.Player.Interact.canceled  += OnInteractCanceled;   // key up
        actions.Player.Attack.performed   += OnAttack;
        actions.Player.Build.performed    += OnBuild;

        UIManager.OnUIFocusChanged += OnUIFocusChanged;
    }

    private void OnDisable()
    {
        actions.Player.Move.performed -= OnMove;
        actions.Player.Move.canceled  -= OnMove;

        actions.Player.Jump.performed     -= OnJump;
        actions.Player.Interact.performed -= OnInteractPerformed;
        actions.Player.Interact.canceled  -= OnInteractCanceled;
        actions.Player.Attack.performed   -= OnAttack;
        actions.Player.Build.performed    -= OnBuild;

        UIManager.OnUIFocusChanged -= OnUIFocusChanged;
        actions.Player.Disable();
    }

    private void OnDestroy() => actions?.Dispose();

    // Suspend gameplay input while a panel is focused; resume when it closes.
    private void OnUIFocusChanged(bool uiFocused)
    {
        playerActive = !uiFocused;
        if (uiFocused) actions.Player.Disable();
        else           actions.Player.Enable();
    }

    // Mouse wheel → discrete hotbar steps. Read directly from the device (still Input
    // System, still only this script) rather than a generated action, so no asset
    // regeneration is needed. Gated by the same focus state as the action map.
    private void Update()
    {
        if (!playerActive) return;
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) >= scrollThreshold)
            HotbarScrolled?.Invoke(scroll > 0 ? -1 : 1);   // wheel up = previous slot

        // Q → drop the active hotbar item (direct device read, like the wheel).
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.qKey.wasPressedThisFrame)
            DropPressed?.Invoke();
    }

    // ── Raw Input System → static events ──────────────────────────

    private void OnMove(InputAction.CallbackContext c)     => MoveChanged?.Invoke(c.ReadValue<Vector2>());
    private void OnJump(InputAction.CallbackContext c)      => Jumped?.Invoke();

    private void OnInteractPerformed(InputAction.CallbackContext c)
    {
        //Debug.Log("[PlayerInputs] Interact (E) DOWN → InteractPressed.");
        InteractPressed?.Invoke();
    }

    private void OnInteractCanceled(InputAction.CallbackContext c)
    {
      //  Debug.Log("[PlayerInputs] Interact (E) UP → InteractReleased.");
        InteractReleased?.Invoke();
    }

    private void OnAttack(InputAction.CallbackContext c)
    {
        int subs = ThrowPressed?.GetInvocationList().Length ?? 0;
       // Debug.Log($"[PlayerInputs] Attack fired → invoking ThrowPressed ({subs} listener(s)).");
        ThrowPressed?.Invoke();
    }

    private void OnBuild(InputAction.CallbackContext c) => BuildToggled?.Invoke();
}
}
