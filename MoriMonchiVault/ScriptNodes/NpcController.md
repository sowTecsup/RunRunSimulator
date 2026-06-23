---
tags: [script, world, npc]
---

# NpcController.cs

**Ruta:** `World/Npc/NpcController.cs`

**Responsabilidad:** Singleton spawn/despawn de NPCs (clientes). Parámetros: `spawnPoint`, `exitPoint` (transforms), `register` (CashRegister), cadence (`minSpawnInterval`, `maxSpawnInterval`, `maxSimultaneous`), fallback `defaultAgentPrefab`. Update() maneja spawn timer; `TrySpawnOne()` instancia, inicializa con `StoreDisplayRegistry.All` (no lista serializada).

**Métodos públicos:**
- `ForceSpawn()` → spawn inmediato + reinicia timer (para debug).
- `TrySpawnOne()` → retorna NpcAgent spawneado (o null si falla).
- `Despawn(NpcAgent)` → remueve de `active` y destruye GO.

**Propiedades:**
- `Active` (IReadOnlyList<NpcAgent>) — clientes vivos en escena.
- `ExitPoint` (Transform) — destino al salir.

**Cambios principales:**
- Ya NO tiene `displays` (List<StoreContainer>) serializada — pasa `StoreDisplayRegistry.All` a `Initialize()`.
- `ForceSpawn()` es público (era privado `TrySpawnOne()`).

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[NpcAgent]], [[StoreContainer]], [[StoreDisplayRegistry]], [[CashRegister]], [[CustomerService]], [[GameEvents]]
