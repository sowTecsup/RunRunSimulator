---
tags: [script, visual, world]
---

# MineralVisual.cs

**Ruta:** `World/Expedition/MineralVisual.cs`

**Responsabilidad:** Animación visual del cristal de mineral: bobeo vertical, rotación, pulsación de emisión, y desgaste por recolección. Lee `MaterialPickup` para determinar tipo (veta/lodo/gota) y remanente; escala el cristal hacia `minScaleFraction` cuando se agota. Escribe colores e intensidad por `MaterialPropertyBlock` en todos los renderers del cristal, incluyendo luz emisiva si hay `glow`.

**Vinculado a:** [[Index/06 - World Architecture]], S114

**Conexiones:** [[MaterialPickup]]

**Campos Configurables (Odin)**

| Sección | Campos |
|---------|--------|
| Vida | `bobAmplitude`, `bobSpeed`, `spinDegreesPerSecond`, `pulseSpeed`, `pulseAmount` |
| Colores | `veinColor`, `lodeColor`, `dropColor`, `emissionStrength` |
| Desgaste | `minScaleFraction`, `shrinkSmoothing` |

**Campos Requeridos**

| Campo | Tipo | Descripción |
|--------|------|-------------|
| `pickup` | `MaterialPickup` | Para tipo y valor remanente |
| `crystal` | `Transform` | Modelo 3D del cristal |
| `crystalRenderer` | `Renderer` | Renderer del cristal |
| `glow` | `Light` | (Opcional) Luz que acompaña |

**Detalles S114**

Fase cíclica: `phase` aleatorio por bootstrap en `Awake`. Pulsación: función `sin` sobre tiempo real × frecuencia + fase. Escala final: si `Taken` o remanente bajo, desciende a `minScaleFraction * 0.5f` o interp lineal de `minScaleFraction` a 1 según fracción restante. MPB se aplica a todos los renderers del árbol del cristal cada `LateUpdate` sin costo de material nuevo.
