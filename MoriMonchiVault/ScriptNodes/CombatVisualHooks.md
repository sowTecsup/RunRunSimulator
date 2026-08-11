---
tags: [script, combat]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatVisualHooks.cs

**Ruta:** `Systems/CombatVisualizer/CombatVisualHooks.cs`

**Responsabilidad:** Bridge MonoBehaviour entre `CombatVisualEvents` y `UnityEvent`. Modo HookKind: Global (eventos de combate, turno, log) o por Side (ataque, golpe recibido/infligido, crítico, muerte, cambio de HP). Permite conectar animaciones/VFX/SFX sin código.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatVisualEvents]]
