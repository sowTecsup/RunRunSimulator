---
tags: [script, furniture]
---

# BuildModeController.cs

**Ruta:** `Systems/Furniture/BuildModeController.cs`

**Responsabilidad:** Máquina 4 estados: Browsing/Placing/Editing/Deleting. Gestiona ghost, validación, wiring con `BuildingInputs`.

**Invariantes S93 (rescatados de comentarios):**
- **Sub-máquina de estados:** Browsing (nada seleccionado; 1-4 empieza a colocar pieza de hotbar — path de test/legado, el browser real usa `SelectPieceFromBrowser`; E sobre pieza colocada → editar; clic derecho → target para borrar) · Placing (pieza NUEVA sigue el aim; R rota; clic izquierdo/F pinea en celda libre → Editing; verde/rojo por `grid.CanPlace`) · Editing (pieza fija en su celda; R rota; F guarda si verde o revierte el giro colisionante si rojo → Browsing) · Deleting (pieza levantada en rojo; F confirma, Esc restaura). Esc cancela la selección (restaurando la pieza levantada) → Browsing; en Browsing sale del build mode.
- `OnDisable`: si `active`, siempre `ExitBuildMode()` — nunca dejar una pieza levantada o un ghost huérfano.
- `Update`: solo Placing sigue el piso bajo la mira; Editing/Deleting quedan fijos. El ray de cámara elige la celda (XZ); la Y y la pendiente vienen de una sonda vertical en esa celda, para que el preview se asiente donde el spawner lo va a re-asentar.
- `TryPickFurnitureCell`: raycast a capas FURNITURE; devuelve la celda ancla leída de `PlacedFurnitureMarker` (que estampa el spawner).
- `OnConfirm` (Editing): si `PlacementValid()` falla, revierte a `lastValidRotation` en vez de bloquear el input.
- `PlacementValid`: única fuente de verdad de "puede sentarse en `currentCell`" (celda libre + piso plano + sin overlap físico) — la usan el tint, `OnPin` y `OnConfirm`.
- `OverlapsObstacle`: box orientado sobre el footprint (XZ de la grilla, altura del mesh del ghost) contra `obstacleMask`; los colliders del ghost están deshabilitados y una pieza levantada ya está despawneada, así que nada se auto-triggerea; un inset chico evita atrapar vecinos a ras. `BuildGhost` deshabilita los colliders del ghost (un preview no colisiona ni bloquea el aim ray) y toma la media altura del mesh para el box.

**Vinculado a:** [[Index/10 - Furniture & Building]]

**Conexiones:** [[BuildingInputs]], [[FurnitureService]], [[PlacementGrid]], [[BuildBrowserUITK]], [[PlacedFurnitureMarker]]
