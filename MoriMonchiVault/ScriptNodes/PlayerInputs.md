---
tags: [script, world]
---

# PlayerInputs.cs

**Ruta:** `Player/PlayerInputs.cs`

**Responsabilidad:** Dueño del action map "Player" (único script que toca Input System). Traduce callbacks del asset a eventos estáticos, decoupled (GameEvents patrón). El gating por foco de UI es automático: el mapa Player se deshabilita en menús vía `UIManager.OnUIFocusChanged`. Look/aim NO está aquí (Cinemachine lo lee directamente).

**Eventos estáticos (S36):**
- `MoveChanged` (Action<Vector2>) — Move action (continuous, fired on change)
- `Jumped` (Action) — Jump pressed
- `InteractPressed` (Action) — Interact key DOWN
- `InteractReleased` (Action) — Interact key UP
- `ThrowPressed` (Action) — Attack button (throw held object)
- `BuildToggled` (Action) — Build mode toggle
- `HotbarScrolled` (Action<int>) — Mouse wheel scroll (+1/-1 por step, threshold configurable)
- `DropPressed` (Action) — Q key (drop active hotbar item)

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[PlayerController]], [[PlayerAnimator]], [[HotbarController]]

**Notas:**
- Action map "Player" se enablea en Awake + OnEnable, se disablea en OnDisable y en `OnUIFocusChanged(true)`.
- Campo `scrollThreshold` (default 0.1) en editor para tuning sensibilidad rueda.
- Desuscripción completa en OnDisable (patrón: OnEnable/OnDisable mirror).
