---
tags: [script, core, singleton]
---

# GameManager

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Singleton que centraliza acceso a databases y registries. **Único orquestador de persistencia local:** escucha `GameEvents.OnRegistryChanged`, `OnFurnitureChanged`, `OnInventoryChanged`, `OnWorldStateChanged` e invoca `SaveSystem` a disco. S128: Push agrupado a nube con `pushDelaySeconds` (default 5); se cancela y sube ya en quit/pause; `Time.unscaledTime` para que pausa no congele timer. S131: Agregado `WorldStateSO` y `PersistWorldState()` para sincronización de estado de mundo.

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
| `CurrentInventory` | `PlayerInventorySO` | Acceso rápido |

## Getters de Referencias (serializados)

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Registry` | `CreatureRegistrySO` | Registro de criaturas vivas/muertas |
| `Database` | `CreatureDatabaseSO` | Bases de partes (Horns, Backs, Wings, Faces) |
| `FurnitureRegistry` | `FurnitureRegistrySO` | Muebles colocados |
| `Inventory` | `PlayerInventorySO` | Cartera y objetos del jugador |
| `FurTypeDatabase` | `FurTypeDatabaseSO` | Tipos de pelaje |
| `RarityOddsTable` | `RarityOddsTableSO` | Odds de rareza |
| `MonchiVisualBank` | `MonchiVisualBankSO` | Banco de visuals |
| `RoleWorldProfiles` | `RoleWorldProfileSO` | Perfiles de rol para arena |
| `WorldState` | `WorldStateSO` | **(S131)** Estado del mundo (día, minuto, tutorial) |

## Persistencia S128+ + S131

**Push agrupado:**
1. Evento gameplay → `Persist()` / `PersistFurniture()` / `PersistInventory()` / `PersistWorldState()` (S131)
2. Guarda a disco vía [[SaveSystem]]
3. `RequestPush()` → `pushPending = true`, `pushDeadline = Time.unscaledTime + pushDelaySeconds`
4. `Update()` → si `Time.unscaledTime >= pushDeadline` → `PushToCloud()`

**Suscriptores de eventos (OnEnable):**
```csharp
GameEvents.OnRegistryChanged  += Persist;
GameEvents.OnFurnitureChanged += PersistFurniture;
GameEvents.OnInventoryChanged += PersistInventory;
GameEvents.OnInventoryReloaded += GrantPlacedFurniture;
GameEvents.OnWorldStateChanged += PersistWorldState;  // S131
```

**PersistWorldState (S131):**
```csharp
private void PersistWorldState(WorldStateSO state)
{
    SaveSystem.SaveWorldState(state);
    RequestPush();
}
```

**Flush forzado:**
- `OnApplicationQuit()` y `OnApplicationPause(paused: true)` → `FlushToCloudAsync()` (guarda a disco + espera push)
- Expedición retorno: [[ExpeditionBridge]] aplica `RegistryChanged` → GameManager persiste
- CreatureLifecycle.Kill/Adopt → RegistryChanged → persist

**Variación S128 vs S93:** Timer usa `Time.unscaledTime` (no `Time.time`), así pausa no bloquea el push.

## Ciclo de Vida

1. `Awake()` → `Instance = this`
2. `OnEnable()` → Suscribe a 5 eventos (Registry/Furniture/Inventory/WorldState + OnInventoryReloaded)
3. `OnInventoryReloaded()` → `GrantPlacedFurniture()` — rellena inventario con muebles colocados
4. Gameplay → eventos → `Persist()` (SaveSystem + RequestPush)
5. `Update()` → si deadline vencido → push async
6. Quit/Pause → `FlushToCloudAsync()`
7. `OnDestroy()` → Limpia `Instance` si es el mismo

## Cambios S131

**Agregado:**
- `worldState` field (serializado, AssetsOnly, required)
- `WorldState` getter property
- `PersistWorldState(WorldStateSO state)` método privado
- Suscripción a `GameEvents.OnWorldStateChanged`
- `SaveSystem.SaveWorldState(worldState)` en FlushToCloudAsync

**Propósito:** Sincronizar estado de mundo (día/minuto) con persistencia y cloud. Ahora Day/MinuteOfDay mutados por GameClock disparan `WorldStateChanged` → autoguardado sin gameplay code adicional.

## Invariantes S128+ / S131

- Singleton: Awake crea, OnDestroy limpia si es el mismo
- No disparador de eventos: solo consumidor de persistencia
- Acceso centralizado: todas las referencias por getter, no SerializeField directo
- Persistencia order: Disco (SaveSystem) → Nube (PushAsync) — nunca inversión
- Push agrupado: múltiples mutaciones → un solo push en 5 s
- Sin escala de tiempo: push no se congela con pausa
- WorldState integrado: estado de mundo unificado y persistido automáticamente

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[Index/28 - Cimientos y camino a Game Ready]]
- [[Index/09 - Active Context]]

## Conexiones

**Data:**
- [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[WorldStateSO]]

**Sistemas:**
- [[CloudSyncService]] — push async
- [[GameClock]] — dispara WorldStateChanged
- [[CreatureGenerator]] — mint
- [[GameEvents]] — bus de eventos
- [[SaveSystem]] — persistencia a disco
- [[CreatureLifecycle]] — vía RegistryChanged
- [[DeliveryBox]] — vía RegistryChanged

## Notas (S131 HC-4)

- **WorldState ownership:** GameManager es propietario; GameClock muta, eventos disparan, GameManager persiste.
- **Bootstrap:** SaveSystem.LoadWorldState() debe llamarse durante init de escena antes de que GameClock load.
- **FlushToCloudAsync:** Ahora también guarda world state (línea `SaveSystem.SaveWorldState(worldState)`).
