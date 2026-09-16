---
tags: [script, system, expedition, bridge]
---

# ExpeditionBridge.cs

**Ruta:** `Systems/Expedition/ExpeditionBridge.cs`

**Responsabilidad:** Componente MonoBehaviour que vive en GameScene (tienda) y orquesta transiciones a/desde expedición. Expone evento estático OnDepartureRequested y método Depart() para iniciar viaje a arena con equipo elegido. Al retornar, espera a que CloudSyncService inicialice (StartupSyncDone) y aplica rewards: suma material al inventario y aplica delta vida a criaturas (del HealthById). S119: complemento de ExpeditionHandoff. S120: maneja RequestDeparture(ids). **S124:** Reemplaza lógica de energía con delta de vida; agrega procesamiento de HealthById, Floors, Fallen, Lost.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `cloudSync` | `CloudSyncService` | Referencia a sincronización en la nube (para esperar StartupSyncDone) |
| `syncTimeoutSeconds` | `float` | Timeout máximo esperando inicio cloud (default 20s) |
| `departFlushTimeout` | `float` | Timeout flush local antes de arena (default 5s) |
| `permadeathEnabled` | `bool` | Si true, marca IsDead si Lost o health <= 0 (default false) |

## Evento Estático (S120)

```csharp
public static event Action<IReadOnlyList<string>> OnDepartureRequested;
public static void RequestDeparture(IReadOnlyList<string> ids) => OnDepartureRequested?.Invoke(ids);
```

Disparado por ExpeditionPanelUITK al hacer clic "Ir" con IDs de criaturas elegidas.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `void Depart()` | [Button] Dev; ejecuta Depart(null) |
| `void Depart(IReadOnlyList<string> ids)` | Inicia DepartRoutine: flush cloud (timeout), desbloquea cursor, dispara ExpeditionHandoff.GoToArena(ids) |

## Ciclo de Vida

### OnEnable/OnDisable

- OnEnable: suscribe a OnDepartureRequested para recibir Depart(ids)
- OnDisable: desuscribe

### Start

- Si ExpeditionHandoff.HasResult: inicia ApplyResult() coroutine

### DepartRoutine

```
1. Si GameManager existe: FlushToCloudAsync() con timeout (5s)
2. Desbloquea cursor (visible=true, lockState=None)
3. ExpeditionHandoff.GoToArena(ids)
```

### ApplyResult (Coroutine, S124 MODIFICADO)

```
1. Esperar que cloudSync.StartupSyncDone (max syncTimeoutSeconds)
2. TryConsumeResult() — lee ExpeditionResult y limpia flags
3. Material = 0 si result.Lost, else result.PlayerSecured
4. Si inventario presente y material > 0:
   - AddAdventureMaterial(material)
   - GameEvents.InventoryChanged(inventory)
5. Si registry presente y result.HealthById ≠ null:
   - Por cada (id, delta) en HealthById:
     - Si criatura existe y no IsDead:
       - dna.Needs.AddHealth(delta)  [delta negativo = daño]
       - Acumular net = suma deltas, creatures = count
   - Si hubo cambios: GameEvents.RegistryChanged(registry)
6. Si permadeathEnabled: marca IsDead = true si Lost OR final health <= 0
7. Dispara GameEvents.ExpeditionReturned(expReturn) con:
   - HealthLost = -net (negativo de suma de deltas)
   - Fallen = result.Fallen
   - Floors = result.Floors
   - Lost = result.Lost
8. Debug.Log con piso, perdida, material, vida
```

## Struct ExpeditionReturn (S124)

```csharp
new ExpeditionReturn
{
    Seed = result.Seed,
    Winner = result.Winner,
    PlayerSecured = material,  // 0 si Lost
    RivalSecured = result.RivalSecured,
    MaterialGained = material,
    HealthLost = -net,  // suma de delta vida (negativo = daño neto)
    Fallen = result.Fallen,  // caídas de ExpeditionResult
    Creatures = creatures,  // criaturas tocadas
    Floors = result.Floors,  // pisos completados
    Lost = result.Lost
}
```

## Puente Tienda-Arena (S119-S124)

```
GameScene (Tienda)
  ↓ [Panel expedición]
ExpeditionPanelUITK: elegir hasta 3 criaturas
  ↓ [Botón "Ir"]
ExpeditionBridge.RequestDeparture(ids)
  ↓ [Depart(ids)]
ExpeditionHandoff.GoToArena(ids) + RunSeed generado
  ↓ Scene Load
ArenaSandbox (Arena)
  ↓ ArenaRunDirector crea ArenaRun(RunSeed, SelectedIds)
  ↓ Ciclo: EnterFloor → Piso → RecordFloor → Continuar/Retirarse
  ↓ run.ToResult() → ExpeditionHandoff.ReturnToStore(result)
GameScene (Tienda)
  ↓ [Start]
ExpeditionBridge.ApplyResult()
  ↓ [HealthById + Material + Permadeath]
Registry + Inventory actualizados
GameEvents.ExpeditionReturned() → InfoOverlay aviso
```

## Invariantes S124

- Material anulado si result.Lost (riesgo irrevocable de bajada)
- HealthById aplicado delta por criatura; muerta (IsDead) no recibe más daño
- Permadeath opcional: false por default (dev flag, no usar en producción sin aprobación)
- Timeout cloud: evita bloqueos indefinidos
- Cursor desbloqueado al partir (herencia evitada)
- Floors y Fallen incluidos en evento para estadísticas

## S119-S124

- **S119:** Introducido; flujo bidireccional tienda-arena
- **S120:** RequestDeparture(ids) evento y Depart(ids) overload
- **S121:** Agregó energyPerTrip/Knock/Max (luego reemplazado)
- **S124:** Reemplaza energía con HealthById; agrega Floors/Fallen; Lost anula material

## Vinculado a

[[Index/24 - Puente Tienda-Arena]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ExpeditionHandoff]], [[ExpeditionPanelUITK]], [[CloudSyncService]], [[GameManager]], [[PlayerInventorySO]], [[CreatureRegistrySO]], [[GameEvents]], [[InfoOverlayUITK]], [[ArenaRunDirector]] (S124)
