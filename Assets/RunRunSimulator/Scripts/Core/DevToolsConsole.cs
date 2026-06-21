using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class DevToolsConsole : MonoBehaviour
{
    // ── Setup ─────────────────────────────────────────────────────

    [BoxGroup("Setup"), Required]
    [SerializeField] private GameManager gameManager;

    // ── Dev Tools ─────────────────────────────────────────────────

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
}
}
