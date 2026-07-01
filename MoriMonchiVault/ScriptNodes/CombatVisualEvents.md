---
tags: [combat, visualization, events, bus]
---

# CombatVisualEvents

Bus estático de eventos para la visualización de combates (replay). Centraliza toda comunicación entre `CombatVisualizerService` (orquestador) y subscribers (UI, animadores, popups). Es puramente un `public static class` con eventos y métodos helper.

## Responsabilidad

Transportar datos de replay (contexto, turnos, hits, popups, log) desde el visualizador a todos los listeners sin acoplamiento directo. Un publisher central para los 8 eventos de replay.

## Enums

### CombatVisualSide

| Valor | Uso |
|-------|-----|
| `A` | Combatante A (generalmente self) |
| `B` | Combatante B (generalmente opponent) |

### CombatVisualLogKind

| Valor | Uso |
|-------|-----|
| `Versus` | Pantalla inicial (vs mensaje) |
| `Hit` | Golpe normal |
| `Crit` | Golpe crítico |
| `Death` | Muerte de combatiente |
| `Result` | Resultado final (ganador/empate) |
| `Proc` | **NUEVO S31** Evento de proc (status tick, daño status, curación, stun, etc.) |

## Structs

### CombatVisualContext

Contexto inicial de replay.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `DnaA` | `CreatureDNA` | DNA del combatiente A |
| `DnaB` | `CreatureDNA` | DNA del combatiente B |
| `HpMaxA` | `float` | HP máx de A (calculado en BuildStates) |
| `HpMaxB` | `float` | HP máx de B |
| `SlotA` | `Transform` | Transform donde spawnar A |
| `SlotB` | `Transform` | Transform donde spawnar B |
| `TotalTurns` | `int` | Cantidad de turnos totales en record |

### CombatVisualHit

Datos de un golpe.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Attacker` | `CombatVisualSide` | Quién ataca (A o B) |
| `Defender` | `CombatVisualSide` | Quién recibe (A o B) |
| `Damage` | `float` | Cantidad de daño |
| `Crit` | `bool` | Si fue crítico |

### CombatVisualPopup

**NUEVO S31** Datos de un popup flotante.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Side` | `CombatVisualSide` | Quién recibe el popup (A o B) |
| `Position` | `Vector3` | Posición mundo donde aparecer |
| `Kind` | `CombatPopupKind` | Tipo de popup (Hit, Crit, Poison, etc.) |
| `Amount` | `float` | Magnitud (daño, curación, turnos stun) |

### CombatVisualLogLine

Línea de log con color/tipo.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Text` | `string` | Texto (puede llevar tags HTML color) |
| `Kind` | `CombatVisualLogKind` | Tipo de línea (Versus, Hit, Proc, etc.) |

### CombatVisualPanelState

Estado de control del replay UI.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `TurnNumber` | `int` | Turno actual (0 = inicio) |
| `TotalTurns` | `int` | Total de turnos |
| `Log` | `CombatVisualLogLine[]` | Log del estado actual |
| `Ended` | `bool` | Si alcanzamos el final |
| `IsDraw` | `bool` | Si fue empate |
| `Winner` | `CombatVisualSide` | Ganador (A o B) |
| `IsAuto` | `bool` | Si playback está en automático |
| `CanForward` | `bool` | Si puede avanzar turno |
| `CanBack` | `bool` | Si puede retroceder turno |
| `Speed` | `float` | Velocidad de playback (0.25-4x) |

## Eventos Estáticos

| Evento | Parámetros | Descripción |
|--------|-----------|-------------|
| `OnVisualCombatStart` | `CombatVisualContext` | Inicia replay (spawn fighters) |
| `OnVisualCombatEnd` | `CombatVisualSide, bool` | Termina replay (winner, isDraw) |
| `OnTurnStart` | `CombatTurn` | Comienza animación de turno |
| `OnTurnEnd` | `CombatTurn` | Termina animación de turno |
| `OnAttack` | `CombatVisualSide` | Comienza windup de ataque |
| `OnHit` | `CombatVisualHit` | Golpe conecta |
| `OnPopup` | `CombatVisualPopup` | **NUEVO S31** Popup flotante a mostrar |
| `OnCrit` | `CombatVisualHit` | Crítico confirmado (duplicado de OnHit con crit=true) |
| `OnHpChanged` | `CombatVisualSide, float, float` | HP cambió (side, current, max) |
| `OnDead` | `CombatVisualSide` | Combatiente muere |
| `OnLog` | `string` | Línea de log agregada |
| `OnPanelState` | `CombatVisualPanelState` | Estado del panel de control (turnos, botones) |

## Métodos Helper Estáticos

| Método | Firma | Descripción |
|--------|-------|-------------|
| `VisualCombatStart` | `(CombatVisualContext ctx)` | Dispara `OnVisualCombatStart` |
| `VisualCombatEnd` | `(CombatVisualSide winner, bool isDraw)` | Dispara `OnVisualCombatEnd` |
| `TurnStart` | `(CombatTurn turn)` | Dispara `OnTurnStart` |
| `TurnEnd` | `(CombatTurn turn)` | Dispara `OnTurnEnd` |
| `Attack` | `(CombatVisualSide side)` | Dispara `OnAttack` |
| `Hit` | `(CombatVisualHit hit)` | Dispara `OnHit` |
| `Popup` | `(CombatVisualPopup p)` | **NUEVO S31** Dispara `OnPopup` |
| `Crit` | `(CombatVisualHit hit)` | Dispara `OnCrit` |
| `HpChanged` | `(CombatVisualSide side, float current, float max)` | Dispara `OnHpChanged` |
| `Dead` | `(CombatVisualSide side)` | Dispara `OnDead` |
| `Log` | `(string line)` | Dispara `OnLog` |
| `PanelState` | `(CombatVisualPanelState st)` | Dispara `OnPanelState` |

## Vinculado a

- [[CombatVisualizerService]] — único publisher (levanta todos los eventos)
- [[CombatDamageNumbers]] — suscriptor de `OnPopup`
- [[MoriMonchiCombatVisualizer]] — suscriptor de OnHit/OnCrit/OnDead/OnHpChanged
- [[MoriMonchiCombatVisualizerUITK]] — suscriptor de OnHpChanged/OnPanelState

## Conexiones

**Entrada:**
- `CombatVisualizerService` (todos los eventos)

**Salida:**
- Múltiples subscribers pasivos vía `event` estático

## Cambios Sesión 31

**MODIFICADO:**
1. Nuevo enum value `CombatVisualLogKind.Proc` para lines de log de eventos de status/procs
2. Nuevo struct `CombatVisualPopup` con campos Side, Position, Kind, Amount
3. Nuevo evento estático `Action<CombatVisualPopup> OnPopup` + método helper `Popup(...)`

Backward compatible: eventos viejos intactos, nuevos son aditivos.

## Notas

- **Event pattern:** `static event Action<T>` mantiene suscriptores vivos (leak potencial si se olvida desuscribir)
- **Desuscripción:** `OnEnable` suscribe, `OnDisable` desuscribe (obligatorio para MonoBehaviours)
- **Publisher único:** `CombatVisualizerService` es el único que dispara eventos
- **Popups:** Levantados por visualizador via `Popup()`, transportan tipo+magnitud para `CombatDamageNumbers`
