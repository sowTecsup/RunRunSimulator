---
tags: [script, world]
---

# MoriMochiSpawner.cs

**Ruta:** `World/Spawning/MoriMochiSpawner.cs`

## Responsabilidad

Convierte DATA (CreatureRegistrySO) → PRESENCIA (MoriMonchiController vivo en escena). Singleton. El cañón dispara criaturas como proyectiles (ragdoll mid-air). PrewarmAndStart ensambla modelos mientras inactivos, espera World Ready (primer NavMesh bake + furniture cargada), luego pump activa. **Gate `dataReady`**: no puebla hasta primera carga autoritativa (OnRegistryReloaded o timeout `dataReadyTimeout` = 6s default). Coroutine `DataReadyFallback` permite offline sin esperar sync nube.

Cola prioritaria **`anchoredQueue`** (antes `breederQueue`): criaturas con `LocationKey != ""` se colocan DIRECTAMENTE en su lugar via `AnchorRegistry.TryGet()` + `place.TryReclaim()` (sin cañonazo). Routan por `TryPlaceAtAnchor()` (antes `TryPlaceInPen()`). Si el lugar desapareció, cae al cañón y limpia `LocationKey`. Timeout `anchorPlaceTimeout` → si el lugar no aparece en tiempo, cannon-fire como fallback.

Criados se lanzan desde el punto registrado por RegisterBirthLaunch (corral de origen). **OnRegistryReloaded**: re-vincula DNA/profile en criaturas spawneadas via `controller.Rebind()` (no re-ensambla, es rápido); re-ancla criaturas sueltas tras pull de nube. Usa ControllerPool para reutilizar controladores y SpawnBallistics para resolver velocidades balísticas.

## Cambios en S21

- Renombrado: `breederQueue` → `anchoredQueue` (aplica a cualquier criatura con LocationKey, no solo breeding).
- Renombrado: `TryPlaceInPen()` → `TryPlaceAtAnchor()` (genérico, consulta AnchorRegistry).
- Renombrado: `DeferBreeder()` → `DeferAnchored()`.
- Borrado: llamadas directas a `BreedingContainer.TryGet()`/`ReclaimDirect()` → ahora todo es via `AnchorRegistry`.
- Nuevo: `anchorPlaceDeadline` (diccionario de deadlines de timeout por criaturaID). Si vence, re-queue + cannon-fire.
- Nuevo: `OnRegistryReloaded` llama `Enqueue(dna)` sin revisar si estaba spawned (el pump rechaza si ya existe).

**Vinculado a:** [[Index/06 - World Architecture]]

**Conexiones:** [[CreatureRegistrySO]], [[GameEvents]], [[MoriMonchiController]], [[MoriMochiAgent]], [[BreedingContainer]], [[ControllerPool]], [[SpawnBallistics]], [[GameManager]], [[PartVisualBankSO]], [[PersonalityProfileSO]]

**Métodos clave:**
- `Despawn(id)`, `ClearAll()`, `RandomLandingPoint()`, `ResolveActivationPoint()` — helpers de spawn
- `RegisterBirthLaunch(childId, worldPoint)` — llamado por breeding pen; punto de lanzamiento del recién nacido
