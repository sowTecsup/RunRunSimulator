---
tags: [script, world, ui, presentation]
---

# MonchiEmoteBubble.cs

**Ruta:** `World/Creatures/MonchiEmoteBubble.cs`

**Responsabilidad:** Burbuja pictográfica flotante world-space sobre un MoriMochi, renderizada con UI Toolkit como Label en lugar de TextMeshPro. COMPARTE el UIDocument de NameTag (mismo GO hijo "WorldUITKInfo", uxml NameTagUITK.uxml) en lugar de ser dueña de un UIDocument separado — inserta su label como slot fijo-altura en el tope del "tag-root", arriba del nombre, así el tag nunca refluye cuando aparece/desaparece la emoción. NameTag.cs posee la transform (billboard + distancia gate); este componente nunca la toca. Pure event-driven — nunca pollea el agente, solo escucha MoriMochiAgent.OnEmote y popea un solo glyph (?, ☺, ♪, !, ♥, Zz…) sin texto, sin localización. Pop in, hold, fade out; una emoción nueva mientras ya hay una reinicia la animación limpiamente.

**Campos:**
- `agent` — MoriMochiAgent propietario (requerido)
- `visibleSeconds` — cuánto tiempo la emoción se ve antes de desvanecerse (default 2s)
- `popSeconds` — duración del pop-in (scale + opacity 0→1, default 0.18s)
- `fadeSeconds` — duración del fade-out final (opacity 1→0, default 0.35s)
- `fontSize` — tamaño de fuente del pictograma (default 25)
- `document` — ref al UIDocument (resuelta en Awake)
- `label` — ref a la Label insertada en tag-root (resuelta lazily en EnsureLabel)
- `colliderSilenced` — bandera: el auto-Collider del UIDocument fue deshabilitado

**Métodos:**
- `Show(EmoteKind) → void` — dispara por MoriMochiAgent.OnEmote: resuelve glyph + color, resetea timer, marca showing=true
- `EnsureLabel() → void` — resolve lazy: si no existe o el panel fue huérfano (pooling), crea Label e inserta en tag-root[0]
- `TryDisableAutoCollider() → void` — un UIDocument world-space auto-genera un picking BoxCollider en este GO, que se une al collider compound de la criatura e interfiere con raycast/throw. Deshabilitarlo una sola vez (re-Destroy lo regeneraría, pero enabled=false persiste). Se llama hasta que silence la bandera.
- `LateUpdate()` — anima: pop (scale 0.4→1 con ease-out), hold, fade. Actualiza scale + opacity según elapsed time

**Static helper:**
- `GlyphOf(EmoteKind) → (string, Color)` — mapeo pictograma+color por emoción
  - Curioso: "?" amarillo
  - Feliz: "☺" verde
  - Jugando: "♪" azul
  - Molesto: "!" rojo
  - Corazon: "♥" rosado
  - Zzz: "Zz" púrpura

**Notas:**
- Comparte UIDocument con NameTag — ambos usan el mismo GO hijo WorldUITKInfo
- El label insertado es PickingMode.Ignore (no interfiere con UI clicks)
- El silenciamiento del auto-Collider es robusto: busca cada frame hasta lograrlo (quirk de Unity: Destroy recrea el collider, pero enabled=false persiste)
- La animación de pop usa easing (1−(1−t)²) para suavidad
- Pool-safe: reactivar el GO reconstruye rootVisualElement del UIDocument, orphanando la Label vieja; EnsureLabel re-resuelve

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiAgent]], [[NameTag]], [[EmoteKind]]
