---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-01
**Foco**: Pulido del modo 3D de los MoriMonchis (lanzamiento/rebote/ragdoll) + arranque del **Sistema de Furniture** (Etapa 3.1, Fase 1: data).

### Qué se hizo

**MoriMonchis (`MoriMochiAgent.cs`, `PersonalityProfileSO.cs`, `MoriMochiSpawner.cs`, `PlayerController.cs`, `Interfaces.cs`, `ThrowableObject.cs`)**
- **Rebote tipo peluche**: en `OnCollisionEnter`, mientras vuela, refleja la velocidad pre-impacto (`lastVelocity`, capturada en `FixedUpdate`) sobre la normal del contacto con `Vector3.Reflect * bounciness`, por N `maxBounces`, + torque random. **100% por código, NADA de PhysicMaterials** (decisión firme). El frenado post-rebote es por `linear/angularDamping` del Rigidbody.
- **Settle robusto**: chequeo real de suelo (`IsGrounded()` raycast) + timeout de seguridad (`maxThrownTime`) → no más deslizamiento infinito.
- **Levantarse natural**: `downedDelay`/`getUpDuration` escalados por `PersonalityProfile.RecoverySpeed` + `getUpJitter`. Estado `Recovering` con slerp a vertical.
- **Knock / ragdoll**: `IThrowable.Knock(Vector3)` agregado al contrato. Un MoriMochi en vuelo que choca a otro `IThrowable` lo manda a volar (handoff NavMesh→física + impulso) y rebota él mismo → reacción en cadena. Tunables `knockTransfer`, `knockUpBias`.
- **Throw hacia la mira**: el lanzamiento apunta desde el `holdAnchor` (que está al costado) HACIA el punto de la cámara (raycast al centro, ignorando el objeto sostenido vía `IsChildOf`). Converge a la mira en vez de viajar paralelo. Campo `throwAimDistance` (30m default).
- **Color por personalidad**: `ApplyTint` vía `MaterialPropertyBlock` (sin fuga). El renderer está en el hijo `Model` (el root NO tiene mesh) → campo `bodyRenderer` serializado, fallback a `Find("Model")`.
- **Preferencia, no confinamiento**: se eliminó `ConfineToArea`. Ahora `AreaPreference` (0-1) = probabilidad de que un punto de roam apunte a `PreferredArea`. Todos se mueven libres (areaMask = AllAreas). Spawn arranca sesgado a la preferida ("casa").
- **Gizmos** simplificados a `Gizmos.DrawWireSphere` (sin Handles, compila en build).
- **Feel-ready**: `UnityEvent` hooks (`onGrab/onThrow/onBounce/onLand/onGetUp`) en el agent. Feel YA está instalado (`Assets/Feel`). Plantilla: cablear `MMF_Player.PlayFeedbacks()` a cada UnityEvent en el inspector, sin tocar código. Prefab: root `MoriMochi Agent` (sin mesh) → hijos `Model` (mesh) y `Feedbacks` (MMF_Players).

**Furniture — Fase 1 (data), Etapa 3.1**
- Calca la arquitectura de criaturas. Nuevos archivos:
  - `Data/FurnitureDefinitionSO.cs` (Id sin '-', DisplayName, Prefab, Footprint Vector2Int, Price, Category)
  - `Data/FurnitureDatabaseSO.cs` (lista + GetById + validate)
  - `Data/PlacedFurniture.cs` (record: DefId, CellX, CellY, Rotation; key = celda ancla "x_y")
  - `Data/FurnitureRegistrySO.cs` (dict, mirror de CreatureRegistrySO)
  - `Systems/Furniture/PlacementGrid.cs` (cellSize+dimensions, WorldToCell/FootprintCenter/CanPlace/Occupy/Free/Clear, ocupación HashSet, gizmos)
  - `Systems/Furniture/FurnitureSpawner.cs` (reconstruye meshes desde el registry por eventos)
  - `Systems/Furniture/FurnitureService.cs` (TryPlace/TryRemove + botones Odin de test)
