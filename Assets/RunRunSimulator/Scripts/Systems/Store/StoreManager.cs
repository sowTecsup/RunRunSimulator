using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// Minimal in-editor storefront for testing the acquisition loop end to end:
// drag items into the catalog, pick a row, press Buy → a DeliveryBox drops at the
// spawn point. No economy yet (free); this stands in for the future "order online
// at the computer" UI (Fase 3). The real shop builds on the same BuyItem flow.
public class StoreManager : MonoBehaviour
{
    [System.Serializable]
    public class StoreEntry
    {
        [AssetsOnly, Required, HorizontalGroup, HideLabel]
        public ItemDefinitionSO Item;

        [MinValue(1), HorizontalGroup(60), LabelText("Qty")]
        public int Quantity = 1;
    }

    [Title("Catalog (test)")]
    [TableList(AlwaysExpanded = true)]
    [SerializeField] private List<StoreEntry> catalog = new List<StoreEntry>();

    [Title("Delivery")]
    [Required, AssetsOnly] [SerializeField] private DeliveryBox deliveryBoxPrefab;
    [Required]             [SerializeField] private Transform deliverySpawnPoint;

    // Enter an index, press Buy → Quantity DeliveryBoxes drop at the spawn point.
    // Odin renders the int param as an inline field next to the button.
    [Button("Buy (by index)", ButtonSizes.Medium), GUIColor(0.4f, 1f, 0.6f)]
    public void BuyItem(int index)
    {
        if (index < 0 || index >= catalog.Count)
        {
            Debug.LogWarning($"[StoreManager] Catalog index {index} out of range.");
            return;
        }

        var entry = catalog[index];
        if (entry == null || entry.Item == null)
        {
            Debug.LogWarning("[StoreManager] Empty catalog entry.");
            return;
        }
        if (deliveryBoxPrefab == null || deliverySpawnPoint == null)
        {
            Debug.LogError("[StoreManager] Assign deliveryBoxPrefab + deliverySpawnPoint first.");
            return;
        }
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[StoreManager] Enter Play mode to buy (spawns a DeliveryBox).");
            return;
        }

        for (int i = 0; i < Mathf.Max(1, entry.Quantity); i++)
        {
            var go  = Instantiate(deliveryBoxPrefab, deliverySpawnPoint.position, deliverySpawnPoint.rotation);
            var box = go.GetComponent<DeliveryBox>();
            if (box != null) box.Configure(entry.Item);
            else Debug.LogError("[StoreManager] deliveryBoxPrefab has no DeliveryBox component.");
        }

        Debug.Log($"[StoreManager] Ordered {entry.Quantity}× '{entry.Item.DisplayName}' ({entry.Item.Id}).");
    }
}
