---
tags: [script, RETIRADO-S58, ui, combat, bar]
---

# MoriMonchiCombatVisualizerUITK.cs — RETIRADO S58

**Estado:** RETIRADO — Migración replay 3v3 al modelo Suriyun (S58)

**Descripción anterior:**
- Barra HP world-space UI legacy
- UIDocument (UXML) con elementos: name, hp-value, atk, spd, fill
- Filas dinámicas de marcas/estados (S42-S47)
- Marcos dorado/rojo (turno activo/objetivo)
- Escudo como segmento azul (S43+)
- Billboard hacia cámara (LateUpdate)

**Reemplazo:** [[CombatRadialHealthBar]]
- Anillo radial world-space generado por código
- Sprites dinámicos (thin/thick rings, ticks)
- Fill Radial360 verde→rojo HP
- Escudo en capas de 10 (azul→púrpura→magenta)
- SetFacingTarget orienta cierre al yaw del MM
- Hover expande + muestra label vida/máx
- MoriMonchi/UIRingOverlay shader (ZTest Always)

**Cuando se eliminó:** S58

**Cambios principales:**
- UIElements world-space → Radial UI generado por código
- UXML → Programmatic mesh/sprites
- Marcos dorado/rojo → intactos en CombatPedestalHighlighter shine
- Escudo azul → capas de 10 con degradado azul/púrpura/magenta

**Parámetros CombatRadialHealthBar (vs antiguo UIElement):**
- ringScale (1.6) — tamaño anillo
- facingAngleOffset (0) — ángulo cierre
- hoverRadius (0.9) — zona interactiva
- shieldLayerColors[] — colores escudo por capa
- popupScale, popupLifetime — knobs ajuste

**Conexiones antiguas:**
- MoriMonchiVisualizer (RETIRADO — ya no existe)
- UIDocument children (UXML legacy — descartado)

**Ver también:** [[CombatRadialHealthBar]], [[CombatVisualUnits]], [[CombatVisualizerService]]
