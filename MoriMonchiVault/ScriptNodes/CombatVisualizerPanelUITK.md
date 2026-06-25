---
tags: [script, ui]
---

# CombatVisualizerPanelUITK.cs

**Ruta:** `UI/CombatVisualizerPanelUITK.cs`

**Responsabilidad:** Panel UITK (screen-space) del visualizer: header de turno (actual/total), log de combate en cartas y controles de reproducción. Se reconstruye entero desde `CombatVisualEvents.OnPanelState` (single source of truth → soporta rewind). `OnVisualCombatStart` solo lo hace visible.

**Log en cartas con scroll:** `RebuildLog` vacía `log-container` (dentro de un `ScrollView` `log-scroll`) y crea una **carta por entrada** (`CombatVisualLogLine`), con clase USS por `Kind` (`log-versus`/`log-hit`/`log-crit`/`log-death`/`log-result`) → color de fondo + borde izquierdo. Los nombres/daño vienen ya con rich-text de color (azul local / rojo oponente / rojo daño). La caja de log tiene **tamaño fijo** (`height` en USS) y el ScrollView navega adentro, auto-scrolleando al último turno.

**Controles (en el UXML, llaman al servicio singleton):** `btn-back` → `Back()`, `btn-play` → `TogglePlay()` (texto ▶/❚❚ según `IsAuto`), `btn-next` → `Next()`, `speed-slider` (0.25–4) → `SetSpeed()`. `btn-back`/`btn-next` se habilitan según `CanBack`/`CanForward`. Llama a `CombatVisualizerService.Instance` (servicio explícito — permitido, no es Find).

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatVisualEvents]], [[CombatVisualizerService]]
