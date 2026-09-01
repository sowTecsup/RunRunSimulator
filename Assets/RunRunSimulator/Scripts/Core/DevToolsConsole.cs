using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class DevToolsConsole : MonoBehaviour
{

    [BoxGroup("Setup"), Required]
    [SerializeField] private GameManager gameManager;

    [Title("Dev Tools")]
    [BoxGroup("Dev Tools"), SerializeField, LabelText("Dabloons to add")]
    private int devDabloonsAmount = 500;

    [Button("Add Dabloons (DEV)", ButtonSizes.Medium), GUIColor(0.9f, 0.75f, 0.2f), BoxGroup("Dev Tools")]
    private void DevAddDabloons()
    {
        if (gameManager == null) { Debug.LogWarning("[DevToolsConsole] No GameManager assigned."); return; }
        var inventory = gameManager.Inventory;
        if (inventory == null) { Debug.LogWarning("[DevToolsConsole] No inventory assigned."); return; }
        inventory.AddDabloons(devDabloonsAmount);
        GameEvents.InventoryChanged(inventory);
        Debug.Log($"[DevToolsConsole] +{devDabloonsAmount} Dabloons → total: {inventory.Dabloons}");
    }

    [Button("Reset Dabloons (DEV)", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.3f), BoxGroup("Dev Tools")]
    private void DevResetDabloons()
    {
        if (gameManager == null) { Debug.LogWarning("[DevToolsConsole] No GameManager assigned."); return; }
        var inventory = gameManager.Inventory;
        if (inventory == null) { Debug.LogWarning("[DevToolsConsole] No inventory assigned."); return; }
        inventory.ResetDabloons();
        GameEvents.InventoryChanged(inventory);
        Debug.Log("[DevToolsConsole] Dabloons reset to 0.");
    }

    [Button("Clear Furniture Owned (DEV)", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.3f), BoxGroup("Dev Tools")]
    private void DevClearFurnitureOwned()
    {
        if (gameManager == null) { Debug.LogWarning("[DevToolsConsole] No GameManager assigned."); return; }
        var inventory = gameManager.Inventory;
        if (inventory == null) { Debug.LogWarning("[DevToolsConsole] No inventory assigned."); return; }
        inventory.ClearFurnitureOwned();
        GameEvents.InventoryChanged(inventory);
        Debug.Log("[DevToolsConsole] Furniture owned list cleared.");
    }

    [Button("Clear World Props (DEV)", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.3f), BoxGroup("Dev Tools")]
    private void DevClearWorldProps()
    {
        if (gameManager == null) { Debug.LogWarning("[DevToolsConsole] No GameManager assigned."); return; }
        var inventory = gameManager.Inventory;
        if (inventory == null) { Debug.LogWarning("[DevToolsConsole] No inventory assigned."); return; }
        inventory.ClearWorldPropsStored();
        inventory.ClearHotbar();
        GameEvents.InventoryChanged(inventory);
        Debug.Log("[DevToolsConsole] World props and hotbar cleared.");
    }

    [BoxGroup("Equipment (DEV)"), SerializeField, AssetsOnly, LabelText("Item to add")]
    private EquipmentSO devEquipmentItem;

    [BoxGroup("Equipment (DEV)"), SerializeField, AssetsOnly]
    private EquipmentDatabaseSO equipmentDatabase;

    [Button("Add Equipment Item (DEV)", ButtonSizes.Medium), GUIColor(0.9f, 0.75f, 0.2f), BoxGroup("Equipment (DEV)")]
    private void DevAddEquipmentItem()
    {
        if (gameManager == null) { Debug.LogWarning("[DevToolsConsole] No GameManager assigned."); return; }
        var inventory = gameManager.Inventory;
        if (inventory == null) { Debug.LogWarning("[DevToolsConsole] No inventory assigned."); return; }
        if (devEquipmentItem == null || string.IsNullOrEmpty(devEquipmentItem.ID))
        {
            Debug.LogWarning("[DevToolsConsole] asigna un EquipmentSO con ID.");
            return;
        }
        inventory.AddEquipment(devEquipmentItem.Slot, devEquipmentItem.ID);
        GameEvents.InventoryChanged(inventory);
        Debug.Log($"[DevToolsConsole] +1 {devEquipmentItem.Name} ({devEquipmentItem.ID})");
    }

    [Button("Add Full Equipment Catalog (DEV)", ButtonSizes.Medium), GUIColor(0.9f, 0.75f, 0.2f), BoxGroup("Equipment (DEV)")]
    private void DevAddFullEquipmentCatalog()
    {
        if (gameManager == null) { Debug.LogWarning("[DevToolsConsole] No GameManager assigned."); return; }
        var inventory = gameManager.Inventory;
        if (inventory == null) { Debug.LogWarning("[DevToolsConsole] No inventory assigned."); return; }
        if (equipmentDatabase == null) { Debug.LogWarning("[DevToolsConsole] No equipment database assigned."); return; }
        var ids = equipmentDatabase.GetAllIDs();
        foreach (var id in ids)
        {
            var item = equipmentDatabase.GetByID(id);
            if (item != null) inventory.AddEquipment(item.Slot, item.ID);
        }
        GameEvents.InventoryChanged(inventory);
        Debug.Log($"[DevToolsConsole] Added {ids.Count} equipment items from catalog.");
    }

    [Button("Clear Equipment (DEV)", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.3f), BoxGroup("Equipment (DEV)")]
    private void DevClearEquipment()
    {
        if (gameManager == null) { Debug.LogWarning("[DevToolsConsole] No GameManager assigned."); return; }
        var inventory = gameManager.Inventory;
        if (inventory == null) { Debug.LogWarning("[DevToolsConsole] No inventory assigned."); return; }
        inventory.ClearEquipmentOwned();
        GameEvents.InventoryChanged(inventory);
        Debug.Log("[DevToolsConsole] Equipment owned list cleared.");
    }
}
}
