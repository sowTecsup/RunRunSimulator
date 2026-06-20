---
tags: [memory-bank, script, player-world]
---

# MoriMonchiController.cs

**Ruta:** `World/MoriMonchiController.cs`

**Responsabilidad:** Facade que cablea `MoriMochiAgent` + `MoriMonchiVisualizer`. `Initialize(dna, profileTable, player, bank)` inicializa el agente y ensambla el visual (si hay bank). `Launch()` y `PrepareForPool()` son passthrough al agente. Propiedad pública `Agent` expone el `MoriMochiAgent` para acceso directo desde `BreedingContainer` y `MoriMochiSpawner`.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiAgent]], [[MoriMonchiVisualizer]], [[MoriMochiSpawner]], [[PersonalityProfileSO]], [[PartVisualBankSO]], [[CreatureDNA]]
