using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[RequireComponent(typeof(Collider))]
public class DeliveryBox : MonoBehaviour, IInteractable
{
    [Tooltip("What this package contains. Stamped by StoreManager at spawn.")]
    [SerializeField, ReadOnly] private ItemDefinitionSO item;

    [SerializeField, ReadOnly] private CreatureBoxSO creatureBox;

    public void Configure(ItemDefinitionSO def) => item = def;
    public void Configure(CreatureBoxSO box) => creatureBox = box;

    public void Interact()
    {
        if (creatureBox != null)
        {
            var registry = GameManager.Instance?.Registry;
            if (registry == null)
            {
                Debug.LogWarning($"[DeliveryBox] '{name}' has no GameManager available — can't open creature box.");
                return;
            }

            int minted = 0;
            for (int i = 0; i < creatureBox.Count; i++)
            {
                var dna = GameManager.Instance.MintCreature();
                if (dna == null) continue;
                MoriMochiSpawner.Instance?.RegisterBirthLaunch(dna.UniqueID, transform.position + Vector3.up * 0.5f);
                minted++;
            }

            if (minted > 0) GameEvents.RegistryChanged(registry);
            Debug.Log($"[DeliveryBox] Creature box '{creatureBox.Id}' opened: {minted} MoriMonchis.");

            Destroy(gameObject);
            return;
        }

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
