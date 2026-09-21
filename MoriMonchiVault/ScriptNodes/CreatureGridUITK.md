---
tags: [script, ui, uitk]
---

# CreatureGridUITK.cs

**Ruta:** `UI/CreatureGridUITK.cs`

**Responsabilidad:** Grilla UITK de criaturas. Display lista de MoriMonchis del registry ordenada por BirthDate descendente. Bindea: nombre, state (viva/muerta/ocupada), portrait, necesidades (3 barras), marca de apta para bajar. **S75:** Sin estado QueuedForCombat (demolición). **S93:** Usa helpers `CreatureDisplay` y `UiPanels`. **S129:** Eliminadas columnas de equipo y stats. Muestra solo: genética, necesidades, estado de vida.

## Campos Bindeados

| Campo | Tipo | Uso |
|-------|------|-----|
| Nombre | string | CustomName o generado |
| Estado | string | "Viva" / "Muerta" / "Ocupada (Cría)" / "Vendida" |
| Portrait | Texture | Retrato visual (MonchiPortraitService) |
| Necesidades | 3 barras | Health/Energy/Affect con color (NeedsDisplay) |
| Apta | bool | ✓ si `CanExplore()` |

## Cambios en S129

- **ELIMINADO:** Columnas de stats (Constitution, Attack, Speed, etc.)
- **ELIMINADO:** Slots de equipo
- **AGREGADO:** 3 barras de necesidades con colores
- **AGREGADO:** Badge ✓ si apta para explorar

## Vinculado a

[[Index/05 - UI System]]
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureDisplay]], [[NeedsDisplay]], [[CreatureAvailability]], [[UiPanels]], [[MonchiPortraitUI]], [[GameEvents]]
