---
tags: [script, ui, visualization, rendering]
---

# MonchiTurntable.cs

**Ruta:** `UI/MonchiTurntable.cs`

**Responsabilidad:** Renderizador de rotación 3D spinning para MoriMonchis en UIElements. Crea N "cabinas" con cámaras independientes, RenderTextures y MonchiVisualizer. Cada cabina rota su modelo y renderiza a RenderTexture que se muestra en un UIElement (fondo de imagen).

**Campos Serializados:**

**Recursos:**
- `visualBank` (MonchiVisualBankSO) — banco visual para Assemble()
- `furDatabase` (FurTypeDatabaseSO) — database de pelajes

**Tuning:**
- `slotCount` (int, Min 1, default 3) — número de cabinas disponibles
- `textureSize` (int, Min 64, default 320) — resolución RenderTexture
- `spinDegreesPerSecond` (float, default 30) — velocidad de rotación
- `cameraPitch` (float, default 12) — ángulo de elevación cámara
- `cameraFov` (float, default 30) — field of view
- `framePadding` (float, default 1.1) — escala de bounds para framing
- `slotSpacing` (float, default 20) — separación horizontal entre modelos
- `stageHeight` (float, default -500) — posición Y de stage
- `focusLayerName` (string, default "MonchiFocus") — layer de renderizado aislado

**Clase Interna:**
- `Booth` — uno por slot, contiene Root GO, Model, Visualizer, Camera, RenderTexture, VisualElement, Yaw, Active flag

**Métodos Públicos:**

- `bool Show(int slot, CreatureDNA dna, VisualElement element) → bool` — renderiza MoriMochi en slot:
  1. Valida índices y referencias
  2. Assemble(dna) en visualizer, SetMood(Neutral), Play("Idle")
  3. Cambia layer si focusLayer válido
  4. Sets booth.Active = true, camera.enabled = true, element
  5. Asigna RenderTexture como backgroundImage del element, backgroundSize=Contain
  6. Llama Frame() para ajustar cámara
  7. Retorna true si éxito

- `void Hide(int slot)` — desactiva booth:
  - camera.enabled = false, Active = false, Element = null

- `void HideAll()` — esconde todos los slots

**Métodos Privados:**

- `void Awake()` — crea N booths (pooling permanente):
  - Cada booth: root GO con Model (MonchiVisualizer) + Camera (RenderTexture ARGB32 16-bit)
  - Culling mask a focusLayer si válido
  - Inicializa Yaw = 0

- `void LateUpdate()` — anima booths activos:
  - Recorre booths, si Active:
    - Chequea element.panel (si null, Hide)
    - Incrementa Yaw += spinDegreesPerSecond * dt
    - Rota Model.localRotation = Quaternion.Euler(0, Yaw, 0)
    - Llama Frame() para ajustar cámara

- `void Frame(Booth booth)` — framing automático:
  1. Calcula bounds de todos SkinnedMeshRenderers
  2. Fallback si no hay renderers: Bounds(up*0.5, 1x1x1)
  3. radius = bounds.extents.magnitude * framePadding
  4. dist = radius / sin(fov*0.5)
  5. Posiciona cámara: bounds.center - dir * dist (dir = Euler(pitch, 180, 0))
  6. LookRotation hacia bounds.center

- `void OnDestroy()` — cleanup:
  - Itera booths y libera RenderTextures con Release() + Destroy()

**Integración:**
- Wired en ArenaPlanPanel (turntable field)
- Show(slot, dna, element) llamado para mostrar preview en panel de plan
- Hide()/HideAll() para limpiar cuando se deselecciona

**S107 (NUEVO):**
- Renderizador standalone para spinning 3D en UI
- Pooling de booths permanentes (eficiente para múltiples previews)
- Aislamiento en layer para evitar interferencia con gameplay

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaPlanPanel]], [[MonchiVisualizer]], [[MonchiVisualBankSO]], [[FurTypeDatabaseSO]], [[CreatureDNA]]
