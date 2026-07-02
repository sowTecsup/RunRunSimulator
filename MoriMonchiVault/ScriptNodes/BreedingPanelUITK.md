---
tags: [script, ui, breeding]
---

# BreedingPanelUITK

**Ruta:** `UI/BreedingPanelUITK.cs`

**Responsabilidad:** Panel UI de breeding (2 pestañas: Criar, Incubando). Implementa `IUINavigable` (focus jerárquico). Obtiene registry de `GameManager.Instance`. Cría local via `BreedingController.BreedCreatures()`, async via `BreedingController.StartBreedingAsync()`, hatch via `BreedingController.HatchAsync()`. Tick de huevos en Update (cuenta atrás).

## Organización (partial class)

| Archivo | Responsabilidad |
|---------|-----------------|
| `BreedingPanelUITK.cs` | Núcleo, lifecycle, wiring, data |
| `BreedingPanelUITK.Content.cs` | Candidatos, huevos, preview, breed, hatch |
| `BreedingPanelUITK.Navigation.cs` | `IUINavigable` + foco jerárquico |

## Cambios S32

**Stats refs:** Cambio de `CombatService.GetEffectiveStats()` → `CombatStats.GetEffectiveStats()` y `CombatService.EffectiveStats` → `EffectiveStats` top-level. Usado en preview de crías (display de stats heredados).

## Vinculado a

- [[Index/05 - UI System]]
- [[BreedingController]] — orquesta crianza
- [[AsyncBreedingService]] — async breeding
- [[GameManager]] — registry
- [[CombatStats]] — calcula stats (S32)
- [[EffectiveStats]] — struct (S32)

## Conexiones

**Entrada:**
- `GameEvents.OnRegistryChanged`, etc. — subscriptor
- Botones UI → llamadas a `BreedingController`

**Salida:**
- UI visual (pestañas, candidatos, huevos, preview stats)

## Notas

- **Stats preview:** Muestra stats de crías proyectadas vía `CombatStats.GetEffectiveStats()` (S32).
