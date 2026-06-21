---
tags: [archive]
---

# 00 - Fur Shaders Index

> 🚫 **PROOF OF CONCEPT DESCARTADO (2026-06-18).**
> Decisión de Juan: en vez de construir los 7 furs a mano en Shader Graph, se va a
> **comprar un asset** que ya traiga los shaders de pelaje definidos y se jugará con eso
> más adelante. Estas guías quedan archivadas solo como referencia del workflow probado
> (el formato "receta de nodos" funcionó para Smooth y Speckled). **No continuar los 5 restantes.**

Guías paso a paso (human-only) para construir los 7 estilos de pelaje en Shader Graph (URP 17.3).
Cada MD es una receta de nodos: "crea A, conecta a B". Sin código.

## Regla compartida
- El color genético entra siempre por la propiedad **`_BaseColor`** (lo inyecta `MoriMonchiVisualizer.ApplyColor`). Ningún fur puede renombrarla.
- Todos parten del **URP Lit Shader Graph**.
- `_Smoothness` default ~0.3 (aspecto de juguete, no plástico brillante).

## Estados

| # | Estilo       | Base técnica           | MD | Probado en Unity |
|---|--------------|------------------------|----|------------------|
| 1 | Smooth       | uniforme + fresnel     | [[FUR_01_Smooth]] | ✅ (probado, ahora archivado) |
| 2 | Speckled     | Simple Noise + Lerp    | [[FUR_02_Speckled]] | ⬜ (escrito, no probado) |
| 3 | Patchwork    | Voronoi + Posterize    | 🚫 no escrito | descartado |
| 4 | Static       | Noise animado + UV warp| 🚫 no escrito | descartado |
| 5 | Iridescent   | Fresnel + View Dir     | 🚫 no escrito | descartado |
| 6 | Veins        | Voronoi Edges + Mask   | 🚫 no escrito | descartado |
| 7 | Swirl        | Polar Coords + Noise   | 🚫 no escrito | descartado |

> Workflow original (descartado): escribir uno, probarlo en Unity, marcar ✅, seguir.
> Funcionó como prueba — se reemplaza por un asset comprado con shaders de pelaje.
