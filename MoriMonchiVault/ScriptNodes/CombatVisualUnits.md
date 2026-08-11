---
tags: [script, combat, visualizer, composition]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatVisualUnits.cs

**Ruta:** `Systems/CombatVisualizer/CombatVisualUnits.cs`

**Responsabilidad:** Colaborador de `CombatVisualizerService` (composición, regla 11) — spawn/lookup/lifecycle de las unidades del replay 3v3. Dueño de mapeo de DNAs → anchors por fila (Front0/Front1, Mid0/Mid1/Mid2, Back0/Back1 convención hex 2-3-2), instantiación de modelos visuales, binding de barras UI con stats del snapshot, lifecycle de despawn. Plain data (DTO `CombatVisualUnit`) + stateless operations.

[Ver nodo completo para detalles de métodos, flujos S58-S59, y estructura CombatVisualUnit]
