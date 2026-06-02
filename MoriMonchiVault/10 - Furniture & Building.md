---
tags: [memory-bank, furniture, building, shop, stage-3]
---

# 10 — Furniture & Building

> Sistema de muebles colocables en la tienda (Etapa 3.1). Calca la arquitectura de criaturas: **data ligera + registry como verdad + spawner event-driven**. Esta nota es la fuente de implementación; el diseño vivo (precios, categorías, economía) vive en el [Notion Wiki](https://www.notion.so/36cac10136a781819b74e176ed7c00d9).

## Estado por fase

| Fase | Qué incluye | Estado |
|------|-------------|--------|
| **Fase 1 — Data + grid + API** | SOs, registry, `PlacementGrid`, `FurnitureSpawner`, `FurnitureService` con `TryPlace`/`TryRemove` + botones Odin de test | ✅ **implementada y commiteada** |
| **Fase 2 — Building mode** | Action map `Building`, `PlayerStateType.Building`, ghost preview, flujo click→F→Esc, borrado con click derecho | 🔲 próxima |
| **Fase 3 — Economía + tienda** | `Wallet` (moneda persistente), `ShopService`, panel UITK con catálogo + precio → entra a placement | 🔲 |
| **Persistencia** (transversal) | JSON propio para `FurnitureRegistrySO` vía `GameManager.Persist` + `SaveSystem`, cloud después | 🔲 **deliberadamente pendiente** (se confirma placement primero) |

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
| `FurnitureDefinitionSO` | `BodyPart` | `Id` (sin `-`), `DisplayName`, `Prefab`, `Footprint` (Vector2Int, en celdas), `Price`, `Category` |
| `FurnitureDatabaseSO` | `*DatabaseSO` | catálogo plano `List<FurnitureDefinitionSO>`, `GetById(id)`, botón Odin **Validate IDs** (detecta `-` y duplicados) |
| `PlacedFurniture` | `CreatureDNA` | record persistente: `DefId`, `CellX`, `CellY`, `Rotation` (0/90/180/270). Key = celda ancla `"x_y"` → una celda ancla = a lo sumo una pieza |
| `FurnitureRegistrySO` | `CreatureRegistrySO` | `SerializedScriptableObject` + `[OdinSerialize]` dict `string→PlacedFurniture`. `Place`/`RemoveAt`/`TryGet`/`GetAll`/`LoadFrom`/`Count`. **Mutar solo vía `FurnitureService`** |

**Invariantes** (heredados de las reglas del proyecto):
- `FurnitureDefinitionSO.Id` **nunca** contiene `-` (separador reservado de saves/red — misma regla que IDs de partes).
- `FurnitureRegistrySO` hereda de `SerializedScriptableObject` con `[OdinSerialize]` (regla Odin #7).
- El registry es DTO-ligero: el mundo se reconstruye desde él, no al revés.

---

## Capa de sistemas (`Scripts/Systems/Furniture/`)

### `PlacementGrid` — math/ocupación
- `cellSize` + `dimensions` (Vector2Int). Origen = `transform.position` (esquina min), plano XZ.
- `WorldToCell(world)` → celda ancla que contiene el punto.
- `FootprintCenter(anchor, footprint, rotation)` → centro world de la huella (lo usa el spawner para posicionar).
- `CanPlace` / `Occupy` / `Free` / `Clear` sobre un `HashSet<Vector2Int>` de celdas ocupadas (estado runtime, derivado del registry).
- Una rotación 90°/270° **intercambia X/Y** de la huella (`Rotated`).
- Gizmos: grid azul (`OnDrawGizmosSelected`) + cubos rojos en celdas ocupadas. Solo `Gizmos.*`, compila en build.

### `FurnitureService` — flujo + API pública
- `TryPlace(cell, rot)`: valida `grid.CanPlace` → `registry.Place` → `grid.Occupy` → `GameEvents.FurnitureChanged`.
- `TryRemove(cell)`: resuelve la def para la huella → `grid.Free` → `registry.RemoveAt` → `FurnitureChanged`.
- `Snap90(deg)` normaliza ángulos a pasos de 90° en [0,270].
- Botones Odin de test (Play mode): **Place at Cell**, **Remove at Cell**, **Clear All**.
- **`TryPlace`/`TryRemove` son LA API que el Building mode (Fase 2) y el shop (Fase 3) van a manejar** — no se reescribe nada, se construye encima.

### `FurnitureSpawner` — meshes (event-driven)
- Suscribe `OnFurnitureChanged` / `OnFurnitureReloaded` en `OnEnable`, desuscribe en `OnDisable` (regla #9).
- `Sync` incremental: instancia keys nuevas, destruye las que ya no están (diff contra `spawned`).
- `OnReloaded` = `ClearAll` + `Sync` (para pull/reset, sin re-push — patrón `OnRegistryReloaded`).
- Posiciona con `grid.FootprintCenter`, rota `Quaternion.Euler(0, Rotation, 0)`.

---

## Enums y eventos

- `Enums.cs`:
  - `PlayerStateType.Building = 3` — modo construcción (lo conmutará el Building action map).
  - `FurnitureCategory` { `Decoration`, `Display`, `Functional` }.
- `GameEvents.cs`:
  - `OnFurnitureChanged(FurnitureRegistrySO)` — toda mutación (place/remove). La dispara `FurnitureService`.
  - `OnFurnitureReloaded(FurnitureRegistrySO)` — reload completo (clear+resync); UI/spawner only, sin push (espejo de `OnRegistryReloaded`).

---

## Setup en Unity para probar Fase 1 (pendiente del usuario)

1. Prefab cubo 1×1.
2. Assets: **Furniture Definition** (`Id = CUBE`, footprint 1×1, prefab), **Furniture Database** (agregar la def), **Furniture Registry**.
3. GameObject con `PlacementGrid`.
4. GameObject con `FurnitureSpawner` + `FurnitureService` (asignar grid/database/registry + `activePiece` = cubo).
5. Play → botón **Place at Cell**.

---

## Fase 2 — Building mode (próximo paso)

- **Action map `Building`** en el Input Actions, mutuamente excluyente con `Player`/`UI` (mismo patrón que `OnUIFocusChanged`, ver [[05 - UI System]]).
- Conmutar a `PlayerStateType.Building` al entrar a construcción.
- **Ghost preview**: sigue la celda bajo el cursor (`grid.WorldToCell`), se tiñe verde/rojo según `grid.CanPlace`.
- **Flujo de colocar**: click posiciona el ghost → **F** confirma (`FurnitureService.TryPlace`) → **Esc** sale del pre-colocado y vuelve al modo.
- **Flujo de borrar**: click derecho sobre un mueble lo marca en rojo → **F** confirma (`TryRemove`).
- Todo se construye sobre `TryPlace`/`TryRemove` (ya existen como API pública — no se toca la Fase 1).

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
