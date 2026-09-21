---
tags: [script, ui, helper]
---

# CreatureDisplay

**Ruta:** `UI/CreatureDisplay.cs`

**Responsabilidad:** Helper estático con métodos de presentación compartidos para criaturas. Centraliza `StateOf()` localizado (sold/dead/breeding/free), colores de rareza, visuales de iconos y bordes.

**S128:** Eliminado caso `status.cooldown` (demolición RPS). StateOf() retorna solo: sold/dead/breeding/free.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `StateOf(CreatureDNA dna)` | `CreatureDisplayState` | Localizado: Sold/Dead/Breeding/Free |
| `RarityColor(Tier tier)` | `Color` | Color visual por rareza (Tier1/2/3) |
| `RarityBorder(Tier tier)` | `Material` | Material de borde por rareza |

## Enum CreatureDisplayState

- `Free` — Libre, disponible
- `Breeding` — En reproducción
- `Dead` — Muerto permanentemente
- `Sold` — Vendido

## Cambios S128

**Eliminado:** caso `status.cooldown` (CombatCooldownUntil fue borrado de CreatureDNA con demolición RPS).

## Historial

**S95:** `status.cooldown` agregado para mostrar HH:mm de cooldown post-combate.
**S128:** Eliminado (RPS demolido).

## Vinculado a

[[Index/05 - UI System]]

**Conexiones:** [[CreatureGridUITK]], [[DetailEquipTabPresenter]], [[CreatureVisualUI]], [[CreatureDNA]]

