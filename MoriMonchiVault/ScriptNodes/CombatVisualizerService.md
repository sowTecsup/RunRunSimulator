---
tags: [combat, visualization, service, replay, ui]
---

# CombatVisualizerService

MonoBehaviour singleton que orquesta la visualización local de un `CombatRecord`, construyendo un árbol de nodos doblemente enlazados y generando la secuencia de animaciones turno-a-turno. Maneja playback manual (fwd/back) y automático, mapea datos de sim a visual (HP, muerte, popups).

## Responsabilidad

Transformar `CombatRecord` en una reproducción visual: armar los nodos de replay, animar cada turno en secuencia, aplicar procs visualmente, rasurar popups y log, sincronizar HP bars, disparar eventos visuales via `CombatVisualEvents`.

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Instance` | `CombatVisualizerService` | Singleton |
| `IsPlaying` | `bool` | Si hay un replay activo |
| `IsAuto` | `bool` | Si playback está en automático |
| `Speed` | `float` | Multiplicador de velocidad (clamped 0.01-max) |

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Play(CreatureDNA self, CreatureDNA opponent, CombatRecord record)` | Inicia replay de un record |
| `Stop()` | Detiene playback y limpia estado |
| `TogglePlay()` | Toggle automático |
| `SetAuto(bool value)` | Setea playback automático |
| `Next()` | Avanza un turno (manual) |
| `Back()` | Retrocede un turno (manual) |
| `SetSpeed(float value)` | Setea velocidad de playback |

## Métodos Privados

### Construcción y Estado

| Método | Descripción |
|--------|-------------|
| `BuildStates()` | Construye árbol de nodos `CombatNode` desde `CombatRecord.Turns` |
| `Restore(CombatNode node)` | Restituye escena a un nodo (jump a turno, restore HP/vivos) |

### Animación

| Método | Descripción |
|--------|-------------|
| `BeginRoutine()` | Corrutina de setup inicial (spawn, bind UI) |
| `AutoRoutine()` | Corrutina de playback automático (loop Next + wait) |
| `ForwardRoutine()` | Corrutina de un turno: procs before, golpe, procs after, muerte |
| `PlayProc(CombatProcEvent pe)` | Corrutina de un proc: popup, HP delta, wait |

### Mapeando Sim → Visual

| Método | Descripción |
|--------|-------------|
| `SimToVisual(bool simIsA)` | Convierte `simIsA` en `CombatVisualSide` basado en `SelfWasA` |
| `FighterPos(CombatVisualSide side)` | Retorna posición mundo del fighter |
| `ProcPopupKind(ModifierEffectKind k)` | Mapea tipo de proc a `CombatPopupKind` |
| `ProcText(CombatProcEvent pe, string who, float delta)` | Genera texto descriptivo del proc |
| `RaiseProcPopup(CombatProcEvent pe, CombatVisualSide side, float delta)` | Dispara evento popup (con lógica especial para Stun) |

### Utilidades

| Método | Descripción |
|--------|-------------|
| `Publish()` | Publica estado actual via `CombatVisualEvents.PanelState()` |
| `PushHp(CombatVisualSide side, float hp, float max)` | Sincroniza HP visual + barra + tracking |
| `SetFighterActive(CombatVisualSide side, bool active)` | Activa/desactiva GameObject del fighter |
| `SpawnFighters(CreatureDNA dnaA, CreatureDNA dnaB)` | Instancia prefabs visuales |
| `DespawnFighters()` | Destruye instancias visuales |

### DEV Test Harness

| Método | Descripción |
|--------|-------------|
| `DevPickRandom()` | Elige criatura + pelea al azar con turnos |
| `DevSimulate()` | Dispara replay de creature/fight seleccionada |
| `DevBack()`, `DevTogglePlay()`, `DevNext()` | Atajos de botones |

## Clases Internas

### CombatNode

Nodo doblemente enlazado en árbol de replay.

**Campos:**
- `bool HasTurn` — si este nodo representa un turno real (false para cabeza)
- `CombatTurn Turn` — el turno (null si !HasTurn)
- `float HpA, HpB` — HP acumulado tras este turno
- `bool ADead, BDead` — si A/B han muerto
- `bool ADiedHere, BDiedHere` — si A/B murieron EN este turno
- `int TurnNumber` — número de turno
- `CombatVisualSide Attacker, Defender`
- `bool Crit` — si golpe fue crítico
- `List<CombatVisualLogLine> Log` — líneas acumuladas hasta aquí
- `CombatNode Prev, Next` — enlaces

