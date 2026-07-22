---
tags: [combat, visualization, events, bus, 3v3]
---

# CombatVisualEvents

Bus estático de eventos para la visualización de combates (replay). Centraliza toda comunicación entre `CombatVisualizerService` (orquestador) y subscribers (UI, animadores, popups). **S61b:** Enum `CombatTurnPhase` nuevo + evento `OnPhase(phase, actorSide)` para sincronizar cámaras Cinemachine por etapa del turno. **S61:** Evento nuevo `OnLogAppend(CombatVisualLogLine)` para append incremental del log en tiempo real (una línea por beat de proc). **S58:** `CombatVisualLogLine` gana campos `HasUnit`, `UnitSide`, `UnitIndex` para filtrado en UI (mostrar solo reacciones/muertes con unit marker). **S59:** evento nuevo `OnUnitHover(CombatVisualSide, int, bool)` para hover externo (UI card slot).

## Responsabilidad

Transportar datos de replay (contexto, turnos, hits, popups, log, speech, orden, eventos elementales por-proc, hover, fases) desde el visualizador a listeners sin acoplamiento directo. Un publisher central para ~20 eventos de replay.

## Enums

### CombatVisualSide

| Valor | Uso |
|-------|-----|
| `A` | Equipo/combatiente A (generalmente self) |
| `B` | Equipo/combatiente B (generalmente opponent) |

### CombatTurnPhase (S61b NEW)

| Valor | Uso |
|-------|-----|
| `Rest` | Pausa entre turnos, inicio/fin combate — cámaras de escena en prioridad base |
| `Passives` | Ejecutan pasivas aliadas — cámara hacia tablero del actor |
| `Attack` | Ejecuta ataque principal — cámara hacia tablero del objetivo (opuesto) |

**Propósito:** Sincronizar cutaways de Cinemachine por etapa del turno. `CombatCameraDirector` suscriptor.

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

### CombatVisualContext

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
| `TeamA` | `List<CreatureDNA>` | Equipo A (3v3) |
| `TeamB` | `List<CreatureDNA>` | Equipo B (3v3) |
| `HpMaxTeamA` | `float[]` | HP máx por unit en A |
| `HpMaxTeamB` | `float[]` | HP máx por unit en B |
| `SnapsA` | `List<CombatFighterSnapshot>` | Snapshots equipo A |
| `SnapsB` | `List<CombatFighterSnapshot>` | Snapshots equipo B |

### CombatVisualHit

Datos de un golpe.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Attacker` | `CombatVisualSide` | Quién ataca (A o B) |
| `Defender` | `CombatVisualSide` | Quién recibe (A o B) |
| `Damage` | `float` | Cantidad daño |
| `Crit` | `bool` | Si fue crítico |
| `AttackerIndex` | `int` | Índice within equipo atacante (0..2) |
| `DefenderIndex` | `int` | Índice within equipo defensor (0..2) |

### CombatVisualPopup

Datos de popup flotante.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Side` | `CombatVisualSide` | Quién recibe popup (A o B) |
| `Position` | `Vector3` | Posición mundo (si Follow es null) |
| `Kind` | `CombatPopupKind` | Tipo (Hit, Crit, Poison, Reaction, Shield, etc.) |
| `Amount` | `float` | Magnitud (daño, curación, escudo, etc.) |
| `Follow` | `Transform` | Transform del luchador para seguimiento |
| `Text` | `string` | Texto custom (p.ej. ReactionName) |
| `OverrideColor` | `Color` | Color custom (p.ej. color del elemento) |
| `HasOverrideColor` | `bool` | Si usar OverrideColor o palette |

### CombatOrderEntry

Entrada de orden de acción para barra superior.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Side` | `CombatVisualSide` | Equipo (A o B) |
| `Index` | `int` | Índice within equipo (0..2) |
| `Alive` | `bool` | Si la unidad está viva |
| `State` | `CombatUnitState` | Estado actual (marcas, estados, afinidad) |

### CombatSpeechData

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

### CombatElementEventData

Evento elemental por-proc para actualizar marcas/estados de la barra en tiempo real.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Side` | `CombatVisualSide` | Equipo de la unidad afectada (A o B) |
| `Index` | `int` | Índice de la unidad within equipo (0..2) |
| `Kind` | `ElementEventKind` | Tipo de evento (MarkApplied, MarkRemoved, Reaction, StateArmed, StateConsumed, StateRemoved) |
| `Element` | `Element` | Elemento aplicado/removido |
| `ElementB` | `Element` | Segundo elemento en Reaction |
| `AllySource` | `bool` | true si marca/estado viene de aliado |
| `State` | `ElementalState` | Estado elemental (para StateArmed/etc) |
| `ReactionName` | `string` | Nombre de la reacción (para parsing a ElementalState o display) |

