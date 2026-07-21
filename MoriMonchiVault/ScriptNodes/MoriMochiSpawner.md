---
tags: [script, world, spawner]
---

# MoriMochiSpawner.cs

**Ruta:** `World/Spawning/MoriMochiSpawner.cs`

**Responsabilidad:** Convierte DATA (CreatureRegistrySO) → PRESENCIA (MoriMonchiController vivo en escena). Singleton. Dispara criaturas como proyectiles (ragdoll mid-aire). `PrewarmAndStart()` ensambla modelos mientras inactivos, espera World Ready (primer NavMesh bake + furniture cargada), luego pump activa. **Gate `dataReady`**: no puebla hasta primera carga autoritativa (OnRegistryReloaded o timeout `dataReadyTimeout` = 6s default). Cola prioritaria **`anchoredQueue`** (criaturas con LocationKey): se colocan DIRECTAMENTE en su lugar via `AnchorRegistry.TryGet()` + `place.TryReclaim()` (sin cañonazo). Si el lugar desaparece, cae al cañón y limpia LocationKey. Timeout `anchorPlaceTimeout` → si la place no aparece en tiempo, cannon-fire fallback. Criados lanzan desde punto registrado por `RegisterBirthLaunch()`. `OnRegistryReloaded()` re-vincula DNA/profile en spawned via `controller.Rebind()` (rápido, sin re-ensamblar); re-ancla sueltos tras pull nube. Usa ControllerPool para reutilizar, SpawnBallistics para balística. **S55 resuelto:** Ya NO es partial; gizmos + debug ahora inline.

## Ciclo de vida

1. **Awake:** instancia ControllerPool
2. **Start:** lanza PrewarmAndStart (si hay registry)
3. **OnEnable:** suscribe a GameEvents (RegistryChanged, RegistryReloaded, NavMeshRebaked)
4. **PrewarmAndStart:** 
   - Itera registry, instancia 1 criatura/frame (inactivo) en prewarmPos
   - Espera resto de startDelay
   - Bloquea en WorldReady (primer NavMesh bake) o navMeshWaitTimeout
   - Llama Sync() y desbloquea pump
5. **SpawnPump:** tickea cada spawnInterval, dequeues anchoredQueue → spawnQueue, despacha SpawnOne()
6. **OnDisable:** limpia coroutines, suscripciones

## Colas de spawn

| Cola | Condición | Ruta |
|------|-----------|------|
| `anchoredQueue` | LocationKey != "" | TryPlaceAtAnchor() (via AnchorRegistry) o cannon fallback |
| `spawnQueue` | LocationKey == "" | Cannon (RandomLandingPoint) |

## Métodos Públicos

**Sincronización:**
- `Sync(CreatureRegistrySO registry)` — reconcilia spawned vs registry, enqueues deltas
- `RegisterBirthLaunch(childId, muzzle, landing)` — pen registra punto de salida criado

**Spawner state (read-only, internal accesores para SpawnerDevConsole):**
- `Instance → MoriMochiSpawner` — singleton
- `WorldReady → bool` — gate de mundo (furniture + NavMesh)
- `DataReady → bool` — gate de datos (primera carga autoritativa)
- `SpawnedCount, QueuedCount, PrewarmedCount, PooledCount → int` — contadores
- `CreaturePrefab → MoriMonchiController` — prefab
- `MuzzlePosition → Vector3` — punto de salida cañón
- `LaunchAngleRange → Vector2` — ángulos min/max
- `SpawnedEntries → IEnumerable<KVP>` — iterador de spawned para debug

**Pool lifecycle:**
- `ClearAll()` — limpia todos los spawned, desqueues, poolea

**Helpers:**
- `RandomLandingPoint() → Vector3` — destino landing aleatorio
- `ResolveActivationPoint() → Vector3` — punto NavMesh válido para activación

## Campos Tuning (inspector)

**Prefab:**
- `creaturePrefab` (MoriMonchiController) — prefab a instanciar

**Cannon:**
- `launchPoint` (Transform) — muzzle (si null, use transform.position)
- `spawnArea` (Transform) — centro landing zone (si null, use transform.position)
- `spawnRadius` (float) — radio landing zone

**Elevación:**
- `launchAngle` (Vector2 minMaxSlider) — degrees para arco balístico

