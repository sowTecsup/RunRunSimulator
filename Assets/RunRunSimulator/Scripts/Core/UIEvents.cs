using System;

// Static bus for UI-domain events, parallel to GameEvents (which stays focused
// on gameplay/registry). Keeps the UI layer decoupled: a world PanelTrigger
// requests a panel toggle without ever referencing the UIManager, and the
// UIManager reacts without knowing who asked. The event carries the data (which
// panel), so the listener never has to look anything up.
public static class UIEvents
{
    // A world interactable asked to show/hide a panel.
    public static event Action<UIPanelType> OnPanelToggleRequested;
    public static void RequestPanelToggle(UIPanelType panel) => OnPanelToggleRequested?.Invoke(panel);
}
