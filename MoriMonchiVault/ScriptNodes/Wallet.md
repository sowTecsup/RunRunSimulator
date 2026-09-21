---
tags: [store, economy, static-service]
---

# Wallet

**Ruta:** `Systems/Store/Wallet.cs`

**Responsabilidad:** Puerta única de todas las operaciones monetarias. Métodos estáticos que consultan/modifican `GameManager.CurrentInventory` registrando el motivo en log y disparando `GameEvents.InventoryChanged` solo si hubo mutación. Nunca llama `SaveSystem` (eso lo hace [[GameManager]] al escuchar el evento).

**S128:** Introducida para centralizar y registrar auditar todas las transacciones. Reemplaza patrones dispersos de `inventory.Add()` / `inventory.TrySpend()` sin logs ni eventos.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Balance(Currency c)` | `int` | Lee saldo actual de la moneda; null inventory → 0 |
| `Add(Currency c, int amount, string reason)` | `void` | Suma cantidad (solo si `amount > 0`), registra log `[Wallet] +N C (reason)`, dispara `InventoryChanged` |
| `TrySpend(Currency c, int amount, string reason)` | `bool` | Intenta gastar; si saldo < amount → false; si éxito → registra log, dispara evento, retorna true |

## Flujo Típico

1. Sistema de gameplay llama `Wallet.TrySpend(Currency.Dabloons, 50, "buy_ring")`
2. Wallet consulta `CurrentInventory.Balance()`, valida saldo
3. Si ok: `inventory.TrySpend()`, log `[Wallet] -50 Dabloons (buy_ring) → 34`, dispara `GameEvents.InventoryChanged`
4. GameManager escucha evento → `SaveSystem.SaveInventory()` → `RequestPush()` a nube
5. Retorna true

**Invariante:** todas las mutaciones son atómicas (saldo valida, concede, cobra). [[StoreManager.BuyFurniture]] verifica saldo **antes**, concede al final, cobra **al final** → un solo evento.

## Integración

**Consumidores:** [[StoreManager]], [[AsyncBreedingService]], `ExpeditionBridge`, dev tools.

**No tiene estado propio:** todo vía `CurrentInventory` SO.

**Registro de auditoría:** debug log con reason; persiste via [[GameEvents.InventoryChanged]].

## Monedas Soportadas

```csharp
public enum Currency { Dabloons, Minerita }
```

**S128:** Dos monedas vigentes. `AdventureMaterial` migrada a `Minerita` en v1 → v2.

## Vinculado a

[[Index/28 - Cimientos y camino a Game Ready]] (§3 · dos monedas)
[[Index/29 - Plan HC - Cimientos (ejecutable)]] (§5 · C3 cartera)

**Conexiones:** [[GameManager]], [[PlayerInventorySO]], [[GameEvents]], [[StoreManager]]

