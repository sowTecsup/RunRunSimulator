---
tags: [script, world, expedition, debug, visualization]
---

# ArenaShapeGizmos.cs

**Ruta:** `World/Expedition/ArenaShapeGizmos.cs`

**Responsabilidad:** Utilidad estática para dibujar debug visualization (gizmos) de la topografía de ArenaShape. Dibuja contornos de regiones en colores distintivos y puntos de entrada. Llamada por ArenaShape.OnDrawGizmos().

**Métodos Públicos:**
- `void Draw(ArenaShape shape)` — dibuja todos los polígonos y entries

**Visualización:**
- Contorno: amarillo (outline)
- Rocas: gris (rocks)
- Lagos: cian (lakes)
- Pozos: negro (pits)
- Bosques: verde (groves), incluye espejo si simétrico
- Entradas player: esfera azul (0.6 radio)
- Entradas rival: esfera roja (0.6 radio)

**Invariantes:**
- y = shape.Center.y + 0.05 (ligero offset sobre el piso)
- Dibuja líneas cerradas (loop → loop[(i+1)%count])
- Si no hay contorno, no dibuja nada

**S111 Nuevo:**
- Visualización de debug para ArenaShape (solo en editor/play con gizmos activos)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]]

**Conexiones:** [[ArenaShape]]
