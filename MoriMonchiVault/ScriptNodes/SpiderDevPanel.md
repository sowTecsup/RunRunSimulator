---
tags: [script, prototype, tooling]
---

# SpiderDevPanel.cs

**Ruta:** `Prototype/Spider/SpiderDevPanel.cs`

**Responsabilidad:** Panel IMGUI de desarrollo (izquierda). Expone sliders para editar todos los knobs de `SpiderTuningSO` en runtime. Botones: toggle `AutoWalk`, switch ragdoll mode, "Lanzar!" (aplica impulso + ragdoll), "Reset" (vuelve a spawn). Guarda spawn point y pose al start. Lee/escribe directamente los fields del SO. NO toca GameEvents ni sistemas del juego real.

**Notas de prototipo:** Tooling puro de dev. Usa `EditorUtility.SetDirty()` para marcar cambios en el SO. Labels en español neutral. No es UI final.

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderTuningSO]], [[SpiderRagdollMode]], [[SpiderBodyController]]
