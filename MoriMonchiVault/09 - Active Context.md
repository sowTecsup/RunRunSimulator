---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-10 (sesión 3)
**Foco**: Tienda (Store) — parte visual UITK + desacople del modelo de datos comercial. Overlay genérico (fecha + tutorial de inputs).

### Qué se hizo (esta sesión)

**Refactor del modelo de datos (desacople item ↔ furniture ↔ comercio):**
- `ItemDefinitionSO` ahora es **exclusivamente WorldProp** — se le quitó `ItemType Type` y el puente `FurnitureDef`. El item y el furniture funcionan distinto (item: mundo/hotbar/almacén; furniture: catálogo/build mode) y ya no comparten una sola definición.
- `DeliveryBox` quedó **WorldProp-only** (sin `switch (item.Type)`, sin branch de furniture). Ya no dispara `InventoryChanged` (el prop queda suelto, no muta el inventario).
- `StoreManager` ya no lleva su lista inline `StoreEntry`; ahora referencia un `ShopCatalogSO` y expone `BuyFurniture(def)` (directo → `inventory.AddFurniture`, F#) y `BuyWorldProp(def)` (spawn `DeliveryBox`). Sin wallet aún: precio es **display-only**, comprar es gratis.

**Estructura comercial nueva (precio fuera de la definición):**
- `StoreShopData` (struct): `BasePrice`, `DiscountBase` (0–1), `DiscountDays` ([Flags] `DiscountDay`), `DiscountMonths` ([Flags] `DiscountMonth`), `TypeFilter` ([Flags] `StoreItemTypeFilter`), `Tags[]`. Helpers `IsDiscountActive(now)` / `FinalPrice(now)`. Regla: flag None en día/mes = "sin restricción en ese eje"; descuento activo si `DiscountBase>0` y ambos ejes ok.
- `ShopCatalogSO` (`SerializedScriptableObject`, uno por tienda): dos `[TableList]` tipados — `FurnitureListing` (FurnitureDef + StoreShopData) y `ItemListing` (ItemDef + StoreShopData). El precio vive acá, no en la definición → mismo item vendible en varias tiendas a precios distintos.

**Por qué dos databases (Furniture + Item) + StoreDatabase aparte:** consumers distintos, namespaces de id distintos (`F#`/`I#`), tipos de dato distintos; el ShopCatalog es el punto de composición que referencia ambos sin que se conozcan. Mergerlos sería falsa economía.

**UI nueva:**
- `StorePanelUITK` (UIManager panel `Store=6`): 3 tabs (Muebles / Objetos / Consumibles), derivados del catálogo (Furniture; Item con `WorldPropCategory.Tool`; Item con Food+Medicine). Cada fila: nombre + precio (tacha base + precio rebajado verde si hay descuento hoy) + botón Comprar. `IUINavigable`: ←→ tab, ↑↓ fila, Submit compra. Rebuild al abrirse (precio depende de la fecha).
- `InfoOverlayUITK` (HUD standalone, **no** en el dict de UIManager, picking-mode Ignore): fecha actual arriba-derecha (refresh 1×/seg, formateada en español) + leyenda de inputs arriba-izquierda (array `InputHint{Key,Action}` editable en inspector).

### Sesión anterior (2026-06-09, sesión 2) — resumen

UI scaling (hotbar/browser +40%) + bugfixes: `HotbarController.ThrowActive` fallback a `linearVelocity`; `FurnitureSpawner.OnReloaded` coroutine `yield return null`; `SpawnOne` isKinematic; `StorageContainer.Eject` `justEjectedId`. Detalle en git log de esa fecha.

---

## Archivos nuevos creados en esta sesión

| Archivo | Tipo | Para qué |
|---------|------|----------|
| `Scripts/Systems/Store/StoreShopData.cs` | struct | Datos comerciales de un listing: precio, descuento (flags día/mes), TypeFilter, tags. `IsDiscountActive`/`FinalPrice` |
| `Scripts/Systems/Store/ShopCatalogSO.cs` | SO | Catálogo de UNA tienda: `List<FurnitureListing>` + `List<ItemListing>` (def + StoreShopData). Uno por tienda |
| `Scripts/UI/StorePanelUITK.cs` | MonoBehaviour | UIManager panel (Store=6); 3 tabs; fila nombre+precio+Comprar; IUINavigable |
| `Scripts/UI/InfoOverlayUITK.cs` | MonoBehaviour | HUD standalone; fecha (arriba-der) + tutorial inputs (arriba-izq) |
| `UI Toolkit/StorePanelUITK.uxml` | UXML | header + "tabs" + ScrollView "list" + "empty" |
| `UI Toolkit/StorePanelUITKStyle.uss` | USS | .store-tab/--active + .store-row/--selected + .store-price__was/now/now--sale |
| `UI Toolkit/InfoOverlayUITK.uxml` | UXML | "hints" (arriba-izq) + "date" (arriba-der), picking-mode Ignore |
| `UI Toolkit/InfoOverlayUITKStyle.uss` | USS | .overlay-hints/.hint-row/.hint-key/.hint-action + .overlay-date |

## Archivos modificados en esta sesión

| Archivo | Qué cambió |
|---------|-----------|
| `Core/Enums.cs` | `ItemType`, `WorldPropCategory`, `UIPanelType.Storage = 5` |
| `Core/GameEvents.cs` | `OnInventoryChanged` + `OnInventoryReloaded` + helpers |
| `Core/SaveSystem.cs` | `ScopedPath`, `SaveFurniture/LoadFurniture`, `SaveInventory/LoadInventory` |
| `Core/GameManager.cs` | Campos `furnitureRegistry` + `inventory`; suscripciones Furniture/Inventory; `CollectLooseWorldProps` en quit/pause |
| `Systems/Cloud/CloudSyncService.cs` | En sign-in: carga furniture + inventory, dispara Reloaded |
| `Systems/Furniture/FurnitureService.cs` | `runtimeActivePiece`, `SetActivePiece`, fallback en `ActivePiece` getter |
| `Systems/Furniture/FurnitureDatabaseSO.cs` | Agregado `public IEnumerable<FurnitureDefinitionSO> All` para que el browser itere el catálogo completo |
| `Player/BuildingInputs.cs` | `BrowseToggled` ya no hardcodea `Keyboard.tabKey` — usa `Building.FurnitureCatalog.performed`; eliminado `Update()` y flag `building` |
| `UI/HotbarHUDUITK.cs` | Suscrito a `BuildModeController.OnBuildModeChanged`; se oculta (`DisplayStyle.None`) durante build mode |
| `UI/BuildBrowserUITK.cs` | Browser muestra catálogo completo (`database.All`) en vez de `furnitureOwned`; eliminada dependencia de `PlayerInventorySO` en esta clase |
| `Interactables/ThrowableObject.cs` | `OnThrow` ya no usa `AddForce` — setea `linearVelocity` directamente para evitar el bug de kinematic→dynamic en el mismo frame |
| `Systems/Furniture/BuildModeController.cs` | `StartPlacing` helper extraído; `SelectPieceFromBrowser(def)` público |
| `Player/PlayerController.cs` | `UpdateGrabHold` solo MoriMonchi física; click → `UseActive` si hotbar tiene item; `ComputeThrowImpulse` extraído |
| `Player/PlayerInputs.cs` | `HotbarScrolled` (wheel) + `DropPressed` (Q) en Update; `playerActive` flag |
| `Player/BuildingInputs.cs` | `BrowseToggled` (Tab) en Update; `building` flag |

---

## Setup pendiente en Unity (código ✅ — solo editor)

### 1 · Assets (crear en Project)

| Asset | Pasos |
|-------|-------|
| `ItemDatabase` SO | Clic der → Create → RunRunSimulator → Item Database |
| `ItemDefinition` × N | Uno por producto vendible. Furniture: asignar `FurnitureDef` (bridge F#). WorldProp: asignar `Prefab` + `Category`. |
| `PlayerInventory` SO | Create → RunRunSimulator → Player Inventory |
| **Validate & Sync IDs** | Abrir `ItemDatabase`, arrastrar defs al buffer → botón **Populate from Buffer** → **Validate & Sync IDs** |

### 2 · Prefabs (crear/modificar)

| Prefab | Componentes requeridos |
|--------|----------------------|
| **WorldProp** | Rigidbody + `ThrowableObject` + `WorldPropInstance` · layer del `grabMask` |
| **DeliveryBox** | Malla + collider sólido (grabMask) + `DeliveryBox` |
| **Furniture** | (ya existentes) — sin cambios |

### 3 · Objetos de escena

| GameObject | Componentes / asignaciones nuevas |
|-----------|----------------------------------|
| **GameManager** | Asignar campo `furnitureRegistry` (FurnitureRegistrySO) + `inventory` (PlayerInventorySO) |
| **StoreManager** *(cambió)* | `StoreManager` → `catalog` (**ShopCatalogSO**, ya NO lista inline) + `deliveryBoxPrefab` + `deliverySpawnPoint` |
| **StorageContainer** *(nuevo)* | Collider sólido (grabMask) + collider trigger (zona captura) + `StorageContainer` → `database` + `ejectPoint` |
| **HotbarController** *(nuevo)* | `HotbarController` → `database` (ItemDatabaseSO) + `handAnchor` (mismo que holdAnchor) |
| **Trigger de tienda** | Objeto con `PanelTrigger` (`panel = Store`) en el mostrador/computadora → tap E abre el StorePanel |

> ⚠️ **Asset nuevo `ShopCatalogSO`**: Create → RunRunSimulator → Shop Catalog. Llenar **Furniture for sale** (arrastrar FurnitureDef + precio/descuento) y **World props for sale** (arrastrar ItemDef + precio/descuento). Uno por tienda.
> ⚠️ Las **ItemDefinition** ya NO tienen opción Furniture (son WorldProp puro). El furniture se vende vía el catálogo de la tienda directo desde su `FurnitureDefinitionSO`.

### 4 · UI (UIDocuments + controllers)

| Panel | UIDocument | Standalone/UIManager | Asignaciones del controller |
|-------|-----------|---------------------|----------------------------|
| **Hotbar HUD** | Siempre activo, `StandartPanelSettings` | Standalone (no mapear) | `database` (ItemDatabaseSO) |
| **Storage** | Puede ser inactivo | UIManager → `UIPanelType.Storage` | `document`, `database` |
| **Store** *(nuevo)* | Puede ser inactivo | UIManager → `UIPanelType.Store` (**=6**) | `document`, `store` (StoreManager de escena) |
| **Info Overlay** *(nuevo)* | Siempre activo, picking-mode Ignore | Standalone (no mapear) | `document`, (opcional) editar `hints[]` |
| **Build Browser** | Puede ser inactivo | Standalone (no mapear) | `document`, `database` (FurnitureDatabaseSO), `buildMode` |

> ⚠️ El `BuildBrowserUITK` y el `InfoOverlayUITK` **NO** se registran en el dict de UIManager (no son panels focusables).
> El `StorePanelUITK` **SÍ** se mapea en el dict (`Store → su GameObject`), como Storage.

### 5 · Flujo de prueba

```
tap E sobre el trigger de tienda → abre StorePanel (UIManager)
  → ←→ cambia tab (Muebles / Objetos / Consumibles) · ↑↓ fila · Submit/Comprar
    → [Muebles]  → inventory.AddFurniture(F#) → aparece en el Build Browser
    → [Objetos / Consumibles] → DeliveryBox cae en deliverySpawnPoint
       → tap E → spawna el WorldProp en escena
         → tap E sobre WorldProp → hotbar · wheel navega · hold E lanza · Q suelta · click usa
           → lanzar hacia StorageContainer → auto-captura → tap E abre Storage → Sacar (Q) ejecta
```

---

## Próximos pasos (retomar acá la próxima sesión)

**Setup pendiente en Unity (código ✅ — solo editor):**
- **ShopCatalogSO**: Create → RunRunSimulator → Shop Catalog. Llenar `Furniture for sale` y `World props for sale` con defs + precios. Asignar al `StoreManager.catalog`.
- **StorePanel**: crear GameObject con UIDocument (apuntando a `StorePanelUITK.uxml`), agregar `StorePanelUITK`, asignar `store` (el StoreManager de escena). Mapear `Store → ese GameObject` en el dict del UIManager.
- **InfoOverlay**: crear GameObject con UIDocument (`InfoOverlayUITK.uxml`, picking-mode Ignore en el PanelSettings), agregar `InfoOverlayUITK`. **No** mapear en UIManager.
- **PanelTrigger de tienda**: añadir a un objeto del mostrador/computadora con `panel = Store`.
- `PlacementGrid` inspector → `floorMask` = **solo layer Floor**.
- `StorageContainer` inspector → `ejectPoint` fuera de la trigger zone (1-2m enfrente).
- Verificar prefabs de furniture: model children en layer `Furniture`, no en `Floor`.

**Pendientes de código (próxima sesión — tienda + economía):**
- **Cloud Save `PlayerData`**: push/pull de `PlayerInventorySO` (inventory + hotbar) y `FurnitureRegistrySO` en `CloudSyncService` — mismo patrón que criaturas. `CurrentStock` de `StoreShopData` también va acá.
- **Wallet**: campo `int Coins` en `PlayerInventorySO` (o `WalletSO` aparte) + restricción de compra en `StoreManager` (`TryConsume()` ya existe en `StoreShopData`).
- **Stock en UI**: `StorePanelUITK` debe mostrar stock restante y deshabilitar "Comprar" si `!InStock`.
- **Restock automático**: al abrir el panel (o al sign-in) comparar `DateTime.Now` con `IsRestockDay` y llamar `Restock()` si aplica.
- Play-mode use effects por `WorldPropCategory` (Food/Medicine aplicados a MoriMonchis vía `OnItemUsed`).

**Pendientes previos (combate / world — siguen vigentes):**
- Batalla instantánea: mostrar `"Instantánea"` en lugar del countdown en la Tab 3 del CombatPanel.
- Ordenar lista de Resultados (Tab 3) de más antiguo a más nuevo por `QueuedAt`.
- Redeploy cloud: `run-combat.js`, `process-matchmaking.js`, `get-queue-status.js`, `dequeue-combat.js`.
- Bloquear `TryLift` de un corral ocupado en `BuildModeController`/`FurnitureService`.
- Cablear `FlushToCloud()` en el logout de `CloudSyncService`.
- **NameTag**: UIDocument en hijo del prefab de criatura (`NameTagUITK.uxml`, WorldUIPanelSettings).
- **Estaciones** (`Feeder`/`RestZone`/`PlayZone`): hijos vacíos como use points en prefabs.

---

## Cómo usar esta nota en sesiones futuras

Cuando arranque una sesión nueva:
1. Leo este archivo primero (después del `CLAUDE.md`).
2. Borro lo de la sesión pasada y escribo qué estoy haciendo ahora.
3. Listo los 2-4 archivos del vault relevantes para esta sesión (no los leo todos).

Si el `Active Context` queda desactualizado (no se ha tocado en muchos días), tratarlo como **stale** — el código y los archivos del vault son autoritativos.
