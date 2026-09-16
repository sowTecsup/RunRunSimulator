---
tags: [script, world, expedition, dev, harness]
---

# ArenaMatrixDev.cs

**Ruta:** `World/Expedition/ArenaMatrixDev.cs`

**Responsabilidad:** Harness de desarrollo que ejecuta simulaciones de arena automatizadas (matriz de planes × rivales × semillas). Itera sobre combinaciones, aplica órdenes y personalidades a DNAs, ejecuta rondas, registra CSV. **S122:** Incluye Role en simulación (lee Role de Entry, abre bases, itera sobre bases en lugar de ocupaciones fijas).

**Propiedades públicas:**
- `ArenaSandbox Sandbox` — escena sandbox
- `ArenaRound Round` — ronda
- `ArenaClockControl Clock` — controlador de velocidad (opcional)
- `float Speed = 10f` — multiplicador
- `bool IsRunning`, `bool Done`
- `int Completed`, `int Total`
- `string OutputPath` — ruta CSV
- `string Progress` — para UI

**Métodos públicos:**
- `void Run(IReadOnlyList<ArenaMatrixTeam> players, IReadOnlyList<ArenaMatrixTeam> rivals, IReadOnlyList<int> seeds, string csvPath)` — inicia simulación
- `void Stop()` — aborta

**Flujo (S122 actualizado):**
1. Itera semillas → players → rivals
2. Para cada player/rival: **S122** lee Role y bases abiertas (vía ArenaBases)
3. `Apply(entry, Team)` — **(S122)** itera sobre ArenaBase en lugar de órdenes fijas
   - Para cada base abierta: calcula órdenes → inyecta en DNA
4. Round.Launch() → registra resultado
5. CSV: detalle incluye Role + base elegida

**Cambios S122:**
- **Apply() itera sobre bases:** no sobre órdenes predefinidas
- Cada simulación: (player_idx, rival_idx, base1, base2) → órdenes + salida
- CSV expandida con Role + base columns

**Invariantes:**
- S122: Role determinístico por Entry
- Regresión de balance: medir con ArenaMatrixDev tras cambios en ArenaBases

**Vinculado a:** [[Index/22 - Bajada Nocturna y Linaje]] (S122)

**Conexiones:** [[ArenaSandbox]], [[ArenaBases]], [[ArenaRosterSO]], [[ArenaRound]]
