using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Identity tag on a world prop loose in the scene: ties the physical object back to
// its ItemDefinitionSO ("I#") so it can be stored, picked up, or swept into the
// inventory on shutdown. Tapping E routes it to the hotbar (IInteractable) — the
// unified pickup for every interactable object EXCEPT MoriMonchi agents, which keep
// their physical grab. Physics/throw lives on a sibling ThrowableObject.
//
// Spawners (DeliveryBox, StorageContainer eject) stamp the id via Configure().
public class WorldPropInstance : MonoBehaviour, IInteractable
{
    [Tooltip("ItemDefinitionSO id (I#). Stamped at spawn; serialized so a prop placed by hand in-scene still has identity.")]
    [SerializeField, ReadOnly] private string itemId;

    public string ItemId => itemId;

    // True while this prop is the active hotbar item shown in the player's hand.
    // Held props are excluded from the shutdown sweep and the storage trigger so the
    // hotbar slot (which already persists the id) isn't double-counted.
    public bool IsHeld { get; set; }

    // Called by whatever spawns the prop (DeliveryBox / storage eject) to bind it to
    // its definition. Editor-placed props keep their serialized id instead.
    public void Configure(string id) => itemId = id;

    // Tap E → into the hotbar (active/first-free slot, auto-selected). The controller
    // despawns this loose object once it's stored.
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
