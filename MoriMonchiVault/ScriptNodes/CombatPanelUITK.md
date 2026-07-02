---
tags: [script, ui, combat]
---

# CombatPanelUITK

**Ruta:** `UI/CombatPanelUITK.cs`

**Responsabilidad:** Panel UI combate (4 pestañas: Batalla Online, Combate Local, Resultados, Historial). Implementa `IUINavigable` (foco jerárquico). Obtiene config vía `CombatController.Instance.Config`, registry de `GameManager.Instance`. Combate local vía `CombatController.SimulateLocal()`, async vía `AsyncCombatService`. Muestra stats de 6 campos: CON/ATK/SPD/DEF/LCK/EVA.

## Organización (partial class)

| Archivo | Responsabilidad |
|---------|-----------------|
| `CombatPanelUITK.cs` | Núcleo, lifecycle, wiring, data, StatsOf |
| `CombatPanelUITK.Tabs.cs` | Contenido de 4 pestañas (MakeCandidate, UI building, DoLocalFight, DoRefresh, etc.) |
| `CombatPanelUITK.Navigation.cs` | `IUINavigable` + foco jerárquico |

## Pestañas

1. **Batalla Online:** Pick criatura tuya, verla (stats+partes), enviarla a async (Instant o Timer)
2. **Combate Local:** Pick dos criaturas, luchan localmente, log inline
3. **Resultados:** Criaturas en cola / con resultados pendientes; right pane muestra log
4. **Historial:** Todos los combates históricos, filtrable por criatura

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

## Vinculado a

- [[Index/05 - UI System]]
- [[CombatController]] — obtiene config
- [[CombatService]] — simula combate local
- [[AsyncCombatService]] — gestiona async
- [[CombatStats]] — calcula stats (S32)
- [[EffectiveStats]] — struct de retorno (S32)
- [[GameManager]] — registry, database

## Conexiones

**Entrada:**
- `GameEvents.OnRegistryChanged`, `OnRegistryReloaded`, `OnCombatLogged` — subscriptor
- Botones UI → llamadas a `CombatController`/`AsyncCombatService`

**Salida:**
- UI visual (pestañas, cards, logs)
- Llamadas a async combat

## Notas

- **Stats display:** CON/ATK/SPD/DEF/LCK/EVA viven en UI; cálculo via `StatsOf()`.
- **S32:** Refactor extrajo `CombatStats` y `EffectiveStats` a clases públicas; panel actualizado.
