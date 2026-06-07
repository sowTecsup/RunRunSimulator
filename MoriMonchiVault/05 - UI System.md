---
tags: [memory-bank, ui, uitk, panels, input]
---

# 05 — UI System

> Relacionados: [[06 - Player & World]] (input maps Player/UI mutuamente excluyentes), [[02 - Genetics & Breeding]] (BreedingPanel), [[03 - Combat]] (CombatPanel).

## Filosofía

**Comportamientos componibles "drop-a-script"** sobre objetos del mundo (requieren **Collider** para el raycast; las primitivas ya lo traen):

- **`IThrowable`** (en `Interfaces.cs`) — `IsHeld`, `OnGrab(anchor)`, `OnRelease()`, `OnThrow(force)`. Implementación: `ThrowableObject` (RequireComponent Rigidbody; mientras se sostiene sigue al anchor por **velocidad** → choca en vez de clippear). Agarrar = hold E.
- **`IInteractable`** (en `Interfaces.cs`) — `Interact()`. Implementación: `PanelTrigger`. Interactuar = tap E.
- Un objeto puede implementar **ambas** (tap interactúa, hold agarra).

## Flujo de paneles (desacoplado por static events)

```
PanelTrigger (mundo, IInteractable) --Interact()--> UIManager.RequestPanelToggle(UIPanelType)
                                                            │  (el evento lleva el enum)
                                                            ▼
UIManager (escena) --Dictionary<UIPanelType,GameObject>--> muestra/oculta el panel
```

## UIManager — hub de eventos UI + gestor de paneles

`SerializedMonoBehaviour` de Odin, en escena. Los eventos UI viven **acá como `static event Action`** (NO en `GameEvents`, que queda solo para gameplay; NO en un `UIEvents` aparte — eliminado).

### Eventos UI (statics en `UIManager`)

| Evento | Helper | Payload | Notas |
|--------|--------|---------|-------|
| `OnPanelToggleRequested` | `RequestPanelToggle` | `UIPanelType` | Toggle (lo usa la E) |
| `OnPanelSetRequested` | `RequestPanelSet` | `UIPanelType, bool` | Show/hide explícito (idempotente) |
| `OnCreatureSelected` | `SelectCreature` | `CreatureDNA, CreatureRegistrySO` | Card clicada; el registry para resolver padres |
| `OnUIFocusChanged` | (interno) | `bool` | true al abrirse el primer panel, false al cerrarse el último. Solo en el borde 0↔1 (cuenta el `stack`) |
| `OnNavigableRegistered` | `RegisterNavigable` | `UIPanelType, IUINavigable` | Cada panel focusable lo llama en `Start` |
| `OnNavigableUnregistered` | `UnregisterNavigable` | `UIPanelType` | En `OnDestroy` |

### Quién escucha `OnUIFocusChanged`

- **Player** (suspende control, ver [[06 - Player & World]])
- **`PlayerInputs`** (deshabilita el mapa `Player`)
- **`UIInputs`** (habilita el mapa `UI`)

## STACK + Router de input

Resuelve el viejo "sistema de prioridad de UI". Los paneles abiertos viven en una **lista ordenada** (`stack`, el último = tope con foco).

El `UIManager` es el **único suscriptor** de `UIInputs` y **despacha solo al tope**:
- `Navigate`/`Submit` → `IUINavigable` del tope.
- `Cancel` (ESC) → primero pregunta `OnUICancel()` al tope:
  - Si devuelve `true` (consumió el ESC para "atrás" interno — cerrar sub-lista, subir un nivel de foco) → no hace nada.
  - Si `false` → **pop del tope** (atrás universal, cierra en orden LIFO: detalle → grilla → gameplay).

Paneles simples (grilla, detalle) devuelven `false`; los multi-nivel (breeding, combate) consumen hasta llegar a la barra. `Push` mueve un panel al tope (re-abrir lo re-enfoca).

## UIInputs

Vive en el objeto del UIManager. Dueño **único** del action map `UI`, espejo de `PlayerInputs`.

