---
tags: [memory-bank, furniture, building, shop, stage-3]
---

# 10 — Furniture & Building

> Sistema de muebles colocables en la tienda (Etapa 3.1). Calca la arquitectura de criaturas: **data ligera + registry como verdad + spawner event-driven**. Esta nota es la fuente de implementación; el diseño vivo (precios, categorías, economía) vive en el [Notion Wiki](https://www.notion.so/36cac10136a781819b74e176ed7c00d9).

## Estado por fase

| Fase | Qué incluye | Estado |
|------|-------------|--------|
| **Fase 1 — Data + grid + API** | SOs, registry, `PlacementGrid`, `FurnitureSpawner`, `FurnitureService` con `TryPlace`/`TryRemove` + botones Odin de test | ✅ **implementada y commiteada** |
| **Fase 2 — Building mode** | Action map `Building` (aditivo) + máquina de estados (Browsing/Placing/Editing/Deleting), ghost, hotbar 1-4, edición/borrado por raycast a muebles | 🔶 **código ✅**, falta setup de escena + persistencia |
| **Fase 3 — Economía + tienda** | `Wallet` (moneda persistente), `ShopService`, panel UITK con catálogo + precio → entra a placement | 🔲 |
| **Fase futura — superficies libres** | Muebles grandes como base que exponen superficies sobre las que acomodar props chicos (free placement local a la superficie), eventualmente con UI | 🔲 **solo diseño** (etapa posterior, ver Notion) |
| **Persistencia** (transversal) | JSON propio para `FurnitureRegistrySO` vía `GameManager.Persist` + `SaveSystem`, cloud después | 🔲 **deliberadamente pendiente** (se confirma placement primero) |

> **Modelo de colocación — decisión confirmada (2026-06-02):** **grilla como base**, no posicionamiento libre. Razones: NavMesh determinista (las criaturas roam, la grilla evita huecos donde quedan atrapadas), persistencia cloud ligera/determinista (`DefId`+celda+rotación vs transform flotante que deriva), y foco de ingeniería en las criaturas (el canvas expresivo) y no en los muebles. El placement libre sobrevive solo como la "Fase futura" de arriba: **acotado a superficies de muebles grandes**, no global.

---

## Arquitectura — separación de responsabilidades

Mirror del pipeline de criaturas. La regla central:

> **grid = math/ocupación · service = flujo · spawner = meshes · registry = verdad.**

Comunicación cross-sistema **solo por `GameEvents`** (regla no-negociable #1): el evento transporta el `registry`, el suscriptor no busca singletons.

```
FurnitureService  ──TryPlace/TryRemove──►  FurnitureRegistrySO  (verdad)
       │                                          │
       │ valida contra                            │ dispara
       ▼                                          ▼
  PlacementGrid (ocupación)            GameEvents.OnFurnitureChanged
                                                  │
                                                  ▼
                                       FurnitureSpawner (meshes)
```

---

## Capa de datos (`Scripts/Data/`)

| Archivo | Análogo en criaturas | Contrato |
|---------|----------------------|----------|
| `FurnitureDefinitionSO` | `BodyPart` | `Id` (**`[ReadOnly]`, lo dicta la DB**), `DisplayName`, `Prefab`, `Footprint` (Vector2Int, celdas), `Price`, `Category`. `SerializedScriptableObject` |
| `FurnitureDatabaseSO` | `PartDatabaseSO<T>` | `[OdinSerialize]` dict `string→FurnitureDefinitionSO` (**la key ES el id**) con inline editor. `GetById(id)`, **Populate from Buffer**, **Validate & Sync IDs** (reindexa a `F0,F1…` y estampa el id → duplicados/`-` imposibles por construcción) |
| `PlacedFurniture` | `CreatureDNA` | record persistente: `DefId`, `CellX`, `CellY`, `Rotation` (0/90/180/270). Key = celda ancla `"x_y"` → una celda ancla = a lo sumo una pieza |
| `FurnitureRegistrySO` | `CreatureRegistrySO` | `SerializedScriptableObject` + `[OdinSerialize]` dict `string→PlacedFurniture`. `Place`/`RemoveAt`/`TryGet`/`GetAll`/`LoadFrom`/`Count`. **Mutar solo vía `FurnitureService`** |

**Invariantes** (heredados de las reglas del proyecto):
- `FurnitureDefinitionSO.Id` lo **dicta la DB** (la key del dict, prefijo `F`) — nunca contiene `-` por construcción. El campo es `[ReadOnly]` (mirror de `BodyPart.ID`).
- `FurnitureRegistrySO` hereda de `SerializedScriptableObject` con `[OdinSerialize]` (regla Odin #7).
- El registry es DTO-ligero: el mundo se reconstruye desde él, no al revés.

---

## Capa de sistemas (`Scripts/Systems/Furniture/`)

### `PlacementGrid` — math/ocupación
- `cellSize` + `dimensions` (Vector2Int). Origen = `transform.position` (esquina min), plano XZ.
- `WorldToCell(world)` → celda ancla que contiene el punto.
- `FootprintCenter(anchor, footprint, rotation)` → centro world de la huella en el plano lógico del grid (XZ). La Y real del suelo **no** viene de aquí sino de `TrySampleFloor`.
- `CanPlace` / `Occupy` / `Free` / `Clear` sobre un `HashSet<Vector2Int>` de celdas ocupadas (estado runtime, derivado del registry).
- Una rotación 90°/270° **intercambia X/Y** de la huella (`Rotated`).
- Gizmos: grid azul + cubos rojos en celdas ocupadas. Solo `Gizmos.*`, compila en build.
- **`TrySampleFloor(anchor, fp, rot, out y, out flat)`** *(nuevo)*: raycast vertical desde `transform.position + floorProbeHeight` hacia abajo, contra `floorMask`. Devuelve la Y real del suelo bajo el centro del footprint y si la normal es plana (`Vector3.Angle(normal, up) ≤ maxSlopeAngle`). Fuente única de Y para el ghost y el spawner — mantiene preview == colocado incluso en terreno irregular.
  - Campos nuevos: `floorMask` (layer del suelo/terreno), `maxSlopeAngle` (default 5°), `floorProbeHeight` (alcance del rayo).
  - **Setup**: poner el transform del grid ligeramente **por encima** del piso más alto de la escena; el rayo baja hasta encontrar el suelo real. `floorMask` = layer Floor (NUNCA incluir muebles).

### `FurnitureService` — flujo + API pública
- **Hotbar**: `activePieces` (`List<FurnitureDefinitionSO>`) + `SelectPiece(index)` (1-4) + `ActivePiece` (la seleccionada). Fuente única de "qué coloco".
- `TryPlace(def, cell, rot)` (core) + `TryPlace(cell, rot)` (usa `ActivePiece`): valida `grid.CanPlace` → `registry.Place` → `grid.Occupy` → `FurnitureChanged`.
- `TryRemove(cell)`: resuelve la def → `grid.Free` → `registry.RemoveAt` → `FurnitureChanged`.
- `TryLift(cell, out def, out rot)`: "levanta" la pieza (remove + devuelve def/rot) para que el build mode la re-coloque tras editar o la descarte al borrar. Simétrico con `TryPlace` (dispara `FurnitureChanged` → el mesh despawnea).
- `Snap90(deg)` normaliza ángulos a 90° en [0,270]. Botones Odin de test: **Place / Remove / Clear All** (usan la pieza seleccionada).
- **`TryPlace`/`TryRemove`/`TryLift` son LA API que maneja el Building mode**; el shop (Fase 3) se construye encima.
- **Rebake de NavMesh** (`navSurface` `NavMeshSurface`): botón Odin **Rebake NavMesh** + **auto-rebake** tras la carga inicial (`Start`) y tras `OnFurnitureReloaded`, **diferido a fin de frame** (`WaitForEndOfFrame` + flag de coalesce) para esperar a que el `FurnitureSpawner` termine de instanciar las mallas (ambos reaccionan al mismo evento; el orden entre suscriptores no está garantizado). Necesario para que el piso pintado de un **corral** (`BreedingRoom`, ver [[06 - Player & World]]) entre al NavMesh. Colocar en build mode dispara `OnFurnitureChanged` (no Reloaded) → **no** rebakea solo: tras poner un corral, usar el botón.

### `FurnitureSpawner` — meshes (event-driven)
- Suscribe `OnFurnitureChanged` / `OnFurnitureReloaded` en `OnEnable`, desuscribe en `OnDisable` (regla #9).
- `Sync` incremental: instancia keys nuevas, destruye las que ya no están (diff contra `spawned`).
- `OnReloaded` = `ClearAll` + `Sync` (para pull/reset, sin re-push — patrón `OnRegistryReloaded`).
- Posiciona el **pivote raíz** del prefab en `grid.FootprintCenter` y rota con `Quaternion.Euler(0, Rotation, 0)`. Runtime "tonto": el control de alineación vive en el prefab.
- **Snap al piso real** *(nuevo)*: tras calcular la posición XZ con `FootprintCenter`, llama `grid.TrySampleFloor` y reemplaza la Y con la del suelo real → muebles se asientan en terreno irregular. La Y no se guarda en `PlacedFurniture` (Opción B): el terreno es la fuente de verdad, si cambia la geometría la pieza se re-asienta al cargar.

### Prefab: pivote y `FurniturePivotAligner`
El runtime coloca el **pivote raíz** del prefab en el centro del footprint y **rota alrededor de ese pivote**. Por lo tanto:

> **El pivote raíz del prefab debe quedar en el centro del footprint (XZ) y en la base (Y).** Pivote en esquina = la pieza se sale de sus celdas al rotar (la rotación es alrededor del pivote).

- **Estructura recomendada**: Root vacío (= el pivote) → Model (mesh) como hijo → Collider. El runtime mueve el Root.
- **`FurniturePivotAligner`** (helper de EDITOR, `Scripts/Systems/Furniture/`): se agrega al Root; botón **Center pivot** mueve los hijos para que el Root quede en el centro-base del mesh. Gizmo del footprint para verificar el calce. **Es removible**: el offset queda horneado en las posiciones locales de los hijos y borrás el componente. Mantenés control total (nudge a mano, `restOnFloor` on/off, re-correr).
- Requiere el mesh en un **hijo** (no se puede mover la raíz respecto a sí misma). Para un primitivo (Cube) envolvelo en un Root vacío.
- `Rotated()` intercambia 1×2 ↔ 2×1 para que la ocupación siga la rotación.
- **Footprint no rectangular (L)**: por ahora declarar el **rectángulo contenedor** (sobre-ocupa la celda vacía de la esquina); máscara de celdas = futuro.

---

## Enums y eventos

- `Enums.cs`:
  - `PlayerStateType.Building = 3` — modo construcción (lo conmuta `BuildModeController.OnBuildModeChanged` → `PlayerController`).
  - `FurnitureCategory` { `Decoration`, `Display`, `Functional` }.
- `GameEvents.cs`:
  - `OnFurnitureChanged(FurnitureRegistrySO)` — toda mutación (place/remove/lift). La dispara `FurnitureService`.
  - `OnFurnitureReloaded(FurnitureRegistrySO)` — reload completo (clear+resync); UI/spawner only, sin push (espejo de `OnRegistryReloaded`).
- Eventos de dominio (fuera de `GameEvents`, estilo `OnUIFocusChanged`): `BuildModeController.OnBuildModeChanged(bool)`, `PlayerInputs.BuildToggled`, y los eventos estáticos de `BuildingInputs`.

---

## Setup en Unity (pendiente del usuario)

**Data + escena base:**
1. Prefab del mueble: Root vacío (= pivote) → Model (mesh, layer **Furniture**, con Collider). Bakeá el pivote con `FurniturePivotAligner` (centro-base) y borrá el helper.
2. Assets: **Furniture Definition** (footprint, prefab) → **Furniture Database**: arrastrá las defs al **Bulk Add** → **Populate from Buffer** (o **Validate & Sync IDs**); **Furniture Registry**.
3. GameObject con `PlacementGrid` (sobre el piso; el piso en layer **Floor** con collider).
4. GameObject con `FurnitureSpawner` + `FurnitureService` (asignar grid/database/registry + la lista **Active Pieces** del hotbar + **`navSurface`** = la `NavMeshSurface` principal, para el rebake de corrales).

**Build mode:**
5. GameObject con `BuildingInputs` + `BuildModeController`. Asignar: `service`, `grid`, `aimTransform` (cámara FP), **`floorMask` = Floor**, **`furnitureMask` = Furniture**, `ghostMaterial` (URP/Lit Transparent).
6. Play → **B** entra; **1-4** elegís pieza; mirás + **R** + click izq. para fijar + **F** confirma; **E** / click der. editás / borrás.

---

## Build mode (Fase 2 — implementado)

**Entrada/salida desde el Player** (`B`); el resto lo maneja el action map `Building`. Cableado (calca `PlayerInputs`/`UIInputs`):

- `PlayerInputs.BuildToggled` (acción **Build** = B, en el map Player) → entra/sale.
- `BuildingInputs` — dueño del map `Building`; emite eventos estáticos: `Confirm` (F), `Cancel` (Esc), `Rotate` (R), `Pin` (click izq.), `Edit` (E), `Delete` (click der.), `SlotSelected` (1-4).
- `BuildModeController` — ciclo de vida + máquina de estados + ghost. Emite `OnBuildModeChanged(bool)`.
- `PlayerController` escucha `OnBuildModeChanged` → estado `Building`.

**Map aditivo (NO mutuamente excluyente):** el map `Building` se enciende ENCIMA del `Player`. Así Move + Look (Cinemachine lee `Look` del map Player) siguen vivos sin duplicar lógica; `PlayerController` apaga grab/throw/jump/interact por estado. Las teclas de build (F/R/E/click der.) no chocan con ninguna del Player. — Desvío deliberado del plan viejo ("mutuamente excluyente"), por simplicidad.

### Máquina de estados

| Estado | Entrada | Controles |
|--------|---------|-----------|
| **Browsing** | B | **1-4** pieza → Placing · **E** (apuntando a mueble) editar · **click der.** (apuntando) borrar · **Esc** salir |
| **Placing** (nueva) | 1-4 | mover (mirás) · **R** rota · **click izq.**/**F** fija en celda libre → Editing |
| **Editing** (nueva fijada o existente) | pin / E | **R** rota · **F** verde→commit→Browsing; rojo→revierte al último giro válido · **Esc** cancela |
| **Deleting** | click der. | mueble en rojo · **F** confirma borrado · **Esc** restaura |

**Esc anidado:** en un sub-estado cancela la selección (restaura si había pieza levantada) → Browsing; en Browsing sale del modo. **B** sale siempre. Tras confirmar (F) una colocación válida → vuelve a **Browsing** (decisión del usuario).

### Selección por raycast — tres máscaras
- **`floorMask`** (layer Floor): el rayo de cámara al suelo durante *Placing* → celda XZ bajo la mira. También la usa `PlacementGrid.TrySampleFloor` (vertical, independiente).
- **`furnitureMask`** (layer Furniture): *Edit* y *Delete*. El rayo pega en el mueble apuntado; `PlacedFurnitureMarker` devuelve la celda ancla → `TryLift`. Sin segundo raycast ni parseo de nombres; correcto para multi-celda.
- **`obstacleMask`** *(nuevo)* (layers de muros/escenografía fija): `OverlapsObstacle()` hace `Physics.CheckBox` orientado al footprint contra esta layer. El ghost gira rojo si colisiona. **No incluir Floor aquí** (siempre daría rojo).

### Validez unificada — `PlacementValid()`
`CanPlace` (celdas libres + dentro de bounds) **+** `floorFlat` (pendiente ≤ `maxSlopeAngle`) **+** `!OverlapsObstacle` (sin colisión física). Única fuente de verdad para tint, `OnPin` y `OnConfirm` → ghost y confirmación siempre coinciden. Pendiente inclinado = siempre inválido (no hay muebles en rampas).

### Mecánica de "levantar" (lift)
Seleccionar una pieza existente (E / click der.) llama `TryLift` → la quita del registry (su mesh despawnea) y la sostiene como ghost. Confirmar la re-coloca (`TryPlace`) en su nuevo estado; cancelar/salir la restaura en su celda/rotación original. Así rota sin chocar consigo misma y todo pasa por la API event-driven.

### Ghost
Instancia `ActivePiece.Prefab` (o la def levantada), desactiva colliders, aplica `ghostMaterial` y lo tiñe verde/rojo por frame con `MaterialPropertyBlock`. Posiciona con `FootprintCenter` + `Euler(0,rot,0)` — igual que el spawner, así **preview == resultado**.

## UI de selección de piezas — Inventario (Hotbar + Browser)

> **Decisión confirmada (2026-06-04):** modelo **Hotbar + Browser temporal**. Diseñado para ser console-first (D-pad) y compatible con PC.

### Problema

El hotbar (slots 1-4) ya está implementado y es el mecanismo correcto para consola. Lo que falta es cómo el jugador **carga** esos slots desde su inventario completo.

### Opciones descartadas

| Opción | Por qué se descartó |
|--------|---------------------|
| **Sidebar fijo** | En FP tapa ~30% de pantalla durante el ghost; molesto en consola |
| **Rueda/radial** | Escala mal: con >8 piezas necesitás "páginas" en una rueda, lo que es horrible de navegar |

### Diseño elegido: Hotbar + Browser temporal

El hotbar es acceso rápido (slots 1-4, ya implementado). El **browser** solo se abre cuando el jugador quiere **reasignar un slot**:

```
Browsing → Hold Tab (PC) / Select (consola)
  → Panel abre (tiempo no pausado, sigue en Build mode)
    → D-pad ↑↓  navega categorías (Decoration / Display / Functional)
    → D-pad ←→  navega piezas dentro de la categoría
    → A / Enter  asigna la pieza al slot activo → cierra el panel
  → Vuelve a Browsing con la nueva pieza en el slot
```

**Rationale:**
- Cero visual clutter durante placement — el browser solo aparece on-demand.
- Escala a cualquier cantidad de piezas (paginación por categoría, sin límite).
- D-pad es el control estándar de grids en consola (Animal Crossing, Planet Zoo, The Sims).
- Una vez configurados los 4 slots favoritos, el loop de placement es fluido y sin fricciones.
- Fase 3 (Shop) se integra naturalmente: comprar una pieza la manda al inventario → aparece en el browser.

### Implicaciones en código (sobre lo ya implementado)

- `FurnitureService.activePieces` (`List<FurnitureDefinitionSO>`) ya es mutable → el browser simplemente escribe ahí.
- `FurnitureCategory` (enum) ya existe en los SOs → tabs de categoría gratis.
- Action map `Building` ya tiene `SlotSelected` → agregar acción `BrowseOpen` (Tab / Select button).
- Nuevo panel UITK que lee `FurnitureDatabaseSO` por categoría. Sin tocar el core de placement ni la FSM del build mode.

---

## Fase 3 — Economía + tienda

- `Wallet`: moneda del jugador, **persiste**.
- `ShopService` + panel UITK que lista `FurnitureDefinitionSO` con precio → comprar entra a placement (Fase 2).
- Diseño de precios/categorías/economía → **Notion** (capa de diseño, dueño = usuario).

## Persistencia (pendiente transversal)

Aún **no** se persiste furniture (ni JSON ni cloud) — decisión deliberada para confirmar el placement primero. Falta:
- Wirear `GameManager.Persist` + `SaveSystem` con un archivo JSON propio para el `FurnitureRegistrySO` (ver [[07 - Persistence & Identity]]).
- Cloud después (mismo patrón que `CloudSyncService`).
- Recordar: ningún script de gameplay llama a `SaveSystem`/`PushToCloud` directo — dispara el evento, `GameManager` persiste (regla #2).

---

## Enlaces

- Persistencia y patrón registry/eventos: [[07 - Persistence & Identity]]
- Action maps mutuamente excluyentes / stack UI: [[05 - UI System]]
- Player FP, cámara, interact: [[06 - Player & World]]
- Estado de la sesión: [[09 - Active Context]]
