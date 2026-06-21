---
tags: [script, world]
---

# MoriMonchiController.cs

**Ruta:** `World/Creatures/MoriMonchiController.cs`

Responsabilidad:** Facade que cablea `MoriMochiAgent` (brain) + `MoriMonchiVisualizer` (3D assembly) sin que ambos se conozcan. `Initialize(dna, profileTable, player, bank, furDb)` inicializa el agente con perfil, pasa furDb al visualizer vía `SetFurDatabase(furDb)`, ensambla visual via `Assemble(dna, bank)`. **Nuevo passthrough `Rebind(dna, profileTable, furDb)`**: delega `agent.Rebind()` + aplica `visualizer.RefreshFur()` (refresco liviano sin re-ensamblar). `Launch()` y `PrepareForPool()` passthrough al agente. Propiedad pública `Agent` expone MoriMochiAgent.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiAgent]], [[MoriMonchiVisualizer]], [[MoriMochiSpawner]], [[PersonalityProfileSO]], [[PartVisualBankSO]], [[FurTypeDatabaseSO]], [[CreatureDNA]]
