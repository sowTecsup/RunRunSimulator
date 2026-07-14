---
tags: [combat, visualization, events, bus, 3v3]
---

# CombatVisualEvents

Bus estático de eventos para la visualización de combates (replay). Centraliza toda comunicación entre `CombatVisualizerService` (orquestador) y subscribers (UI, animadores, popups). Es puramente un `public static class` con eventos y métodos helper. **S41:** Aditivo con datos de 3v3 (equipos, indices, eventos por unit). **S42:** Nuevos eventos para barra de orden de acción y afinidad/energía. **S43:** Struct `CombatSpeechData` + evento `OnSpeech` para globos de habla cómic + `Negative` en `ElementChipData`. **S45:** Nuevo struct `CombatElementEventData` + evento `OnUnitElement` — canal por-proc para actualizar marcas/estados de la barra de orden en tiempo real (MarkApplied/MarkRemoved/Reaction/StateArmed/StateConsumed/StateRemoved).

## Responsabilidad

Transportar datos de replay (contexto, turnos, hits, popups, log, speech, orden, eventos elementales por-proc) desde el visualizador a listeners sin acoplamiento directo. Un publisher central para ~17 eventos de replay (S45 aditivo: OnUnitElement).

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

### CombatVisualContext (S41 ADITIVO, S42 con SnapsA/SnapsB)

Contexto inicial de replay.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `DnaA` | `CreatureDNA` | DNA de A (1v1 legacy, null en 3v3) |
| `DnaB` | `CreatureDNA` | DNA de B (1v1 legacy, null en 3v3) |
| `HpMaxA` | `float` | HP máx de A (1v1 legacy) |
| `HpMaxB` | `float` | HP máx de B (1v1 legacy) |
| `SlotA` | `Transform` | Transform spawn A (1v1 legacy) |
| `SlotB` | `Transform` | Transform spawn B (1v1 legacy) |
| `TotalTurns` | `int` | Cantidad turnos totales |
| `TeamA` | `List<CreatureDNA>` | **S41 NEW** Equipo A (3v3) |
| `TeamB` | `List<CreatureDNA>` | **S41 NEW** Equipo B (3v3) |
| `HpMaxTeamA` | `float[]` | **S41 NEW** HP máx por unit en A |
| `HpMaxTeamB` | `float[]` | **S41 NEW** HP máx por unit en B |
| `SnapsA` | `List<CombatFighterSnapshot>` | **S42 NEW** Snapshots equipo A |
| `SnapsB` | `List<CombatFighterSnapshot>` | **S42 NEW** Snapshots equipo B |

**S41 Cambio:** Aditivo — campos 1v1 legacy intactos, nuevos campos para 3v3.
**S42 Cambio:** Aditivo — SnapsA/SnapsB para acceso a CombatOrderBarUITK.

### CombatVisualHit (S41 ADITIVO)

Datos de un golpe.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Attacker` | `CombatVisualSide` | Quién ataca (A o B) |
| `Defender` | `CombatVisualSide` | Quién recibe (A o B) |
| `Damage` | `float` | Cantidad daño |
| `Crit` | `bool` | Si fue crítico |
| `AttackerIndex` | `int` | **S41 NEW** Índice within equipo atacante (0..2) |
| `DefenderIndex` | `int` | **S41 NEW** Índice within equipo defensor (0..2) |

### CombatVisualPopup (S42 ADITIVO con Text/OverrideColor)

Datos de popup flotante.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Side` | `CombatVisualSide` | Quién recibe popup (A o B) |
| `Position` | `Vector3` | Posición mundo (si Follow es null) |
| `Kind` | `CombatPopupKind` | Tipo (Hit, Crit, Poison, Reaction, Shield, etc.) |
| `Amount` | `float` | Magnitud (daño, curación, escudo, etc.) |
| `Follow` | `Transform` | Transform del luchador para seguimiento (S34+) |
| `Text` | `string` | **S42 NEW** Texto custom (p.ej. ReactionName) |
| `OverrideColor` | `Color` | **S42 NEW** Color custom (p.ej. color del elemento) |
| `HasOverrideColor` | `bool` | **S42 NEW** Si usar OverrideColor o palette |

### ElementChipData (S42 NEW, S43 + Negative)

