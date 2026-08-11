---
tags: [script, combat, visual]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatFeelDirector.cs

**Ruta:** `Systems/CombatVisualizer/CombatFeelDirector.cs`

**Responsabilidad (S46):** Único propietario de los MMFeedbacks (Feel) del replay de combate. `SerializedMonoBehaviour` de Odin que vive en la escena CombatVisualizerMM (GO CombatVisualizer). En lugar de duplicar 12 sistemas de partículas por prefab del MM, este director reproduce todos los feedbacks EN la posición del MM afectado usando `MMFeedbacks.PlayFeedbacks(Vector3)`. Obtiene posiciones via `CombatVisualizerService.PosOf(side, index)` — mismo patrón que `CombatCameraDirector` usa para `VCamOf`. **S47:** Tres nuevos toggles de mute (muteSoporte, muteMarcas, muteEstados) permiten silenciar secciones de feedbacks independientemente para testeo.

[Ver nodo completo para campos, métodos, flujo de reproducción, cambios S47, y button editor]
