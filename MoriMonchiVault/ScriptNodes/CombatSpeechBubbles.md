---
tags: [script, combat, ui]
---

# CombatSpeechBubbles.cs

**Ruta:** `Systems/CombatVisualizer/CombatSpeechBubbles.cs`

**Responsabilidad:** Renderiza globos de habla cómic en la UI (UIDocument) con borde coloreado, texto, y flecha ▼ que apunta al aliado objetivo. Suscribe a `CombatVisualEvents.OnSpeech/OnVisualCombatStart/OnVisualCombatEnd` y reposita dinámicamente el globo y la flecha por frame vía `RuntimePanelUtils.CameraTransformWorldToPanel` con `Camera.main`.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatVisualEvents]]
