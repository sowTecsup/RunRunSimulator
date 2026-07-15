---
tags: [combat, visualization, events, bus, 3v3]
---

# CombatVisualEvents

Bus estático de eventos para la visualización de combates (replay). Centraliza toda comunicación entre `CombatVisualizerService` (orquestador) y subscribers (UI, animadores, popups). Es puramente un `public static class` con eventos y métodos helper. **S46:** `OnUnitAffinity` firma cambió (se quitó parámetro `energy`). `CombatElementEventData` gana campo `ReactionName`. **S47:** ElementChipData struct ELIMINADO (ya no se usa en pipeline visual).

## Responsabilidad

Transportar datos de replay (contexto, turnos, hits, popups, log, speech, orden, eventos elementales por-proc) desde el visualizador a listeners sin acoplamiento directo. Un publisher central para ~17 eventos de replay.

## Enums

### CombatVisualSide

| Valor | Uso |
|-------|-----|
| `A` | Equipo/combatiente A (generalmente self) |
| `B` | Equipo/combatiente B (generalmente opponent) |

### CombatVisualLogKind

| Valor | Uso |
|-------|-----|
| `Versus` | Pantalla inicial |
| `Hit` | Golpe normal |
| `Crit` | Golpe crítico |
| `Death` | Muerte de unit |
| `Result` | Resultado final |
| `Proc` | Evento de proc/elemental |

## Structs

### CombatVisualContext (S41+)

Contexto inicial de replay.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `DnaA` | `CreatureDNA` | DNA de A (1v1 legacy) |
| `DnaB` | `CreatureDNA` | DNA de B (1v1 legacy) |
| `HpMaxA` | `float` | HP máx de A (1v1 legacy) |
| `HpMaxB` | `float` | HP máx de B (1v1 legacy) |
| `SlotA` | `Transform` | Transform spawn A (1v1 legacy) |
| `SlotB` | `Transform` | Transform spawn B (1v1 legacy) |
| `TotalTurns` | `int` | Cantidad turnos totales |
| `TeamA` | `List<CreatureDNA>` | **S41** Equipo A (3v3) |
| `TeamB` | `List<CreatureDNA>` | **S41** Equipo B (3v3) |
| `HpMaxTeamA` | `float[]` | **S41** HP máx por unit en A |
| `HpMaxTeamB` | `float[]` | **S41** HP máx por unit en B |
| `SnapsA` | `List<CombatFighterSnapshot>` | **S42** Snapshots equipo A |
| `SnapsB` | `List<CombatFighterSnapshot>` | **S42** Snapshots equipo B |

### CombatVisualHit (S41+)

Datos de un golpe.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Attacker` | `CombatVisualSide` | Quién ataca (A o B) |
| `Defender` | `CombatVisualSide` | Quién recibe (A o B) |
| `Damage` | `float` | Cantidad daño |
| `Crit` | `bool` | Si fue crítico |
| `AttackerIndex` | `int` | **S41** Índice within equipo atacante (0..2) |
| `DefenderIndex` | `int` | **S41** Índice within equipo defensor (0..2) |

### CombatVisualPopup (S42+)

Datos de popup flotante.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Side` | `CombatVisualSide` | Quién recibe popup (A o B) |
| `Position` | `Vector3` | Posición mundo (si Follow es null) |
| `Kind` | `CombatPopupKind` | Tipo (Hit, Crit, Poison, Reaction, Shield, etc.) |
| `Amount` | `float` | Magnitud (daño, curación, escudo, etc.) |
| `Follow` | `Transform` | Transform del luchador para seguimiento |
| `Text` | `string` | **S42** Texto custom (p.ej. ReactionName) |
| `OverrideColor` | `Color` | **S42** Color custom (p.ej. color del elemento) |
| `HasOverrideColor` | `bool` | **S42** Si usar OverrideColor o palette |

### CombatOrderEntry (S42+)

