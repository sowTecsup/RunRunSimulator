using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class WorldPropInstance : MonoBehaviour, IInteractable
{
    [Tooltip("ItemDefinitionSO id (I#). Stamped at spawn; serialized so a prop placed by hand in-scene still has identity.")]
    [SerializeField, ReadOnly] private string itemId;

    public string ItemId => itemId;

    public bool IsHeld { get; set; }

    public void Configure(string id) => itemId = id;

    public void Interact()
    {
        if (HotbarController.Instance == null)
        {
            Debug.LogWarning("[WorldPropInstance] No HotbarController in scene — can't pick up.");
            return;
        }
        HotbarController.Instance.PickUp(this);
    }
}
}
