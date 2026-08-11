---
tags: [dev-tools, combat, testing]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatDevConsole

**Ruta:** `Systems/Combat/CombatDevConsole.cs`

**Responsabilidad:** Componente dev (MonoBehaviour) para testing manual de combate local y async. **Local:** Fill Random Fighters, Simulate Combat, **Verify Determinism (seed)** (S32). **Async:** Pick Random for Queue, Enqueue (Instant/Scheduled), Dequeue, Check status/results. **S37:** Local combate es ahora 3v3 (pero console puede seguir usando 1 creature por lado = combate de 1v1 vía equipos). Refs serializadas [Required] a GameManager + CombatController. Solo desarrollo; **sin usar en production**.

## Botones / Métodos Dev

### Combat (Local)

| Botón | Método | Descripción |
|-------|--------|-------------|
| **Fill Random Fighters** | `FillRandomFighters()` | **S37** Elige 2 criaturas elegibles aleatoriamente (legacy 1v1, Teams de tamaño 1) |
| **Simulate Combat** | `SimulateCombatButton()` | **S37** Corre `CombatController.SimulateLocal(idsA=[A], idsB=[B], rowsA=[0], rowsB=[0])`, log result |
| **Verify Determinism (seed)** | `VerifyDeterminismButton()` | **S37** Clona A/B vía Serialize→DeserializeCreature, corre SimulateCore 2× mismo seed + rows, compara huella JSON |

### Async Combat

| Botón | Método | Descripción |
|-------|--------|-------------|
| **Pick Random for Queue** | `PickRandomForQueue()` | Elige criatura elegible random, asigna a `asyncCreatureID` |
| **Enqueue for Combat (Instant)** | `EnqueueInstantButton()` | `CombatController.EnqueueForAsyncCombat(..., scheduled: false)` |
| **Enqueue for Combat (Timer)** | `EnqueueScheduledButton()` | `CombatController.EnqueueForAsyncCombat(..., scheduled: true)` |
| *Más helpers en UI* | — | Dequeue, show queue, poll results (no métodos públicos, UI only) |

## Cambios S37

**FillRandomFighters() y SimulateCombatButton():**
- Cambio interno: FillRandomFighters busca 2 criaturas, crea `idsA=[creatureA]` y `idsB=[creatureB]` (lists de tamaño 1)
- SimulateCombatButton() llama `CombatController.SimulateLocal(idsA, idsB, rowsA=[0], rowsB=[0])` con rows por defecto
- Output sigue siendo idéntico (1v1 legacy via equipos de 1 criatura)

**VerifyDeterminismButton():**
- Ahora verifica `SimulateCore(List<dnaA>, List<dnaB>, List<int> rowsA, List<int> rowsB, ...)` 2× con mismo seed
- Las listas tienen tamaño 1 (legacy 1v1), pero el motor es 3v3

## Nuevo en S32: VerifyDeterminism

Testea que mismo seed + mismo DNA snapshot producen idéntico resultado:

```csharp
private void VerifyDeterminismButton()
{
    int seed = System.Guid.NewGuid().GetHashCode();
    
    string Fingerprint(CombatResult r) =>
        JsonConvert.SerializeObject(new {
            r.TeamAWon, r.IsDraw, r.EvolvedUnitId, r.DiedUnitId,
            r.EvolvedSlot, r.Turns
        });
    
    // Clona A/B vía JSON roundtrip
    var cloneA1 = SaveSystem.DeserializeCreature(SaveSystem.Serialize(dnaA));
    var cloneB1 = SaveSystem.DeserializeCreature(SaveSystem.Serialize(dnaB));
    var r1 = CombatService.SimulateCore(
        new List<CreatureDNA> { cloneA1 },
        new List<CreatureDNA> { cloneB1 },
        new List<int> { 0 },
        new List<int> { 0 },
        db, cfg, equipDb, new CombatRng(seed));
    
    var cloneA2 = SaveSystem.DeserializeCreature(SaveSystem.Serialize(dnaA));
    var cloneB2 = SaveSystem.DeserializeCreature(SaveSystem.Serialize(dnaB));
    var r2 = CombatService.SimulateCore(
        new List<CreatureDNA> { cloneA2 },
        new List<CreatureDNA> { cloneB2 },
        new List<int> { 0 },
        new List<int> { 0 },
        db, cfg, equipDb, new CombatRng(seed));
    
    string fp1 = Fingerprint(r1);
    string fp2 = Fingerprint(r2);
    
    if (fp1 == fp2)
        Debug.Log($"[CombatDevConsole] DETERMINISM OK");
    else
        Debug.LogError($"[CombatDevConsole] DETERMINISM BROKEN");
}
```

**Utilidad:** Verifica que `CombatRng` + `SimulateCore` son deterministas (no hay mutaciones no-reproducibles).

## Campos Serializados

### Combat

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `combatAID`, `combatBID` | `string` | Unique IDs de los 2 combatientes |
| `fighterAInfo`, `fighterBInfo` | `string` (ReadOnly) | Info display de combatientes |
| `lastCombatResult` | `string` (ReadOnly) | Outcome del último combate |

### Async Combat

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `asyncCreatureID` | `string` | ID de criatura a encolar |
| `asyncCreatureInfo` | `string` (ReadOnly) | Display info |
| `queuedCreaturesInfo` | `string` (ReadOnly) | Estado de cola |
| `dequeueIndex` | `int` | Índice para dequeue (UI) |

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/09 - Dev Tools]] (future link)
- [[CombatService]] — `SimulateCore()` (S37 3v3 overload)
- [[CombatController]] — wrapper a `SimulateLocal()`, `EnqueueForAsyncCombat()`
- [[SaveSystem]] — `Serialize()`, `DeserializeCreature()` (S32)
- [[GameManager]] — registry, database, equipment database

## Conexiones

**Entrada:**
- Refs [Required]: GameManager, CombatController
- Botones interactivos (inspector Odin)

**Salida:**
- `CombatController.SimulateLocal()` / `EnqueueForAsyncCombat()` — con teams (S37)
- Debug logs (console)

## Notas (S32 + S37)

- Dev-only: no debe estar habilitado en builds de production
- S37: Combate "1v1" es en realidad 3v3 con equipos de tamaño 1, pero la gameplay es idéntica (legacy compat)
- Determinism test es crítico: si falla, algo en SimulateCore está no-determinista (RNG global leak, floating point issue, etc.)
