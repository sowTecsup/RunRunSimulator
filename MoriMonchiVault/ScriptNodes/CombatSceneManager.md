---
tags: [script, ui, combat, scene, replay, 3v3]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatSceneManager.cs

**Ruta:** `Systems/CombatVisualizer/CombatSceneManager.cs`

**Responsabilidad:** Gestor de navegación de la escena de combate (CombatVisualizerMM). Cables el botón "Volver" para regresar al juego. Consume replay requests cross-escena (`CombatReplayRequest`), resolviendo datos con timeout defensivo. **S41:** Firma cambió — `ConsumeReplayRequest()` ya NO resuelve rival. Solo resuelve `self` + obtiene `record`, luego llama `CombatVisualizerService.Play(self, record)` directamente (el service resuelve equipos vía registry).

[Ver nodo completo para detalles de corrutinas, métodos, y cambios S41]
