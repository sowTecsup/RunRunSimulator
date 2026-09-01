---
tags: [script, world, spawner]
---

# MoriMochiSpawner.cs

**Ruta:** `World/Spawning/MoriMochiSpawner.cs`

**Responsabilidad:** Convierte DATA (CreatureRegistrySO) → PRESENCIA (MoriMonchiController vivo en escena). Singleton. Dispara criaturas como proyectiles (ragdoll mid-aire). **S57:** `PrewarmAndStart()` ensambla modelos mientras inactivos pasando `bank=GameManager.MonchiVisualBank` en Initialize (Assemble completo del modelo Suriyun off-screen). `Acquire()` con controller prewarmed pasa `bank=null` A PROPÓSITO (contrato: saltear re-ensamblado — el controller hace `RefreshLook` y CONSERVA el banco guardado en prewarm); cold spawn del pool pasa `bank=MonchiVisualBank` (Assemble completo). Espera World Ready (primer NavMesh bake + furniture cargada), luego pump activa. **Gate `dataReady`**: no puebla hasta primera carga autoritativa (OnRegistryReloaded o timeout `dataReadyTimeout` = 6s default). Cola prioritaria **`anchoredQueue`** (criaturas con LocationKey): se colocan DIRECTAMENTE en su lugar via `AnchorRegistry.TryGet()` + `place.TryReclaim()` (sin cañonazo). Si el lugar desaparece, cae al cañón y limpia LocationKey. Timeout `anchorPlaceTimeout` → si la place no aparece en tiempo, cannon-fire fallback. Criados lanzan desde punto registrado por `RegisterBirthLaunch()`. `OnRegistryReloaded()` re-vincula DNA/profile en spawned via `controller.Rebind()` (rápido, sin re-ensamblar); re-ancla sueltos tras pull nube. Usa ControllerPool para reutilizar, SpawnBallistics para balística.

## Ciclo de vida (S57)

1. **Awake:** instancia ControllerPool
2. **Start:** lanza PrewarmAndStart (si hay registry)
3. **OnEnable:** suscribe a GameEvents (RegistryChanged, RegistryReloaded, NavMeshRebaked)
4. **PrewarmAndStart (S57):**
   - Itera registry, instancia 1 criatura/frame (inactivo) en prewarmPos
   - Llama `controller.Initialize(dna, table, player, bank=MonchiVisualBank, furDb)` → Assemble completo mientras inactivo
   - Espera resto de startDelay
   - Bloquea en WorldReady (primer NavMesh bake) o navMeshWaitTimeout
   - Llama Sync() y desbloquea pump
5. **SpawnPump:** tickea cada spawnInterval, dequeues anchoredQueue → spawnQueue, despacha SpawnOne()
6. **Acquire (S57):** 
   - Si prewarmed: activa + `Initialize(dna, table, player, bank=null, furDb)` — null intencional: el controller hace RefreshLook y conserva el banco del prewarm (modelo ya armado, no se re-ensambla)
   - Si cold pool: `Initialize(dna, table, player, bank=MonchiVisualBank, furDb)` (Assemble completo)
7. **OnDisable:** limpia coroutines, suscripciones

## Colas de spawn

| Cola | Condición | Ruta |
|------|-----------|------|
| `anchoredQueue` | LocationKey != "" | TryPlaceAtAnchor() (via AnchorRegistry) o cannon fallback |
| `spawnQueue` | LocationKey == "" | Cannon (RandomLandingPoint) |

## Propiedades públicas (Readonly, internal accessores)

**Spawn state:**
- `Registry → CreatureRegistrySO` — fuente de datos (null si GameManager no inicializado)
- `Table → RoleWorldProfileSO` — perfiles comportamiento por Role (S39)
- `Bank → MonchiVisualBankSO` — banco visual Suriyun (S57)
- `FurDb → FurTypeDatabaseSO` — database de pelajes

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

## Gizmos

**OnDrawGizmos (siempre visible):**
- Esfera amarilla: muzzle
- Anillo verde: spawn radius (48 segmentos)
- Línea semi-transparente: muzzle → spawn center

**OnDrawGizmosSelected (cuando seleccionado):**
- 8 arcos simulados (naranja = max elevation, cyan = min elevation)
- Muestra trayectorias reales que el cañón produce (S93: usa SpawnBallistics.DrawSimulatedArc/DrawRing)

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

## Invariantes S93 (rescatados de comentarios)