Descriptor de chip elemental para barra de orden.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Label` | `string` | Nombre del elemento o estado (DisplayName) |
| `Color` | `Color` | Color UI del elemento |
| `AllySource` | `bool` | true = marca aliada, false = marca enemiga |
| `Negative` | `bool` | **S43 NEW** true = estado negativo/marca enemiga visible (rojo), false = aliado/positivo |

### CombatOrderEntry (S42 NEW)

Entrada de orden de acción para barra superior.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Side` | `CombatVisualSide` | Equipo (A o B) |
| `Index` | `int` | Índice within equipo (0..2) |
| `Alive` | `bool` | Si la unidad está viva |
| `State` | `CombatUnitState` | Estado actual (marcas, estados, afinidad) |

### CombatSpeechData (S43 NEW)

Datos de globo de habla cómic.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Side` | `CombatVisualSide` | Quién habla (A o B) |
| `Index` | `int` | Índice unit dentro equipo |
| `Text` | `string` | Texto del globo |
| `Color` | `Color` | Color borde globo y flecha |
| `HasColor` | `bool` | Si usar color custom o default |
| `Duration` | `float` | Segundos a mostrar globo |
| `Follow` | `Transform` | Transform hablante (para posicionar globo) |
| `HasTarget` | `bool` | Si mostrar flecha hacia objetivo |
| `TargetSide` | `CombatVisualSide` | Lado objetivo (solo si HasTarget) |
| `TargetIndex` | `int` | Índice objetivo within equipo |
| `TargetFollow` | `Transform` | Transform objetivo (para flecha) |

### CombatElementEventData (S45 NEW)

Evento elemental por-proc para actualizar marcas/estados de la barra en tiempo real.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Side` | `CombatVisualSide` | Equipo de la unidad afectada (A o B) |
| `Index` | `int` | Índice de la unidad within equipo (0..2) |
| `Kind` | `ElementEventKind` | Tipo de evento (MarkApplied, MarkRemoved, Reaction, StateArmed, StateConsumed, StateRemoved) |
| `Element` | `Element` | Elemento aplicado/removido |
| `ElementB` | `Element` | **S45 NEW** Segundo elemento en Reaction (para ambos reactantes) |
| `AllySource` | `bool` | true si marca/estado viene de aliado, false si enemigo |
| `State` | `ElementalState` | Estado elemental (para StateArmed/StateConsumed/StateRemoved) |

**S45:** Canal por-proc independiente de OnActionOrder — permite que la barra incremente marcas/estados en tiempo real conforme se disparan procs, sin esperar a fin de turno.

### CombatVisualLogLine

