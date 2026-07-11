---
tags: [combat, async, cloud, online]
---

# AsyncCombatService

**Ruta:** `Systems/Combat/AsyncCombatService.cs`

**Responsabilidad:** Orquesta combate async server-side. No simula (S32 cambio): recibe match blob (seed + DNA snapshots de A/B), deserializa snapshots, corre `CombatService.SimulateCore` con seed compartida en ambos clientes, aplica consecuencias (tier-up, muerte, FightCount). **S37:** Transicional 1v1→3v3 (ApplyResult maneja equipos de tamaño 1 como 1v1 legacy, pero backend JS genera equipos 3v3 con rowsA/rowsB). Cloud Code scripts: "enqueue-combat" (cola), "run-combat" (instant match), "get-queue-status" (verifica pool). Reconcilia estado local post-resultado (ghost queue cleanup). Dispara `GameEvents.CombatLogged` tras `ApplyResult`.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `EnqueueInstantAsync(CreatureDNA dna)` | `Task` | Llama "run-combat" — si otro jugador en pool, matchea inmediato; sino espera |
| `EnqueueScheduledAsync(CreatureDNA dna)` | `Task` | Llama "enqueue-combat" — solo agrega a pool, matching cron-triggered server-side |
| `PollResultsAsync()` | `Task` | Lee `combat_results` de cloud, aplica results, reconcilia ghosts |
| `FetchQueuedIdsAsync()` | `Task<HashSet<string>>` | Retorna IDs de nuestras criaturas en el pool server (null si unreachable) |
| `FetchPendingResultIdsAsync()` | `Task<HashSet<string>>` | Retorna IDs con resultado pendiente |
| `DequeueAsync(CreatureDNA dna)` | `Task` | Llama "dequeue-combat" — remove de pool, limpia BusyState |

## Flujo Async (S32 + S37)

1. **Enqueue:** LocalDNA → "enqueue-combat" Cloud Code → append pool
2. **Matchmaking (server):** cron trigger "process-matchmaking" → match pares → crea blob + emite seed
3. **Apply (client):** Recibe `CloudMatchBlob { Seed, CreatureJsonsA[], CreatureJsonsB[], RowsA[], RowsB[], ... }` → `ApplyResult()`
   - Deserializa snapshots vía `SaveSystem.DeserializeCreature()` para cada unit
   - Corre `CombatService.SimulateCore(dnasA[], dnasB[], rowsA[], rowsB[], db, config, equipDb, new CombatRng(seed))`
   - **Idéntico resultado en ambos clientes** (mismo seed + mismos snapshots + mismas filas)
   - Aplica mutaciones a DNA local: FightCount++, WinCount++ (si gana), tier-up, muerte
   - Construye `CombatRecord` vía `CombatService.BuildRecord()` para cada unit ganador/perdedor
   - Dispara `GameEvents.CombatLogged()`

## Estructura: CloudMatchBlob (S32 + S37)

**S32 (1v1):**
```csharp
[Serializable]
private class CloudMatchBlob
{
    public string CreatureId;           // Nuestra criatura
    public int    Seed;                 // Seed para SimulateCore
    public bool   SelfWasA;             // Si true, somos A; false = B
    public string CreatureJsonA;        // JSON snapshot de A
    public string CreatureJsonB;        // JSON snapshot de B
    public string OpponentName;
    public string OpponentPlayerId;
    public string OpponentPlayerName;
    public string Date;                 // ISO-8601 UTC del match
}
```

**S37 (3v3, transicional):**
```csharp
[Serializable]
private class CloudMatchBlob
{
    // ... campos S32 ...
    public string[] CreatureIdsA;       // **S37** IDs del equipo A (1..3)
    public string[] CreatureIdsB;       // **S37** IDs del equipo B (1..3)
    public int[]    RowsA;              // **S37** Filas del equipo A (0..2)
    public int[]    RowsB;              // **S37** Filas del equipo B (0..2)
    public string[] CreatureJsonsA;     // **S37** JSON snapshots de equipo A
    public string[] CreatureJsonsB;     // **S37** JSON snapshots de equipo B
    // Legacy campos 1v1 deprecated pero aún presentes para backward compat
}
```

## Método ApplyResult (S32 + S37 transicional)

