---
tags: [memory-bank, script, combat]
---

# AsyncCombatService.md

**Ruta:** `Systems/Combat/AsyncCombatService.cs`

**Responsabilidad:** Combate async server-side. Encola criaturas, espera resolución UGS, aplica resultado. Dispara `GameEvents.OnCombatLogged`.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CloudSyncService]], [[GameEvents]], [[CreatureRegistrySO]], [[CombatRecord]]
