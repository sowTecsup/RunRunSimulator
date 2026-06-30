---
tags: [script, core]
---

# GameManager.cs

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Singleton que centraliza acceso a assets (database, registries, configs). Único orquestador de persistencia: escucha `GameEvents.OnRegistryChanged`, `OnFurnitureChanged`, `OnInventoryChanged` y ejecuta persistencia local/cloud. En AppQuit/AppPause, flush a cloud. `MintRandomCreature()` genera partes/color/FurType, asigna personalidad, **llama `CreatureGenerator.RandomBaseStats()` para generar Constitution/Attack/Speed**, y registra. Dev tooling para inventario/genética vive en `GeneticsLabPreview`, `DevToolsConsole`. Getters actuales: `Registry`, `FurnitureRegistry`, `Inventory`, `Database`, `RarityOddsTable`, `PersonalityProfiles`, `PartVisualBank`, `FurTypeDatabase`, `EquipmentDatabase`.

**Vinculado a:** [[Index/07 - Persistence & Identity]]

**Conexiones:** [[GameEvents]], [[SaveSystem]], [[CloudSyncService]], [[CreatureRegistrySO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[CreatureGenerator]], [[EquipmentDatabaseSO]], [[GeneticsLabPreview]], [[DevToolsConsole]]
