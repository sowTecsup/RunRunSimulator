---
tags: [script, ui, combat, partial]
---

# CombatPanelUITK

**Ruta:** `UI/CombatPanelUITK.cs`

**Responsabilidad:** Panel UI combate **3 pestañas principales** (S37/S39): 
- Tab 0: "Batalla Online" — pick 1 criatura tuya, verla (stats + partes), enviarla a async (Instant o Timer)
- Tab 1: "Resultados" — criaturas en cola / con resultados pendientes; pane derecho muestra log
- Tab 2: "Historial" — todos los combates históricos, filtrable por criatura, con boton replay (S34)

**Hermano:** `CombatLineupUITK` (componente sibling en mismo GameObject + UIDocument) maneja tab 3 "Equipo 3v3" (lineup drag&drop + combate local 3v3). 

Implementa `IUINavigable` (foco jerárquico). Obtiene config vía `CombatController.Instance.Config`, registry de `GameManager.Instance`. Combate local vía `CombatController.SimulateLocal()`, async vía `AsyncCombatService`.

## Cambios S37/S38/S39

**Tab "Combate Local" eliminada (S37/S38):**
- Antigua tab 1 desapareció. 
- Lineup UI 3v3 ahora vive en CombatLineupUITK (componente sibling).
- Tabs reindexadas: Online=0, Resultados=1, Historial=2.
- Equipo 3v3 = tab manejada por sibling (entrada en TabView pero lógica separada).

**Navegación teclado (S38):**
- Tab Equipo 3v3 es navegable por teclado (A/D cambia, Espaço/Intro actúa).
- Contenido es mouse-only (drag&drop).

**S39:** Sin cambios de API en CombatPanelUITK. Sistema elemental integrado; Display de stats ya incluye 6 stats (CON/ATK/SPD/DEF/LCK/EVA).

## Organización (partial class — Deuda Activa)

| Archivo | Responsabilidad |
|---------|-----------------|
| `CombatPanelUITK.cs` | Núcleo, lifecycle, wiring, data, StatsOf |
| `CombatPanelUITK.Tabs.cs` | Contenido de 3 pestañas (MakeCandidate, UI building, Historial con replay S34) |
| `CombatPanelUITK.Navigation.cs` | `IUINavigable` + foco jerárquico (sin T2* legacy de Combate Local) |

## Pestañas (Post-S37)

| Tab | Nombre | Contenido |
|-----|--------|----------|
| 0 | Batalla Online | Lista criaturas, selecciona 1, muestra stats+partes, envía a async (Instant/Timer) |
| 1 | Resultados | Criaturas en cola (`QueuedForCombat`), countdown a próximo server tick |
| 2 | Historial | Todos los combates históricos, filtrable por criatura, **boton replay** |
| (3) | Equipo 3v3 | Manejado por **CombatLineupUITK** (sibling component) |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | Raíz de la UI |
| `panel` | `UIPanelType` | Tipo de panel (Combat) |
| `sortingOrder` | `int` | Orden de rendering |
| `database` | `CreatureDatabaseSO` | Stats/partes |
| `asyncCombatService` | `AsyncCombatService` | Ref async service |

## Método StatsOf (S32+)

```csharp
private EffectiveStats StatsOf(CreatureDNA dna) =>
    database != null ? CombatStats.GetEffectiveStats(dna, database)
                     : new EffectiveStats(dna.BaseConstitution, dna.BaseAttack,
                                         dna.BaseSpeed, dna.BaseDefense,
                                         dna.BaseLuck, dna.BaseEvasion);
```

**S32 cambio:** Usa `CombatStats.GetEffectiveStats()` (clase extraída) en lugar de legacy `CombatService.GetEffectiveStats()`.

## Vinculado a

- [[Index/05 - UI System]]
- [[Index/13 - Combat Design Direction]]
- [[CombatPanelUITK.Tabs]] — implementación de pestañas (S34)
- [[CombatPanelUITK.Navigation]] — navegación (S38)
- [[CombatLineupUITK]] — tab 3 (sibling component, S37)
- [[CombatController]] — obtiene config
- [[AsyncCombatService]] — gestiona async
- [[CombatStats]] — calcula stats (S32)
- [[EffectiveStats]] — struct de retorno (S32)
- [[GameManager]] — registry, database

## Conexiones

**Entrada:**
- `UIManager` panel toggle/set events
- `GameEvents.OnRegistryChanged/Reloaded` — rebuild listas
- `GameEvents.OnCombatLogged` — nuevo combate en historial

**Salida:**
- `AsyncCombatService.EnqueueInstantAsync/EnqueueScheduledAsync()` — async enqueue
- Refs a CombatLineupUITK para navegación cruzada

## Notas

- **Partial class:** Deuda activa (Fase 8, refactor a componentes pequeños).
- **S37 impacto:** Tab Combate Local removida. Lineup UI movida a sibling. Tabs reindexadas.
- **S38 navegación:** Teclado puede acceder tab Equipo 3v3, pero contenido es mouse-only.
- **Registry validación:** Cada rebuild de listas revalida elegibilidad (vivos, no busy, fights < max).
- **Historial filtrable:** Dropdown por criatura; cada selección rebuildea lista.
