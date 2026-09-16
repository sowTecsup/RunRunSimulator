---
tags: [script, world, expedition, orchestrator]
---

# ArenaRound.cs

**Ruta:** `World/Expedition/ArenaRound.cs`

**Responsabilidad:** Orquestador de tiempo y puntuación de ronda de arena. Máquina de estados: reposo → activa → finalizada. Corre contador, determina ganador por asegurados. Launch = SpawnCast + Begin. Reset(newSeed) = ResetRoom + reset. End() congela puntos y captura ArenaRoundSummary. **S124:** Detecta fin temprano en pisos Buff si todo el material fue recogido.

**S124 Cambio:** Verifica `sandbox.FloorKind == ArenaFloorKind.Buff && sandbox.AllMaterialTaken` en Update para terminación anticipada.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Launch()` | SpawnCast() + Begin() (inicia ronda) |
| `Reset(bool newSeed)` | ResetRoom(newSeed) + reset contadores |
| `Begin()` | Elapsed=0, IsRunning=true, IsOver=false (llamado por Launch) |
| `End()` | Congela puntos, captura Summary, calcula Winner, IsRunning=false, IsOver=true |
| `Restart()` [Button] | Reset(false) + Launch() (debug) |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `IsRunning` | `bool` | Ronda activa |
| `IsOver` | `bool` | Ronda terminada |
| `Elapsed` | `float` | Segundos desde Begin |
| `Remaining` | `float` | Max(0, RoundSeconds - Elapsed) |
| `PlayerSecured` | `int` | Material asegurado jugador (vivo si Running, congelado si Over) |
| `RivalSecured` | `int` | Material asegurado rival (vivo si Running, congelado si Over) |
| `Winner` | `ExpeditionTeam` | Player, Rival, o None (empate) |
| `Summary` | `IReadOnlyList<ArenaRoundStat>` | Estadísticas capturadas al End() |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `sandbox` | `[Required] ArenaSandbox` | Referencia al sandbox |
| `roundSeconds` | `float` | Duración máxima ronda (default 90s) |
| `autoStart` | `bool` | Si true, Begin() en Start() (default true) |

## Ciclo de Vida

### Launch (Inicio)

1. `sandbox.SpawnCast()` — genera elenco
2. `Begin()` → Elapsed=0, IsRunning=true

### Update (Conteo)

Mientras IsRunning:
```
Elapsed += deltaTime
Si (Buff && AllMaterialTaken): End()  [S124: terminación temprana]
Si (Elapsed >= RoundSeconds): End()   [timeout normal]
```

### End (Finalización)

1. Congela puntos: frozenPlayerSecured = SumSecured(Player), ídem Rival
2. Determina ganador por comparación
3. Captura Summary vía ArenaRoundSummary.Capture()
4. IsRunning=false, IsOver=true

### Reset

1. `sandbox.ResetRoom(newSeed)` — limpia escena
2. Limpia contadores y state
3. Permite volver a Begin/Launch

## S124 Cambio: Terminación Temprana en Buff

**Condición:** `sandbox.FloorKind == ArenaFloorKind.Buff && sandbox.AllMaterialTaken`

**Propósito:** En pisos de Buff (recuperación), no necesita timeout de 90s; cuando se recoge todo el material gratis, la ronda termina inmediatamente.

**Implementación en Update:**
```csharp
if (sandbox.FloorKind == ArenaFloorKind.Buff && sandbox.AllMaterialTaken) 
{ 
    End(); 
    return; 
}
```

## Determinación de Ganador

```csharp
Winner = frozenPlayerSecured == frozenRivalSecured
    ? ExpeditionTeam.None
    : (frozenPlayerSecured > frozenRivalSecured 
        ? ExpeditionTeam.Player 
        : ExpeditionTeam.Rival);
```

Empate si ambos aseguran lo mismo.

## Invariantes

- Launch es punto único de SpawnCast
- IsRunning y IsOver mutuamente excluyentes
- Summary inmutable tras End()
- Remaining nunca negativo
- PlayerSecured/RivalSecured vivos si Running, congelados si Over
- SumSecured sumará de ExitZone.Secured activos

## Vinculado a

[[Index/23 - Arena Sandbox & Expedicion]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ArenaSandbox]], [[ExitZone]], [[ArenaRoundSummary]], [[ArenaPlanPanel]], [[ArenaRunDirector]] (S124), [[WorldEnums]]
