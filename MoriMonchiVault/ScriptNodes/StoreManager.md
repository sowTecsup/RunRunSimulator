---
tags: [script, store, transactions]
---

# StoreManager

**Ruta:** `Systems/Store/StoreManager.cs`

**Responsabilidad:** Orquestador de compras. Valida saldo vía [[Wallet]], stock, ownership. Muta inventario y dispara eventos. Crea `DeliveryBox` para entregas (props y cajas de criaturas). **S128:** ahora valida saldo con `Wallet.Balance()` y cobra con `Wallet.TrySpend()` (puerta única); orden de operaciones fija: comprueba saldo → concede mueble/prop → cobra al final (un solo evento de persistencia). **S130:** añade `BuyCreatureBox()` con flujo idéntico a props.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `BuyFurniture(FurnitureDefinitionSO def, StoreShopData shop)` | `BuyResult` | Compra mueble; valida stock/saldo/ownership, añade al inventario, cobra |
| `BuyWorldProp(ItemDefinitionSO def, StoreShopData shop)` | `BuyResult` | Compra prop; instancia `DeliveryBox`, spawna en punto, cobra |
| `BuyCreatureBox(CreatureBoxSO box, StoreShopData shop)` | `BuyResult` | Compra caja de criaturas; instancia `DeliveryBox`, configura caja, cobra (S130 NUEVO) |
| `RestockIfNeeded()` | `void` | Comprueba schedule en catálogo, recarga si aplica |

## BuyResult (enum)

- `Success` — transacción completada
- `OutOfStock` — no hay en stock o sistema no disponible
- `AlreadyOwned` — mueble ya poseído (furniture solo)
- `InsufficientFunds` — saldo insuficiente

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
7. Instancia DeliveryBox via SpawnDeliveryBox()
8. Configure(item)
9. Si price == 0: dispara InventoryChanged manualmente
```

**Nota:** Props cobran ANTES de instanciar (distinto de muebles); si falla al crear box → reembolso vía `Wallet.Add()`.

## Flujo BuyCreatureBox (S130 NUEVO)

```
1. Valida args (box, shop)
2. Valida stock (shop.InStock)
3. Valida inventario no-nulo
4. Calcula precio
5. Cobra primero (BuyResult si insuficiente)
6. TryConsume stock
7. Instancia DeliveryBox via SpawnDeliveryBox()
8. Configure(box)
9. Si price == 0: dispara InventoryChanged manualmente
```

**Identidad a BuyWorldProp:** cobro anterior a spawn, reembolso si falla.

## Helper SpawnDeliveryBox

```csharp
private DeliveryBox SpawnDeliveryBox(int price, StoreShopData shop)
{
    var go  = Instantiate(deliveryBoxPrefab, deliverySpawnPoint.position, rotation);
    var box = go.GetComponent<DeliveryBox>();
    if (box == null)
    {
        Destroy(go);
        if (price > 0) { Wallet.Add(price, "store-refund"); shop.CurrentStock++; }
        return null;
    }
    return box;
}
```

Centraliza validación y reembolso ante fallo de spawn.

## Referencias

| Referencia | Tipo | Uso |
|-----------|------|-----|
| `catalog` | `ShopCatalogSO` | Catálogo, precios finales, cálculo restock |
| `deliveryBoxPrefab` | `DeliveryBox` (prefab) | Instancia para props + cajas de criaturas |
| `deliverySpawnPoint` | `Transform` | Punto de spawn de cajas |

## Integración S128

- **Acceso a saldo:** `Wallet.Balance(Currency)` (no directo a SO)
- **Gasto:** `Wallet.TrySpend()` (registra en log, dispara evento automático)
- **Reembolsos:** `Wallet.Add()` si error post-gasto

## Vinculado a

[[Index/04 - Store & Transactions]]
[[Index/28 - Cimientos y camino a Game Ready]] (§3 · two currencies)
[[Index/29 - Plan HC - Cimientos (ejecutable)]] (§5 · C3 wallet)

**Conexiones:** [[Wallet]], [[GameManager]], [[PlayerInventorySO]], [[ShopCatalogSO]], [[StoreShopData]], [[DeliveryBox]], [[StorePanelUITK]], [[GameEvents]], [[CreatureBoxSO]]

