---
tags: [script, world, ui, uitk, expedition]
---

# ArenaPlanPanel.cs

**Ruta:** `World/Expedition/ArenaPlanPanel.cs`

**Responsabilidad:** Panel UITK de planificación pre-ronda y gestión de plan. Muestra bases abiertas por rol, permite elegir órdenes. S122: pilares → bases. **S124:** Colaborador ArenaFloorPanel maneja transición entre pisos; ArenaPlanPanel sin ReturnToStore, sin lastResult; no resetea si ArenaRunDirector activa.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `void Update()` | Gestiona visible/oculto |
| `void Play()` | Lanza ronda (Launch) |

## UI (S122-S124)

- `plan-root`
  - `plan-header` — forma, sala, entrada
  - `cast-list` — tarjetas criaturas
    - Cada `cast-card`: nombre + **3 píldoras BASE** (2 abiertas, 1 cerrada)
  - `plan-rival` — rival lectura
  - Botones: **play** (no return)

## Campos Privados

- `director` [Required] — ArenaRunDirector (S124)
- `floorPanel` — ArenaFloorPanel (S124)

## Métodos Privados (S122-S124)

| Método | Descripción |
|--------|-------------|
| `BuildCard(...)` | Crea tarjeta con píldoras base |
| `ChooseBase(card, base)` | ArenaBases.ToOrders → SetPlayerOrders |
| `RefreshRivalLine()` | Muestra rival rol + bases |

## Flujo Ronda (S124)

1. Player elige bases (píldoras)
2. Clic Play() → round.Launch()
3. ArenaRound detecta fin (IsOver)
4. ArenaFloorPanel.Refresh() muestra decisiones (Continuar/Retirarse)
5. Continuar → director.Continue() → siguiente piso

## Cambios S124

- **Sin ReturnToStore** — ArenaFloorPanel lo maneja
- **Sin lastResult**
- **Sin reset** si director.Active (bajada en curso)
- Colabora con ArenaFloorPanel para flujo multi-piso

## Invariantes

- Base abierta → órdenes concretas (S122)
- Lectura rival por rol (S122)
- Play() solo si editor/dev o no editor

## Vinculado a

[[Index/24 - Puente Tienda-Arena]], [[Index/22 - Bajada Nocturna y Linaje]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ArenaSandbox]], [[ArenaRound]], [[ArenaBases]], [[ArenaPaletteApplier]], [[ArenaFloorPanel]] (S124), [[ArenaRunDirector]] (S124)
