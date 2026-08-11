---
tags: [script, combat, visualizer, replay, utility]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatReplayRequest

**Ruta:** `Systems/CombatVisualizer/CombatReplayRequest.cs`

**Responsabilidad:** Servicio estático cross-escena para solicitar el replay de un combate 3v3. Almacena transitoriamente el ID del luchador y el índice del combate, coordinando con `CombatSceneManager` para cargar la escena de visualización. **S41:** Firma de `CanReplay()` INVERTIDA — ahora **EXIGE record 3v3** (SelfTeam != null) + valida los 6 IDs resolubles en registry. **ResolveOpponent() BORRADO** (deprecated, equipos resueltos por CombatVisualizerService).

[Ver nodo completo para detalles de métodos públicos, lógica CanReplay() S41, y cambios]
