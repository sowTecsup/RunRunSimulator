---
tags: [script, system, expedition, bridge]
---

# ExpeditionBridge

**Ruta:** `Systems/Expedition/ExpeditionBridge.cs`

**Responsabilidad:** Componente MonoBehaviour en GameScene que orquesta transiciones tienda ↔ arena. Expone evento estático `OnDepartureRequested` y método `Depart()` para iniciar viaje con equipo elegido. Al retornar, espera startup cloud y aplica rewards: suma **Minerita** vía [[Wallet]], aplica delta vida a criaturas. **S128:** ahora suma Minerita vía `Wallet.Add()` (puerta única).

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `cloudSync` | `CloudSyncService` | Ref a sincronización (esperar StartupSyncDone) |
| `syncTimeoutSeconds` | `float` | Timeout máximo esperando cloud (default 20s) |
| `departFlushTimeout` | `float` | Timeout flush local antes de arena (default 5s) |
| `permadeathEnabled` | `bool` | Si true, marca IsDead si Lost o health <= 0 (default false) |

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

### ApplyResult (Coroutine, S128)

```
1. Esperar cloudSync.StartupSyncDone (max syncTimeoutSeconds)
2. TryConsumeResult() — lee ExpeditionResult, limpia flags
3. Minerita = 0 si result.Lost, else result.PlayerSecured
4. Si Minerita > 0: Wallet.Add(Currency.Minerita, material, "expedition")
   (un solo evento InventoryChanged automático)
5. Si registry y result.HealthById:
   - Por cada (id, delta):
     - Si criatura existe, viva:
       - dna.Needs.AddHealth(delta)
       - Acumular net, count
   - Si cambios: GameEvents.RegistryChanged(registry)
6. Si permadeathEnabled: marca IsDead si Lost OR health <= 0
7. Dispara GameEvents.ExpeditionReturned(expReturn) con summary
8. Debug.Log con piso, material, vida
```

## Flujo Tienda ↔ Arena (S124)

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
    Wallet.Add Minerita + Needs.AddHealth + Registry persistida
  ↓ GameEvents.ExpeditionReturned → UI actualiza
```

## Integración S128

| Antes | Ahora |
|-------|-------|
| `inventory.AddAdventureMaterial(material)` | `Wallet.Add(Currency.Minerita, material, "expedition")` |
| Evento manual `InventoryChanged` | Automático de Wallet |
| Acceso directo SO | Puerta única [[Wallet]] |

**Invariante:** si Lost → material = 0 (riesgo irrevocable).

## Campos Locales

- `departing` — bool; flag para evitar múltiples DepartRoutines simultáneas

## Invariantes S124+

- Material anulado si `result.Lost`
- HealthById aplicado delta por criatura (negativo = daño)
- Permadeath opcional (false default)
- Timeout cloud: evita bloqueos indefinidos
- Cursor desbloqueado al partir (herencia evitada)

## Vinculado a

[[Index/24 - Puente Tienda-Arena]]
[[Index/26 - Plan H0 - Bajada por pisos]] (S124)
[[Index/28 - Cimientos y camino a Game Ready]]
[[Index/29 - Plan HC - Cimientos (ejecutable)]] (§5 · C3 wallet)

**Conexiones:** [[ExpeditionHandoff]], [[ExpeditionPanelUITK]], [[CloudSyncService]], [[GameManager]], [[Wallet]], [[CreatureRegistrySO]], [[GameEvents]], [[InfoOverlayUITK]], [[ArenaRunDirector]]

