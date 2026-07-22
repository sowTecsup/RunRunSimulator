---
tags: [script, combat, ui, uitk]
---

# CombatSpeechBubbles.cs

**Ruta:** `Systems/CombatVisualizer/CombatSpeechBubbles.cs`

**Responsabilidad:** Renderiza globos de habla cómic en la UI (UIDocument) con borde coloreado, texto, y flecha ▼ que apunta al aliado objetivo. Suscribe a `CombatVisualEvents.OnSpeech/OnVisualCombatStart/OnVisualCombatEnd` y reposita dinámicamente el globo y la flecha por frame vía `RuntimePanelUtils.CameraTransformWorldToPanel` con `Camera.main`. **S61:** BuildBubble/BuildTail/BuildArrow usan `root.Insert(0, …)` en lugar de `root.Add(…)` — las burbujas pintan DETRÁS de los demás elementos del UIDocument (compartido con la barra de orden). **S45:** Nuevo campo serializado `hideForDebug` (bool) — si true, desactiva por completo el renderizado de globos.

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
| `BuildBubble()` | **S61** Construye VisualElement bubble con `root.Insert(0, bubble)` — pintar detrás |
| `BuildTail()` | **S61** Construye Label tail ("▼") con `root.Insert(0, bubbleTail)` — detrás |
| `BuildArrow()` | **S61** Construye Label arrow ("▼") con `root.Insert(0, targetArrow)` — detrás |
| `HandleStart(CombatVisualContext)` | EnsureRefs() + HideAll() al iniciar replay |
| `HandleEnd(CombatVisualSide, bool)` | HideAll() al terminar replay |
| `HandleSpeech(CombatSpeechData)` | **S45:** gate hideForDebug. Actualiza activeData, colores, muestra bubble/tail |
| `HideAll()` | Oculta bubble, tail, arrow; marca hasActive = false |
| `Update()` | **S45:** gate hideForDebug. Reposita bubble/tail/arrow por frame vía RuntimePanelUtils |
| `SetBorderColor(VisualElement, Color)` | Helper — setea todos los bordes |

## Cambios S61 (Insert(0) para z-order)

**BuildBubble() línea 91:**
```csharp
private void BuildBubble()
{
    bubble = new VisualElement { pickingMode = PickingMode.Ignore };
    // ... styling ...
    root.Insert(0, bubble);  // CAMBIO: Insert(0) en lugar de Add()
}
```

**BuildTail() línea 101:**
```csharp
private void BuildTail()
{
    bubbleTail = new Label("▼") { pickingMode = PickingMode.Ignore };
    // ... styling ...
    root.Insert(0, bubbleTail);  // CAMBIO: Insert(0) en lugar de Add()
}
```

**BuildArrow() línea 112:**
```csharp
private void BuildArrow()
{
    targetArrow = new Label("▼") { pickingMode = PickingMode.Ignore };
    // ... styling ...
    root.Insert(0, targetArrow);  // CAMBIO: Insert(0) en lugar de Add()
}
```

**Contexto:**
- UIDocument.rootVisualElement es un árbol jerárquico de elementos
- `root.Add(elemento)` agrega al final (top layer, pinta encima)
- `root.Insert(0, elemento)` agrega al inicio (bottom layer, pinta detrás)
- **Problema:** La barra de orden (CombatOrderBarUITK) también usa root, agregada después de BuildBubble/BuildTail/BuildArrow
- **Solución S61:** Insert(0) hace que burbujas pinten DETRÁS de la barra de orden, evitando oclusión

**Impacto visual S61:**
- Antes S61: Burbuja ocluye barra de orden (orden pinta detrás)
- S61: Burbuja pinta detrás, barra de orden visible encima (orden pinta detrás solo si habla ocurre ANTES de que se agregue)
- Resultado: Más limpio — información de orden no es tapada por diálogos

**Nota de arquitectura:**
- Ambos componentes (CombatSpeechBubbles + CombatOrderBarUITK) comparten el mismo rootVisualElement
- Insert(0) es solución pragmática: permite coexistencia limpia sin refactorizar jerarquía UITK
- Alternativa (futura): panel UITK dedicado por subsistema (speech vs order bar)

## Flujo

**Evento OnVisualCombatStart:**
1. EnsureRefs() → construye elementos dinámicos con Insert(0) (S61)
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
1. **S45:** Si `hideForDebug` es true: limpia y retorna
2. Sino: Reposita bubble/tail/arrow por frame vía RuntimePanelUtils

## Cambios S45

**Aditivos (append-only):**
- **Nuevo campo serializado:** `hideForDebug` (bool, default false)
- **Gate en HandleSpeech (S45):** if (hideForDebug) return; (línea 136)
- **Gate en Update (S45):** if (hideForDebug) { if (hasActive) HideAll(); return; } (línea 165)

**Invariante:** Lógica de posicionamiento, timing, colores, HideAll() sin cambios. Gate es puramente cosmético/debug.

## Uso (S45)

- Developers pueden marcar `hideForDebug = true` en inspector para desactivar globos sin alterar código
- Útil para concentrarse en orden de acción o mecánicas sin que globos tapeen la pantalla
- False por default (globos activos)

## Vinculado a

- [[CombatVisualEvents]] — suscriptor OnSpeech/OnVisualCombatStart/OnVisualCombatEnd
- [[CombatVisualizerService]] — publicador Speech eventos
- [[CombatOrderBarUITK]] — **S61** comparte rootVisualElement (z-order: burbuja detrás)
- [[Index/03 - Combat System]]

## Notas S61

- Insert(0) vs Add() es decisión de z-order (visual layering)
- Ambos componentes (speech + order bar) suscriptores del mismo evento bus/root
- Pragmático: evita refactorización de UITK doc sin cambiar comportamiento
- Resultado visual: Información crítica (orden) no ocluida por diálogos
