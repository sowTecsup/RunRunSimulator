---
tags: [script, core, component, bridge, expedition]
---

# ExpeditionBridge.cs

**Ruta:** `Systems/Expedition/ExpeditionBridge.cs`

**Responsabilidad:** Componente MonoBehaviour que vive en GameScene (tienda) y orquesta transiciones a/desde expedición. Expone evento estático OnDepartureRequested y método Depart() para iniciar viaje a arena con equipo elegido. Al retornar, espera a que CloudSyncService inicialice (StartupSyncDone) y aplica rewards: suma material al inventario y gasta energía en criaturas. S119: complemento de ExpeditionHandoff; maneja lógica de tienda. **S120-S121:** maneja RequestDeparture(ids) y el gasto de energía con configuración.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `cloudSync` | CloudSyncService | Referencia a sincronización en la nube (para esperar StartupSyncDone) |
| `syncTimeoutSeconds` | float, Min(1f) | Timeout máximo esperando inicio cloud (default 20s) |
| `energyPerTrip` | int, Min(0) | Costo base de energía al bajar (default 20, **S121**) |
| `energyPerKnock` | int, Min(0) | Costo adicional por cada tumbada (default 5, **S121**) |
| `maxEnergyPerTrip` | int, Min(0) | Tope de energía gastado por criatura (default 40, **S121**) |

## Evento Estático (S120)

```csharp
public static event Action<IReadOnlyList<string>> OnDepartureRequested;
public static void RequestDeparture(IReadOnlyList<string> ids) => OnDepartureRequested?.Invoke(ids);
```
Disparado por ExpeditionPanelUITK al hacer clic "Ir" con IDs de criaturas elegidas.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `void Depart()` | [Button] Sin IDs (flujo antiguo o debug); fija cursor visible, guarda tienda (GameManager.FlushToCloud), dispara ExpeditionHandoff.GoToArena(null) |
| `void Depart(IReadOnlyList<string> ids)` | **(S120)** Con IDs del equipo elegido; fija cursor visible, guarda tienda, dispara ExpeditionHandoff.GoToArena(ids) |

## Ciclo de Vida

**OnEnable (S120):**
- Suscribe a OnDepartureRequested para recibir Depart(ids)

**OnDisable:**
- Desuscribe

**Start:**
- Si ExpeditionHandoff.HasResult: inicia ApplyResult() coroutine

**ApplyResult (Coroutine, S119-S121):**

```
1. Esperar que cloudSync != null && StartupSyncDone (max syncTimeoutSeconds)
2. TryConsumeResult() — lee y limpia resultado
3. Si inventario presente y PlayerSecured > 0:
   - AddAdventureMaterial(PlayerSecured)
   - GameEvents.InventoryChanged(inventory)
4. Si registry presente y hay Stats (S121):
   - Por cada stat del equipo del jugador:
     - Obtener criatura por stat.Id
     - Calcular costo = min(maxEnergyPerTrip, energyPerTrip + energyPerKnock × stat.TimesKnocked)
     - dna.Needs.SpendEnergy(costo)
     - Acumular energía total y contador
   - Si hay criaturas que gastaron: GameEvents.RegistryChanged(registry)
5. GameEvents.ExpeditionReturned(ExpeditionReturn) con totales acumulados
6. Debug.Log con sala, marcador, material y energía
```

## Puente Tienda-Arena (S119-S121)

```
GameScene (Tienda)
  ↓ [Terminal PanelUI / Panel expedición]
ExpeditionPanelUITK: elegir hasta 3 criaturas
  ↓ [Botón "Ir"]
ExpeditionBridge.RequestDeparture(ids)
  ↓ [Depart(ids)]
ExpeditionHandoff.GoToArena(ids)
  ↓ Scene Load
ArenaSandbox (Arena)
  ↓ Lee SelectedIds, arma equipo elegido
  ↓ [Ronda termina]
ExpeditionHandoff.ReturnToStore(result)
  ↓ Scene Load
GameScene (Tienda)
  ↓ [Start]
ExpeditionBridge.ApplyResult()
  ↓ [Material + Energía]
Inventario + Registry actualizados
GameEvents.ExpeditionReturned() → InfoOverlay aviso 6s
```

## Invariantes

- Depart(ids) sin flushing previo puede perder cambios de tienda (mitigado por botón no-interruptible)
- ApplyResult() solo se ejecuta si HasResult=true (ExpeditionHandoff protege esto)
- Cursor deshabilitado en arena, re-habilitado al partir
- Timeout previene hang si cloud init falla
- Energía gastada = min(maxEnergyPerTrip, base + penalización tumbadas)
- Si registry es null no gasta energía (flujo sandbox limpio)

## S120-S122

- **S120:** RequestDeparture evento y Depart(ids) overload. Introduce SelectedIds → Arena.
- **S121:** Campos energyPerTrip/Per/Knock/Max. ApplyResult calcula costo por criatura desde Stats.TimesKnocked. GameEvents.ExpeditionReturned(return) con materiales/energía totales.
- **S122:** Sin cambios.

## Vinculado a

[[Index/24 - Puente Tienda-Arena]]

**Conexiones:** [[ExpeditionHandoff]], [[ExpeditionPanelUITK]], [[CloudSyncService]], [[GameManager]], [[PlayerInventorySO]], [[CreatureRegistrySO]], [[GameEvents]], [[InfoOverlayUITK]]
