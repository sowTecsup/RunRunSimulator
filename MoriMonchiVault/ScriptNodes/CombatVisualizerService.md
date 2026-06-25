---
tags: [script, combat]
---

# CombatVisualizerService.cs

**Ruta:** `Systems/CombatVisualizer/CombatVisualizerService.cs`

**Responsabilidad:** Singleton apex que reproduce visualmente un `CombatRecord` en escena (replay local, no simula). Dueño de la vida/destrucción de los dos combatientes visuales y del estado de reproducción.

**Arquitectura por lista doblemente enlazada:** `BuildStates()` precomputa una cadena de `CombatNode` (uno por estado del combate), cada uno con `Prev`/`Next`, el `CombatTurn` que llevó a él, HP A/B, vivos/muertos, nº de turno y el log acumulado. Navegar = mover el puntero `current`. `head` = estado inicial (100%). El último nodo es el final (`IsEnd`).

**Slots fijos: A = tu MM (`self`), B = oponente.** No hay swap. Cada turno se orienta con `attackerIsSelf = (turn.AttackerIsA == record.SelfWasA)` (cada pelea se guarda en ambas criaturas con su POV; sin este mapeo el replay saldría espejado). El nombre del oponente sale de `record.OpponentName` (autoritativo, coincide con el log).

**API de control (la llama el panel / DEV harness):**
- `Play(self, opponent, record)`: construye estados y arranca **en pausa** en `head`.
- `TogglePlay()` / `SetAuto(bool)`: modo auto (avanza solo respetando `playbackSpeed`). Si estaba al final, reinicia desde `head`.
- `Next()` / `Back()`: paso manual (pausan el auto). `Back` revive al derrotado (reconstruye el estado del nodo previo).
- `SetSpeed(float)`: 0.25x–4x; divide los timings (windup/impacto/entre-turnos/pausa de muerte).
- `Stop()`: corta coroutines y despawnea.

**Forward vs Restore:** `ForwardRoutine` = transición con juice (windup → hit/crit → HP tween → muerte) disparando los eventos granulares. `Restore(node)` = estado puro (para `Back`/seek): fija visibilidad + HP + log sin juice. `busy` bloquea inputs durante la animación.

**Muerte estilo Pokémon:** al llegar a 0 el defensor, tras `deathPauseSeconds` se hace `SetActive(false)` (desaparece); al final queda el ganador. `Back` lo reactiva.

**Barras por referencia directa:** guarda `barA`/`barB` (no por evento de side). `PushHp(side,…)` empuja a la barra correcta y además dispara `OnHpChanged` para los hooks. Los nombres se bindean tras 2 frames (cuando el UIDocument ya construyó su árbol).

**DBs por `GameManager.Instance`** (`Database`/`PartVisualBank`/`FurTypeDatabase`); las únicas refs de inspector son `visualizerPrefab` + `slotA`/`slotB` + timings + `playbackSpeed`.

**Log coloreado:** construye `CombatVisualLogLine` con rich-text — nombre local azul (`#5AA0FF`), oponente rojo (`#FF6B6B`), daño rojo (`#FF3B3B`).

**DEV — Test Harness (Odin, solo Play):** dropdowns Combatiente A / Pelea (sin Rival B: se autoresuelve por `record.OpponentName`), "🎲 MM al azar con pelea", "▶ Simular" y fila ◀/▶❚❚/▶▶.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatVisualEvents]], [[CombatRecord]], [[CombatTurn]], [[CreatureDNA]], [[MoriMonchiVisualizer]], [[MoriMonchiCombatVisualizerUITK]], [[CombatVisualizerPanelUITK]], [[CombatService]], [[GameManager]]
