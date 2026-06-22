---
tags: [script, cloud]
---

# GameEvents.cs

**Ruta:** `Core/GameEvents.cs`

**Responsabilidad:** Bus de eventos cross-system estático. Eventos registry/breeding/combat/furniture: `OnRegistryChanged`, `OnRegistryReloaded`, `OnCreatureMinted`, `OnCombatCompleted`, `OnCombatLogged`, `OnBreedingCompleted`, `OnFurnitureChanged`, `OnFurnitureReloaded`. Eventos cliente/NPC: `OnCustomerSpawned(NpcAgent)`, `OnCustomerDecided(NpcAgent, CreatureDNA)`, `OnCustomerArrivedAtRegister(NpcAgent)`, `OnCustomerSold(NpcAgent, CreatureDNA, int)`, `OnCustomerLeft(NpcAgent, bool)`.

**Vinculado a:** [[Index/07 - Persistence & Identity]]

**Conexiones:** [[GameManager]], [[CloudSyncService]], [[BreedingService]], [[CombatService]], [[FurnitureService]]
