---
tags: [cloud, authentication, service]
---

# CloudSyncService

**Ruta:** `Systems/Cloud/CloudSyncService.cs`

**Responsabilidad:** Orquestador de nube (autenticación + sincronización). Expone métodos públicos para push/pull/reset. En `Start()` inicializa `CloudAuth` (autenticación UGS) y `CloudSyncOps` (reconciliación). Al autenticarse, carga datos locales scoped y dispara startup sync. Propiedad `StartupSyncDone` para aguardar sincronización.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `InitializeAsync()` | `Task` | Inicia autenticación UGS |
| `PushAsync()` | `Task` | Sube 4 claves a nube |
| `PullAsync()` | `Task` | Baja 4 claves (no recomendado post-S128) |
| `SyncOnStartupAsync()` | `Task` | Reconciliae nube vs local sin perder datos |
| `ResetProgressAsync()` | `Task` | Borra TODOS datos nube (debug) |
| `UpdatePlayerNameAsync(string)` | `Task` | Actualiza nombre de jugador en UGS |

## Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `StartupSyncDone` | `bool` | True si startup sync completó (o sin sesión) |
| `ServerOffset` | `TimeSpan` | Offset UTC servidor para hora sincronizada |

## Ciclo de Vida

1. `Start()` → Obtiene referencias (registry, furniture, inventory) desde `GameManager`
2. `Start()` → Crea `CloudAuth` y `CloudSyncOps`
3. `Start()` → `auth.InitializeAsync()` (UI UGS o anónimo)
4. Auth success → `HandleSignedInAsync(method)`
   - Setea `SaveSystem.SetUserScope(PlayerID)`
   - Carga datos locales scoped vía `SaveSystem.LoadInto()` / `LoadFurniture()` / `LoadInventory()` / `LoadSocialGraph()`
   - Dispara `GameEvents.RegistryReloaded` / `FurnitureReloaded` / `InventoryReloaded`
   - Sincroniza nube vía `syncOps.SyncOnStartupAsync()`
   - Setea `StartupSyncDone = true`
5. Auth fail → `StartupSyncDone = true` (permite continuar sin nube)
6. `OnDestroy()` → `auth.Teardown()` (cleanup UGS)

## Integración S128

**SyncOnStartupAsync():** El arranque ahora es inteligente. En lugar de pull ciego (que sobrescribía todo), [[CloudSyncOps]] decide:
- Nube vacía → sube local
- Al día → nada
- Otra PC subió, local sin cambios → baja
- Conflicto → respaldo + gana el más nuevo

**Claves nuevas (S128):** se agregó `socialgraph` a la sincronización.

## DisplayDebug (Odin)

- `PlayerID`, `PlayerName`, `SignedIn`, `AuthMethod` — desde `CloudAuth`
- `LastPullDisplay`, `LastKnownCloudDisplay`, `SecurityStatus` — desde `CloudSyncOps`
- `ServerTimeOffset` — offset UTC

## Vinculado a

[[Index/07 - Persistence & Identity]] (S128 sección)

**Conexiones:** [[CloudAuth]], [[CloudSyncOps]], [[SaveSystem]], [[GameManager]], [[CreatureRegistrySO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[GameEvents]]

