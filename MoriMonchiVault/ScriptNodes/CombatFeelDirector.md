---
tags: [script, combat, visual]
---

# CombatFeelDirector.cs

**Ruta:** `Systems/CombatVisualizer/CombatFeelDirector.cs`

**Responsabilidad (S46):** Único propietario de los MMFeedbacks (Feel) del replay de combate. `SerializedMonoBehaviour` de Odin que vive en la escena CombatVisualizerMM (GO CombatVisualizer). En lugar de duplicar 12 sistemas de partículas por prefab del MM, este director reproduce todos los feedbacks EN la posición del MM afectado usando `MMFeedbacks.PlayFeedbacks(Vector3)`. Obtiene posiciones via `CombatVisualizerService.PosOf(side, index)` — mismo patrón que `CombatCameraDirector` usa para `VCamOf`.

**Campos públicos:** 
- `shieldFeedback`: sobre el aliado que recibe escudo
- `healFeedback`: sobre el aliado que recibe curación
- `markFeedbacks` (Dict<Element, MMFeedbacks>): 4 elementos, sobre quien recibe marca
- `stateFeedbacks` (Dict<ElementalState, MMFeedbacks>): 12 estados, sobre quien detona reacción
- `offset`: altura sumada a la posición antes de reproducir

**Eventos suscritos:** `CombatVisualEvents.OnPopup` (Shield/Heal), `CombatVisualEvents.OnUnitElement` (MarkApplied, Reaction)

**Nota:** Los 12 estados se identifican parseando `ReactionName` contra el enum `ElementalState`. Si se renombra una reacción en ElementTable, esa partícula deja de salir en silencio. Botón editor "Crear objetos de feedback y wirear" es idempotente; crea 18 GameObjects hijos con `MMF_Player` auto-wireados.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatVisualEvents]], [[CombatVisualizerService]]
