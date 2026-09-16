---
tags: [script, core, component, bridge, expedition]
---

# ExpeditionBridge.cs

**Ruta:** `Core/ExpeditionBridge.cs`

**Responsabilidad:** Componente MonoBehaviour que vive en GameScene (tienda) y orquesta transiciones a/desde expedición. Expone botón Depart() para iniciar viaje a arena. Al retornar, espera a que CloudSyncService inicialice (StartupSyncDone) y aplica rewards de resultado (materiales al inventario). S119: complemento de ExpeditionHandoff; maneja lógica de tienda.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `cloudSync` | CloudSyncService | Referencia a sincronización en la nube (para esperar StartupSyncDone) |
| `syncTimeoutSeconds` | float, Min(1f) | Timeout máximo esperando inicio cloud (default 20s) |

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `void Depart()` | [Button] Fija cursor visible, guarda tienda (GameManager.FlushToCloud), dispara ExpeditionHandoff.GoToArena() |

## Ciclo de Vida

**OnEnable:**
- Se vincula al Handoff (sin código explícito, solo Start lo ejecuta)

**Start:**
- Si ExpeditionHandoff.HasResult: inicia ApplyResult() coroutine
- ApplyResult() espera a que CloudSyncService esté listo (timeout 20s)
- Lee resultado, suma PlayerSecured a inventario, dispara InventoryChanged

**ApplyResult (Coroutine, S119):**

```
1. Esperar que cloudSync != null && StartupSyncDone (max 20s)
2. TryConsumeResult() — lee y limpia resultado
3. Si InventoryActual() no es null y PlayerSecured > 0:
   - AddAdventureMaterial(PlayerSecured)
   - GameEvents.InventoryChanged(inventory)
4. Debug.Log(f"sala {Seed}: {PlayerSecured}-{RivalSecured} {Winner} → +{PlayerSecured}")
```

## Puente Tienda-Arena (S119)

```
GameScene (Tienda)
  ↓ [Depart Button]
ExpeditionHandoff.GoToArena()
  ↓ Scene Load
ArenaSandbox (Arena)
  ↓ [Ronda termina]
ExpeditionHandoff.ReturnToStore(result)
  ↓ Scene Load
GameScene (Tienda)
  ↓ [Start]
ExpeditionBridge.ApplyResult()
  ↓ [Rewards]
Inventario actualizado
```

## Invariantes

- Depart() fija Time.timeScale=1f (garantiza pausa deshabilitada)
- ApplyResult() solo se ejecuta si HasResult=true (ExpeditionHandoff protege esto)
- Cursor deshabilitado en arena, re-habilitado al partir
- Timeout 20s previene hang si cloud init falla
- GameManager.FlushToCloud() asegura save pre-transición

## Vinculado a

[[Index/24 - Puente Tienda-Arena]]

**Conexiones:** [[ExpeditionHandoff]], [[CloudSyncService]], [[GameManager]], [[PlayerInventorySO]], [[GameEvents]]
