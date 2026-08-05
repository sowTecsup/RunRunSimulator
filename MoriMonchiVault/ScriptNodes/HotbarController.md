---
tags: [script, world, props]
---

# HotbarController.cs

**Ruta:** `World/Props/HotbarController.cs`

**Responsabilidad:** Hotbar 6-slots en modo play. Pickup de `WorldPropInstance`, uso, throw, drop. Singleton `Instance` + eventos estáticos. Persiste slots en `PlayerInventorySO.hotbarSlots`. Mantiene visual en mano (`heldVisual`) para el slot activo, re-spawneado en load. S69: método `TryConsumeActiveFood() → bool` (si hotbar activo es Food, quita del slot, destruye visual en mano, emite `OnHotbarChanged` + `InventoryChanged`, NO dispara `OnItemUsed`). Consumo por AgentBrain.TickHandFeed() para comida de la mano.

## Estructura

**Slots (0–5):**
- Almacenados en `PlayerInventorySO.hotbarSlots` (string[6], ids "I#" o null)
- Un slot activo (default 0)
- Visual en mano cuando ocupado (`heldVisual`, GameObject spawneado de prefab)

**Singleton:**
- `HotbarController.Instance` — acceso global
- Espera a que `GameManager.Inventory` (PlayerInventorySO) cargue, luego `EquipActive()` re-spawnea el visual

## Propiedades Públicas

- `ActiveSlot` (int, read-only) — slot activo actual (0–5)
- `ActiveItemId` (string, read-only) — id "I#" del slot activo, null si vacío
- `HasActiveItem` (bool, read-only) — true si `ActiveItemId` no es null
- `IsOfferingFood` (bool, read-only) — true si slot activo contiene ítem con `ItemDefinitionSO.Category == WorldPropCategory.Food`

## Métodos Públicos

**Gestión de slot:**
- `ScrollActive(int dir)` — rueda del mouse; cambia slot activo con wrap-around (0→5, 5→0)
- `SetActiveSlot(int index)` — establece slot activo explícitamente, re-spawnea el visual, emite `OnHotbarChanged`

**Pickup (tap E en prop suelto):**
- `PickUp(WorldPropInstance prop) → bool` — almacena id en primer slot libre (preferencia: slot activo), **destruye el GameObject del prop**, emite `InventoryChanged` + `OnHotbarChanged`. Retorna true si éxito, false si hotbar lleno.

**Uso:**
- `UseActive()` — emite `OnItemUsed` con id del slot activo. El efecto específico (comer, sanar, etc.) es wired por listeners (AgentBrain, etc.), **no aquí**.
- `TryConsumeActiveFood() → bool` — **S69, core food handFeed (verificado E2E en S70)**:
  1. Si `!IsOfferingFood`, retorna false
  2. Limpia slot activo en inventario
  3. Destruye `heldVisual`
  4. Emite `InventoryChanged` + `OnHotbarChanged`
  5. Retorna true
  6. **NO dispara** `OnItemUsed` (que es para equipo; food es consumo directo)

**Throw (hold E) / Drop (Q):**
- `ThrowActive(Vector3 force) → bool` — suelta item activo al mundo con impulso. Busca `IThrowable`; sino, aplica impulso al Rigidbody. Limpia slot + emite `InventoryChanged` + `OnHotbarChanged`. Retorna false si no hay item o inventario.
- `DropActive() → bool` — suelta item activo al mundo sin impulso (cae a los pies). Limpia slot + emite eventos. Retorna false si no hay item.

## Eventos Estáticos

- `OnHotbarChanged` (Action) — emitido tras cambio de contenido, slot activo, o pickup/drop
- `OnItemUsed` (Action<string>) — emitido al usar item (UseActive), transporta id "I#"

## Campos Privados Significativos

- `activeSlot` (int) — slot actual (0–5)
- `heldVisual` (GameObject) — visual spawneado en mano, kinematic + colliders off mientras esté aquí
- `database` (ItemDatabaseSO, serialized) — ref para lookups de definiciones
- `handAnchor` (Transform, serialized) — donde se spawneatea el visual en mano

## Ciclo de Vida

1. **Awake:** `Instance = this`
2. **OnEnable:** suscribe a `GameEvents.OnInventoryReloaded`, `PlayerInputs.HotbarScrolled`, `PlayerInputs.DropPressed`
3. **OnInventoryReloaded:** (fires after sign-in) `EquipActive()` re-spawneatea visual para slot activo, `OnHotbarChanged()`
4. **OnDisable:** desuscribe eventos

## Persistencia

- Slots: `PlayerInventorySO.hotbarSlots` (string[6]) — persisten entre sesiones
- Visual: re-spawneado en Awake via `OnInventoryReloaded`

## Notas de Implementación

- `FirstFreeSlot()` busca preferred (activeSlot si vacío, else primer vacío, else -1)
- `ReleaseActiveIntoWorld()` es helper shared por throw+drop: limpia slot + despadre visual + emite eventos
- `EquipActive()` es privada; spawneatea visual desde prefab, configura WorldPropInstance.IsHeld
- `HoldInHand()` / `ReleaseHeld()` son helpers para flip rigidbody + colliders
- `IsOfferingFood` es check barato (lookup id → def → Category == Food)
- `TryConsumeActiveFood()` es idempotent: sin comida activa = false sin efectos

## Vinculado a

[[Index/06 - Player & World]]

## Conexiones

[[WorldPropInstance]], [[ItemDefinitionSO]], [[ItemDatabaseSO]], [[PlayerInventorySO]], [[GameManager]], [[GameEvents]], [[PlayerInputs]], [[AgentBrain]], [[HotbarHUDUITK]], [[UIManager]]
