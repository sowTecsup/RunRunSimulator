---
tags: [script, store, transactions]
---

# StoreManager

**Ruta:** `Systems/Store/StoreManager.cs`

**Responsabilidad:** Orquestador de compras. Valida saldo vía [[Wallet]], stock, ownership. Muta inventario y dispara eventos. Crea `DeliveryBox` para entregas. **S128:** ahora valida saldo con `Wallet.Balance()` y cobra con `Wallet.TrySpend()` (puerta única); orden de operaciones fija: comprueba saldo → concede mueble/prop → cobra al final (un solo evento de persistencia).

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `BuyFurniture(FurnitureDefinitionSO def, StoreShopData shop)` | `BuyResult` | Compra mueble; valida stock/saldo/ownership, añade al inventario, cobra |
| `BuyWorldProp(ItemDefinitionSO def, StoreShopData shop)` | `BuyResult` | Compra prop; instancia `DeliveryBox`, spawna en punto, cobra |

## Flujo BuyFurniture (S128)

```
1. Valida args (def, shop)
2. Valida stock (shop.InStock)
3. Valida inventario no-nulo
4. Valida no duplicado (HasFurniture)
5. Calcula precio final (ShopCatalogSO)
6. Valida saldo: Wallet.Balance(Dabloons) >= price
7. TryConsume stock
8. AddFurniture
9. Cobra: Wallet.TrySpend(Dabloons, price)
   SI price == 0: dispara InventoryChanged manualmente
```

**Invariante S128:** saldo validado ANTES, concedido EN MEDIO, cobrado AL FINAL → un solo evento.

## Flujo BuyWorldProp (S128)

```
1. Valida args + delivery system (prefab, spawn point)
2. Valida Application.isPlaying
3. Valida inventario no-nulo
4. Calcula precio
5. Cobra primero (BuyResult si insuficiente)
6. TryConsume stock
7. Instancia DeliveryBox
8. Configure prop
9. Si price == 0: dispara InventoryChanged manualmente
```

**Nota:** Props cobran ANTES de instanciar (distinto de muebles); si falla al crear box → reembolso vía `Wallet.Add()`.

## BuyResult (enum)

- `Success` — transacción completada
- `OutOfStock` — no hay en stock o sistema no disponible
- `AlreadyOwned` — mueble ya poseído (furniture solo)
- `InsufficientFunds` — saldo insuficiente

## Referencias

| Referencia | Tipo | Uso |
|-----------|------|-----|
| `catalog` | `ShopCatalogSO` | Catálogo, precios finales, cálculo restock |
| `deliveryBoxPrefab` | `DeliveryBox` (prefab) | Instancia para props del mundo |
| `deliverySpawnPoint` | `Transform` | Punto de spawn de cajas |

## Métodos Helper

- Precio: `catalog.FinalPrice(shop, GameManager.Now)` → aplica descuentos
- Stock: `shop.InStock` (propiedad booleana) y `shop.TryConsume()`

## Integración S128

- **Acceso a saldo:** `Wallet.Balance(Currency)` (no directo a SO)
- **Gasto:** `Wallet.TrySpend()` (registra en log, dispara evento automático)
- **Reembolsos:** `Wallet.Add()` si error post-gasto

## Vinculado a

[[Index/04 - Store & Transactions]]
[[Index/28 - Cimientos y camino a Game Ready]] (§3 · two currencies)
[[Index/29 - Plan HC - Cimientos (ejecutable)]] (§5 · C3 wallet)

**Conexiones:** [[Wallet]], [[GameManager]], [[PlayerInventorySO]], [[ShopCatalogSO]], [[StoreShopData]], [[DeliveryBox]], [[StorePanelUITK]], [[GameEvents]]