- `Enums.cs`: +`FurnitureCategory`, +`PlayerStateType.Building`. `GameEvents.cs`: +`OnFurnitureChanged`/`OnFurnitureReloaded`.
- Separación: **grid = math/ocupación, service = flujo, spawner = meshes**, registry = verdad.

### Setup en Unity para probar Furniture (pendiente del usuario)
1. Prefab cubo 1×1. 2. Assets: Furniture Definition (Id `CUBE`, footprint 1×1, prefab), Furniture Database (agregar def), Furniture Registry. 3. GameObject con `PlacementGrid`. 4. GameObject con `FurnitureSpawner` + `FurnitureService` (asignar grid/database/registry + `activePiece`=cubo). 5. Play → "Place at Cell".

## Próximos pasos (retomar acá la próxima sesión)

**Furniture — Fase 2: Building mode** (lo que sigue)
- Action map nuevo **"Building"** en el Input Actions (mutuamente excluyente con Player/UI, como ya se hace en `OnUIFocusChanged`).
- Conmutar a `PlayerStateType.Building` al entrar a construcción.
- **Ghost preview**: sigue la celda bajo el cursor (`grid.WorldToCell`), se tiñe verde/rojo según `grid.CanPlace`.
- Flujo: **click** posiciona el ghost → **F** confirma (`FurnitureService.TryPlace`) → **Esc** sale del pre-colocado y vuelve al modo. Para borrar: **click derecho** sobre un mueble lo marca en rojo → **F** confirma eliminación (`TryRemove`).
- Se construye TODO sobre `TryPlace`/`TryRemove` (ya existen como API pública).

**Furniture — Fase 3: economía + tienda**
- `Wallet` (moneda del jugador, persiste) + `ShopService`. Panel UITK listando `FurnitureDefinitionSO` con precio → comprar entra a placement.

**Furniture — persistencia (pendiente transversal)**
- AÚN NO se persiste furniture (ni JSON ni cloud) — fue deliberado para confirmar placement primero. Falta wirear `GameManager.Persist` + `SaveSystem` (archivo JSON propio) para el `FurnitureRegistrySO`, y cloud después.

**MoriMonchis**
- Acordarse de pulsar **Populate Defaults** en `PersonalityProfileTable` (los campos `ConfineToArea` viejos en el .asset se reemplazan por `AreaPreference`).
- Setup de escena Etapa 2.5 sigue pendiente (NavMesh bake + 3 Areas + prefab + wiring spawner).
- Probar en Unity y ajustar números de rebote/knock/throw.

## Archivos en juego en la sesión actual

| Archivo | Por qué |
|---------|---------|
| `Scripts/World/MoriMochiAgent.cs` | Rebote, knock, settle, tint, preferencia, gizmos, Feel hooks |
| `Scripts/Player/PlayerController.cs` | Throw hacia la mira |
| `Scripts/Core/Interfaces.cs` · `Interactables/ThrowableObject.cs` | `IThrowable.Knock` |
| `Scripts/Data/PersonalityProfileSO.cs` | AreaPreference, RecoverySpeed, Tint |
| `Scripts/World/MoriMochiSpawner.cs` | Spawn sesgado a área preferida |
| `Scripts/Data/Furniture*.cs` · `Systems/Furniture/*.cs` | Sistema de muebles Fase 1 |
| `Scripts/Core/Enums.cs` · `GameEvents.cs` | Building state, FurnitureCategory, eventos furniture |

## Cómo usar esta nota en sesiones futuras

Cuando arranque una sesión nueva:
1. Leo este archivo primero (después del `CLAUDE.md`).
2. Borro lo de la sesión pasada y escribo qué estoy haciendo ahora.
3. Listo los 2-4 archivos del vault relevantes para esta sesión (no los leo todos).

Si el `Active Context` queda desactualizado (no se ha tocado en muchos días), tratarlo como **stale** — el código y los archivos del vault son autoritativos.

## Notas / pendientes que el usuario quiere recordar

- Furniture: retomar en **Fase 2 (Building mode)** la próxima sesión (ver arriba).
