---
tags: [script, system, expedition, bridge]
---

# ExpeditionBridge

**Ruta:** `Systems/Expedition/ExpeditionBridge.cs`

**Responsabilidad:** Componente MonoBehaviour en GameScene que orquesta transiciones tienda ↔ arena. Expone evento estático `OnDepartureRequested` y método `Depart()` para iniciar viaje con equipo elegido. Al retornar, espera startup cloud y aplica rewards: suma **Minerita** vía [[Wallet]], mata criaturas caídas vía [[CreatureLifecycle]]. **S128:** suma Minerita vía `Wallet.Add()` (puerta única). **S129:** matar criaturas es responsabilidad de `CreatureLifecycle`, no toca `Needs` ni stats.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `cloudSync` | `CloudSyncService` | Ref a sincronización (esperar StartupSyncDone) |
| `syncTimeoutSeconds` | `float` | Timeout máximo esperando cloud (default 20s) |
| `departFlushTimeout` | `float` | Timeout flush local antes de arena (default 5s) |

## Evento Estático

```csharp
public static event Action<IReadOnlyList<string>> OnDepartureRequested;
public static void RequestDeparture(IReadOnlyList<string> ids) => OnDepartureRequested?.Invoke(ids);
```

Disparado por `ExpeditionPanelUITK` al hacer clic "Ir" con IDs de criaturas.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `void Depart()` | [Button] Dev; ejecuta Depart(null) |
| `void Depart(IReadOnlyList<string> ids)` | Inicia DepartRoutine: flush cloud (timeout), desbloquea cursor, dispara ExpeditionHandoff.GoToArena(ids) |

## Ciclo de Vida

### OnEnable/OnDisable

- OnEnable: suscribe a `OnDepartureRequested`
- OnDisable: desuscribe

### Start

Si `ExpeditionHandoff.HasResult`: inicia `ApplyResult()` coroutine.

### DepartRoutine

```
1. Si GameManager existe: FlushToCloudAsync() con timeout (5s)
2. Desbloquea cursor (visible=true, lockState=None)
3. ExpeditionHandoff.GoToArena(ids)
```

### ApplyResult (Coroutine, S129)

```
1. Esperar cloudSync.StartupSyncDone (max syncTimeoutSeconds)
2. TryConsumeResult() — lee ExpeditionResult, limpia flags
3. Minerita = 0 si result.Lost, else result.PlayerSecured
4. Si Minerita > 0: Wallet.Add(Currency.Minerita, material, "expedition")
   (un solo evento InventoryChanged automático)
5. Matar criaturas caídas (result.FallenIds):
   - Por cada id en FallenIds:
     - Si dna existe: CreatureLifecycle.Kill(dna)
     (dispara OnCreatureDeparted + RegistryChanged)
6. Dispara GameEvents.ExpeditionReturned(expReturn) con summary
7. Debug.Log con piso, material, caídos
```

## Flujo Tienda ↔ Arena (S124-S129)

```
GameScene (Tienda)
  ↓ ExpeditionPanelUITK: elegir hasta 3 criaturas
  ↓ Botón "Ir"
ExpeditionBridge.RequestDeparture(ids)
  ↓ Depart(ids) → DepartRoutine
    Flush cloud + ExpeditionHandoff.GoToArena(ids)
  ↓ Scene Load
ArenaSandbox (Arena)
  ↓ ArenaRunDirector: ArenaRun(RunSeed, SelectedIds)
  ↓ Ciclo piso: juega → decide Continuar/Retirarse
  ↓ Retreat() → ExpeditionHandoff.ReturnToStore(result)
  ↓ Scene Load
GameScene (Tienda, Start)
  ↓ ApplyResult() coroutine
    Wallet.Add Minerita + CreatureLifecycle.Kill(FallenIds) + Registry persistida
  ↓ GameEvents.ExpeditionReturned → UI actualiza
```

## Cambios S129

- **ELIMINADO:** Aplicar delta vida (`HealthById` removido de ExpeditionResult)
- **ELIMINADO:** Tocar `dna.Needs`
- **AGREGADO:** `CreatureLifecycle.Kill()` para cada ID en `result.FallenIds`
- **CAMBIO:** `ExpeditionResult.FallenIds` → lista de IDs muertos (antes era diccionario de deltas)
- **CAMBIO:** `ExpeditionResult.TeamIds` → nuevos, lista de IDs del equipo

## Invariantes S129+

- Material anulado si `result.Lost`
- Criaturas caídas marcadas `IsDead` vía `CreatureLifecycle`
- Cargas todas criaturas caídas, no solo algunas
- Timeout cloud: evita bloqueos indefinidos
- Cursor desbloqueado al partir (herencia evitada)

## Vinculado a

[[Index/24 - Puente Tienda-Arena]]
[[Index/26 - Plan H0 - Bajada por pisos]] (S124)
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[ExpeditionHandoff]], [[ExpeditionPanelUITK]], [[CloudSyncService]], [[GameManager]], [[Wallet]], [[CreatureRegistrySO]], [[GameEvents]], [[InfoOverlayUITK]], [[ArenaRunDirector]], [[CreatureLifecycle]]
