using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[RequireComponent(typeof(Collider))]
public class DeliveryBox : MonoBehaviour, IInteractable
{
    [Tooltip("What this package contains. Stamped by StoreManager at spawn.")]
    [SerializeField, ReadOnly] private ItemDefinitionSO item;

    public void Configure(ItemDefinitionSO def) => item = def;

    public void Interact()
    {
        if (item == null)
        {
            Debug.LogWarning($"[DeliveryBox] '{name}' has no item configured — nothing to open.");
            return;
        }
        if (item.Prefab == null)
        {
            Debug.LogError($"[DeliveryBox] '{item.Id}' has no Prefab — nothing to spawn.");
            return;
        }

        var go = Instantiate(item.Prefab, transform.position, transform.rotation);
        var marker = go.GetComponent<WorldPropInstance>();
        if (marker != null) marker.Configure(item.Id);
        else Debug.LogWarning($"[DeliveryBox] Spawned '{item.Id}' prefab has no WorldPropInstance — it can't be stored or persisted.");
        Debug.Log($"[DeliveryBox] World prop '{item.Id}' spawned at delivery point.");

        Destroy(gameObject);
    }
}
}
