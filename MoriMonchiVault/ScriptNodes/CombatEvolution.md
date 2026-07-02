---
tags: [combat, evolution, tier-up]
---

# CombatEvolution

**Ruta:** `Systems/Combat/CombatEvolution.cs`

**Responsabilidad:** Autoridad única de evolución de tiers en combate. Elige aleatoriamente un slot elegible y lo sube de tier (Tier1→Tier2→Tier3). Usado por local `CombatService` y async `AsyncCombatService` para garantizar determinismo idéntico.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `TryEvolveRandomSlot(CreatureDNA dna, CombatRng rng)` | `string` | Elige slot aleatorio no-Tier3, lo evoluciona, retorna nombre ("Body"/"Arm"/"Eye"/"Mouth") o null si todos están max |
| `AdvanceTier(CreatureDNA dna, string slot)` | void | Incrementa tier del slot si < Tier3 |
| `GetSlotTier(CreatureDNA dna, string slot)` | int | Retorna tier actual como int (1/2/3) |

## Lógica

**TryEvolveRandomSlot:**
```csharp
  eligible = []
  if (BodyTier  < Tier3) eligible.Add("Body")
  if (ArmTier   < Tier3) eligible.Add("Arm")
  if (EyeTier   < Tier3) eligible.Add("Eye")
  if (MouthTier < Tier3) eligible.Add("Mouth")
  
  if (eligible.Count == 0) return null  // Todos max
  slot = eligible[rng.Range(0, eligible.Count)]
  AdvanceTier(dna, slot)
  return slot
```

**AdvanceTier:**
```csharp
  switch (slot):
    "Body":  if (dna.BodyTier < Tier3) dna.BodyTier = +1
    "Arm":   if (dna.ArmTier  < Tier3) dna.ArmTier  = +1
    ...
```

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatService]] — llama en ganador post-combate
- [[AsyncCombatService]] — llama al aplicar resultado async
- [[CreatureDNA]] — mutación de tiers
- [[CombatRng]] — inyección de RNG determinista

## Conexiones

**Entrada:**
- `CombatService.SimulateCore()` línea `CombatEvolution.TryEvolveRandomSlot(winner, rng)`
- `AsyncCombatService.ApplyResult()` línea `CombatEvolution.AdvanceTier(dna, result.EvolvedSlot)`

**Salida:**
- Mutación de `dna.{BodyTier,ArmTier,EyeTier,MouthTier}` (Tier enum)

## Notas

- Solo el **ganador** evolucion tras ganar combate.
- `TryEvolveRandomSlot()` es pure y stateless (solo lee dna, mutación ocurre vía `AdvanceTier`).
- La evolución **no** es probabilística en sesión actual; siempre ocurre si hay slot elegible.
- Futuro (Sesión 33): agregar `EvolutionChance` a `CombatManagerSO` para hacer la evolución probabilística (es deuda en roadmap).