### CombatVisualLogLine

Línea de log con tipo e información de unidad.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Text` | `string` | Texto (puede llevar HTML color tags) |
| `Kind` | `CombatVisualLogKind` | Tipo de línea |
| `HasUnit` | `bool` | **S58** Si esta línea representa a una unidad (reacción/muerte) |
| `UnitSide` | `CombatVisualSide` | **S58** Lado de la unidad (A o B), si HasUnit=true |
| `UnitIndex` | `int` | **S58** Índice de la unidad, si HasUnit=true |

**S58:** Las líneas de reacción (Proc/Reaction) y muerte (Death) llevan `HasUnit=true` + `UnitSide/UnitIndex` para filtrado en UI. La línea Versus inicial no tiene unit marker. Resultado final tiene `HasUnit=false`.

### CombatVisualPanelState

Estado de control del replay UI.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `TurnNumber` | `int` | Contador de acciones/turnos jugador |
| `TotalTurns` | `int` | Total turnos/acciones |
| `Log` | `CombatVisualLogLine[]` | Log acumulado (S58: con unit markers) |
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
| `OnHit` | `CombatVisualHit` | Golpe conecta (con indices) |
| `OnPopup` | `CombatVisualPopup` | Popup a mostrar (con Text/OverrideColor) |
| `OnCrit` | `CombatVisualHit` | Crítico |
| `OnHpChanged` | `CombatVisualSide, float, float` | HP cambió (side, cur, max) — 1v1 legacy |
| `OnDead` | `CombatVisualSide` | Unit muere — 1v1 legacy |
| `OnUnitHpChanged` | `CombatVisualSide, int, float, float` | HP unit cambió (side, index, cur, max) |
| `OnUnitDead` | `CombatVisualSide, int` | Unit muere (side, index) |
| `OnLog` | `string` | Línea log agregada |
| `OnLogAppend` | **S61 NEW** `CombatVisualLogLine` | **S61** Línea log appended en vivo (una por beat de proc) — subscriptor agrega inmediatamente a UI sin rebuild |
| `OnPanelState` | `CombatVisualPanelState` | Estado panel control |
| `OnActionOrder` | `List<CombatOrderEntry>` | Orden de próxima acción |
| `OnUnitAffinity` | `CombatVisualSide, int, int` | Afinidad cambió (side, index, affinity) — sin energy |
| `OnActiveUnit` | `CombatVisualSide, int` | Unit activa en turno |
| `OnSpeech` | `CombatSpeechData` | Globo de habla |
| `OnUnitElement` | `CombatElementEventData` | Evento elemental por-proc (con ReactionName) |
| `OnUnitHover` | **S59 NEW** `CombatVisualSide, int, bool` | Hover externo de UI (side, index, hover=true/false) — emitido por CombatOrderBarUITK al entrar/salir slot |
| `OnPhase` | **S61b NEW** `CombatTurnPhase, CombatVisualSide` | Etapa del turno (Rest/Passives/Attack) + lado del actor — emitido por CombatVisualizerService.ForwardRoutine |

## Cambios S61b

**Enum CombatTurnPhase nuevo:**
```csharp
public enum CombatTurnPhase { Rest, Passives, Attack }
```

**Evento OnPhase nuevo:**
```csharp
public static event Action<CombatTurnPhase, CombatVisualSide> OnPhase;
public static void Phase(CombatTurnPhase phase, CombatVisualSide actorSide) => OnPhase?.Invoke(phase, actorSide);
```

**Propósito:**
- Sincronizar Cinemachine cutaways por etapa del turno
- `CombatCameraDirector.HandlePhase()` suscriptor — conmuta prioridades de 3 cámaras estáticas (sceneCamera, allyCamera, enemyCamera) según fase
- Antes S61b: DirectorService seguía por unidad activa (VCamOf); ahora pasivo por fase

**Flujo S61b:**
1. `CombatVisualizerService.ForwardRoutine()` comienza
2. Si hay pasivas armadas: emite `Phase(Passives, actorSide)` → cámara hacia tablero del actor (phasePriority=30)
3. Espera `phasePauseSeconds` (0.9s)
4. Entra ataque: emite `Phase(Attack, actorSide)` → cámara hacia tablero opuesto (objetivo)
5. Fin etapa: emite `Phase(Rest, A)` → cámaras escena en prioridad base (scenePriority=10)

## Cambios S61

**Evento OnLogAppend nuevo (línea 149-150):**
```csharp
public static event Action<CombatVisualLogLine> OnLogAppend;
public static void LogAppend(CombatVisualLogLine line) => OnLogAppend?.Invoke(line);
```

**Contexto:**
- **Antes S61:** Log construido al completo en `OnPanelState` (rebuild total del panel)
- **S61:** OnLogAppend permite **append incremental** — una línea por beat de proc
- Dispara sincronizado con `OnUnitElement` (después del chip elemental en barra)

**Flujo S61:**
1. PlayProc() en CombatVisualizerService
2. Si `pe.ElementEvent == ElementEventKind.Reaction`: emite `LogAppend()` con línea Proc/Reaction
3. CombatVisualizerPanelUITK.HandleLogAppend() recibe → AddCard(line) → ScrollLogToEnd()
4. Resultado: Línea aparece en log conforme se reproduce el beat

**Propósito:**
- Sincronización visual: el log se actualiza en tiempo real con la animación
- Sin rebuild: AddCard es O(1) vs RebuildLog O(n)
- Permite future: auto-scroll a la línea activa, highlight del beat actual

**Suscriptores S61:**
- `CombatVisualizerPanelUITK.HandleLogAppend()` — append e historia en vivo

## Cambios S58

**CombatVisualLogLine cambios:**
- Nuevos campos: `HasUnit` (bool), `UnitSide` (CombatVisualSide), `UnitIndex` (int)
- Aditivo — no rompe código viejo (inicializa a default si no se setean)
- Línea Versus: `HasUnit=false` (no renderiza)
- Línea Reacción: `HasUnit=true, UnitSide=?, UnitIndex=?` (renderiza con headshot)
- Línea Muerte: `HasUnit=true, UnitSide=?, UnitIndex=?` (renderiza con headshot)
- Línea Resultado: `HasUnit=false` (renderiza sin headshot)
- Líneas Hit/Crit: `HasUnit=false` (no se muestran en log filtrado "Eventos")

**Impacto en UI:**
- CombatVisualizerPanelUITK.RebuildLog() filtra: `if (!line.HasUnit && line.Kind != CombatVisualLogKind.Result) continue;` — solo muestra reacciones/muertes (con unit marker) + resultado final
- Cada línea con HasUnit muestra mini-headshot del MM afectado (ResolveDna desde contexto)

## Cambios S59

**Evento OnUnitHover nuevo (línea 167-168):**
- Parámetros: `(CombatVisualSide side, int index, bool hover)`
- Significado: `hover=true` al entrar slot, `hover=false` al salir
- Emitido por: CombatOrderBarUITK.BuildTeam() (línea 139-140)
- Consumido por: CombatRadialHealthBar.HandleUnitHover() (línea 93-98) — setea `externalHover`
- Propósito: Hacer visible el anillo de vida del unit al pasar sobre su card en la barra de orden

## Vinculado a

- [[CombatVisualizerService]] — único publisher principal (S61b: emite OnPhase; S61: emite OnLogAppend en PlayProc)
- [[CombatCameraDirector]] — **S61b NEW** suscriptor OnPhase, HandlePhase(phase, actorSide) conmuta cámaras por etapa
- [[CombatOrderBarUITK]] — **S59** publisher OnUnitHover, suscriptor (OnActionOrder, OnUnitAffinity, OnActiveUnit, OnUnitElement)
- [[CombatRadialHealthBar]] — **S59** suscriptor OnUnitHover, (OnUnitHpChanged legacy)
- [[CombatVisualizerPanelUITK]] — **S61** nuevo handler OnLogAppend (append en vivo); **S58** suscriptor OnPanelState (filtra log por HasUnit)
- [[CombatSpeechBubbles]] — suscriptor OnSpeech
- [[CombatDamageNumbers]] — suscriptor OnPopup
- [[CombatPedestalHighlighter]] — **S58 NEW** suscriptor OnActiveUnit/OnVisualCombatEnd

## Notas S61b

- OnPhase es aditivo — no rompe código viejo (CombatCameraDirector es nuevo suscriptor)
- Fases emitidas: **Rest** (pausa/init/end), **Passives** (pasivas aliadas), **Attack** (ataque principal)
- actorSide parámetro permite filtrado de lógica (ej: cámara al actor en pasivas, al defensor en ataque)
- Cinemachine CM automáticamente interpola (blend 0.6s) entre cortes — suavidad sin lerp manual

## Notas S61

- OnLogAppend es aditivo con OnLog — ambos existen, OnLog para rebuild total, OnLogAppend para append incremental
- Sincronización: LogAppend emite después de UnitElement en el mismo beat
- AddCard es método eficiente para append (no reconstruye toda la UI)
- ScrollLogToEnd auto-scrollea en cada línea nueva

## Notas S59

- OnUnitHover es aditivo: no rompe subscribers existentes, solo nuevo listener (CombatRadialHealthBar).
- Flujo hover: CombatOrderBarUITK emite → CombatRadialHealthBar.externalHover setea → UpdateVisibility() fade canvas.
- El raycast local (mathHover) en CombatRadialHealthBar coexiste con hover externo: OR lógico.
