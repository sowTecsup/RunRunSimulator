---
tags: [index, ui]
---

# 05 - UI System

**Responsabilidad:** Interfaz UITK, enrutamiento input a paneles, aislamiento Gameplay/Menu. Stack LIFO de paneles.

**Core:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[UIManager]] | `UI/UIManager.cs` | Hub paneles + stack LIFO + router input + bus eventos UI static |
| [[UIInputs]] | `UI/UIInputs.cs` | Dueno action map UI (Navigate/Submit/Cancel stepped) |
| [[PanelTrigger]] | `Interactables/PanelTrigger.cs` | Bridge mundo UI (IInteractable abre panel) |

**Panel Controllers:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[BreedingPanelUITK]] | `UI/BreedingPanelUITK.cs` | Panel breeding |
| [[BuildBrowserUITK]] | `UI/BuildBrowserUITK.cs` | Build browser muebles |
| [[CombatPanelUITK]] | `UI/CombatPanelUITK.cs` | Panel combate |
| [[CreatureGridUI]] | `UI/CreatureGridUI.cs` | Grid criaturas uGUI |
| [[CreatureGridUITK]] | `UI/CreatureGridUITK.cs` | Grid criaturas UITK |
| [[CreatureGridView]] | `UI/CreatureGridView.cs` | Vista grid base |
| [[CreatureVisualUI]] | `UI/CreatureVisualUI.cs` | Render 3D en UI |
| [[HotbarHUDUITK]] | `UI/HotbarHUDUITK.cs` | HUD hotbar (overlay) |
| [[InfoOverlayUITK]] | `UI/InfoOverlayUITK.cs` | Overlay contextual |
| [[MorimonchiDetailInfoUITK]] | `UI/MorimonchiDetailInfoUITK.cs` | Detalle criatura |
| [[CombatLineupUITK]] | `UI/CombatLineupUITK.cs` | Tab Equipo 3v3: pool + grillas 2-3-2 + drag&drop + combate local (S38) |
| [[CombatLineupBoard]] | `UI/CombatLineupBoard.cs` | Widget grilla 2-3-2 (clase plana, estado+render, instanciada x2) |
| [[StoragePanelUITK]] | `UI/StoragePanelUITK.cs` | Panel almacenamiento |
| [[StorePanelUITK]] | `UI/StorePanelUITK.cs` | Panel tienda |

**Reglas de Oro:**
- Jamas usar SetActive en UITK (destruye rootVisualElement). Usar style.display = None.
- Action Maps Player y UI son mutuamente excluyentes. Solo uno activo a la vez.
- Paneles siempre en GameObjects activos (pueden escuchar eventos aunque ocultos).

---

## Recetario UITK (S38 — lecciones del panel de lineup 3v3, aprobado por Juan)

> Leer ANTES de construir cualquier UI nueva. Todo esto salio de iterar la tab "Equipo 3v3" contra mockups de Juan; el resultado final quedo aprobado como referencia de calidad.

### Layout que llena la ventana (el bug clasico)
1. Dentro de un `TabView`, el contenido NO llena la altura salvo que `flex-grow: 1` este en **TRES** niveles: `.unity-tab-view__content-container`, **`.unity-tab`** (el que todos olvidan) y `.unity-tab__content-container`. Sin el del medio, cada tab mide su contenido y deja banda muerta abajo.
2. El root del tab ademas necesita `flex-grow: 1` **y** `height: 100%`.
3. Anclar una barra al fondo: `margin-top: auto` (soportado en Unity 6 UITK).

### El feedback loop de auto-medicion
Un contenedor centrado (`align-items: center` en el padre) **sin width definido** mide lo que mide su contenido. Si un calculo dinamico lee ese ancho para dimensionar el contenido → loop: contenido chico → contenedor chico → "no hay espacio" → contenido chico. Fix: `width: 100%` en el contenedor medido.

### Sizing dinamico (grillas/boards que se adaptan a la resolucion)
- NUNCA fijar px pensando en una resolucion (el Game view del editor suele ser 720p; el monitor de Juan no).
- Patron: `GeometryChangedEvent` en el contenedor → calcular tamano desde `resolvedStyle.width/height` → aplicar por **inline style** (pisa al USS, que queda como valor inicial) → guard anti-loop (`if (Mathf.Abs(nuevo - actual) < 1f) return;`).

### Drag & drop runtime (no existe API en UITK runtime)
Patron completo en [[CombatLineupUITK]]: `PointerDownEvent` (guardar candidato + `CapturePointer`) → `PointerMoveEvent` (umbral ~8px antes de arrancar; ghost con `PickingMode.Ignore` agregado al root, posicionado con `root.WorldToLocal(evt.position)`) → `PointerUpEvent` (hit-test por `worldBound.Contains`). **Quirk critico**: en el Up, `UnregisterCallback` ANTES de `ReleasePointer` — si no, el `PointerCaptureOutEvent` dispara el camino de cancelacion sobre un drop ya resuelto.
- Click derecho para inspeccion sin chocar con el drag: `if (evt.button == 1)` en el mismo `PointerDownEvent`.
- Click izquierdo "limpio" (sin superar umbral) se detecta en el Up con `!dragActive`.

### Scrollbars y rueda
- Scrollbar custom (fino, oscuro, sin flechas): estilizar `.unity-scroller--horizontal` scoped al contenedor + `display: none` a `__low-button`/`__high-button` + colores en `.unity-base-slider__tracker`/`__dragger`.
- Carrusel horizontal con rueda del mouse: `WheelEvent` → `scrollOffset.x += evt.delta.y * 20f` + `StopPropagation()`.

### Quirks de editor/workflow
- **Hot-reload de USS/UXML con Play corriendo huerfana el arbol**: UIDocument recrea `rootVisualElement` y TODAS las refs cacheadas (Q<>) mueren → panel vacio. No es bug del juego: reiniciar Play. Afecta a todos los paneles (guard `wired`).
- Tab nueva agregada AL FINAL del TabView queda fuera de la navegacion por teclado (el clamp de `CombatPanelUITK.Navigation` es 0..3) — patron util para tabs WIP mouse-only sin tocar la navegacion.
- Iterar diseno con Unity MCP: abrir el panel por codigo (`UIManager.RequestPanelSet` via reflection en `execute_code`) + `manage_camera screenshot` del Game view = ver el layout real antes de entregar. Los numeros "se ve bien" del USS mienten; el screenshot no.
- Componente de UI nuevo = MonoBehaviour hermano con su ref `UIDocument` serializada (patron F3/DevConsoles) — no engordar el partial del panel.