**Métodos:**
- `FireWindup(a, b)` — anima windup (si !NoAttack)
- `FireImpact(a, b, maxA, maxB)` — anima impacto + daño visual (si !NoAttack)
- `FireDeath(a, b)` — anima muerte si ocurrió

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `instanceA`, `instanceB` | `MoriMonchiVisualizer` | Prefabs instanciados |
| `hooksA`, `hooksB` | `MoriMonchiCombatVisualizer` | Refs a hooks de animación |
| `barA`, `barB` | `MoriMonchiCombatVisualizerUITK` | Refs a UI bars |
| `animA`, `animB` | `MoriMonchiProceduralAnimator` | Refs a animadores |
| `selfDna`, `oppDna` | `CreatureDNA` | DNAs actual |
| `activeRecord` | `CombatRecord` | Record en replay |
| `head`, `current` | `CombatNode` | Nodos de árbol (head = inicio, current = posición) |
| `totalTurns` | `int` | Cantidad de turnos |
| `hpMaxA`, `hpMaxB` | `float` | HP máx calculado |
| `shownHpA`, `shownHpB` | `float` | HP mostrado actualmente (para delta en popup) |
| `statsA`, `statsB` | `CombatService.EffectiveStats` | Stats finales |
| `endIsDraw`, `endWinner` | `bool`, `CombatVisualSide` | Resultado final |
| `isAuto`, `busy` | `bool` | Estados de playback |
| Corrutinas | `Coroutine` | `beginRoutine`, `autoRoutine`, `fwdRoutine` |

## Cambios Sesión 31

**MODIFICADO:** `BuildStates()`, `ForwardRoutine()`, métodos de helpers de proc

1. **BuildStates():** Ahora procesa `Turn.Procs` aplicando before-strike y after-strike en HP acumulado; genera lineas de log tipo `Proc`
2. **CombatNode:** Nuevos campos `ADiedHere`, `BDiedHere` (no solo ADead/BDead); `FireWindup`/`FireImpact` respetan `Turn.NoAttack`
3. **ForwardRoutine():** Reescrito para animar before procs → golpe (si !NoAttack) → after procs; llama `PlayProc()` para cada proc
4. **PlayProc():** Nueva corrutina; anima popup + HP delta
5. **Helpers nuevos:**
   - `SimToVisual()` — convierte bool simIsA a side
   - `ProcPopupKind()` — mapea ModifierEffectKind → CombatPopupKind
   - `ProcText()` — genera texto descriptivo
   - `RaiseProcPopup()` — dispara evento popup con lógica especial
   - `FighterPos()` — posición mundo para popups
6. **PushHp():** Trackea `shownHpA`/`shownHpB` para calcular delta en procs

Backward compatible: records viejos sin procs animan normalmente (listas vacías).

## Vinculado a

- [[CombatRecord]] — lee `Turn` y `Turn.Procs`
- [[CombatService]] — cálculos de stats via `GetEffectiveStats()`
- [[CombatVisualEvents]] — publisher de todos los eventos
- [[CombatDamageNumbers]] — consumidor de `OnPopup`
- [[MoriMonchiVisualizer]] — prefab a instanciar
- [[MoriMonchiCombatVisualizer]] — hooks de animación

## Conexiones

**Entrada:**
- `Play(self, opponent, record)` — llamado desde UI/test

**Salida:**
- `CombatVisualEvents.OnVisualCombatStart`, `.OnTurnStart`, `.OnHit`, `.OnPopup`, `.OnDead`, etc.
- Referencias a fighters: instancias y animadores

## Notas

- **Árbol de nodos:** Doubly linked list permite jump fwd/back eficientemente
- **HP tracking:** `shownHpA/B` para calcular delta en proctext; `PushHp()` sincroniza barra
- **NoAttack:** Turnos sin golpe (stun-skip, muerte por status) saltan animación windup/impact
- **Popups:** Levantados vía `RaiseProcPopup()` que filtra por delta pequeño (Stun siempre popup)
- **Sincronización:** Restore() salta a nodo arbitrario, respaldando animadores a estado muerto/vivo
