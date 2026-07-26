---
tags: [script, ui, detail]
---

# MorimonchiDetailInfoUITK.cs

**Ruta:** `UI/MorimonchiDetailInfoUITK.cs`

**Responsabilidad (S54 — Fase 7 composición, S57c, S57d, S67, S68):** Resumen detallado de MoriMochi (ventana modal, FireRed-summary inspired, 5 tabs). Núcleo delgado MonoBehaviour que orquesta: Tab 0 "Información" (stats+equipo, género/estado/nacimiento, rol+elemento, partes, contadores), Tab 1 "Combate" (historial 3v3 newest-first con replay), Tab 2-3 "Linaje + Descendencia" (árbol genealógico), Tab 4 "Equipo" (3 slots + stats Base→Final), Tab 5 "Relaciones" (visualizador SocialGraph: amigos/enemigos por afinidad efectiva). Escucha `UIManager.OnCreatureSelected` (evento carga dna + registry en campo). Implementa `IUINavigable` (solo A/D navega tabs — no Submit/Cancel internos). Setup: `Wire()` instancia presenters, obtiene refs via `document.Q<>()`. Eventos: `OnRegistryChanged` repopula en-place (equipping desde mochila actualiza tab Equipo sin mover foco). **S57d fix:** `OnRegistryChanged` ahora chequea `document.rootVisualElement.resolvedStyle.display == DisplayStyle.None` antes de Populate — evita loop infinito donde panel oculto rearmaba la cámara live justo después de su auto-apagado de MonchiLivePortrait; Show() siempre llama Populate al reabrir así que no se pierde frescura. Backdrop modal con sortingOrder > grid. **S57b:** Retrato del header vía [[MonchiPortraitUI]].Apply(). **S57c:** Retrato del header ahora vía [[MonchiPortraitUI]].ApplyLive() — cámara live si criatura está spawneada, foto fotomatón si no. **S67:** Tab 5 "Relaciones" via [[DetailRelationsPresenter]] (patrón S54). **S68:** Strings de labels/títulos extraídos a Loc.Tr (sin cambio de contrato).

**Organización (S54 composición — Fase 7, S67 +1 presenter):**
- `MorimonchiDetailInfoUITK.cs` — núcleo MonoBehaviour: lifecycle, wiring, población, IUINavigable routing (A/D tabs)
- Cinco presenters colaboradores (NO implementan ITabPresenter — son ro Rebuild sin navegación interna):
  - `DetailInfoTabPresenter.cs` — Tab 0: stats (base + bonus equipo), gender/state/nacimiento, rol+elemento, partes, contadores
  - `DetailCombatTabPresenter.cs` — Tab 1: historial 3v3 (replay, outcomes, stats)
  - `DetailTreesPresenter.cs` — Tab 2-3: Linaje (ancestros 2gen) + Descendencia (cría, agrupa por pareja) — comparten dominio genetics
  - `DetailEquipTabPresenter.cs` — Tab 4: 3 slots (Arma/Armadura/Amuleto) + cards clickeables abren mochila + stats tabla
  - `DetailRelationsPresenter.cs` — Tab 5: SocialGraph (amigos ≥ threshold, enemigos ≤ threshold, ordenados por afinidad efectiva)

**Decisión de arquitectura S54:**
- Presenters del Detail NO son `ITabPresenter` (son ro Rebuild, sin navegación A/D internas ni state jerárquico)
- Son colaboradores planos ctor(root, deps) + Rebuild(dna), sin Teardown (callbacks se recrean en cada rebuild)
- `DetailTreesPresenter` cubre DOS tabs (Linaje + Descendencia) por compartir `MakeChip()` + `ParseGenetics()`
- Core navega A/D entre tabs; cada tab es responsabilidad del presenter (sin sub-foco interno)

**Eliminadas partials (S54):**
- `MorimonchiDetailInfoUITK.Trees.cs` — descomposed en `DetailTreesPresenter.cs` (BuildLineage + BuildBreed + helpers)

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | UIToolkit doc tree (root de la ventana modal) |
| `panel` | `UIPanelType` | Tipo de panel (MorimonchiDetail) para routing UIManager |
| `database` | `CreatureDatabaseSO` | Resuelve part names/sets/rarity + stats efectivos |
| `equipmentDatabase` | `EquipmentDatabaseSO` | Resuelve item IDs → EquipmentSO (icon, rarity, effects) |
| `equipmentPalette` | `EquipmentPaletteSO` | Colores rarity + acentos por slot |
| `backpack` | `EquipmentBackpackUITK` | Popup mochila (equip desde tab Equipo) |
| `sortingOrder` | `int` | Z-order (100 default, sobre grid) |

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `titleLabel` | `Label` | Nombre MoriMochi (custom o ToStringID) |
| `portrait` | `VisualElement` | VisualElement retrato header |
| `tabs` | `TabView` | Selector de tabs (A/D navega) |
| `closeButton` | `Button` | Botón X (OnClose) |
| `wired` | `bool` | Flag: Wire() ejecutado |
| `info` | `DetailInfoTabPresenter` | Tab 0 presenter |
| `combat` | `DetailCombatTabPresenter` | Tab 1 presenter |
| `trees` | `DetailTreesPresenter` | Tab 2-3 presenter |
| `equip` | `DetailEquipTabPresenter` | Tab 4 presenter |
| `relations` | `DetailRelationsPresenter` | Tab 5 presenter (S67) |
| `registry` | `CreatureRegistrySO` | Guardado desde OnCreatureSelected (para genealogía) |
| `current` | `CreatureDNA` | Guardado desde OnCreatureSelected (para re-populate) |

