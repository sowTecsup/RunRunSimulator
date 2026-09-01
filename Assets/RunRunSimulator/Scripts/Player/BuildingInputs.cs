using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MoriMonchiSimulator
{

public class BuildingInputs : MonoBehaviour
{
    public static event Action ConfirmPressed;
    public static event Action CancelPressed;
    public static event Action RotatePressed;
    public static event Action PinPressed;
    public static event Action EditPressed;
    public static event Action DeletePressed;
    public static event Action<int> SlotSelected;
    public static event Action BrowseToggled;

    private InputSystem_Actions actions;

    private void Awake() => actions = new InputSystem_Actions();

    private void OnEnable()
    {
        actions.Building.Confirm.performed += OnConfirm;
        actions.Building.Cancel.performed  += OnCancel;
        actions.Building.Rotate.performed  += OnRotate;
        actions.Building.Pin.performed     += OnPin;
        actions.Building.Edit.performed    += OnEdit;
        actions.Building.Delete.performed  += OnDelete;
        actions.Building.Slot1.performed   += OnSlot1;
        actions.Building.Slot2.performed   += OnSlot2;
        actions.Building.Slot3.performed   += OnSlot3;
        actions.Building.Slot4.performed   += OnSlot4;
        actions.Building.FurnitureCatalog.performed += OnFurnitureCatalog;

        BuildModeController.OnBuildModeChanged += OnBuildModeChanged;
    }

    private void OnDisable()
    {
        actions.Building.Confirm.performed -= OnConfirm;
        actions.Building.Cancel.performed  -= OnCancel;
        actions.Building.Rotate.performed  -= OnRotate;
        actions.Building.Pin.performed     -= OnPin;
        actions.Building.Edit.performed    -= OnEdit;
        actions.Building.Delete.performed  -= OnDelete;
        actions.Building.Slot1.performed   -= OnSlot1;
        actions.Building.Slot2.performed   -= OnSlot2;
        actions.Building.Slot3.performed   -= OnSlot3;
        actions.Building.Slot4.performed   -= OnSlot4;
        actions.Building.FurnitureCatalog.performed -= OnFurnitureCatalog;

        BuildModeController.OnBuildModeChanged -= OnBuildModeChanged;
        actions.Building.Disable();
    }

    private void OnDestroy() => actions?.Dispose();

    private void OnBuildModeChanged(bool isBuilding)
    {
        if (isBuilding) actions.Building.Enable();
        else            actions.Building.Disable();
    }

    private void OnConfirm(InputAction.CallbackContext c) => ConfirmPressed?.Invoke();
    private void OnCancel(InputAction.CallbackContext c)  => CancelPressed?.Invoke();
    private void OnRotate(InputAction.CallbackContext c)  => RotatePressed?.Invoke();
    private void OnPin(InputAction.CallbackContext c)     => PinPressed?.Invoke();
    private void OnEdit(InputAction.CallbackContext c)    => EditPressed?.Invoke();
    private void OnDelete(InputAction.CallbackContext c)  => DeletePressed?.Invoke();
    private void OnSlot1(InputAction.CallbackContext c)   => SlotSelected?.Invoke(0);
    private void OnSlot2(InputAction.CallbackContext c)   => SlotSelected?.Invoke(1);
    private void OnSlot3(InputAction.CallbackContext c)   => SlotSelected?.Invoke(2);
    private void OnSlot4(InputAction.CallbackContext c)   => SlotSelected?.Invoke(3);
    private void OnFurnitureCatalog(InputAction.CallbackContext c) => BrowseToggled?.Invoke();
}
}
