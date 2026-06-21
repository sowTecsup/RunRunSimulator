using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MoriMonchiSimulator
{

// Single owner of the "Building" action map — the construction-mode mirror of
// PlayerInputs / UIInputs. It only translates raw input into STATIC events that
// BuildModeController consumes; no build logic here (same decoupling as the other
// input owners — listeners never reference this component).
//
// Map switching: the Building map is enabled ONLY while build mode is active. It
// listens to BuildModeController.OnBuildModeChanged and enables/disables on the edge.
// The Player map STAYS enabled during building (so Move and the Cinemachine Look keep
// working); PlayerController gates grab/throw/jump/interact off by state, so the few
// shared keys (E, left-click) only do their build-mode job here.
//
// Lives on the same GameObject as BuildModeController. Input map ("Building"):
//   Confirm → ConfirmPressed | Cancel → CancelPressed | Rotate → RotatePressed
//   Pin → PinPressed | Edit → EditPressed | Delete → DeletePressed | Slot1-4 → SlotSelected(0-3)
//   FurnitureCatalog → BrowseToggled
public class BuildingInputs : MonoBehaviour
{
    public static event Action ConfirmPressed;     // F — confirm / save
    public static event Action CancelPressed;      // Esc — cancel selection / exit mode
    public static event Action RotatePressed;      // R — rotate 90°
    public static event Action PinPressed;         // left-click — pin a new piece
    public static event Action EditPressed;        // E — select aimed placed piece to edit
    public static event Action DeletePressed;      // right-click — target aimed placed piece to delete
    public static event Action<int> SlotSelected;  // 1-4 — pick hotbar piece (0-based)
    public static event Action BrowseToggled;      // FurnitureCatalog (Tab) — open/close the furniture browser

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
        // The map starts disabled; the first build-mode edge enables it.
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
