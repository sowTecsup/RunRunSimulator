---
tags: [script, world]
---

# MoriMochiSpawner.cs

**Ruta:** `World/Spawning/MoriMochiSpawner.cs`

Responsabilidad:** Convierte DATA (CreatureRegistrySO) → PRESENCIA (MoriMonchiController vivo en escena). Singleton. El cañón dispara criaturas como proyectiles (ragdoll mid-air). PrewarmAndStart ensambla modelos mientras inactivos, espera World Ready (primer NavMesh bake + furniture cargada), luego pump activa. **Gate `dataReady`**: no puebla hasta primera carga autoritativa (OnRegistryReloaded o timeout `dataReadyTimeout` = 6s default). Coroutine `DataReadyFallback` permite offline sin esperar sync nube. Cola prioritaria breederQueue → se colocan directamente en corrales via BreedingContainer.ReclaimDirect (sin cañonazo). Criados se lanzan desde el punto registrado por RegisterBirthLaunch (corral de origen). **OnRegistryReloaded**: re-vincula DNA/profile en criaturas spawneadas via `controller.Rebind()` (no re-ensambla, es rápido). Usa ControllerPool para reutilizar controladores y SpawnBallistics para resolver velocidades balísticas.

**Vinculado a:** [[Index/06 - World Architecture]]

**Conexiones:** [[CreatureRegistrySO]], [[GameEvents]], [[MoriMonchiController]], [[MoriMochiAgent]], [[BreedingContainer]], [[ControllerPool]], [[SpawnBallistics]], [[GameManager]], [[PartVisualBankSO]], [[PersonalityProfileSO]]

**Métodos clave:**
- `Despawn(id)`, `ClearAll()`, `RandomLandingPoint()`, `ResolveActivationPoint()` — helpers de spawn
- `RegisterBirthLaunch(childId, worldPoint)` — llamado por breeding pen; punto de lanzamiento del recién nacido