Entrada de orden de acción para barra superior.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Side` | `CombatVisualSide` | Equipo (A o B) |
| `Index` | `int` | Índice within equipo (0..2) |
| `Alive` | `bool` | Si la unidad está viva |
| `State` | `CombatUnitState` | Estado actual (marcas, estados, afinidad) |

### CombatSpeechData (S43+)

Datos de globo de habla cómic.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Side` | `CombatVisualSide` | Quién habla (A o B) |
| `Index` | `int` | Índice unit dentro equipo |
| `Text` | `string` | Texto del globo |
| `Color` | `Color` | Color borde globo y flecha |
| `HasColor` | `bool` | Si usar color custom o default |
| `Duration` | `float` | Segundos a mostrar globo |
| `Follow` | `Transform` | Transform hablante |
| `HasTarget` | `bool` | Si mostrar flecha hacia objetivo |
| `TargetSide` | `CombatVisualSide` | Lado objetivo |
| `TargetIndex` | `int` | Índice objetivo within equipo |
| `TargetFollow` | `Transform` | Transform objetivo (para flecha) |

### CombatElementEventData (S45+, S46 con ReactionName, S47 sin ElementChipData)

Evento elemental por-proc para actualizar marcas/estados de la barra en tiempo real.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Side` | `CombatVisualSide` | Equipo de la unidad afectada (A o B) |
| `Index` | `int` | Índice de la unidad within equipo (0..2) |
| `Kind` | `ElementEventKind` | Tipo de evento (MarkApplied, MarkRemoved, Reaction, StateArmed, StateConsumed, StateRemoved) |
| `Element` | `Element` | Elemento aplicado/removido |
| `ElementB` | `Element` | **S45** Segundo elemento en Reaction |
| `AllySource` | `bool` | true si marca/estado viene de aliado |
| `State` | `ElementalState` | Estado elemental (para StateArmed/etc) |
| `ReactionName` | `string` | **S46** Nombre de la reacción (para parsing a ElementalState o display) |

**S46:** Nuevo campo `ReactionName` — usado para parseando a `ElementalState` en CombatOrderBarUITK, o display en popups.

### CombatVisualLogLine

Línea de log con tipo.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Text` | `string` | Texto (puede llevar HTML color tags) |
| `Kind` | `CombatVisualLogKind` | Tipo de línea |

### CombatVisualPanelState (S42+)

Estado de control del replay UI.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `TurnNumber` | `int` | Contador de acciones/turnos jugador |
| `TotalTurns` | `int` | Total turnos/acciones |
| `Log` | `CombatVisualLogLine[]` | Log acumulado |
| `Ended` | `bool` | Si alcanzó fin |
| `IsDraw` | `bool` | Si fue empate |
| `Winner` | `CombatVisualSide` | Ganador (A o B) |
| `IsAuto` | `bool` | Si playback automático |
| `CanForward` | `bool` | Si puede avanzar |
| `CanBack` | `bool` | Si puede retroceder |
| `Speed` | `float` | Velocidad playback |

## Eventos Estáticos

| Evento | Parámetros | Descripción |
|--------|-----------|-------------|
| `OnVisualCombatStart` | `CombatVisualContext` | Inicia replay |
| `OnVisualCombatEnd` | `CombatVisualSide, bool` | Termina replay |
| `OnTurnStart` | `CombatTurn` | Comienza animación turno |
| `OnTurnEnd` | `CombatTurn` | Termina animación turno |
| `OnAttack` | `CombatVisualSide` | Comienza windup ataque |
| `OnHit` | `CombatVisualHit` | Golpe conecta (S41: con indices) |
| `OnPopup` | `CombatVisualPopup` | Popup a mostrar (S42: con Text/OverrideColor) |
| `OnCrit` | `CombatVisualHit` | Crítico |
| `OnHpChanged` | `CombatVisualSide, float, float` | HP cambió (side, cur, max) — 1v1 legacy |
| `OnDead` | `CombatVisualSide` | Unit muere — 1v1 legacy |
| `OnUnitHpChanged` | `CombatVisualSide, int, float, float` | **S41** HP unit cambió (side, index, cur, max) |
| `OnUnitDead` | `CombatVisualSide, int` | **S41** Unit muere (side, index) |
| `OnLog` | `string` | Línea log agregada |
| `OnPanelState` | `CombatVisualPanelState` | Estado panel control |
| `OnActionOrder` | `List<CombatOrderEntry>` | **S42** Orden de próxima acción |
| `OnUnitAffinity` | `CombatVisualSide, int, int` | **S46 FIRMA CAMBIÓ** Afinidad cambió (side, index, affinity) — sin energy |
| `OnActiveUnit` | `CombatVisualSide, int` | **S42** Unit activa en turno |
| `OnSpeech` | `CombatSpeechData` | **S43** Globo de habla |
| `OnUnitElement` | `CombatElementEventData` | **S45** Evento elemental por-proc (S46: con ReactionName) |