## Métodos Públicos

### `OnUINavigate(Vector2 dir) → void`

IUINavigable: A/D (stick/dpad) navega entre tabs.

**Lógica:**
1. Si tabs null, retorna
2. Obtiene count de tabs: `tabs.Query<Tab>().ToList().Count`
3. Si count < 1, retorna
4. Si `dir.x > 0.5` (derecha): `selectedTabIndex = min(+1, last)`
5. Si `dir.x < -0.5` (izquierda): `selectedTabIndex = max(-1, 0)`

### `OnUISubmit() → void`

No-op (sin confirmación interna). Cierre vía X-button o ESC.

### `OnUICancel() → bool`

Retorna false — deja que UIManager cierre el panel en ESC.

## Métodos Privados

### `Wire() → void`

Una sola vez: obtiene refs UITree via Q<>, instancia presenters, wirea closeButton.

**Precondición:** `document != null` (requerido en inspector).

**Pasos:**
1. Si ya `wired`, retorna (idempotente)
2. Obtiene root: `document.rootVisualElement`
3. Si root null, retorna
4. Q<Label>("title") → titleLabel
5. Q<VisualElement>("portrait") → portrait
6. Q<TabView>("tabs") → tabs
7. Q<Button>("close-button") → closeButton, wirea `clicked += OnClose`
8. Instancia presenters (lazy-loaded en primera Show):
   - `new DetailInfoTabPresenter(root, database, equipmentDatabase)` → info
   - `new DetailCombatTabPresenter(root, () => registry)` → combat
   - `new DetailTreesPresenter(root, database, () => registry)` → trees
   - `new DetailEquipTabPresenter(root, database, equipmentDatabase, equipmentPalette, backpack, () => registry)` → equip
   - `new DetailRelationsPresenter(root, () => registry)` → relations (S67)
9. Asigna `wired = true`

**Nota:** Presenters reciben `Func<CreatureRegistrySO>` para lazy-load (resuélvese en tiempo de rebuild, no ctor).

### `Show(CreatureDNA dna, CreatureRegistrySO registry) → void`

UIManager.OnCreatureSelected handler: cachea dna+registry, popula, abre panel.

**Pasos:**
1. `this.registry = registry` — guardado para genealogía
2. `current = dna` — guardado para re-populate post-equip
3. Wire() — primera vez asegura refs
4. Populate(dna) — llena todos presenters
5. `tabs.selectedTabIndex = 0` — abre Tab 0 (Info)
6. `UIManager.RequestPanelSet(panel, true)` — muestra modal

### `OnClose() → void`

X-button handler.

**Pasos:**
1. `UIManager.RequestPanelSet(panel, false)` — cierra modal
2. `backpack?.Close()` — cierra mochila si abierta

### `OnRegistryChanged(CreatureRegistrySO _) → void`

GameEvents.OnRegistryChanged handler: repopula en-place si MoriMochi actual cambió (e.g. equipping).

**Lógica (S57d):**
- Si `current == null` o `!wired`: retorna (nada que repoblar o no inicializado)
- Obtiene root: `document.rootVisualElement`
- Si root null: retorna (documento no disponible)
- **S57d FIX: Si `root.resolvedStyle.display == DisplayStyle.None`: retorna (panel oculto, evita repoblar)**
  - Previene loop infinito donde OnRegistryChanged dispara Populate que activa cámara live vía ApplyLive, pero el panel está oculto así que MonchiLivePortrait.LateUpdate detecta IsHidden y llama End() automáticamente, lo que re-dispara ApplyLive → Begin, todo en el mismo frame — loop infinito
  - Solución: no repoblar si panel está oculto. Show() siempre llama Populate al reabrir así que no se pierde frescura de datos
- Si visible: `Populate(current)` — refresca todos presenters con estado nuevo
- Tab activo NO cambia (usuario mantiene foco)

**Nota anterior (ahora supersedida por check display):** No pedía `dna` en evento; usa `current` guardado desde Show.

### `Populate(CreatureDNA dna) → void`

Llena título + retrato + todos presenters con DNA actual.

**Pasos:**
1. Si dna null, retorna
2. Actualiza titleLabel: `dna.CustomName` si existe, else `dna.ToStringID()`
3. Actualiza retrato portrait:
   - **S57c:** `MonchiPortraitUI.ApplyLive(portrait, dna)` — intenta live, fallback foto
4. Repopula presenters:
   - `info.Rebuild(dna)`
   - `combat.Rebuild(dna)`
   - `trees.Rebuild(dna)`
   - `equip.Rebuild(dna)`
   - `relations.Rebuild(dna)` — S67