Línea de log con tipo.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Text` | `string` | Texto (puede llevar HTML color tags) |
| `Kind` | `CombatVisualLogKind` | Tipo de línea |

### CombatVisualPanelState (S42: ActionIndex como contador)

Estado de control del replay UI.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `TurnNumber` | `int` | **S42 RENAME** Antes TurnNumber, ahora ActionIndex (contador de acciones/turnos jugador, no Turns.Count del record) |
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
| `OnVisualCombatStart` | `CombatVisualContext` | Inicia replay (spawn fighters) |
| `OnVisualCombatEnd` | `CombatVisualSide, bool` | Termina replay (winner, isDraw) |
| `OnTurnStart` | `CombatTurn` | Comienza animación turno |
| `OnTurnEnd` | `CombatTurn` | Termina animación turno |
| `OnAttack` | `CombatVisualSide` | Comienza windup ataque |
| `OnHit` | `CombatVisualHit` | Golpe conecta (S41: con indices) |
| `OnPopup` | `CombatVisualPopup` | Popup a mostrar (S42: con Text/OverrideColor) |
| `OnCrit` | `CombatVisualHit` | Crítico (S41: con indices) |
| `OnHpChanged` | `CombatVisualSide, float, float` | HP cambió (side, cur, max) — 1v1 legacy |
| `OnDead` | `CombatVisualSide` | Unit muere — 1v1 legacy |
| `OnUnitHpChanged` | `CombatVisualSide, int, float, float` | **S41 NEW** HP unit cambió (side, index, cur, max) |
| `OnUnitDead` | `CombatVisualSide, int` | **S41 NEW** Unit muere (side, index) |
| `OnLog` | `string` | Línea log agregada |
| `OnPanelState` | `CombatVisualPanelState` | Estado panel control |
| `OnActionOrder` | `List<CombatOrderEntry>` | **S42 NEW** Orden de próxima acción (para barra) |
| `OnUnitAffinity` | `CombatVisualSide, int, int, int` | **S42 NEW** Afinidad/energía cambió (side, index, affinity, energy) |
| `OnActiveUnit` | `CombatVisualSide, int` | **S42 NEW** Unit activa en turno actual (side, index) |
| `OnSpeech` | `CombatSpeechData` | **S43 NEW** Emite globo de habla (Protector/Empático/Agresivo, narrador) |
| `OnUnitElement` | `CombatElementEventData` | **S45 NEW** Evento elemental por-proc (marca/estado cambió en tiempo real) |

**S41 Cambios:** Eventos 1v1 legacy (OnHpChanged/OnDead) intactos; nuevos eventos OnUnitHpChanged/OnUnitDead para 3v3.

**S42 Cambios:** Tres nuevos eventos para barra de orden + UI afinidad/energía.

**S43 Cambios:** Nuevo struct CombatSpeechData + evento OnSpeech; ElementChipData gana bool Negative.

**S45 Cambios:** Nuevo struct CombatElementEventData + evento OnUnitElement para actualizar marcas/estados per-proc (independiente de OnActionOrder).

## Métodos Helper Estáticos

| Método | Firma | Descripción |
|--------|-------|-------------|
| `VisualCombatStart` | `(CombatVisualContext ctx)` | Dispara `OnVisualCombatStart` |
| `VisualCombatEnd` | `(CombatVisualSide winner, bool isDraw)` | Dispara `OnVisualCombatEnd` |
| `TurnStart` | `(CombatTurn turn)` | Dispara `OnTurnStart` |
| `TurnEnd` | `(CombatTurn turn)` | Dispara `OnTurnEnd` |
| `Attack` | `(CombatVisualSide side)` | Dispara `OnAttack` |
| `Hit` | `(CombatVisualHit hit)` | Dispara `OnHit` |
| `Popup` | `(CombatVisualPopup p)` | Dispara `OnPopup` (S42: con Text/OverrideColor) |
| `Crit` | `(CombatVisualHit hit)` | Dispara `OnCrit` |
| `HpChanged` | `(CombatVisualSide side, float current, float max)` | Dispara `OnHpChanged` (1v1) |
| `Dead` | `(CombatVisualSide side)` | Dispara `OnDead` (1v1) |
| `UnitHpChanged` | `(CombatVisualSide side, int index, float current, float max)` | **S41 NEW** Dispara `OnUnitHpChanged` |
| `UnitDead` | `(CombatVisualSide side, int index)` | **S41 NEW** Dispara `OnUnitDead` |
| `Log` | `(string line)` | Dispara `OnLog` |
| `PanelState` | `(CombatVisualPanelState st)` | Dispara `OnPanelState` |
| `ActionOrder` | `(List<CombatOrderEntry> order)` | **S42 NEW** Dispara `OnActionOrder` |
| `UnitAffinity` | `(CombatVisualSide side, int index, int affinity, int energy)` | **S42 NEW** Dispara `OnUnitAffinity` |
| `ActiveUnit` | `(CombatVisualSide side, int index)` | **S42 NEW** Dispara `OnActiveUnit` |
| `Speech` | `(CombatSpeechData d)` | **S43 NEW** Dispara `OnSpeech` |
| `UnitElement` | `(CombatElementEventData d)` | **S45 NEW** Dispara `OnUnitElement` |

## Vinculado a

- [[CombatVisualizerService]] — único publisher (S41: por unit, S42: con orden/afinidad, S43: con speech, S45: con OnUnitElement per-proc)
- [[CombatVisualUnits]] — suscriptor de eventos spawn (S41)
- [[CombatDamageNumbers]] — suscriptor `OnPopup` (S42: renderiza ReactionName si Reaction)
- [[CombatOrderBarUITK]] — **S42 NEW** suscriptor (OnVisualCombatStart, OnActionOrder, OnUnitAffinity, OnActiveUnit); **S45 NEW** suscriptor OnUnitElement para actualizar marcas/estados por-proc
- [[CombatSpeechBubbles]] — **S43 NEW** suscriptor `OnSpeech` (renderiza globos cómic); **S45:** sin cambios
- [[CombatCameraDirector]] — **S43 NEW** suscriptor `OnActiveUnit` (maneja vcam priorities); **S45:** sin cambios
- [[MoriMonchiCombatVisualizer]] — suscriptor effects (1v1 legacy)
- [[MoriMonchiCombatVisualizerUITK]] — suscriptor panel state + elementos + shield; **S45:** sin cambios

## Cambios S41

**Aditivos (backward compatible):**
- `CombatVisualContext`: TeamA/TeamB + HpMaxTeamA/HpMaxTeamB (float[])
- `CombatVisualHit`: AttackerIndex/DefenderIndex
- Eventos nuevos: `OnUnitHpChanged(side, index, cur, max)` + `OnUnitDead(side, index)`

**Invariante:** Eventos 1v1 legacy (OnHpChanged/OnDead) aún se disparan para compat transicional.

## Cambios S42

**Aditivos (append-only):**
- `CombatVisualContext`: SnapsA/SnapsB (para CombatOrderBarUITK)
- `CombatVisualPopup`: Text + OverrideColor + HasOverrideColor (para popups de reacciones con nombre custom)
- `CombatVisualPanelState`: TurnNumber → ActionIndex (fix contador ronda vs acción)
- **Nuevos structs:** ElementChipData, CombatOrderEntry
- **Nuevos eventos:** OnActionOrder (orden de próxima acción para barra), OnUnitAffinity (afinidad/energía cambió), OnActiveUnit (unit activa en turno)
- **Nuevos helpers:** ActionOrder(), UnitAffinity(), ActiveUnit()

**Invariante:** Eventos 1v1 legacy siguen disparándose, structs viejos solo adquieren campos nuevos opcionales.

## Cambios S43

**Aditivos (append-only):**
- `ElementChipData`: bool Negative — marca si es estado negativo/enemigo para styling rojo en barra de orden
- **Nuevo struct:** `CombatSpeechData` (10 campos) — para globos cómic con borde coloreado, texto, flecha hacia objetivo, duración
- **Nuevo evento:** `OnSpeech(CombatSpeechData)` — disparado por PlayProc en CombatVisualizerService (Protector pre-golpe, Empático post-golpe, Agresivo al ganar energía, narrador en StateArmed)
- **Nuevo helper:** `Speech(CombatSpeechData d)` — wrapper que dispara `OnSpeech`

**Invariante:** Eventos/structs S42 siguen intactos; CombatSpeechData es aditivo para globos visuales.

## Cambios S45

**Aditivos (append-only):**
- **Nuevo struct:** `CombatElementEventData` (7 campos) — para eventos elementales por-proc (MarkApplied, MarkRemoved, Reaction, StateArmed, StateConsumed, StateRemoved)
- **Nuevo evento:** `OnUnitElement(CombatElementEventData)` — disparado por PlayProc en CombatVisualizerService cada vez que un proc elemental mueve/agrega/quita marcas o estados
- **Nuevo helper:** `UnitElement(CombatElementEventData d)` — wrapper que dispara `OnUnitElement`

**Invariante:** Eventos/structs S43 siguen intactos; CombatElementEventData es aditivo para actualizar barra de orden en tiempo real, sin esperar a fin de turno.

## Notas Implementación S43 / S45

- CombatSpeechBubbles suscribe OnSpeech y renderiza globo + flecha dinámicamente por frame
- CombatCameraDirector suscribe OnActiveUnit para cortes de cámara (vcam priority)
- PlayProc en CombatVisualizerService emite Speech con textos tweakeables (protectorLine, empaticoLine, agresivoLine)
- Speech por narrador (personaje nulo) en StateArmed: "¡Quedé {stateName}!" (rojo/verde según negativo)
- **S45:** PlayProc emite OnUnitElement por cada proc elemental (MarkApplied/MarkRemoved/Reaction/StateArmed/StateConsumed/StateRemoved); CombatOrderBarUITK suscriptor HandleUnitElement muta Marks/States lists por-proc para mostrar cambios en vivo
