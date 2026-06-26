---
tags: [script, ui, combat]
---

# CombatSceneManager.cs

**Ruta:** `Systems/CombatVisualizer/CombatSceneManager.cs`

**Responsabilidad:** Gestor de navegación de escena en la escena de combate. Singleton implicito (aunque no se declara `Instance`; el GameObject persiste en la escena). Cablea el botón "Volver" (`btn-home` de CombatTopBar.uxml) y navega de vuelta a la escena de juego. Public API: `ReturnToGameScene()` — detiene el `CombatVisualizerService`, luego carga la escena `gameSceneName` (configurable, por defecto "GameScene").

**Setup:** Requiere un `UIDocument` con `Source Asset` = CombatTopBar.uxml. En Start resuelve la raíz visual del documento y busca el botón por nombre `btn-home`; al clickearlo llama `ReturnToGameScene()`. OnDisable desuscribe el botón para evitar memory leaks. Botón Odin Test con atributo `[DisableInEditorMode]` para play.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatVisualizerService]]
