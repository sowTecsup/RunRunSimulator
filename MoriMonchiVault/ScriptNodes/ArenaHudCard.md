---
tags: [script, ui, arena, expedition, presentation]
---

# ArenaHudCard.cs

**Ruta:** `World/Expedition/ArenaHudCard.cs` (presentador, no MonoBehaviour)

**Responsabilidad:** Constructor de tarjeta de HUD para un agente. Genera VisualElement con nombre, color, intención, carry, 3 slots radiales de habilidades. **S115:** Detecta Dazed/Tumbling → clase `hud-card--dazed`. **S118:** Super abilities Armed state. **S122:** Tarjeta simplificada: sin pilares visibles durante combate (opacidad 0.7, sin órdenes).

**Campos Públicos:**
- `Agent` (MoriMochiAgent) — referencia
- `Root` (VisualElement) — contenedor (PickingMode.Position)

**Construcción:**
1. Header: swatch + nombre
2. **S122:** Sin pilares mostrados (simplificación)
3. Action: label intención
4. Carry: barra de progreso
5. Powers: 3 RadialSlot

**Estados dinámicos (Refresh):**
- lastAction, lastCapacity, lastCarried, lastMining
- lastFired[3], lastDim[3] — abilities
- lastDazed — **S115:** Dazed/Tumbling
- **S122:** `hud-card--fighting` estado: opacidad 0.7, sin órdenes, radiales clickeables

**S122 Cambios:**
- Clase `.hud-card--fighting`: opacidad 0.7, sin pilares mostrados
- Radiales siguen clickeables (carga + poder)
- Simplificación visual durante combate

**Vinculado a:** [[Index/24 - Puente Tienda-Arena]]

**Conexiones:** [[MoriMochiAgent]], [[RadialSlot]], [[ArenaRoundHud]]
