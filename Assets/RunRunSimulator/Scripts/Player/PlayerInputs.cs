using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MoriMonchiSimulator
{

public class PlayerInputs : MonoBehaviour
{
    public static event Action<Vector2> MoveChanged;

    public static event Action Jumped;
    public static event Action InteractPressed;
    public static event Action InteractReleased;
    public static event Action ThrowPressed;
    public static event Action BuildToggled;
    public static event Action<int> HotbarScrolled;
    public static event Action DropPressed;

    private InputSystem_Actions actions;

    [Tooltip("Minimum |scroll delta| to count as one hotbar step.")]
    [SerializeField] private float scrollThreshold = 0.1f;

    private void Awake() => actions = new InputSystem_Actions();

    private void OnEnable()
    {
        actions.Player.Enable();

        actions.Player.Move.performed += OnMove;
        actions.Player.Move.canceled  += OnMove;

        actions.Player.Jump.performed     += OnJump;
        actions.Player.Interact.performed += OnInteractPerformed;
        actions.Player.Interact.canceled  += OnInteractCanceled;
        actions.Player.Attack.performed   += OnAttack;
        actions.Player.Build.performed    += OnBuild;
        actions.Player.Drop.performed      += OnDrop;
        actions.Player.HotbarStep.performed += OnHotbarStep;

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
        actions.Player.Drop.performed      -= OnDrop;
        actions.Player.HotbarStep.performed -= OnHotbarStep;

        UIManager.OnUIFocusChanged -= OnUIFocusChanged;
        actions.Player.Disable();
    }

    private void OnDestroy() => actions?.Dispose();

    private void OnUIFocusChanged(bool uiFocused)
    {
        if (uiFocused) actions.Player.Disable();
        else           actions.Player.Enable();
    }

    private void OnMove(InputAction.CallbackContext c)     => MoveChanged?.Invoke(c.ReadValue<Vector2>());
    private void OnJump(InputAction.CallbackContext c)      => Jumped?.Invoke();

    private void OnInteractPerformed(InputAction.CallbackContext c)
    {
        InteractPressed?.Invoke();
    }

    private void OnInteractCanceled(InputAction.CallbackContext c)
    {
        InteractReleased?.Invoke();
    }

    private void OnAttack(InputAction.CallbackContext c)
    {
        int subs = ThrowPressed?.GetInvocationList().Length ?? 0;
        ThrowPressed?.Invoke();
    }

    private void OnBuild(InputAction.CallbackContext c) => BuildToggled?.Invoke();

    private void OnDrop(InputAction.CallbackContext c) => DropPressed?.Invoke();

    private void OnHotbarStep(InputAction.CallbackContext c)
    {
        float step = c.ReadValue<float>();
        if (Mathf.Abs(step) >= scrollThreshold)
            HotbarScrolled?.Invoke(step > 0 ? -1 : 1);
    }
}
}
