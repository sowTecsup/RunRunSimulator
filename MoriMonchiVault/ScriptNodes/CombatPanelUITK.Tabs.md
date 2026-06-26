---
tags: [script, ui, partial]
---

# CombatPanelUITK.Tabs.md

**Ruta:** `UI/CombatPanelUITK.Tabs.cs`

**Responsabilidad:** Contenido de las 4 pestañas del panel de combate: Batalla Online (Tab 0), Combate Local (Tab 1), Resultados (Tab 2), Historial (Tab 3).

**Vinculado a:** [[CombatPanelUITK]], [[Index/05 - UI System]]

**Conexiones:** [[CombatService]], [[AsyncCombatService]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]]

**Métodos principales:**

- `RebuildOnlineList()`: lista de criaturas elegibles (Tab 0) vía `MakeCandidate()`
- `RebuildFighterLists()`: dos listas izquierda/derecha para seleccionar combatientes (Tab 1)
- `MakeCandidate(dna, bucket, onClick)`: crea una fila con nombre + 6 stats (CON/ATK/SPD/DEF/LCK/EVA) + ratio peleas/límite. Usa `StatsOf(dna)` (heredado de clase principal)
- `SetCenter()`: muestra detalles del candidato online seleccionado (imagen, nombre, stats, partes)
- `EnqueueOnline(instant)`: envía a async combate (Instant o Scheduled)
- `SelectFighterA()`, `SelectFighterB()`: selecciona combatientes locales
- `RefreshSlots()`: actualiza slots A/B con nombres e imágenes
- `DoLocalFight()`: ejecuta combate local vía `CombatService.Simulate()`, dispara `GameEvents.CombatCompleted()`
- `RebuildResults()`: Tab 3 con criaturas en cola (BusyState == QueuedForCombat)
- `DoRefresh()`: poll async via `AsyncCombatService.PollResultsAsync()`
- `UpdateClock()`: countdown a próximo tick servidor (hh:mm)
- `RebuildHistory()`: Tab 4 — historial de combates (flattened, all creatures)
- `RebuildHistoryFilter()`: dropdown "Todos" + creatures con historia
- `RebuildHistoryList()`: muestra combates filtrados (outcome, oponente, fecha)
- `ShowHistory(it)`: detalles de un combate (turnos, daño, resultado)

**Stats mostrados:**
- `MakeCandidate`: "CON X ATK Y SPD Z DEF A LCK B EVA C"
- `SetCenter`: "CON X   ATK Y   SPD Z   DEF A   LCK B   EVA C"
