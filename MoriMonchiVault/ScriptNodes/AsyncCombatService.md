---
tags: [script, combat]
---

# AsyncCombatService.md

**Ruta:** `Systems/Combat/AsyncCombatService.cs`

**Responsabilidad:** Orquesta combate async server-side. Cloud Code scripts: "run-combat" (inmediato), "enqueue-combat" (siempre espera), "get-queue-status" (verifica pool). Resuelve registry de GameManager.Instance. Obtiene config vía CloudEndpoint. Reconcilia estado local post-resultado. Dispara `GameEvents.OnCombatLogged` tras ApplyResult.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CloudEndpoint]], [[GameEvents]], [[CreatureRegistrySO]], [[CombatRecord]], [[CombatController]]
