---
tags: [script, combat-visual, effects]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatPedestalHighlighter.cs

**Ruta:** `Systems/CombatVisualizer/CombatPedestalHighlighter.cs`

**Responsabilidad:** Destaca el pedestal del combatiente activo mediante outline y colores de brillo. Suscribe a `OnActiveUnit` (SetHighlight) y `OnVisualCombatEnd` (ClearHighlight) para aplicar/remover modificación instantanciada del material del renderer del MM. Utiliza campos del Unity Toon Shader: _Outline_Width, _Outline_Color, _BaseColor, _Color, _1st_ShadeColor, _2nd_ShadeColor. **S59d:** Defaults ajustados: activeOutlineWidth 4→10 (outline más grueso), activeOutlineColor dorado→negro (contraste superior). Pedestal mantiene dorado (activeBaseColor, activeShadeColor). Getter AnchorOf() desde CombatVisualizerService resuelve el renderer.

## API Pública

| Método | Parámetros | Descripción |
|--------|-----------|-------------|
| `HandleActiveUnit(side, index)` | `CombatVisualSide, int` | Evento handler: aplica shine al MM activo |
| `HandleVisualCombatEnd(winner, isDraw)` | `CombatVisualSide, bool` | Evento handler: limpia shine al finalizar combate |

## Campos Serializados (S59d)

| Campo | Tipo | S59a Default | S59d Default | Descripción |
|-------|------|------|------|-------------|
| `activeOutlineWidth` | `float` | 4 | **10** | Ancho outline ToonShader (_Outline_Width) — **S59d AUMENTADO** |
| `activeOutlineColor` | `Color` | dorado (golden) | **Color.black** | Color outline (_Outline_Color) — **S59d CAMBIADO a negro** |
| `activeBaseColor` | `Color` | dorado claro | dorado claro | Color base (_BaseColor, _Color) — pedestal dorado |
| `activeShadeColor` | `Color` | dorado oscuro | dorado oscuro | Color sombra (_1st/_2nd_ShadeColor) — pedestal sombra dorada |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe eventos OnActiveUnit, OnVisualCombatEnd |
| `OnDisable()` | Desuscribe eventos, limpia highlight |
| `HandleActiveUnit(side, index)` | Resuelve anchor via CombatVisualizerService.AnchorOf(), aplica shine |
| `HandleVisualCombatEnd(winner, isDraw)` | Llama ClearHighlight() para restaurar material |
| `ClearHighlight()` | Restaura sharedMaterial original, destruye instancedMaterial |

## Flujo de Highlight

```
1. OnActiveUnit(side, index) dispara
2. ClearHighlight() — restaura material anterior (si existe)
3. CombatVisualizerService.AnchorOf(side, index) → Transform anchor
4. anchor.GetComponentInChildren<Renderer>() → renderer del MM
5. Instancia material: renderer.material (copia)
6. SetFloat("_Outline_Width", activeOutlineWidth=10)
7. SetColor("_Outline_Color", activeOutlineColor=black)
8. SetColor("_BaseColor", activeBaseColor=dorado)
9. SetColor("_Color", activeBaseColor=dorado)
10. SetColor("_1st_ShadeColor", activeShadeColor=dorado oscuro)
11. SetColor("_2nd_ShadeColor", activeShadeColor=dorado oscuro)
12. Guarda referencia highlightedRenderer, originalSharedMaterial, instancedMaterial
```

## Estados Internos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `highlightedRenderer` | `Renderer` | Renderer del MM actualmente resaltado (null si ninguno) |
| `originalSharedMaterial` | `Material` | Material original (shared) para restaurar |
| `instancedMaterial` | `Material` | Material instanciado (copia mutable) en uso |

## Cambios S59d (Outline ajustado: negro grueso)

**Outline width increase:**
- Línea 8: `[SerializeField] private float activeOutlineWidth = 10f;` — de 4 a 10
- Propósito: outline más visible/impactante al seleccionar unit activo

**Outline color change:**
- Línea 9: `[SerializeField] private Color activeOutlineColor = Color.black;` — de dorado a negro
- Propósito: contraste superior (negro pop sobre pedestal dorado), efecto "borde afilado"

**Pedestal mantiene dorado:**
- Línea 10: `activeBaseColor = new Color(1f, 0.84f, 0.35f)` — dorado claro (sin cambios S59d)
- Línea 11: `activeShadeColor = new Color(0.85f, 0.66f, 0.2f)` — dorado oscuro (sin cambios S59d)
- Efecto visual: cuerpo dorado con outline negro grueso = máximo contraste + elegancia

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CombatVisualEvents]] — OnActiveUnit, OnVisualCombatEnd eventos
- [[CombatVisualizerService]] — AnchorOf() resolver anchor del MM
- [[CombatRadialHealthBar]] — barra radial del MM (paralelo, no dependencia)

## Conexiones

**Entrada:**
- `CombatVisualEvents.OnActiveUnit(CombatVisualSide, int)` — dispara al iniciar turno de unit
- `CombatVisualEvents.OnVisualCombatEnd(CombatVisualSide, bool)` — dispara al fin de combate

**Salida:**
- Modificación instantanciada del material del renderer (outline + colores)
- Visual feedback: "este MM es el actor del turno actual"

## Notas S59d

- Outline negro grueso (S59d: 4→10, dorado→negro) mejora legibilidad y drama visual sin comprometer estética dorada del pedestal
- AnchorOf() público en CombatVisualizerService permite este acceso (S58+)
- Material instantanciado no afecta prefab ni otros instances
- ClearHighlight() defensivo: null-checks en Destroy(instancedMaterial)
- Timing: shine ON durante turno, OFF al fin combate (feedback claro del turno actual)
