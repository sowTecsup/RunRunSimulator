---
tags: [script, world, npc]
---

# NpcController.cs

**Ruta:** `World/Npc/NpcController.cs`

**Responsabilidad:** Singleton spawn/despawn de NPCs (clientes). Posee refs: `spawnPoint`, `exitPoint` (transforms), lista de `StoreContainer` displays, `CashRegister`. Parámetros cadence: `minSpawnInterval`, `maxSpawnInterval`, `maxSimultaneous`. Fallback `defaultAgentPrefab` si el arquetipo no tiene prefab. Lifecycle: Awake inicia timer, Update tira spawn timer y llama `TrySpawnOne()`. TrySpawnOne: obtiene arquetipo random de CustomerService, instancia prefab, busca NpcAgent, inicializa con displays/register/self, agrega a lista activa, dispara `GameEvents.CustomerSpawned()`. API: `Despawn(NpcAgent)` (remueve de lista y destruye GO). Getters: `Active` (IReadOnlyList), `ExitPoint`.

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[NpcAgent]], [[StoreContainer]], [[CashRegister]], [[CustomerService]], [[GameEvents]]
