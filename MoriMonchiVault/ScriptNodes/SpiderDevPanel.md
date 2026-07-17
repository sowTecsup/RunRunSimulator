---
tags: [script, prototype, tooling]
---

# SpiderDevPanel.cs

**Ruta:** `Prototype/Spider/SpiderDevPanel.cs`

**Responsabilidad:** Panel IMGUI de desarrollo (izquierda). Expone sliders para editar todos los knobs de `SpiderTuningSO` en runtime, organizados por secciones: Cuerpo, Patas, Cuerpo vivo (con Elasticidad y Salto en S50), Ragdoll. Botones: toggle `AutoWalk`, "Saltar!" (llama `jump.Jump()`), switch ragdoll mode, "Lanzar!" (aplica impulso + ragdoll), "Reset" (vuelve a spawn). Guarda spawn point y pose al start. Lee/escribe directamente los fields del SO. Escala automáticamente con el tamaño de pantalla vía `GUI.matrix = Scale(max(1, Screen.height/1080))` para mantener legibilidad. NO toca GameEvents ni sistemas del juego real.

**Notas de prototipo:** Tooling puro de dev. Usa `EditorUtility.SetDirty()` para marcar cambios en el SO. Labels en español neutral. No es UI final. Regla nueva de Juan (S50): GUI.matrix scaling dinámico por altura de pantalla.

**Cambios S50:** Se agregó scaling automático vía GUI.matrix (escala por `Screen.height/1080`); se agregó ref serializada `SpiderJump jump`; se agregaron sliders "Elasticidad" y "Salto" en sección Cuerpo vivo; se agregó botón "Saltar!" que dispara `jump.Jump()`.

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderTuningSO]], [[SpiderRagdollMode]], [[SpiderBodyController]], [[SpiderJump]]
