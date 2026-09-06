---
tags: [script, world, expedition, orchestrator]
---

# ArenaRound.cs

**Ruta:** `World/Expedition/ArenaRound.cs`

**Responsabilidad:** Orquestador de tiempo y puntuación de una ronda de arena. Máquina de estados (reposo → activa → finalizada). **S102 NUEVO:** separa construcción de ronda (Launch/Reset) de lógica de puntuación. Launch = SpawnCast + Begin. Reset(newSeed) = ResetRoom + contador a cero. Determina ganador por mayoría de puntos asegurados. Propiedades de lectura: Elapsed, Remaining, PlayerSecured, RivalSecured, Winner. Congela puntuaciones en End().

## Campos Serializados

- **sandbox** (ArenaSandbox, Required)
- **roundSeconds** (float, default 90f, Min 10f) — duración del round
- **autoStart** (bool, default false) — **S102 NUEVO:** false por defecto (se llama Launch desde ArenaPlanPanel)

## Propiedades Públicas (S102)

- **IsRunning → bool** — el round está en progreso
- **IsOver → bool** — el round terminó
- **Elapsed → float** — tiempo transcurrido desde Begin()
- **Remaining → float** — Max(0, roundSeconds - Elapsed)
- **PlayerSecured → int** — unidades aseguradas Player (vivo o congelado)
- **RivalSecured → int** — unidades aseguradas Rival (vivo o congelado)
- **Winner → ExpeditionTeam** — Player, Rival, o None (empate)

## Métodos Públicos (S102 refactor)

**Orquestación de ronda:**

- `Launch() → void` — **S102 NUEVO:** comienza la ronda
  1. sandbox.SpawnCast() — spawnea elenco planeado
  2. Begin() — Elapsed=0, IsRunning=true

- `Reset(bool newSeed) → void` — **S102 NUEVO:** reinicia sala (opcionalmente nueva semilla)
  1. sandbox.ResetRoom(newSeed) — limpia y reconstruye
  2. Elapsed = 0 (no comienza a correr)
  3. IsRunning = false, IsOver = false (reposo)

**Lógica de tiempo/puntos:**

- `Begin() → void` — inicia contador
  - Elapsed = 0, IsRunning = true, IsOver = false

- `End() → void` — finaliza ronda
  1. congela: frozenPlayerSecured = SumSecured(Player), frozenRivalSecured = SumSecured(Rival)
  2. calcula Winner
  3. IsRunning = false, IsOver = true

**Debug:**

- `Restart() → void` — Reset(false) + Launch() (botón Odin)

## Flujo Típico S102

```
1. Panel visible (ArenaPlanPanel)
2. Jugador ajusta plan y presiona ¡A LA SALA!
   → ArenaPlanPanel.Play() → round.Launch()
   → SpawnCast() → Begin() → IsRunning=true
3. Update() incrementa Elapsed += Time.deltaTime
4. Si Elapsed >= roundSeconds → End()
5. End() congela puntos, IsRunning=false, IsOver=true
6. Panel detecta IsOver y espera resultHoldSeconds
7. Jugador ve resultado → round.Reset(false)
   → ResetRoom sin nueva semilla → vuelve al plan visible
8. O: round.Reset(true) para nueva sala + nuevo plan
```

## Campos Privados

- `frozenPlayerSecured`, `frozenRivalSecured` (int) — puntuaciones congeladas en End()
- `winner` (ExpeditionTeam) — ganador calculado en End()

## Métodos Privados

- `SumSecured(ExpeditionTeam team) → int` — suma .Secured de todos los ExitZone con .Team == team (lectura viva)
- `Begin() → void` — reinicia estado para comenzar a contar
- `End() → void` — congela y calcula ganador

## Invariantes S102

- **Determinístico:** una sola ronda por sesión (antes del próximo Reset)
- **Launch único:** SpawnCast solo se llama desde Launch, no desde Reset
- **Reset sin comienza:** Reset(newSeed) prepara la sala pero no inicia el contador (Launch lo hace)
- **IsRunning ↔ IsOver:** mutuamente excluyentes
- **Puntuaciones congeladas:** frozenXXX calculadas solo en End(), no se modifican post-End()
- **autoStart = false:** la escena no comienza automáticamente; se controla desde ArenaPlanPanel
- **Remaining nunca negativo:** clamp(0, ∞)

## Conexiones

**Entrada:**
- Time.deltaTime
- sandbox.Exits (ExitZone list)
- ArenaPlanPanel.Play() → Launch()
- ArenaPlanPanel resultado → Reset()

**Salida:**
- Propiedades IsRunning, IsOver, Elapsed, Remaining, PlayerSecured, RivalSecured, Winner leídas por:
  - [[ArenaRoundHud]] (mostrar timer y puntos)
  - [[ArenaPlanPanel]] (detectar IsOver para mostrar resultado)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
