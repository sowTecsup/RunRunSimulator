---
tags: [script, core]
---

# GameManager.cs

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Singleton que centraliza acceso a assets (database, registries, configs). Único orquestador de persistencia: escucha `GameEvents.OnRegistryChanged`, `OnFurnitureChanged`, `OnInventoryChanged` y ejecuta persistencia local/cloud. En AppQuit/AppPause, flush a cloud. Dev tooling para inventario/genética vive en `GeneticsLabPreview`, `DevToolsConsole`. Getters actuales: `Registry`, `FurnitureRegistry`, `Inventory`, `Database`, `RarityOddsTable`, `PersonalityProfiles`, `PartVisualBank`, `FurTypeDatabase` (ya NO posee combatConfig ni inheritanceOddsTable).

**Vinculado a:** [[Index/07 - Persistence & Identity]]

**Conexiones:** [[GameEvents]], [[SaveSystem]], [[CloudSyncService]], [[CreatureRegistrySO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[CreatureGenerator]], [[GeneticsLabPreview]], [[DevToolsConsole]]