**Nota:** ApplyLive intenta montar cámara live (si criatura en mundo), cae a foto fotomatón (si no está spawneada).

## Lifecycle

**Awake:**
- Configura `document.sortingOrder = sortingOrder` (sobre grid)

**OnEnable:**
- Suscribe `UIManager.OnCreatureSelected += Show`
- Suscribe `GameEvents.OnRegistryChanged += OnRegistryChanged`

**OnDisable:**
- Desuscribe `UIManager.OnCreatureSelected -= Show`
- Desuscribe `GameEvents.OnRegistryChanged -= OnRegistryChanged`

**Start:**
- Wire() — primera carga refs
- `UIManager.RegisterNavigable(panel, this)` — registra como handler input

**OnDestroy:**
- Desuscribe `closeButton.clicked -= OnClose`
- `UIManager.UnregisterNavigable(panel)` — desregistra input

## Vinculado a

- [[Index/05 - UI System]]
- [[Index/11 - Technical Debt]] (Fase 7 deuda — composición)
- [[DetailInfoTabPresenter]]
- [[DetailCombatTabPresenter]]
- [[DetailTreesPresenter]]
- [[DetailEquipTabPresenter]]
- [[DetailRelationsPresenter]]
- [[UIManager]]
- [[GameManager]]
- [[GameEvents]]
- [[EquipmentBackpackUITK]]
- [[CreatureDatabaseSO]]
- [[EquipmentDatabaseSO]]
- [[EquipmentPaletteSO]]
- [[CombatReplayRequest]]
- [[MonchiPortraitUI]] — ApplyLive (S57c)
- [[MonchiLivePortrait]] — motor live (S57c, S57d)
- [[Loc]] — S68 localización

## Conexiones

**Entrada:**
- `UIManager.OnCreatureSelected(dna, registry)` — abre panel
- `GameEvents.OnRegistryChanged(registry)` — repopula en-place (equip), con check display (S57d)
- `IUINavigable.OnUINavigate(dir)` — A/D navega tabs
- `closeButton.clicked` — cierra panel

**Salida:**
- `UIManager.RequestPanelSet(panel, show)` — abre/cierra modal
- Presenters rebuild llenan tabs (sin mutación de estado exterior)
- `backpack?.Close()` — cierra submenu

## Notas S68

- Strings de labels/títulos ahora vía Loc.Tr (sin cambio de interfaz pública)
- Todos los presenters populados delegaron strings de display a Loc.Tr/LocEnumMaps

## Notas S54

- Presenters son clases planas (no MonoBehaviour) sin estado excepto UI; reciben `Func<CreatureRegistrySO>` para lazy-load
- Callbacks de botones (equipo: mochila, combate: replay) se recrean en cada Rebuild
- OnRegistryChanged repopula in-place sin mover tab (user mantiene foco)
- IUINavigable: OnUINavigate A/D navega tabs (0-4), OnUISubmit vacío, OnUICancel retorna false (cierra vía UIManager)
- Stats en Info/Equipo usan `CombatStats.GetEffectiveStats()` + `EquipmentStats.Apply()` (S32+S26)
- Rol/Elemento heredables desde DNA (S39)

## Notas S57c

- **Retrato live:** `ApplyLive(portrait, dna)` intenta montar cámara live (MonchiLivePortrait.Begin)
- **Fallback automático:** Si criatura no está spawneada o no es accesible, caída transparente a foto fotomatón (MonchiPortraitUI.Apply)
- **Transición dinámica:** Si criatura despawneó después de Begin, MonchiLivePortrait.LateUpdate detecta y cierra automáticamente (repinta Apply)
- **Sin acople a cierre:** Panel cierra sin notificar a MonchiLivePortrait; el singleton lo gestiona solo via validación en LateUpdate

## Notas S57d

- **Loop infinito fix:** OnRegistryChanged ahora chequea `document.rootVisualElement.resolvedStyle.display == DisplayStyle.None` y corta si el panel está oculto
- **Raíz del bug:** Panel oculto + OnRegistryChanged → Populate → ApplyLive(Begin) → cámara se activa → LateUpdate detecta IsHidden(element) → End() → repinta Apply → MonchiPortraitUI.Apply pide foto fotomatón... pero el elemento está oculto así que MonchiLivePortrait no ve que Begin falló y deja la cámara activa — loop infinito
- **La solución:** No repoblar (y por tanto no activar cámara live) si el panel UI está oculto (display:none). Show() siempre llama Populate cuando se abre, así que datos no quedan stale
- **Garantía de frescura:** Al reabrir el panel, Show() es llamado por UIManager o el panel abre via RequestPanelSet + Show, así que Populate se dispara automático con el registry actual

## Notas S67

- **Tab 5 Relaciones:** Presenter independiente [[DetailRelationsPresenter]] — renderiza dos listas (amigos/enemigos) por afinidad efectiva (seed + historia)
- **Patrón S54:** Constructor ctor(root, deps) + Rebuild(dna), sin ITabPresenter, sin state jerárquico
- **Decisión UI:** Muestra solo monchis vivos con afinidad dentro de los thresholds (no intermedio)
