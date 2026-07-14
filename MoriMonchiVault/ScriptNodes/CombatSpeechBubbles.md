---
tags: [script, combat, ui, uitk]
---

# CombatSpeechBubbles.cs

**Ruta:** `Systems/CombatVisualizer/CombatSpeechBubbles.cs`

**Responsabilidad:** Renderiza globos de habla cómic en la UI (UIDocument) con borde coloreado, texto, y flecha ▼ que apunta al aliado objetivo. Suscribe a `CombatVisualEvents.OnSpeech/OnVisualCombatStart/OnVisualCombatEnd` y reposita dinámicamente el globo y la flecha por frame vía `RuntimePanelUtils.CameraTransformWorldToPanel` con `Camera.main`. **S45:** Nuevo campo serializado `hideForDebug` (bool) — si true, desactiva por completo el renderizado de globos. Gate en `HandleSpeech()` y `Update()` para saltar todo si está activado.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatVisualEvents]]

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | Documento UITK que contiene bubble + tail + arrow |
| `hideForDebug` | `bool` | **S45 NEW** Si true, desactiva renderizado de globos (gate en HandleSpeech y Update) |
| `speakerOffset` | `Vector3` | Offset (mundo) desde transform del hablante hasta bubble (default 0, 1.6, 0) |
| `targetOffset` | `Vector3` | Offset (mundo) desde transform del objetivo hasta arrow (default 0, 1.9, 0) |

## Métodos Públicos/Privados

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe OnSpeech, OnVisualCombatStart, OnVisualCombatEnd |
| `OnDisable()` | Desuscribe eventos |
| `EnsureRefs()` | Localiza/construye bubble, bubbleTail, targetArrow dinámicamente si faltan |
| `BuildBubble()` | Construye VisualElement bubble (fondo blanco, borde redondeado, paddingx8, maxWidth 220) |
| `BuildTail()` | Construye Label tail ("▼", posición absoluta) |
| `BuildArrow()` | Construye Label arrow ("▼", mayor, color default oro) |
| `HandleStart(CombatVisualContext)` | EnsureRefs() + HideAll() al iniciar replay |
| `HandleEnd(CombatVisualSide, bool)` | HideAll() al terminar replay |
| `HandleSpeech(CombatSpeechData)` | **S45: gate hideForDebug** Si hideForDebug, retorna early. Sino, actualiza activeData/activeUntil/activeLabel/borderColor/arrowColor, muestra bubble/tail |
| `HideAll()` | Oculta bubble, tail, arrow; marca hasActive = false |
| `Update()` | **S45: gate hideForDebug** Si hideForDebug, oculta todo si hasActive. Sino, chequea activeUntil, reposita bubble/tail/arrow por frame via RuntimePanelUtils |
| `SetBorderColor(VisualElement, Color)` | Helper — setea todos los bordes (top/right/bottom/left) del mismo color |

## Flujo

**Evento OnVisualCombatStart:**
1. EnsureRefs() → construye elementos dinámicos si faltan
2. HideAll() → limpia estado previo

**Evento OnSpeech(CombatSpeechData d):**
1. **S45:** Si `hideForDebug` es true, retorna sin hacer nada
2. Sino:
   - Almacena d en activeData
   - Fija activeUntil = Time.time + max(0.5f, d.Duration)
   - Actualiza bubbleLabel.text = d.Text
   - Setea borde/arrow colors (d.HasColor ? d.Color : defaults)
   - Muestra bubble + tail + arrow (si d.HasTarget && d.TargetFollow != null)

**Evento OnVisualCombatEnd:**
- HideAll()

**Cada Update():**
1. **S45:** Si `hideForDebug` es true:
   - Si hasActive, llama HideAll() y retorna
   - Sino, retorna early
2. Sino (hideForDebug == false):
   - Si no hasActive, retorna
   - Si Time.time >= activeUntil, HideAll() y retorna
   - Si activeData.Follow es null, HideAll() y retorna
   - EnsureRefs()
   - Calcula speakerPanelPos = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, activeData.Follow.position + speakerOffset, Camera.main)
   - Posiciona bubble en speakerPanelPos (centrada horizontalmente, encima verticalmente)
   - Posiciona tail bajo bubble
   - Si activeData.HasTarget && activeData.TargetFollow != null: calcula targetPanelPos, posiciona arrow apuntando al objetivo
   - Sino: oculta arrow

## S45 Cambios

**Aditivos (append-only):**
- **Nuevo campo serializado:** `hideForDebug` (bool, default false)
- **Gate en HandleSpeech (S45):** if (hideForDebug) return; (línea 136)
- **Gate en Update (S45):** if (hideForDebug) { if (hasActive) HideAll(); return; } (línea 165)

**Invariante:** Lógica de posicionamiento, timing, colores, HideAll() sin cambios. Gate es puramente cosmético/debug.

## Uso (S45)

- Developers pueden marcar `hideForDebug = true` en inspector para desactivar globos de habla durante sesiones de debug sin alterar código
- Útil para concentrarse en orden de acción o mecánicas de marcas/estados sin que los globos tapeen la pantalla
- False por default (globos activos)

## Vinculado a

- [[CombatVisualEvents]] — suscriptor OnSpeech/OnVisualCombatStart/OnVisualCombatEnd
- [[CombatVisualizerService]] — publicador Speech eventos
- [[Index/03 - Combat System]]
