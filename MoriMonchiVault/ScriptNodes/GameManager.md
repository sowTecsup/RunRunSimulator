---
tags: [script, core, singleton]
---

# GameManager

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Singleton que centraliza acceso a databases y registries. **Único orquestador de persistencia local:** escucha `GameEvents.OnRegistryChanged`, `OnFurnitureChanged`, `OnInventoryChanged` e invoca `SaveSystem` a disco. **S128:** Push agrupado a nube con `pushDelaySeconds` (default 5); se cancela y sube ya en quit/pause; `Time.unscaledTime` para que pausa no congele timer. **S129:** Propulsada por eventos `OnCreatureDeparted` indirectamente vía `RegistryChanged`. **S130:** `MintCreature()` sin evento automático — el caller dispara `RegistryChanged` tras mintear (patrón: multi-birth en `DeliveryBox.Interact()`).

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `PushToCloud()` | Dispara `cloudSync.PushAsync()` (fire-and-forget) |
| `FlushToCloudAsync()` | Guarda ALL a disco + espera push cloud (síncrono de persist) |
| `MintRandomCreature()` | Genera random, asigna género/elemento/rol/diales/nombre, registra, **dispara RegistryChanged** |
| `MintCreature()` | Genera random DNA, registra, retorna CreatureDNA — **NO dispara evento** (responsibility del caller) |
| `CollectLooseWorldProps()` | Busca en escena props sueltos (debug) |

## Propiedades Estáticas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Instance` | `GameManager` | Singleton; null si destroyed |
| `CurrentInventory` | `PlayerInventorySO` | Acceso rápido (nueva en S93) |
| `Now` | `DateTime` | Hora con offset servidor (CloudSyncService.ServerOffset) |

## Getters de Referencias (serializados)

- `Registry` — CreatureRegistrySO
- `Database` — CreatureDatabaseSO (Horns, Backs, Wings, Faces)
- `FurnitureRegistry` — FurnitureRegistrySO
- `Inventory` — PlayerInventorySO
- `FurTypeDatabase` — FurTypeDatabaseSO
- `RarityOddsTable` — RarityOddsTableSO
- `MonchiVisualBank` — MonchiVisualBankSO
- `RoleWorldProfiles` — RoleWorldProfileSO

## Persistencia S128+

**Push agrupado:**
1. Evento gameplay → `Persist()` / `PersistFurniture()` / `PersistInventory()`
2. Guarda a disco vía [[SaveSystem]]
3. `RequestPush()` → `pushPending = true`, `pushDeadline = Time.unscaledTime + pushDelaySeconds`
4. `Update()` → si `Time.unscaledTime >= pushDeadline` → `PushToCloud()`

**Flush forzado:**
- `OnApplicationQuit()` y `OnApplicationPause(paused: true)` → `FlushToCloudAsync()` (guarda a disco + espera push)
- Expedición retorno: [[ExpeditionBridge]] aplica `RegistryChanged` → GameManager persiste
- CreatureLifecycle.Kill/Adopt → RegistryChanged → persist

**Variación S128 vs S93:** Timer usa `Time.unscaledTime` (no `Time.time`), así pausa no bloquea el push.

## Integración S130: MintCreature sin evento

`MintCreature()` solo genera DNA, registra y retorna. **No dispara `RegistryChanged`**. El caller es responsable de dispararlo una sola vez tras múltiples minteos (ej. `DeliveryBox.Interact()` mintea `CreatureBox.Count` criaturas, luego dispara un único evento). Contrasta con `MintRandomCreature()` (botón dev) que sí dispara.

## Ciclo de Vida

1. `Awake()` → `Instance = this`
2. `OnEnable()` → Suscribe a 4 eventos (Registry/Furniture/Inventory + OnInventoryReloaded)
3. `OnInventoryReloaded()` → `GrantPlacedFurniture()` — rellena inventario con muebles colocados
4. Gameplay → eventos → `Persist()` (SaveSystem + RequestPush)
5. `Update()` → si deadline vencido → push async
6. Quit/Pause → `FlushToCloudAsync()`
7. `OnDestroy()` → Limpia `Instance` si es el mismo

## Invariantes S128+

- Singleton: Awake crea, OnDestroy limpia si es el mismo
- No disparador de eventos: solo consumidor de persistencia
- Acceso centralizado: todas las referencias por getter, no SerializeField directo
- Persistencia order: Disco (SaveSystem) → Nube (PushAsync) — nunca inversión
- Push agrupado: múltiples mutaciones → un solo push en 5 s
- Sin escala de tiempo: push no se congela con pausa
- S129: Indirecto vía RegistryChanged cuando CreatureLifecycle mata/vende
- S130: GrantPlacedFurniture llena inventario local de muebles ya colocados en el mundo (evita duplicados, mantiene SSOT en furniture registry)

## Vinculado a

[[Index/07 - Persistence & Identity]]
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[CloudSyncService]], [[CreatureGenerator]], [[GameEvents]], [[SaveSystem]], [[CreatureLifecycle]], [[DeliveryBox]]

