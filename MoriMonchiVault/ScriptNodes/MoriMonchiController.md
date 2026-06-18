---
tags: [memory-bank, script, player-world]
---

# MoriMonchiController.md

**Ruta:** `World/MoriMonchiController.cs`

**Responsabilidad:** Facade que cablea `MoriMochiAgent` + `MoriMonchiVisualizer`. `Initialize()`, `Launch()`, `PrepareForPool()`. Propiedad pública `Agent` expone el `MoriMochiAgent` para acceso directo desde `BreedingContainer` y `MoriMochiSpawner`.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiAgent]], [[MoriMonchiVisualizer]], [[MoriMochiSpawner]], [[PersonalityProfileSO]]
