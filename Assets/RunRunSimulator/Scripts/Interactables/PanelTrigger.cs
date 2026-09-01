using UnityEngine;
namespace MoriMonchiSimulator
{

public class PanelTrigger : MonoBehaviour, IInteractable
{
    [Tooltip("Which Canvas panel this object opens/closes.")]
    [SerializeField] private UIPanelType panel;

    public void Interact()
    {
        Debug.Log($"[PanelTrigger] '{name}' interacted → requesting toggle of '{panel}'.");
        UIManager.RequestPanelToggle(panel);
    }
}
}
