---
tags: [script, ui]
---

# UIManager.cs

**Ruta:** `UI/UIManager.cs`

**Responsabilidad:** Hub de paneles + stack LIFO + router de input UI. Static event bus para UI-domain.

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[UIInputs]], [[PanelTrigger]], [[CreatureGridUITK]], [[MorimonchiDetailInfoUITK]], [[StorePanelUITK]], [[Interfaces]]

**Invariantes (S93):**
- Eventos UI-domain viven aquí (`OnPanelToggleRequested`, `OnPanelSetRequested`, `OnCreatureSelected`, `OnUIFocusChanged`, etc.), separados de `GameEvents` (gameplay-only). Cada dominio tiene su bus.
- **Nunca** `SetActive(false)` a un GameObject con `UIDocument`: un documento inactivo pierde `rootVisualElement` y deja de actualizarse. Se togglea `display` del root en su lugar.
- `UIManager` es el **único** suscriptor de `UIInputs`: Navigate/Submit se despachan al panel top del stack; Cancel (ESC) popea el top. Los paneles cierran en orden inverso.
- `RouteCancel`: el panel top maneja el cancel internamente (cerrar sub-vista); solo si no lo consume se popea.
- `UpdateFocus`: dispara `OnUIFocusChanged` solo en el flanco 0↔1.
- `OnEnable` corre antes que `Start`, por eso `rootVisualElement` ya existen para ocultar paneles en `Start`.
