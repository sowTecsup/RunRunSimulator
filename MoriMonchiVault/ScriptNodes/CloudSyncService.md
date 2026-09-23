---
tags: [cloud, authentication, service]
---

# CloudSyncService

**Ruta:** `Systems/Cloud/CloudSyncService.cs`

**Responsabilidad:** Orquestador de nube (autenticación + sincronización). Expone métodos públicos para push/pull/reset. En `Start()` inicializa `CloudAuth` (autenticación UGS) y `CloudSyncOps` (reconciliación). Al autenticarse, carga datos locales scoped y dispara startup sync. Propiedad `StartupSyncDone` para aguardar sincronización. S131: Integra `WorldStateSO` para sincronización de estado de mundo.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `InitializeAsync()` | `Task` | Inicia autenticación UGS |
| `PushAsync()` | `Task` | Sube 5 claves a nube (registry, furniture, inventory, social, world state) |
| `PullAsync()` | `Task` | Baja 5 claves (no recomendado post-S128) |
| `SyncOnStartupAsync()` | `Task` | Reconcilia nube vs local (5 archivos) |
| `ResetProgressAsync()` | `Task` | Borra TODOS datos nube (debug) |
| `UpdatePlayerNameAsync(string)` | `Task` | Actualiza nombre de jugador en UGS |

## Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `StartupSyncDone` | `bool` | True si startup sync completó (o sin sesión) |
| `ServerOffset` | `TimeSpan` | Offset UTC servidor para hora sincronizada |

## Ciclo de Vida

1. `Start()` → Obtiene referencias desde `GameManager`:
   - registry, furniture, inventory, **worldState** (S131)
2. `Start()` → Crea `CloudAuth` y `CloudSyncOps` (pasando worldState)
3. `Start()` → `auth.InitializeAsync()` (UI UGS o anónimo)
4. Auth success → `HandleSignedInAsync(method)`
   - Setea `SaveSystem.SetUserScope(PlayerID)`
   - Carga datos locales scoped:
     - `SaveSystem.LoadInto(registry)` → dispara `RegistryReloaded`
     - `SaveSystem.LoadFurniture(furnitureRegistry)` → dispara `FurnitureReloaded`
     - `SaveSystem.LoadInventory(inventory)` → dispara `InventoryReloaded`
     - **`SaveSystem.LoadWorldState(worldState)` → dispara `WorldStateReloaded`** (S131)
     - `SaveSystem.LoadSocialGraph(registry)` (sin evento)
   - Sincroniza nube vía `syncOps.SyncOnStartupAsync()`
   - Setea `StartupSyncDone = true`
5. Auth fail → `StartupSyncDone = true` (permite continuar sin nube)
6. `OnDestroy()` → `auth.Teardown()` (cleanup UGS)

## Cambios S131

**Agregado:**
- `private WorldStateSO worldState;` (resolución en Start)
- Pasado a `CloudSyncOps` constructor: `new CloudSyncOps(auth, registry, furnitureRegistry, inventory, worldState, s => status = s)`
- `HandleSignedInAsync()` ahora carga y dispara `WorldStateReloaded`

**Propósito:** Sincronización de estado de mundo (día/minuto) con cloud, integrado al bootstrap.

## Integración S128/S131

**SyncOnStartupAsync():** Reconciliación inteligente. En lugar de pull ciego:
- Nube vacía → sube local (5 archivos: registry, furniture, inventory, social, **world state**)
- Al día → nada
- Otra PC subió → baja, dispara Reloaded events
- Conflicto → respaldo + gana el más nuevo

**Claves sincronizadas:**
- `creature_database` (registry)
- `furniture_registry` (muebles)
- `player_inventory` (cartera)
- `social_graph` (relaciones)
- **`world_state`** (S131 nuevo)

## Referenceencías Resueltas en Start

```csharp
registry          = GameManager.Instance.Registry;
furnitureRegistry = GameManager.Instance.FurnitureRegistry;
inventory         = GameManager.Instance.Inventory;
worldState        = GameManager.Instance.WorldState;  // S131
auth = new CloudAuth(...);
syncOps = new CloudSyncOps(auth, registry, furnitureRegistry, inventory, worldState, ...);
```

## DisplayDebug (Odin)

- `PlayerID`, `PlayerName`, `SignedIn`, `AuthMethod` — desde `CloudAuth`
- `LastPullDisplay`, `LastKnownCloudDisplay`, `SecurityStatus` — desde `CloudSyncOps`
- `ServerTimeOffset` — offset UTC

## Invariantes S131

- `worldState` puede ser null (fallback graceful en CloudSyncOps)
- `LoadWorldState()` null-safe (devuelve estado default si no existe)
- `WorldStateReloaded` dispara tras carga (permite GameClock actualizar)

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[Index/09 - Active Context]]

## Conexiones

**Cloud:**
- [[CloudAuth]] — autenticación UGS
- [[CloudSyncOps]] — lógica de reconciliación (S131: maneja 5 claves)

**Data:**
- [[CreatureRegistrySO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[WorldStateSO]]

**Sistemas:**
- [[SaveSystem]] — carga/serialización
- [[GameManager]] — proporciona referencias
- [[GameClock]] — reacciona a WorldStateReloaded
- [[GameEvents]] — dispara Reloaded events

## Notas (S131 HC-4)

- **Bootstrap ordering:** CloudSyncService.Start() resuelve worldState antes de sync, permitiendo que GameClock se inyecte con estado precargado.
- **Reconciliación:** 5 archivos ahora (era 4), pero lógica de merge sin cambios.
- **Event chain:** LoadWorldState → WorldStateReloaded → GameClock.HandleWorldStateReloaded → DayBlockChanged.