- Static events `NavigatePressed` (stepped: 1 paso por pulsación, con debounce de tecla/stick sostenido), `SubmitPressed`, `CancelPressed`.
- Habilita el mapa `UI` solo con foco.
- **Gamepad gratis** (Navigate=stick/dpad, Cancel=B, Submit=A).
- El detalle clickeable sigue por los punteros del UITK (no por este mapa).

## Action maps mutuamente excluyentes

`InputSystem_Actions` tiene **dos maps**:

| Map | Acciones | Dueño |
|-----|----------|-------|
| `Player` | `Move`/`Jump`/`Interact`/`Attack` | `PlayerInputs` |
| `UI` | `Navigate`/`Submit`/`Cancel` | `UIInputs` |

Solo uno está habilitado a la vez, conmutado en el borde `OnUIFocusChanged` (gameplay ↔ menú).

⚠️ La acción `Interact` debe tener la interacción **Hold desactivada** (Press) — el hold-vs-tap lo decidimos nosotros con un timer (`grabHoldDuration`).

## Estrategias para ocultar paneles

| Tipo | Cómo se oculta | Por qué |
|------|----------------|---------|
| **UI Toolkit** (`UIDocument`) | Togglea `rootVisualElement.style.display`. El GameObject queda **ACTIVO**. | ⚠️ NUNCA `SetActive(false)` un `UIDocument`: sin objeto activo no hay `rootVisualElement` y deja de poblarse. |
| **uGUI** | `SetActive` normal. | — |

**Todos los paneles arrancan ocultos** en `UIManager.Start` (por `display` los UITK, por `SetActive` los uGUI).

## UIPanelType (enum en `Enums.cs`)

`None / CreatureGrid / MorimonchiDetail / Breeding / Combat`. Convención: sufijo `Type` + `None = 0` (los enums viejos NO se renombran).

## PanelTrigger (`Interactables/`)

`IInteractable`. Campo `UIPanelType panel`; al tap E dispara `UIManager.RequestPanelToggle(panel)` (static). No conoce instancia del `UIManager`.

## Capa UI Toolkit (UXML/USS)

Assets UXML/USS/PanelSettings viven en `Assets/RunRunSimulator/UI Toolkit/` (carpeta normal, **NO Resources** — se referencian por inspector, no por ruta).

Unity 6.3; `TabView`/`Tab` y el autosize de texto son nativos:
```css
-unity-text-generator: advanced;
-unity-text-auto-size: best-fit <min> <max>;
```
Requiere activar **Project Settings → UI Toolkit → Advanced Text Generator**.

### Patrón común de todos los paneles UITK

- Viven en el **objeto del UIManager** (siempre activo).
- Referencian su `UIDocument` (oculto por `display`, nunca SetActive).
- Se registran como `IUINavigable` (`RegisterNavigable` en `Start` / `Unregister` en `OnDestroy`).
- Arrancan en la tab por defecto al abrirse (escuchan `OnPanelToggle/Set` → `ResetFocus`).
- Los modales usan `sortingOrder` alto + backdrop full-screen.
- **Tabs**: pills centradas, sin flechas de scroll (`.unity-scroller--horizontal { display:none }`).
- **Navegación jerárquica**: `TabBar ⇄ contenido ⇄ lista` con un enum `Region`. A/D en la barra cambia tab, ↓/Submit entra, **Submit = barra espaciadora**, ESC sube un nivel (`OnUICancel` devuelve `true` para consumir, `false` en la barra para que el UIManager cierre).
- **Foco visible** con una clase `*-focus` (borde) sobre un borde transparente fijo de 3px (sin reflow).
- **Acciones async largas** → botón gris + texto "…ando" + inputs congelados (flag local).

### Paneles existentes

#### CreatureGridUITK (gemelo UITK de `CreatureGridUI`, implementa `IUINavigable`)

