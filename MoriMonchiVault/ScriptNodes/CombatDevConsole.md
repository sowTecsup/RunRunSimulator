---
tags: [script, combat]
---

# CombatDevConsole.cs

**Ruta:** `Systems/Combat/CombatDevConsole.cs`

**Responsabilidad:** Componente dev (MonoBehaviour) para testing de combate local y async: Fill Random Fighters, Simulate Combat (local, captura resultado), Pick Random for Queue / Enqueue for Combat (Instant/Timer, async), Dequeue / Show Queued MoriMonchis / Check Pending Results (async polling). Info combatientes, cola async, estado (In Queue / Result Ready / GHOST). Refs serializadas [SerializeField] a GameManager + CombatController. Solo para desarrollo.

**Vinculado a:** [[Index/03 - Combat]], [[Index/09 - Dev Tools]]

**Conexiones:** [[GameManager]], [[CombatController]], [[CreatureRegistrySO]], [[CombatManagerSO]], [[CombatService]], [[AsyncCombatService]]

**Uso en escena:** Adjuntar a un GameObject con acceso a GameManager + CombatController. Inspect, configura refs y usa botones para test combate local y async.