```csharp
private bool ApplyResult(CloudMatchBlob r, string myPlayerName)
{
    // 1. Valida nuestras criaturas existen en registry
    var myDnas = new List<CreatureDNA>();
    foreach (var id in r.CreatureIdsA.Concat(r.CreatureIdsB))
    {
        if (!registry.TryGet(id, out var dna)) return false;
        if (dna.IsBusy) myDnas.Add(dna);  // Filtra mías (asume solo 1 equipo nuestro)
    }
    
    // 2. Deserializa snapshots de ambos equipos
    var dnasA = r.CreatureJsonsA.Select(j => SaveSystem.DeserializeCreature(j)).ToList();
    var dnasB = r.CreatureJsonsB.Select(j => SaveSystem.DeserializeCreature(j)).ToList();
    if (dnasA.Any(x => x == null) || dnasB.Any(x => x == null)) return false;
    
    // 3. Simula core determinista con seed compartida
    var result = CombatService.SimulateCore(
        dnasA, dnasB, r.RowsA, r.RowsB, db, config, equipDb, new CombatRng(r.Seed));
    
    // 4. Determina outcome desde nuestro POV (nuestras criaturas)
    // Si somos el team A o B, uno de nuestros units está en TeamA o TeamB
    var isSelfTeamA = r.CreatureIdsA.Any(id => myDnas.Any(d => d.UniqueID == id));
    var myTeam = isSelfTeamA ? dnasA : dnasB;
    var oppTeam = isSelfTeamA ? dnasB : dnasA;
    bool teamWon = !result.IsDraw && result.TeamAWon == isSelfTeamA;
    
    // 5. Aplica mutaciones a cada DNA vivo nuestro
    foreach (var dna in myDnas)
    {
        dna.BusyState = BusyReason.None;
        dna.FightCount++;
        if (teamWon) {
            dna.WinCount++;
            // Si esta criatura evolucionó (verificar vs EvolvedUnitId)
            if (!string.IsNullOrEmpty(result.EvolvedUnitId) && dna.UniqueID == result.EvolvedUnitId)
                CombatEvolution.AdvanceTier(dna, result.EvolvedSlot);
        }
        // Si esta criatura murió
        if (!string.IsNullOrEmpty(result.DiedUnitId) && dna.UniqueID == result.DiedUnitId)
            dna.IsDead = true;
    }
    
    // 6. Construye records (uno per unit de ambos equipos)
    foreach (var dna in myDnas)
    {
        dna.CombatHistory ??= new List<CombatRecord>();
        // **S37:** BuildRecord ahora recibe teams + myDna (perspectiva)
        dna.CombatHistory.Add(
            CombatService.BuildRecord(result, myTeam, oppTeam, dna, isSelfTeamA,
                r.OpponentPlayerName ?? "", r.OpponentPlayerId ?? "",
                r.Seed, ParseUtcOrNow(r.Date)));
    }
    
    // 7. Dispara evento UI
    GameEvents.CombatLogged(new CombatLogEntry { ... });
    
    return true;
}
```

## Reconciliación de Ghosts

Post-poll, limpia creaturas que la registry cree encoladas pero el servidor no retiene:
- ¿Genuinamente en pool? → skip
- ¿Mid-enqueue (5s delay)? → skip (inFlightEnqueues)
- ¿Ghost? → limpia BusyState

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `inFlightEnqueues` | `HashSet<string>` | IDs encolados en 5s delay (no limpiar) |
| `enqueueGate` | `SemaphoreSlim(1)` | Serializa cloud ops (evita double-match) |
| `registry` | `CreatureRegistrySO` | Resuelto en Awake desde GameManager |

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/13 - Combat Design Direction]]
- [[CombatService]] — `SimulateCore()`, `BuildRecord()` (S37 overloads)
- [[CombatRng]] — inyectado con seed de blob
- [[SaveSystem]] — `DeserializeCreature()`
- [[CombatEvolution]] — `AdvanceTier()`
- [[CombatManagerSO]] — config
- [[GameManager]] — registry, database
- [[GameEvents]] — `CombatLogged()`

## Conexiones

**Entrada:**
- Cloud Code emite `CloudMatchBlob` → Cloud Save `combat_results` (S37 con teams)
- `PollResultsAsync()` lee y aplica

**Salida:**
- `GameEvents.CombatLogged()` — notifica UI
- `GameEvents.RegistryChanged()` (implícito al final) — persistencia

## Notas (S32 + S37)

- **Cambio cardinal (S32):** No simula server-side (JS). Cloud Code solo matchea + emite seed/snapshots.
- **Determinismo:** Seed compartida + snapshots idénticos = ambos clientes derivan idéntico record.
- **S37 Transicional:** ApplyResult maneja equipos (listas de DNAs + rowsA/rowsB), pero el cambio es aditivo — backends v1 (1v1) seguirán funcionando si Cloud Code emite arrays de tamaño 1. BuildRecord firma cambió a recibir teams.
- **Backward compatible:** `CloudMatchBlob` aditivo (nuevos campos CreatureIdsA/B, CreatureJsonsA/B, RowsA/B). Cliente legacy (1v1) esperaría arrays de tamaño 1.
- **DRAW soportado:** `IsDraw=true` → sin FightCount++, ambos ignoran evolución/muerte.
- **Anti-cheat diferido:** Hoy sin validación; roadmap S33+: Cloud Code verifica formación de DNA antes de emitir.
- **Cloud Code JS:** Debe ser actualizado en paralelo para emitir estructuras S37 con teams + rows (Fase 5, pendiente).
