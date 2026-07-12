---
tags: [script, world, spawner]
---

# MoriMochiSpawner

**Ruta:** `World/Spawning/MoriMochiSpawner.cs` (partial class)

## Responsabilidad

Convierte DATA (CreatureRegistrySO) → PRESENCIA (MoriMonchiController vivo en escena). Singleton. El cañón dispara criaturas como proyectiles (ragdoll mid-air). PrewarmAndStart ensambla modelos mientras inactivos, espera World Ready (primer NavMesh bake + furniture cargada), luego pump activa. **Gate `dataReady`**: no puebla hasta primera carga autoritativa (OnRegistryReloaded o timeout `dataReadyTimeout` = 6s default). Coroutine `DataReadyFallback` permite offline sin esperar sync nube.

Cola prioritaria **`anchoredQueue`** (antes `breederQueue`): criaturas con `LocationKey != ""` se colocan DIRECTAMENTE en su lugar via `AnchorRegistry.TryGet()` + `place.TryReclaim()` (sin cañonazo). Routan por `TryPlaceAtAnchor()` (antes `TryPlaceInPen()`). Si el lugar desapareció, cae al cañón y limpia `LocationKey`. Timeout `anchorPlaceTimeout` → si el lugar no aparece en tiempo, cannon-fire como fallback.

Criados se lanzan desde el punto registrado por RegisterBirthLaunch (corral de origen). **OnRegistryReloaded**: re-vincula DNA/profile en criaturas spawneadas via `controller.Rebind()` (no re-ensambla, es rápido); re-ancla criaturas sueltas tras pull de nube. Usa ControllerPool para reutilizar controladores y SpawnBallistics para resolver velocidades balísticas.

**S39 cambio:** Consume `GameManager.RoleWorldProfiles` (antes `PersonalityProfiles`).

## Cambios en S21

- Renombrado: `breederQueue` → `anchoredQueue` (aplica a cualquier criatura con LocationKey, no solo breeding).
- Renombrado: `TryPlaceInPen()` → `TryPlaceAtAnchor()` (genérico, consulta AnchorRegistry).
- Renombrado: `DeferBreeder()` → `DeferAnchored()`.
- Borrado: llamadas directas a `BreedingContainer.TryGet()`/`ReclaimDirect()` → ahora todo es via `AnchorRegistry`.
- Nuevo: `anchorPlaceDeadline` (diccionario de deadlines de timeout por criaturaID).
- Nuevo: `OnRegistryReloaded` llama `Enqueue(dna)` sin revisar si estaba spawned (el pump rechaza si ya existe).

## Cambios S39

**Resolución de profile:**
- Antes: `GameManager.PersonalityProfiles` → `GetProfile(dna.Personality)`
- Ahora: `GameManager.RoleWorldProfiles` → `GetProfile(dna.Role)`

**En Initialize (MoriMonchiController):**
```csharp
controller.Initialize(dna, gameManager.RoleWorldProfiles, player, bank, furDb);
```

**En Rebind (OnRegistryReloaded):**
```csharp
controller.Rebind(dna, gameManager.RoleWorldProfiles, furDb);
```

## Vinculado a

- [[Index/06 - Player & World]]
- [[CreatureRegistrySO]] — fuente de criaturas
- [[GameEvents]] — listeners NavMesh, RegistryReloaded, BreedingCompleted
- [[MoriMonchiController]] — controlador (Initialize/Rebind)
- [[MoriMochiAgent]] — brain (inyectado via controller)
- [[MoriMonchiVisualizer]] — assembly visual (inyectado via controller)
- [[BreedingContainer]] — pide RegisterBirthLaunch
- [[ControllerPool]] — reutilización de controladores
- [[SpawnBallistics]] — resuelve velocidades
- [[GameManager]] — acceso a registry, database, `RoleWorldProfiles` (S39)
- [[PartVisualBankSO]] — partes visuales
- [[FurTypeDatabaseSO]] — tipos pelaje
- [[RoleWorldProfileSO]] — tabla de perfiles (S39, antes PersonalityProfileSO)

## Conexiones

**Entrada:**
- `GameManager.Instance` resuelve en Awake
- `GameEvents.OnNavMeshRebaked` → WorldReady gate
- `GameEvents.OnRegistryReloaded` → re-vinculación (Rebind)
- `GameEvents.OnBreedingCompleted` → criados en cola

**Salida:**
- MoriMonchiController instancias en escena
- Creaciones / despawns via ControllerPool

## Métodos clave

- `Despawn(id)` — desactiva + poolea
- `ClearAll()` — limpia todos los spawned
- `RandomLandingPoint()` — punto landing aleatorio
- `ResolveActivationPoint()` — determina si cannon vs anchor
- `RegisterBirthLaunch(childId, launchPoint, landingPoint)` — llamado por breeding pen

## Notas (S21 + S39)

- **S21 refactor:** Generalización de anchored containers (no solo breeding).
- **S39 cambio:** `RoleWorldProfiles` reemplaza `PersonalityProfiles`. Perfiles ahora data-driven vía Role (S37/S39).
- **Backward compat:** Si profile tabla == null, fallback a perfiles neutrales.
- **Pool optimization:** ControllerPool reutiliza GameObjects; Rebind es rápido.
