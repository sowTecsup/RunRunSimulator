---
name: legibilidad-primero
description: En el MVP de combate (y en general) la prioridad de Juan es la LEGIBILIDAD para el usuario; nunca cumplir con lo mínimo en feedback visual/UI
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 438160be-40b8-4d80-bcca-6ea29b1a7639
  modified: 2026-08-26T16:06:31.242Z
---

Juan (S83, 2026-08-26): *"lo que más me importa en este MVP es que sea legible, así que asegúrate que sea legible, no cumplas con lo mínimo; siempre pregúntate: ¿esto que estoy planteando es legible para el usuario o debería añadir algo más para que lo sea?"*

**Why:** El pilar del prototipo de combate es planificación + lectura (proyección == ejecución). Si el jugador no LEE qué está seleccionado, qué va a pasar y qué pasó, el pilar muere aunque la lógica sea correcta. Ya en `Index/20` la prioridad declarada era gameplay > legibilidad > todo lo demás.

**How to apply:** Al implementar cualquier feature de presentación/UI del combate, no entregar el mínimo funcional: preguntarse activamente qué más necesita el usuario para leer el estado (indicadores en mundo + HUD redundantes, hints de controles visibles, estados bloqueados/gastados marcados, impactos con juice perceptible). Complementa [[diseno-legible-en-simientos]] (la legibilidad se diseña en el sistema, no se parcha con VFX) — esto añade: la capa de presentación también debe ser generosa, no austera.