## Métodos Helper Estáticos

| Método | Firma | Descripción |
|--------|-------|-------------|
| `VisualCombatStart` | `(CombatVisualContext ctx)` | Dispara `OnVisualCombatStart` |
| `VisualCombatEnd` | `(CombatVisualSide winner, bool isDraw)` | Dispara `OnVisualCombatEnd` |
| `TurnStart` | `(CombatTurn turn)` | Dispara `OnTurnStart` |
| `TurnEnd` | `(CombatTurn turn)` | Dispara `OnTurnEnd` |
| `Attack` | `(CombatVisualSide side)` | Dispara `OnAttack` |
| `Hit` | `(CombatVisualHit hit)` | Dispara `OnHit` |
| `Popup` | `(CombatVisualPopup p)` | Dispara `OnPopup` |
| `Crit` | `(CombatVisualHit hit)` | Dispara `OnCrit` |
| `HpChanged` | `(CombatVisualSide side, float current, float max)` | Dispara `OnHpChanged` (1v1) |
| `Dead` | `(CombatVisualSide side)` | Dispara `OnDead` (1v1) |
| `UnitHpChanged` | `(CombatVisualSide side, int index, float current, float max)` | **S41** Dispara `OnUnitHpChanged` |
| `UnitDead` | `(CombatVisualSide side, int index)` | **S41** Dispara `OnUnitDead` |
| `Log` | `(string line)` | Dispara `OnLog` |
| `PanelState` | `(CombatVisualPanelState st)` | Dispara `OnPanelState` |
| `ActionOrder` | `(List<CombatOrderEntry> order)` | **S42** Dispara `OnActionOrder` |
| `UnitAffinity` | `(CombatVisualSide side, int index, int affinity)` | **S46 FIRMA CAMBIÓ** Dispara `OnUnitAffinity` (sin energy) |
| `ActiveUnit` | `(CombatVisualSide side, int index)` | **S42** Dispara `OnActiveUnit` |
| `Speech` | `(CombatSpeechData d)` | **S43** Dispara `OnSpeech` |
| `UnitElement` | `(CombatElementEventData d)` | **S45** Dispara `OnUnitElement` (S46: con ReactionName) |

## Cambios S47

**ElementChipData struct ELIMINADO:**
- Struct que captaba (Label, Color, AllySource, Negative) de marcas/estados elementales
- Ya no se emite ni se consume
- Era usada en el pipeline visual antiguo (pre-S47) de la barra de orden
- Reemplazada por lógica más directa en CombatOrderBarUITK (parsea ElementalState de ReactionName)

**Impacto:** Simplificación del bus de eventos, pipeline de datos más directo (sin intermediarios DTO).

## Cambios S46

**OnUnitAffinity firma cambió:**
- Antes (S42-S45): `Action<CombatVisualSide, int, int, int>` (side, index, affinity, energy)
- Ahora (S46): `Action<CombatVisualSide, int, int>` (side, index, affinity) — se quitó energy

**CombatElementEventData ganó ReactionName:**
- Campo nuevo `ReactionName` (string)
- Usado para parseando a `ElementalState` en CombatOrderBarUITK (Reaction event)
- También display en popups de reacción

## Vinculado a

- [[CombatVisualizerService]] — único publisher
- [[CombatOrderBarUITK]] — suscriptor (S46: HandleAffinity sin energy; S47: parsea ReactionName)
- [[CombatSpeechBubbles]] — suscriptor `OnSpeech`
- [[CombatCameraDirector]] — suscriptor `OnActiveUnit`
- [[CombatFeelDirector]] — **S46 NEW** suscriptor `OnPopup` + `OnUnitElement`

## Notas Implementación S47

- ElementChipData completamente removido (no genera errores de compilación, era solo un DTO)
- Flujo de coreografía de pasivas no usa ElementChipData; SetActiveTurn/SetTargeted reemplazan visualización de marcos
- CombatOrderBarUITK S47 parsea estados directamente de ReactionName sin intermediario

## Notas Implementación S46

- CombatOrderBarUITK.HandleAffinity ahora toma 3 parámetros (side, index, affinity)
- CombatElementEventData incluye ReactionName para parseando a ElementalState
- Enums `ElementEventKind.EnergyGained` y `EnergySpent` siguen existiendo (append-only, nadie los emite)
