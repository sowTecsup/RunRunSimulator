---
tags: [script, combat, cinemachine, visualization]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatCameraDirector.cs

**Ruta:** `Systems/CombatVisualizer/CombatCameraDirector.cs`

**Responsabilidad:** **S61b SIMPLIFICADO:** Conmuta prioridades de 3 cámaras Cinemachine estáticas (sceneCamera, allyCamera, enemyCamera) según etapa del turno emitida por `OnPhase(phase, actorSide)`. Sin seguimiento por unidad activa — las cámaras son fijas por tablero. Suscriptor de `OnPhase`, `OnVisualCombatStart`, `OnVisualCombatEnd`.

[Ver nodo completo para flujos S61b, métodos, campos, e invariantes Cinemachine]