- **Startup:** prewarm de 1 criatura por frame mientras corre `startDelay`, luego espera `WorldReady` (primer `OnNavMeshRebaked` tras cargar muebles, con debounce) y `DataReady` (reload autoritativo o `dataReadyTimeout`); el cañón no dispara antes.
- **Free spawn:** Toda criatura libre sale como RAGDOLL (velocidad balística resuelta para caer dentro de `spawnRadius`); el agente solo toma control al asentarse. Las ancladas (`LocationKey`) se colocan DIRECTO en su lugar; si el lugar no está registrado se difieren hasta `anchorPlaceTimeout` y recién ahí van por cañón limpiando el anchor huérfano (con `RegistryChanged`).
- **Birth spawn:** Recién nacidos: `RegisterBirthLaunch` (lo llama el corral) fija muzzle y aterrizaje para que la cría salga del corral.
- **Activación:** Activar siempre sobre un punto de NavMesh válido (`ResolveActivationPoint`) antes de `Launch`, nunca al revés (`NavMeshAgent.OnEnable` fuera de malla da error).

## Cambios principales (S57)

**PrewarmAndStart():**
- Antes: `bank = GameManager.Instance.PartVisualBank` (pipeline de partes viejo)
- Ahora: `bank = GameManager.Instance.MonchiVisualBank` → Assemble completo del modelo Suriyun mientras inactivo (el prewarm SIEMPRE ensambla, igual que antes)

**Acquire():**
- Prewarm path (sin cambios de contrato): `Initialize(dna, table, player, bank=null, furDb)` — null intencional = "no re-ensambles, el modelo ya está armado". El controller S57 hace RefreshLook y CONSERVA el banco que el visualizer guardó en el prewarm (bug cazado en Play: la versión inicial hacía SetBank(null) y pisaba el banco → moods/shiny muertos en prewarmed; fix en MoriMonchiController)
- Cold spawn path: `Initialize(dna, table, player, bank=GameManager.MonchiVisualBank, furDb)` → Assemble completo

**Banco visual:**
- Antes: `PartVisualBankSO partVisualBank`
- Ahora: Referencias a `GameManager.MonchiVisualBank` (MonchiVisualBankSO, centralizado)
- OnRegistryReloaded: pasa `GameManager.MonchiVisualBank` a Rebind via `furDb`

**Impacto:** S57 — optimización prewarm: modelo inactivo con RefreshLook liviano; Assemble solo cuando se activa (ahorra memoria/tiempo startup). Banco único MonchiVisualBankSO centralizado.

## Cambios principales (S21-S39)

**S21 (Generalización a AnchorPlace):**
- Renombrado: `breederQueue` → `anchoredQueue`
- Renombrado: `TryPlaceInPen()` → `TryPlaceAtAnchor()`
- Renombrado: `DeferBreeder()` → `DeferAnchored()`
- Consulta genérica: AnchorRegistry en lugar de BreedingContainer directo

**S39 (Role-based profiles):**
- Antes: `GameManager.PersonalityProfiles` → `GetProfile(dna.Personality)`
- Ahora: `GameManager.RoleWorldProfiles` → `GetProfile(dna.Role)`

## Vinculado a

- [[Index/06 - Player & World]], [[Index/10 - Visualization]]

## Conexiones

**Datos:**
- [[CreatureRegistrySO]] — fuente de criaturas
- [[CreatureDNA]] — DNA de cada criatura

**Servicios:**
- [[GameManager]] — registry, databases, role profiles, **MonchiVisualBank (S57)**
- [[GameEvents]] — listeners (NavMeshRebaked, RegistryReloaded, RegistryChanged)
- [[ControllerPool]] — reutilización GameObject

**Componentes spawneados:**
- [[MoriMonchiController]] — fachada (Initialize, Rebind, Launch)
- [[MoriMochiAgent]] — behavior brain
- [[MonchiVisualizer]] — visual assembly (S57)

**Mundo:**
- [[AnchorRegistry]] — búsqueda de IAnchorPlace (pens, store, furniture)
- [[MoriMochiContainer]] — pen/breeding (registra puntos de salida)
- [[SpawnBallistics]] — balística

**Dev:**
- [[SpawnerDevConsole]] — herramientas Odin para testing

**Bases de datos:**
- [[MonchiVisualBankSO]] — banco Suriyun (S57)
- [[FurTypeDatabaseSO]] — tipos pelaje
- [[RoleWorldProfileSO]] — perfiles comportamiento (S39)

## Notas

- **Prewarm (S57):** El prewarm ensambla el modelo Suriyun completo mientras la criatura está inactiva (bank real). Al activarla, Acquire pasa bank=null para que el controller NO re-ensamble (RefreshLook conservando el banco guardado).
- **Cold spawn (S57):** Pasa bank=MonchiVisualBank, Initialize hace Assemble completo. Usado si prewarmed agotado.
- **Rebind (S39+S57):** Via Rebind() con table + furDb; no re-ensambla, solo RefreshLook liviano.
