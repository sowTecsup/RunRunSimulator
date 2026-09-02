---
tags: [script, ui, combat, visual-element]
---

# RpsTriangleElement.cs

**Ruta:** `UI/RpsTriangleElement.cs`

**Responsabilidad:** VisualElement personalizado (Painter2D) que dibuja triángulo RPS interactivo. Nodos Cuernos (arriba) > Alas (derecha) > Espalda (izquierda) según `DragonRpsRules.Beats()`. Colores por propiedades CSS custom (--tri-horns, --tri-wings, --tri-back, --tri-ink, --tri-hi, --tri-fill). Propiedad `Highlight` activa pulsación en nodo seleccionado e ilumina relaciones ganadoras. Etiquetas de tipo posicionadas alrededor. Sin UxmlElement, instanciado por código.

**Vinculado a:** [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[CombatDuelPresenter]], [[DragonRpsRules]]
