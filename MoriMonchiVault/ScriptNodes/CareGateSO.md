---
tags: [scriptable-object, expedition, configuration]
---

# CareGateSO

**Ruta:** `Data/Expedition/CareGateSO.cs`

**Responsabilidad:** Configuración de umbrales mínimos de cuidado para que una criatura pueda explorar. Define cuándo está "bien cuidada". Usado por `CreatureAvailability.IsWellCared()` y filtros UI de bajada.

## Campos Públicos

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `minHealth` | float | 60f | Mínimo de Health requerido |
| `minEnergy` | float | 60f | Mínimo de Energy requerido |
| `minAffect` | float | 0f | Mínimo de Affect requerido |

**Propiedades (read-only):**
- `MinHealth` → `minHealth`
- `MinEnergy` → `minEnergy`
- `MinAffect` → `minAffect`

## CreateAssetMenu

**Menu path:** `RunRunSimulator/Expedition/Care Gate`

## Caso de uso

1. Asignar instancia en inspector a `ExpeditionBridge` o panel UI de bajada
2. `CreatureAvailability.CanExplore(dna, gate)` → valida si está apta
3. UI hint: `WeakestNeed()` → muestra cuál necesidad cuidar

## Notas

- Gate es **null-safe**: si es null, se asume "bien cuidada" (sin requerimientos)
- Affect puede ser negativo (rango [-100, 100]); umbral permite >= minAffect

## Vinculado a

[[Index/23 - Arena y bajada nocturna]]

**Conexiones:** [[CreatureAvailability]], [[ExpeditionBridge]], [[NeedsState]]
