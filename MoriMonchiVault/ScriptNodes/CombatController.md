---
tags: [script, combat]
---

# CombatController.cs

**Ruta:** `Systems/Combat/CombatController.cs`

**Responsabilidad:** Apex del sistema de combate (domain owner). Singleton: `static Instance`. Posee `config` (CombatManagerSO serializado). Public API: getter `Config` (lectura del SO), `SimulateLocal()` (combate local + evento), `EnqueueForAsyncCombat()` + wrappers async (DequeueAsync, PollResultsAsync, FetchQueuedIdsAsync, FetchPendingResultIdsAsync). Dev tooling vive en `CombatDevConsole`. Resuelve registry y database de GameManager.Instance en Awake.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatService]], [[AsyncCombatService]], [[CombatManagerSO]], [[GameManager]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[GameEvents]], [[CombatDevConsole]]
