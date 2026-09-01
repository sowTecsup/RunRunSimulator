using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MoriMonchiSimulator
{

public class UIInputs : MonoBehaviour
{
    public static event Action<Vector2> NavigatePressed;
    public static event Action SubmitPressed;
    public static event Action CancelPressed;

    private InputSystem_Actions actions;

    private Vector2 lastStep;

    private void Awake() => actions = new InputSystem_Actions();

    private void OnEnable()
    {
        actions.UI.Navigate.performed += OnNavigate;
        actions.UI.Navigate.canceled  += OnNavigate;
        actions.UI.Submit.performed   += OnSubmit;
        actions.UI.Cancel.performed   += OnCancel;

        UIManager.OnUIFocusChanged += OnFocusChanged;
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

    private void OnFocusChanged(bool focused)
    {
        if (focused) actions.UI.Enable();
        else         { actions.UI.Disable(); lastStep = Vector2.zero; }
    }

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