**Pooling / cadence:**
- `startDelay` (float, 2-15s) — ventana prewarm + espera inicial
- `spawnInterval` (float) — segundos entre ticks del pump
- `spawnPerTick` (int) — cuántas criaturas por tick
- `navMeshWaitTimeout` (float) — fallback si no hay NavMeshSurface
- `dataReadyTimeout` (float) — fallback si no llega carga de nube
- `anchorPlaceTimeout` (float) — timeout criatura anchorada esperando su place

**Status (read-only):**
- `WorldReady`, `DataReady`, `SpawnedCount`, `PooledCount`, `QueuedCount`, `PrewarmedCount`

## Gizmos (S55)

**OnDrawGizmos (siempre visible):**
- Esfera amarilla: muzzle
- Anillo verde: spawn radius (48 segmentos)
- Línea semi-transparente: muzzle → spawn center

**OnDrawGizmosSelected (cuando seleccionado):**
- 8 arcos simulados (naranja = max elevation, cyan = min elevation)
- Muestra trayectorias reales que el cañón produce

## State internals

| Variable | Tipo | Responsabilidad |
|----------|------|-----------------|
| `spawned` | Dictionary<id, controller> | criaturas vivas en escena |
| `prewarmed` | Dictionary<id, controller> | pre-ensambladas (pending activation) |
| `controllerPool` | ControllerPool | reutilización de GameObjects |
| `spawnQueue` | Queue<DNA> | criaturas libres (cañón) |
| `anchoredQueue` | Queue<DNA> | criaturas con lugar (directo placement) |
| `queued` | HashSet<id> | membresía rápida |
| `birthLaunchPoints` | Dictionary<id, Vector3> | puntos de salida criados |
| `birthLandingPoints` | Dictionary<id, Vector3> | puntos de aterrizaje criados |
| `anchorPlaceDeadline` | Dictionary<id, float> | deadline timeout por criatura |
| `pump` | Coroutine | SpawnPump() en vivo |
| `prewarmRoutine` | Coroutine | PrewarmAndStart() en vivo |
| `isPrewarming` | bool | bandera de startup |
| `worldReady` | bool | gate: furniture + NavMesh |
| `dataReady` | bool | gate: primera carga autoritativa |
| `player` | Transform | ref al jugador (resuelto en Start) |

## Cambios principales (S21-S55)

**S21 (Generalización a AnchorPlace):**
- Renombrado: `breederQueue` → `anchoredQueue`
- Renombrado: `TryPlaceInPen()` → `TryPlaceAtAnchor()`
- Renombrado: `DeferBreeder()` → `DeferAnchored()`
- Consulta genérica: AnchorRegistry en lugar de BreedingContainer directo

**S39 (Role-based profiles):**
- Antes: `GameManager.PersonalityProfiles` → `GetProfile(dna.Personality)`
- Ahora: `GameManager.RoleWorldProfiles` → `GetProfile(dna.Role)`

**S55 (Composición/gizmos):**
- Antes: partial MoriMochiSpawner.Debug.cs (gizmos + debug buttons)
- Ahora: gizmos inline (OnDrawGizmos, OnDrawGizmosSelected)
- Debug buttons: delegados a SpawnerDevConsole (componente separado)
- Accesores internos públicos para SpawnerDevConsole: CreaturePrefab, MuzzlePosition, LaunchAngleRange, SpawnedEntries

## Vinculado a

- [[Index/06 - Player & World]]

## Conexiones

**Datos:**
- [[CreatureRegistrySO]] — fuente de criaturas
- [[CreatureDNA]] — DNA de cada criatura

**Servicios:**
- [[GameManager]] — registry, databases, role profiles
- [[GameEvents]] — listeners (NavMeshRebaked, RegistryReloaded, RegistryChanged)
- [[ControllerPool]] — reutilización GameObject

**Componentes spawneados:**
- [[MoriMonchiController]] — fachada (Initialize, Rebind, Launch)
- [[MoriMochiAgent]] — behavior brain
- [[MoriMonchiVisualizer]] — visual assembly

**Mundo:**
- [[AnchorRegistry]] — búsqueda de IAnchorPlace (pens, store, furniture)
- [[MoriMochiContainer]] — pen/breeding (registra puntos de salida)
- [[SpawnBallistics]] — balística

**Dev:**
- [[SpawnerDevConsole]] — herramientas Odin para testing

**Bases de datos:**
- [[PartVisualBankSO]] — partes 3D
- [[FurTypeDatabaseSO]] — tipos pelaje
- [[RoleWorldProfileSO]] — perfiles comportamiento (S39)
