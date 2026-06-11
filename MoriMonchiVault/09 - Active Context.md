---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-11 (sesión 5)
**Foco**: MoriMochiAgent — Petting system (follow cooldown + react cooldown + pet hint en NameTag + petting vía IInteractable sin raycast).

### Qué se hizo (esta sesión)

**Follow cooldown + React cooldown (MoriMochiAgent):**
- `followDuration` (10s, tab Movement): límite de tiempo en reacción amistosa → `EnterRoaming()` al expirar.
- `reactCooldown` (15s, tab Movement): tiempo de espera post-reacción. Se activa al expirar timer, al alejarse el jugador (reacción no-Flee), o al acariciar.
- `reactCooldownTimer` se descuenta en `Update()`; `ReactIfPlayerNear()` hace early-return mientras > 0.
- `reactingTimer` se resetea en `BeginReaction()` y en `RestoreNavMeshControl()` (pool cleanup).

**Petting (MoriMochiAgent implementa IInteractable):**
- `IsPlayerFacingMe()`: único check de orientación — `player.forward` (body yaw, ya horizontal) · `(creature − player).normalized` (XZ) `>= cos(petLookAngle)` y distancia `<= petRadius`. Sin cámara, sin forward de la criatura.
- `IsInFriendlyReaction` (public): estado Reacting con reacción no-Flee. NameTag lo usa para el primer filtro del hint (sin dot, sin flicker).
- `CanBePetted` (public): `IsInFriendlyReaction && IsPlayerFacingMe()`.
- `Interact()`: guard `CanBePetted` → `AddAffect(+20)` → `reactCooldownTimer` → `pettingDisplayTimer = 1.5f` → `onPet` UnityEvent → `EnterRoaming()`.
- `IsBeingPetted` (public): `pettingDisplayTimer > 0`.

**Pet hint en NameTag:**
- Label `pet-hint-label` añadida a `NameTagUITK.uxml` + estilo `.tag__pet-hint` en USS (amarillo, oculto por defecto).
- `NameTag.Refresh()`: muestra `"Petting..."` si `IsBeingPetted`; muestra `"[E] Acariciar"` si `IsInFriendlyReaction && IsPlayerFacingMe()`; oculta en cualquier otro caso.

**TryPetNearbyCreature (PlayerController):**
- `Physics.OverlapSphere(transform.position, grabRange, creatureLayer)` — sin raycast, los UIDocuments world-space del NameTag no pueden bloquear.
- Se llama primero en `OnInteractReleased()`, antes del raycast de IInteractable general.
- Campo `creatureLayer` (LayerMask) en el inspector del PlayerController → asignar layer "Creature".

**Inspector (tab Movement del agente):**
- Nuevos campos: `followDuration`, `reactCooldown`, `petRadius`, `petLookAngle`.
- Radios de personalidad como `[ShowInInspector, ReadOnly]`: `ProfileProximityRadius`, `ProfileRoamRadius`, `ProfileFollowDistance`.

### Sesión anterior (2026-06-10, sesión 4) — resumen

Tienda: Dabloons wallet, Cloud Save de inventario + furniture, StorePanelUITK (balance + stock + toast), ShopCatalogSO (descuento + restock desacoplados), server time anti-cheat (`get-server-time.js`), Dev Tools en GameManager. Detalle en git log.

---

## Archivos modificados esta sesión

| Archivo | Qué cambió |
|---------|-----------|
| `World/MoriMochiAgent.cs` | `IInteractable` · `followDuration/reactCooldown/petRadius/petLookAngle` (tab Movement) · `reactingTimer/reactCooldownTimer/pettingDisplayTimer` · `IsInFriendlyReaction/CanBePetted/IsBeingPetted` props públicas · `IsPlayerFacingMe()` (reemplaza IsFacingPlayer + IsPlayerLookingAt) · `Interact()` · cooldown en todos los exit paths de Reacting |
| `World/NameTag.cs` | `petHintLabel` · `Refresh()` muestra "Petting..." / "[E] Acariciar" / oculta según estado |
| `Player/PlayerController.cs` | `creatureLayer` (LayerMask) · `TryPetNearbyCreature()` (OverlapSphere, llamado antes del interact general) |
| `UI Toolkit/NameTagUITK.uxml` | Label `pet-hint-label` |
| `UI Toolkit/NameTagUITKStyle.uss` | `.tag__pet-hint` (amarillo, oculto por defecto) |

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

### Deploy pendiente (usuario)

```bash
ugs cloud-code modules deploy
```
→ Sube `get-server-time.js`. Sin esto `FetchServerTimeAsync` falla y cae al fallback de reloj local.

### Pendientes de código

- **CurrentStock en Cloud Save**: `StoreShopData.CurrentStock` no se persiste en cloud (volátil por sesión). Evaluar si es necesario antes de release.
- **Play-mode use effects**: `WorldPropCategory.Food`/`Medicine` → efecto en MoriMochi objetivo vía `OnItemUsed`. Etapa futura.
- **Batalla instantánea**: mostrar `"Instantánea"` en Tab 3 de CombatPanel.
- **Ordenar Resultados** (Tab 3) de más antiguo a más nuevo por `QueuedAt`.
- Redeploy cloud: `run-combat.js`, `process-matchmaking.js`, `get-queue-status.js`, `dequeue-combat.js`.
- Bloquear `TryLift` de corral ocupado en `BuildModeController`/`FurnitureService`.
- Cablear `FlushToCloud()` en el logout de `CloudSyncService`.

---

## Cómo usar esta nota en sesiones futuras

Cuando arranque una sesión nueva:
1. Leo este archivo primero (después del `CLAUDE.md`).
2. Borro lo de la sesión pasada y escribo qué estoy haciendo ahora.
3. Listo los 2-4 archivos del vault relevantes para esta sesión (no los leo todos).

Si el `Active Context` queda desactualizado (no se ha tocado en muchos días), tratarlo como **stale** — el código y los archivos del vault son autoritativos.
