---
tags: [dev-tools, combat, testing]
---

# CombatDevConsole

**Ruta:** `Systems/Combat/CombatDevConsole.cs`

**Responsabilidad:** Componente dev (MonoBehaviour) para testing manual de combate local y async. **Local:** Fill Random Fighters, Simulate Combat, **Verify Determinism (seed)** (S32). **Async:** Pick Random for Queue, Enqueue (Instant/Scheduled), Dequeue, Check status/results. Refs serializadas [Required] a GameManager + CombatController. Solo desarrollo; **sin usar en production**.

## Botones / Métodos Dev

### Combat (Local)

| Botón | Método | Descripción |
|-------|--------|-------------|
| **Fill Random Fighters** | `FillRandomFighters()` | Elige 2 criaturas elegibles aleatoriamente |
| **Simulate Combat** | `SimulateCombatButton()` | Corre `CombatController.SimulateLocal()`, log result |
| **Verify Determinism (seed)** | `VerifyDeterminismButton()` | **NUEVO S32** Clona A/B vía Serialize→DeserializeCreature, corre SimulateCore 2× mismo seed, compara huella JSON |

### Async Combat

| Botón | Método | Descripción |
|-------|--------|-------------|
| **Pick Random for Queue** | `PickRandomForQueue()` | Elige criatura elegible random, asigna a `asyncCreatureID` |
| **Enqueue for Combat (Instant)** | `EnqueueInstantButton()` | `CombatController.EnqueueForAsyncCombat(..., scheduled: false)` |
| **Enqueue for Combat (Timer)** | `EnqueueScheduledButton()` | `CombatController.EnqueueForAsyncCombat(..., scheduled: true)` |
| *Más helpers en UI* | — | Dequeue, show queue, poll results (no métodos públicos, UI only) |

## Nuevo en S32: VerifyDeterminism

Testea que mismo seed + mismo DNA snapshot producen idéntico resultado:

```csharp
private void VerifyDeterminismButton()
{
    int seed = System.Guid.NewGuid().GetHashCode();
    
    string Fingerprint(CombatResult r) =>
        JsonConvert.SerializeObject(new {
            r.WinnerID, r.LoserID, r.IsDraw, r.LoserDied,
            r.EvolvedSlot, r.Turns
        });
    
    // Clona A/B vía JSON roundtrip
    var cloneA1 = SaveSystem.DeserializeCreature(SaveSystem.Serialize(dnaA));
    var cloneB1 = SaveSystem.DeserializeCreature(SaveSystem.Serialize(dnaB));
    var r1 = CombatService.SimulateCore(cloneA1, cloneB1, db, cfg, equipDb, new CombatRng(seed));
    
    var cloneA2 = SaveSystem.DeserializeCreature(SaveSystem.Serialize(dnaA));
    var cloneB2 = SaveSystem.DeserializeCreature(SaveSystem.Serialize(dnaB));
    var r2 = CombatService.SimulateCore(cloneA2, cloneB2, db, cfg, equipDb, new CombatRng(seed));
    
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
- [[CombatService]] — `SimulateCore()`
- [[CombatController]] — wrapper a `SimulateLocal()`, `EnqueueForAsyncCombat()`
- [[SaveSystem]] — `Serialize()`, `DeserializeCreature()` (S32)
- [[GameManager]] — registry, database, equipment database

## Conexiones

**Entrada:**
- Refs [Required]: GameManager, CombatController
- Botones interactivos (inspector Odin)

**Salida:**
- `Debug.Log` (no persiste; dev only)
- Llamadas a `CombatController` que disparan `GameEvents`

## Notas

- **Exclusivamente dev:** Sin incluir en build release.
- **Refs serializadas:** [BoxGroup], [LabelText], [GUIColor] vía Odin.
- **S32 adición:** Botón Verify Determinism valida architecture post-refactor.
- **Test útil:** Ejecutar VerifyDeterminism si se sospecha regresión de determinismo (ej. accidental System.Random en combate).
