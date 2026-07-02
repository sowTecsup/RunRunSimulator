---
tags: [combat, async, cloud, online]
---

# AsyncCombatService

**Ruta:** `Systems/Combat/AsyncCombatService.cs`

**Responsabilidad:** Orquesta combate async server-side. No simula (S32 cambio): recibe match blob (seed + DNA snapshots de A/B), deserializa snapshots, corre `CombatService.SimulateCore` con seed compartida en ambos clientes, aplica consecuencias (tier-up, muerte, FightCount). Cloud Code scripts: "enqueue-combat" (cola), "run-combat" (instant match), "get-queue-status" (verifica pool). Reconcilia estado local post-resultado (ghost queue cleanup). Dispara `GameEvents.CombatLogged` tras `ApplyResult`.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `EnqueueInstantAsync(CreatureDNA dna)` | `Task` | Llama "run-combat" — si otro jugador en pool, matchea inmediato; sino espera |
| `EnqueueScheduledAsync(CreatureDNA dna)` | `Task` | Llama "enqueue-combat" — solo agrega a pool, matching cron-triggered server-side |
| `PollResultsAsync()` | `Task` | Lee `combat_results` de cloud, aplica results, reconcilia ghosts |
| `FetchQueuedIdsAsync()` | `Task<HashSet<string>>` | Retorna IDs de nuestras criaturas en el pool server (null si unreachable) |
| `FetchPendingResultIdsAsync()` | `Task<HashSet<string>>` | Retorna IDs con resultado pendiente |
| `DequeueAsync(CreatureDNA dna)` | `Task` | Llama "dequeue-combat" — remove de pool, limpia BusyState |

## Flujo Async (S32)

1. **Enqueue:** LocalDNA → "enqueue-combat" Cloud Code → append pool
2. **Matchmaking (server):** cron trigger "process-matchmaking" → match pares → crea blob + emite seed
3. **Apply (client):** Recibe `CloudMatchBlob { Seed, CreatureJsonA, CreatureJsonB, ... }` → `ApplyResult()`
   - Deserializa snapshots vía `SaveSystem.DeserializeCreature()`
   - Corre `CombatService.SimulateCore(dnaA, dnaB, db, config, equipDb, new CombatRng(seed))`
   - **Idéntico resultado en ambos clientes** (mismo seed + mismo snapshot)
   - Aplica mutaciones: FightCount++, WinCount++ (si gana), tier-up, muerte
   - Construye `CombatRecord` vía `CombatService.BuildRecord()`
   - Dispara `GameEvents.CombatLogged()`

## Estructura: CloudMatchBlob (S32)

```csharp
[Serializable]
private class CloudMatchBlob
{
    public string CreatureId;           // Nuestra criatura
    public int    Seed;                 // **NUEVO S32** Seed para SimulateCore
    public bool   SelfWasA;             // **NUEVO S32** Si true, somos A; false = B
    public string CreatureJsonA;        // **NUEVO S32** JSON snapshot de A
    public string CreatureJsonB;        // **NUEVO S32** JSON snapshot de B
    public string OpponentName;
    public string OpponentPlayerId;     // **NUEVO S32**
    public string OpponentPlayerName;
    public string Date;                 // ISO-8601 UTC del match
}
```

## Método ApplyResult (S32)

```csharp
private bool ApplyResult(CloudMatchBlob r, string myPlayerName)
{
    // 1. Valida nuestra criatura existe en registry
    if (!registry.TryGet(r.CreatureId, out var dna)) return false;
    
    // 2. Deserializa snapshots
    var dnaA = SaveSystem.DeserializeCreature(r.CreatureJsonA);
    var dnaB = SaveSystem.DeserializeCreature(r.CreatureJsonB);
    if (dnaA == null || dnaB == null) return false;
    
    // 3. Simula core determinista con seed compartida
    var result = CombatService.SimulateCore(
        dnaA, dnaB, db, config, equipDb, new CombatRng(r.Seed));
    
    // 4. Determina outcome desde nuestro POV
    var self = r.SelfWasA ? dnaA : dnaB;
    var opp  = r.SelfWasA ? dnaB : dnaA;
    bool won = !result.IsDraw && result.WinnerID == self.UniqueID;
    bool died = !won && !result.IsDraw && result.LoserDied;
    
    // 5. Aplica mutaciones a DNA vivo
    dna.BusyState = BusyReason.None;
    dna.FightCount++;
    if (won) {
        dna.WinCount++;
        if (!string.IsNullOrEmpty(result.EvolvedSlot))
            CombatEvolution.AdvanceTier(dna, result.EvolvedSlot);
    }
    if (died) dna.IsDead = true;
    
    // 6. Construye record (replayable, simétrico)
    dna.CombatHistory ??= new List<CombatRecord>();
    dna.CombatHistory.Add(
        CombatService.BuildRecord(result, self, opp, r.SelfWasA,
            r.OpponentPlayerName ?? "", r.OpponentPlayerId ?? "",
            r.Seed, ParseUtcOrNow(r.Date)));
    
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
- [[CombatService]] — `SimulateCore()`, `BuildRecord()`
- [[CombatRng]] — inyectado con seed de blob
- [[SaveSystem]] — `DeserializeCreature()`
- [[CombatEvolution]] — `AdvanceTier()`
- [[CombatManagerSO]] — config
- [[GameManager]] — registry, database
- [[GameEvents]] — `CombatLogged()`

## Conexiones

**Entrada:**
- Cloud Code emite `CloudMatchBlob` → Cloud Save `combat_results`
- `PollResultsAsync()` lee y aplica

**Salida:**
- `GameEvents.CombatLogged()` — notifica UI
- `GameEvents.RegistryChanged()` (implícito al final) — persistencia

## Notas (S32)

- **Cambio cardinal:** No simula server-side (JS). Cloud Code solo paiea + emite seed/snapshots.
- **Determinismo:** Seed compartida + snapshots idénticos = ambos clientes derivan idéntico record.
- **Backward compatible:** `CloudMatchBlob` aditivo (nuevos campos).
- **DRAW soportado:** `IsDraw=true` → sin FightCount++, ambos ignoran evolución/muerte.
- **Anti-cheat diferido:** Hoy sin validación; roadmap S33+: Cloud Code verifica formación de DNA antes de emitir.