- Vive en el **objeto del UIManager** (siempre activo) y referencia un `UIDocument` separado (`CreatureGridUITK.uxml`) + un `VisualTreeAsset` (`CreatureCardUITK.uxml`).
- Por estar en un objeto activo, sigue suscrito a `OnRegistryChanged/Reloaded` y **repuebla la grilla aunque el panel esté oculto** (resuelve el gap de `OnDisable`).
- Clona una card por criatura (saca la card del `TemplateContainer` para no romper el wrap, guarda el `CreatureDNA` en `card.userData`), las hace clicables (`ClickEvent → Select(idx) + UIManager.SelectCreature`), y cablea el botón cerrar (`Start`) → `RequestPanelToggle`.
- Grilla = flexbox `flex-wrap` + `ScrollView`; margen 10% en los 4 bordes (`position:absolute` con `left/right/top/bottom:10%`); cards 20%/60%/20% (nombre/icono/estado) con `cardSize` configurable.
- **Navegación teclado/mando**: ←/→ mueven 1 card, ↑/↓ saltan una fila completa (el nº de columnas se **mide del layout real** — cuenta las cards que comparten la `y` de la primera fila).
- Highlight `.card--selected` (la `.card` lleva un borde transparente de 3px fijo → al seleccionar **no hay reflow**).
- **Auto-scroll** vía `ScrollView.ScrollTo` (la barra sigue a la selección).
- **Enter/A** (`OnUISubmit`) → abre el detalle de la card seleccionada. Mouse y teclado quedan sincronizados.

#### MorimonchiDetailInfoUITK (modal, implementa `IUINavigable`)

- Ventana de detalle **modal**, estilo resumen de Pokémon FireRed. Referencia su `UIDocument` + la `CreatureDatabaseSO`.
- Escucha `OnCreatureSelected` → `Populate(dna)` → `selectedTabIndex = 0` (**siempre abre en Info**) → `RequestPanelSet(true)`.
- **Modalidad**: `document.sortingOrder` alto (encima de la grilla, misma `PanelSettings`) + backdrop full-screen que captura los clicks → no se puede tocar la grilla detrás hasta cerrar con la **X** o **ESC**.
- **A/D** (`OnUINavigate`) cambia de tab (clamp).

**Tabs implementadas:**

- **Info**: retrato teñido `PrimaryColor` + stats coloreados `FINAL (base + bonus)` vía `CombatService.GetEffectiveStats` · identidad · **Personalidad** (nombre + descripción en español, switch sobre `Personality` enum) · partes con swatch · progresión.
- **Combate**: `ScrollView` (`combat-history`). Un `Foldout` por pelea más reciente primero. Color del toggle: verde=Won / rojo=Lose. Dentro: meta (`combat-meta`= fecha + oponente) + turno a turno (`combat-turn` labels). Vacío: label `combat-empty`.
- **Breed**: árbol **descendente** — yo (arriba) → parejas (fila) → crías por pareja (fila por debajo). Escanea el registry buscando criaturas cuyo `MotherID`/`FatherID` == selfId (robusto, no depende de `ChildrenIDs`). Grupos por el otro progenitor (partner). Chip con swatch + nombre + rol. Vacío: `breed-empty`.
- **Linaje**: árbol **ascendente** — abuelos → padres → yo. Recurse hasta depth 2 o sin padres. Criaturas muertas/ausentes resueltas desde `ParseGenetics(uniqueId)` (strip timestamp → `CreatureDNA.FromID()`). Chip con swatch + nombre + rol; `tree-dead` si ausente. Vacío: `lineage-empty`.
- **Equipo**: placeholder "Próximamente".

Chips de árbol: clase `tree-chip` (104px, fondo oscuro) + `tree-chip--self` (borde morado) / `tree-chip--unknown` (fondo más oscuro) / `tree-chip--partner` (borde rosa). Conectores: `tree-connector-v` (vertical 14px) / `tree-connector-h` (horizontal, flex-grow). Las **flechas de scroll del header de tabs se ocultan** vía `.unity-scroller--horizontal { display:none }` dentro de `.detail-tabs`.

#### BreedingPanelUITK (modal)

