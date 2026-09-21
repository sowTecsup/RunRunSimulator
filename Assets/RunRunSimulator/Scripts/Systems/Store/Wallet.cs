using UnityEngine;
namespace MoriMonchiSimulator
{

public static class Wallet
{
    public static int Balance(Currency c)
    {
        var inventory = GameManager.CurrentInventory;
        return inventory != null ? inventory.Balance(c) : 0;
    }

    public static void Add(Currency c, int amount, string reason)
    {
        var inventory = GameManager.CurrentInventory;
        if (inventory == null) { Debug.LogWarning($"[Wallet] no inventory, cannot add {amount} {c} ({reason})"); return; }
        if (amount <= 0) return;
        inventory.Add(c, amount);
        Debug.Log($"[Wallet] +{amount} {c} ({reason}) → {inventory.Balance(c)}");
        GameEvents.InventoryChanged(inventory);
    }

    public static bool TrySpend(Currency c, int amount, string reason)
    {
        var inventory = GameManager.CurrentInventory;
        if (inventory == null) { Debug.LogWarning($"[Wallet] no inventory, cannot spend {amount} {c} ({reason})"); return false; }
        if (!inventory.TrySpend(c, amount)) return false;
        Debug.Log($"[Wallet] -{amount} {c} ({reason}) → {inventory.Balance(c)}");
        GameEvents.InventoryChanged(inventory);
        return true;
    }
}
}
