---
tags: [script, ui, combat]
---

# CombatPanelUITK

**Ruta:** `UI/CombatPanelUITK.cs`

**Responsabilidad:** Panel UI combate (4 pestañas: Batalla Online, Combate Local, Resultados, Historial). Implementa `IUINavigable` (foco jerárquico). Obtiene config vía `CombatController.Instance.Config`, registry de `GameManager.Instance`. Combate local vía `CombatController.SimulateLocal()`, async vía `AsyncCombatService`. **S34:** Tab Historial muestra combates con boton replay (▶) lazy-creado.

## Organización (partial class)

| Archivo | Responsabilidad |
|---------|-----------------|
| `CombatPanelUITK.cs` | Núcleo, lifecycle, wiring, data, StatsOf |
| `CombatPanelUITK.Tabs.cs` | Contenido de 4 pestañas (MakeCandidate, UI building, DoLocalFight, Historial con replay S34) |
| `CombatPanelUITK.Navigation.cs` | `IUINavigable` + foco jerárquico |

## Pestañas

1. **Batalla Online:** Pick criatura tuya, verla (stats+partes), enviarla a async (Instant o Timer)
2. **Combate Local:** Pick dos criaturas, luchan localmente, log inline
3. **Resultados:** Criaturas en cola / con resultados pendientes; right pane muestra log
4. **Historial:** Todos los combates históricos, filtrable por criatura, **con boton replay (S34)**

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | Raíz de la UI |
| `panel` | `UIPanelType` | Tipo de panel (Combat) |
| `sortingOrder` | `int` | Orden de rendering |
| `database` | `CreatureDatabaseSO` | Stats/partes |
| `asyncCombatService` | `AsyncCombatService` | Ref async service |

## Campos Privados (S34 nuevos)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `histReplayBtn` | `Button` | **S34** Boton "▶ Ver replay" creado lazy en ShowHistory |
| `histCurrent` | `HistItem` | **S34** Struct (Self, Rec) del combate mostrado actualmente |

## Struct HistItem (S34)

```csharp
private struct HistItem { public CreatureDNA Self; public CombatRecord Rec; }
```

Par (criatura, record) para manejo efficient del historial. Usado en `historyItems`, `historyRendered`, y `histCurrent` (para acceso rápido al combate mostrado en detail pane).

## Método StatsOf (S32)

```csharp
private EffectiveStats StatsOf(CreatureDNA dna) =>
    database != null ? CombatStats.GetEffectiveStats(dna, database)
                     : new EffectiveStats(dna.BaseConstitution, dna.BaseAttack,
                                         dna.BaseSpeed, dna.BaseDefense,
                                         dna.BaseLuck, dna.BaseEvasion);
```

**S32:** Cambio de referencias:
- `CombatService.GetEffectiveStats()` → `CombatStats.GetEffectiveStats()` (clase extraída)
- `CombatService.EffectiveStats` → `EffectiveStats` (struct público top-level)

Fallback: sin database, construye `EffectiveStats` manualmente desde DNA base.

## Cambios S34 (Tab 4 — Historial con Replay)

Ver detalles completos en [[CombatPanelUITK.Tabs]].

**Breve:**
- Tab Historial ahora llama `CombatReplayRequest.Request()` en boton "▶ Ver replay"
- Boton creado lazy en `ShowHistory()` si no existe
- Habilitado/deshabilitado dinámicamente vía `CombatReplayRequest.CanReplay()`

## Vinculado a

- [[Index/05 - UI System]]
- [[CombatPanelUITK.Tabs]] — implementación de pestañas (S34)
- [[CombatController]] — obtiene config
- [[CombatService]] — simula combate local
- [[AsyncCombatService]] — gestiona async
- [[CombatStats]] — calcula stats (S32)
- [[EffectiveStats]] — struct de retorno (S32)
- [[CombatReplayRequest]] — S34 replay request
- [[GameManager]] — registry, database

## Conexiones

**Entrada:**
- `UIManager` panel toggle/set events
- `GameEvents.OnRegistryChanged/Reloaded` — rebuild listas
- `GameEvents.OnCombatLogged` — nuevo combate en historial

**Salida:**
- `CombatController.SimulateLocal()` — combate local
- `AsyncCombatService.EnqueueInstantAsync/EnqueueScheduledAsync()` — async enqueue
- `CombatReplayRequest.Request()` — replay request (S34)

## Notas

- **Historial filtrable:** Dropdown por criatura; cada selección rebuildea lista
- **Tab 4 layout:** Left = lista combates, right = detail pane con log + outcome + **boton replay (S34)**
- **Lazy creation:** Boton replay se crea en ShowHistory si no existe; permite reutilizaciOn
- **Registry validación:** CanReplay revalidado en cada ShowHistory para captar cambios (rival vendido, etc.)