- Panel de breeding **modal**. Layout de 3 columnas: **Padres** (izq) y **Madres** (der) **siempre presentes** (listas de elegibles: vivos, no Busy, hembra/macho, `BreedCount<4`), con el **TabView al centro** (headers centrados).
- **Criar**: slots Padre/Madre con imagen tintada `PrimaryColor` + nombre en 1 línea con elipsis a tamaño fijo · preview de ambos con stats coloreados + partes con swatch · ❓ + tiempo estimado `≈ {BreedDurationMinutes} min` · botón **Breed** → `AsyncBreedingService.StartBreedingAsync`. Al pulsarlo: gris **"Breeding..."** + `breedBusy` congela toda la navegación hasta la respuesta async, luego limpia slots y salta a Incubando.
- **Incubando**: una carta por huevo `Madre 💗 Padre` con countdown server-side `BreedReadyAt` refrescado cada 1s en `Update`. Al llegar a 0 aparece **Hatch** → `HatchAsync`, que al pulsarse queda **gris "Hatching..."** y deshabilitado hasta el server — `btn.panel != null` distingue éxito (huevo eclosionado, fila reconstruida) de `not_ready`.
- Las listas siempre visibles → "abrir" un slot solo mueve el foco a la lista.
- Navegación jerárquica (TabBar⇄contenido⇄lista, ESC=atrás vía `OnUICancel`).

#### CombatPanelUITK (modal, 4 tabs)

- Panel de combate **modal**, **4 tabs**.
- **Batalla Online** (Tab 1): izq disponibles · centro seleccionado con retrato `PrimaryColor` + stats + partes + **2 botones Instant/Timer** → `EnqueueInstantAsync`/`EnqueueScheduledAsync` fire-and-forget, la criatura cae a Resultados · der **Equipo** placeholder.
- **Combate Local** (Tab 2): igual a Breeding: 2 listas + slots A/B + **Pelear** → `CombatService.Simulate` → **log inline** turno por turno + outcome.
- **Resultados** (Tab 3, solo async): cola de criaturas encoladas + countdown al próximo :00 UTC (reloj grande `cbt-clock`, se actualiza 1×/seg). Cada fila muestra nombre + "encolado HH:mm" (hora local de `QueuedAt`). Botón **Revisar resultados** → `PollResultsAsync`. Sin panel de log (movido a Historial). Las criaturas con modo Instant deberían mostrar "Instantánea" en lugar del countdown — **pendiente**.
- **Historial** (Tab 4): lista global de todos los combates pasados. `DropdownField` para filtrar por criatura (o "Todas"). Seleccionar una fila muestra log turno a turno + resultado en el panel derecho. Reconstruido vía `OnCombatLogged` y al abrir el panel.
- Referencia `CreatureDatabaseSO` + `AsyncCombatService`; `registry`/`config` desde `GameManager.Instance`.

## Archivos clave

```
Assets/RunRunSimulator/Scripts/UI/
├── UIManager.cs                      # SerializedMonoBehaviour: Dictionary<UIPanelType, GameObject>. HUB de eventos UI (static)
├── UIInputs.cs                       # MonoBehaviour: dueño único del action map "UI"
├── CreatureGridUI.cs                 # MonoBehaviour (uGUI): grilla in-game (Canvas)
├── CreatureVisualUI.cs               # MonoBehaviour: card de UN MoriMochi (prefab uGUI)
├── CreatureGridUITK.cs               # MonoBehaviour (UI Toolkit) + IUINavigable: gemelo UITK
├── MorimonchiDetailInfoUITK.cs       # MonoBehaviour (UI Toolkit) + IUINavigable: ventana detalle modal
├── BreedingPanelUITK.cs              # MonoBehaviour (UI Toolkit) + IUINavigable: panel breeding modal
├── CombatPanelUITK.cs                # MonoBehaviour (UI Toolkit) + IUINavigable: panel combate modal (3 tabs)
└── CreatureGridView.cs               # MonoBehaviour: grilla read-only de inspector (Odin TableList)

Assets/RunRunSimulator/Scripts/Interactables/
├── ThrowableObject.cs                # IThrowable: Rigidbody que el player sostiene y lanza. Para props NO-criatura
└── PanelTrigger.cs                   # IInteractable: al tap E dispara UIManager.RequestPanelToggle

Assets/RunRunSimulator/Scripts/Core/
└── Interfaces.cs                     # IThrowable · IInteractable · IUINavigable
```
