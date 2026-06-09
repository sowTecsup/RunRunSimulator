---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-09  
**Foco**: Sistema de Inventario completo — adquisición (tienda → caja), hotbar en play-mode, almacén de mundo, y browser de muebles en build-mode.

### Qué se hizo (esta sesión)

Sistema de inventario end-to-end diseñado con Opus y luego implementado paso a paso:

- **Capa de datos**: `ItemDefinitionSO`, `ItemDatabaseSO`, `PlayerInventorySO` (furnitureOwned F# + worldPropsStored I# + hotbarSlots[6]).
- **Persistencia**: `SaveSystem` ahora persiste también `FurnitureRegistrySO` (estaba faltando) y `PlayerInventorySO`. `CloudSyncService` los carga en sign-in.
- **Adquisición**: `DeliveryBox` (IInteractable, bifurca Furniture/WorldProp) + `StoreManager` (Odin TableList, bot ón Buy — sin economía, para testing).
- **Hotbar runtime**: `HotbarController` (singleton, pickup/use/throw/drop + scroll); `WorldPropInstance` (marker, IInteractable); `StorageContainer` (IInteractable + trigger auto-collect + Eject).
- **Interacción unificada**: tap E = pickup a hotbar; hold E = lanzar; Q = soltar; click = usar. MoriMonchiAgent mantiene grab físico propio (excepción explícita).
- **Input**: `PlayerInputs` agregó `HotbarScrolled` (wheel) + `DropPressed` (Q) leídos en Update. `BuildingInputs` agregó `BrowseToggled` (Tab).
- **Build browser**: `BuildBrowserUITK` standalone (NO UIManager — evita ExitBuildMode). `BuildModeController` agregó `SelectPieceFromBrowser`. `FurnitureService` agregó `SetActivePiece` + `runtimeActivePiece`.
- **UITK (3 paneles)**: `HotbarHUDUITK` (always-on), `StoragePanelUITK` (UIManager panel Storage=5), `BuildBrowserUITK` (standalone).

---

## Archivos nuevos creados en esta sesión

| Archivo | Tipo | Para qué |
|---------|------|----------|
| `Scripts/Data/ItemDefinitionSO.cs` | SO | Entrada de catálogo; Id I#, ItemType, WorldPropCategory, Prefab o FurnitureDef |
| `Scripts/Data/ItemDatabaseSO.cs` | SO | Dict I# → ItemDefinitionSO; Populate + Validate & Sync IDs |
| `Scripts/Data/PlayerInventorySO.cs` | SO | furnitureOwned (F#), worldPropsStored (I#), hotbarSlots[6] + InventoryData DTO |
| `Scripts/World/WorldPropInstance.cs` | MonoBehaviour | Marker en objetos world prop; ItemId + IsHeld; IInteractable → HotbarController.PickUp |
| `Scripts/World/HotbarController.cs` | MonoBehaviour | Singleton; gestiona 6 slots, PickUp/Use/Throw/Drop/Scroll, spawn visual en mano |
| `Scripts/Systems/Store/DeliveryBox.cs` | MonoBehaviour | IInteractable; Configure(ItemDefinitionSO); bifurca Furniture→furnitureOwned / WorldProp→spawn |
| `Scripts/Systems/Store/StoreManager.cs` | MonoBehaviour | TableList Odin de StoreEntry; Button BuyItem(index) → spawn DeliveryBox en deliverySpawnPoint |
| `Scripts/Systems/Store/StorageContainer.cs` | MonoBehaviour | Singleton; IInteractable → abre Storage UI; OnTriggerEnter captura WorldPropInstance; Eject(id) |
| `Scripts/UI/HotbarHUDUITK.cs` | MonoBehaviour | Always-on HUD; 6 slots procedurales; actualiza en OnHotbarChanged + OnInventoryReloaded |
| `Scripts/UI/StoragePanelUITK.cs` | MonoBehaviour | UIManager panel (Storage=5); lista worldPropsStored agrupada; IUINavigable ↑↓ + Submit ejecta |
| `Scripts/UI/BuildBrowserUITK.cs` | MonoBehaviour | Standalone overlay; Tab toggle; tabs por FurnitureCategory; ←→ piezas; Enter → SelectPieceFromBrowser |
| `UI Toolkit/HotbarHUDUITK.uxml` | UXML | Hotbar HUD (6 slots, picking-mode Ignore, bottom-center absolute) |
| `UI Toolkit/HotbarHUDUITKStyle.uss` | USS | .hotbar-slot 64×64 + .hotbar-slot--active (borde amarillo, scale 1.08) |
| `UI Toolkit/StoragePanelUITK.uxml` | UXML | Modal overlay; header + ScrollView "list" + empty label + close button |
| `UI Toolkit/StoragePanelUITKStyle.uss` | USS | .storage-row (flex-row) + .storage-row--selected (borde amarillo) |
| `UI Toolkit/BuildBrowserUITK.uxml` | UXML | Browser panel; "tabs" VisualElement + "pieces" ScrollView horizontal + "empty" Label |
| `UI Toolkit/BuildBrowserUITKStyle.uss` | USS | .browser-tab/--active + .browser-piece/--selected (96×96 cards) |

## Archivos modificados en esta sesión

| Archivo | Qué cambió |
|---------|-----------|
| `Core/Enums.cs` | `ItemType`, `WorldPropCategory`, `UIPanelType.Storage = 5` |
| `Core/GameEvents.cs` | `OnInventoryChanged` + `OnInventoryReloaded` + helpers |
| `Core/SaveSystem.cs` | `ScopedPath`, `SaveFurniture/LoadFurniture`, `SaveInventory/LoadInventory` |
| `Core/GameManager.cs` | Campos `furnitureRegistry` + `inventory`; suscripciones Furniture/Inventory; `CollectLooseWorldProps` en quit/pause |
| `Systems/Cloud/CloudSyncService.cs` | En sign-in: carga furniture + inventory, dispara Reloaded |
| `Systems/Furniture/FurnitureService.cs` | `runtimeActivePiece`, `SetActivePiece`, fallback en `ActivePiece` getter |
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
| **StoreManager** *(nuevo)* | `StoreManager` + lista de `StoreEntry` (item + quantity) + `deliveryBoxPrefab` + `deliverySpawnPoint` |
| **StorageContainer** *(nuevo)* | Collider sólido (grabMask) + collider trigger (zona captura) + `StorageContainer` → `database` + `ejectPoint` |
| **HotbarController** *(nuevo)* | `HotbarController` → `database` (ItemDatabaseSO) + `handAnchor` (mismo que holdAnchor) |

### 4 · UI (UIDocuments + controllers)

| Panel | UIDocument | Standalone/UIManager | Asignaciones del controller |
|-------|-----------|---------------------|----------------------------|
| **Hotbar HUD** | Siempre activo, `StandartPanelSettings` | Standalone (no mapear) | `database` (ItemDatabaseSO) |
| **Storage** | Puede ser inactivo | UIManager → `UIPanelType.Storage` | `document`, `database` |
| **Build Browser** | Puede ser inactivo | Standalone (no mapear) | `document`, `database` (FurnitureDatabaseSO), `buildMode` |

> ⚠️ El `BuildBrowserUITK` **NO** debe registrarse en el dict de UIManager — hacerlo activaría `OnUIFocusChanged` → `ExitBuildMode`.

### 5 · Flujo de prueba

```
StoreManager inspector → BuyItem(i)
  → DeliveryBox aparece en deliverySpawnPoint
    → tap E → [Furniture] entra a furnitureOwned F# ó [WorldProp] spawna en escena
      → tap E sobre WorldProp → va al hotbar (slot activo o primer libre)
        → wheel navega slots · hold E lanza · Q suelta · click usa
          → lanzar hacia StorageContainer → auto-captura
            → tap E sobre StorageContainer → abre Storage UI
              → botón Sacar (Q) → ejecta a escena
```

---

## Próximos pasos (retomar acá la próxima sesión)

**Pendientes de código no solicitados aún:**
- Play-mode use effects por `WorldPropCategory` (Food/Medicine aplicados a MoriMonchis vía `OnItemUsed`).
- Economía: `Wallet` + restricción de compra en `StoreManager` (Fase 3 real).
- Cloud sync de inventory + furniture (mismo patrón que criaturas en `CloudSyncService`).

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
