---
tags: [index, core]
---

# 11 - Technical Debt & Refactor Roadmap

> Auditoría 2026-06-19 (Sesión de mantenimiento). 97 scripts, ~15.000 líneas.
> Esta nota es la hoja de ruta viva de saneamiento. Las fases están priorizadas por (impacto × leverage ÷ riesgo).

---

## 🗺️ Mapa por secciones (capas de la arquitectura)

| Capa | Carpetas | Responsabilidad | Salud |
|------|----------|-----------------|-------|
| **Data (estado puro)** | `Data/` (24), `Data/Parts`, `Data/Databases` | DNA, partes, SOs, registros. Sin orquestación. | 🟢 Sana |
| **Core (servicios + bus)** | `Core/` (7) | GameManager, GameEvents, SaveSystem, Enums, Interfaces, ColorGenetics | 🟡 GameManager monolítico |
| **Systems (orquestación)** | `Systems/Combat`, `Breeding`, `Cloud`, `Furniture`, `Store` | Lógica de dominio, red, dueños de persistencia | 🟡 Controllers mezclan debug/UI |
| **World (representación 3D)** | `World/` (17) | AI, spawn, contenedores, needs, nametags | 🔴 MoriMochiAgent monstruoso |
| **UI (representación 2D)** | `UI/` (14) | Paneles UITK | 🔴 Paneles gigantes |
| **Player / Input** | `Player/` (4) | FP controller, action maps | 🟢 Sana |
| **Interactables** | `Interactables/` (2) | IInteractable triggers | 🟢 Sana |

### Hotspots medidos (archivos > 450 líneas = parten 2+ dominios)
| Líneas | Archivo | Dominios mezclados |
|--------|---------|--------------------|
| **1189** | `World/MoriMochiAgent.cs` | FSM + ragdoll/física + NavMesh-rebake + needs + reacción-jugador + confinamiento/cortejo + carry/throw |
| **849** | `UI/CombatPanelUITK.cs` | datos + binding + animación |
| **637** | `UI/BreedingPanelUITK.cs` | datos + binding + selección |
| **622** | `World/MoriMochiSpawner.cs` | pool + colas + colocación en corral |
| **568** | `Systems/Cloud/CloudSyncService.cs` | pull/push/reset + reconciliación |
| **478** | `UI/MorimonchiDetailInfoUITK.cs` | datos + binding |
| **468** | `World/BreedingContainer.cs` | corral + cortejo + nacimiento + server |
| **462** | `Systems/Combat/AsyncCombatService.cs` | endpoints + reconciliación |

---

## 🧭 Regla de arquitectura general (la regla de oro técnica)

> **Una responsabilidad por archivo, una dirección de comunicación, un dueño por dato.**

1. **Capas, sin saltos de dos niveles**: `Data` (estado) → `Systems/Core` (orquestación, dueños de persistencia y red) → `World/UI` (representación). La representación LEE estado y reacciona a eventos; **nunca** persiste ni toca la nube directamente.
2. **Comunicación cruzada solo por bus o servicio explícito**: `GameEvents` (gameplay), eventos `static` de `UIManager` (UI), eventos de Inputs. Un consumidor **nunca** hace `Find*`/`GetComponentInParent` para localizar otro sistema. El evento transporta la data.
3. **Límite de tamaño/dominio**: si un archivo supera ~400 líneas **o** mezcla 2+ dominios (datos, presentación, física, red), se parte en colaboradores con una sola responsabilidad cada uno.
4. **Singleton = servicio runtime; SO = data**. Un servicio de runtime puede ser singleton (`GameManager.Instance`). Un ScriptableObject expone su instancia activa **de una sola forma elegida** (ver Fase 4). No mezclar ambos criterios.

Esta regla resume y subordina las 10 reglas de código de `CLAUDE.md`; cuando una decisión no esté cubierta por las 10, se aplica ésta.

---

## 🛠️ Hoja de ruta (fases)

### Fase 0 — Higiene barata 🟢 riesgo bajo
- [x] ~~Eliminar FirstPerson/ThirdPersonController~~ (ya no existen — nota previa estaba stale).
- [x] **Auditoría de código muerto (2026-06-19): SIN código muerto.** `CombatManagerSO` está vivo (GameManager, AsyncCombatService, CombatController, CombatService, CombatPanelUITK). `CloudCodeTester` es herramienta dev legítima (botones Odin, 0 refs porque se invoca desde el Inspector). No hay nada que borrar.
- [x] **Regla de arquitectura general codificada en `CLAUDE.md`** (sección propia antes de las 10 reglas).
- [x] **Namespacing ✅ HECHO (2026-06-20, Sesión 13):** raíz único `MoriMonchiSimulator`, block-scoped (Unity 6.3 = C# 9, file-scoped no disponible), aplicado a los 116 `.cs` por script determinista (CRLF + BOM preservados; sin re-indentar → diff de +3 líneas/archivo). Cambio atómico hecho con Unity cerrado. **Auditoría de serialización previa:** 0 `SerializeReference`/`TypeNameHandling`/binders → saves locales/cloud/DNA string (Newtonsoft por nombre, tipos genéricos) y Odin (Parts = SO por GUID, dicts con claves enum/concretas) son namespace-safe. Validado: 1 namespace/archivo, llaves balanceadas. Pendiente solo: Juan recompila en Unity.
- [x] ~~Estandarizar `#region`~~ — **moot:** medido = 0 ocurrencias de `#region` en todo el código. Nada que estandarizar.
- [x] **Organizar `Scripts/` ✅ HECHO (Sesión 13):** `World/` (24) → `AI`/`Spawning`/`Creatures`/`Containers`/`Needs`/`Props`. Top-level ya estaba limpio (Core/Data/Systems/World/UI/Player/Interactables). 47 `.cs` movidos con su `.meta` (GUID intacto).
- [x] **Organizar SO ✅ HECHO (Sesión 13):** `Data/` (24 sueltos) → por dominio espejando assets + carpetas de `Systems/`: `Databases`/`Genetics`/`Breeding`/`Combat`/`Furniture`/`Items`/`Player`; `NeedsState` en raíz.

### Fase 1 — Partir `MoriMochiAgent` (1189) ✅ HECHO (2026-06-19)
**Corrección de enfoque vs. plan original:** extraer MonoBehaviours colaboradores habría EMPEORADO el acoplamiento — el FSM comparte un núcleo mutable único (`state`, `NavMeshAgent`, `Rigidbody`, timers) que todos los dominios leen/escriben; separarlos exigiría exponer ese estado como público. **Se usó `partial class`**: un solo componente (misma serialización, mismo prefab, cero estado público nuevo), código repartido por concern. Quirk de arquitectura: para un FSM cohesivo, "partir en colaboradores" = `partial class`, no componentes separados.
- `MoriMochiAgent.cs` (~243) — núcleo: campos, lifecycle, dispatch, helpers NavMesh compartidos, gizmos.
- `MoriMochiAgent.Tuning.cs` (~201) — todos los `[SerializeField]` Odin + readouts + dev buttons.
- `MoriMochiAgent.Brain.cs` (~358) — estados + needs + reacciones + intent + queries.
- `MoriMochiAgent.Physics.cs` (~274) — colisión/knock/throw/ragdoll/recovery/handoff.
- `MoriMochiAgent.Confinement.cs` (~136) — pen + courtship + supervivencia a rebake + pooling.
- Verificación: diff de contenido contra el original = exacto (857 líneas idénticas), llaves balanceadas por archivo, sin cambio de comportamiento ni de API pública.

### Fase 2 — Partir paneles UI gigantes ✅ HECHO (2026-06-19)
Mismo veredicto que F1: los paneles comparten estado UI mutable (refs de elementos, listas de cards, índices, `region`) → `partial class`, no componentes. Cortados por concern contiguo:
- `CombatPanelUITK` (849) → `.cs` (241, núcleo/lifecycle/wiring/data) + `.Tabs.cs` (390, contenido de 4 pestañas) + `.Navigation.cs` (234, IUINavigable+foco). Verificado exacto (563 líneas), llaves balanceadas.
- `BreedingPanelUITK` (637) → `.cs` (170) + `.Content.cs` (273, candidatos/huevos/preview/breed/hatch) + `.Navigation.cs` (210). Verificado exacto (418 líneas), llaves balanceadas.
- Patrón establecido para UITK: **Core (lifecycle+wiring+data) / Content (build+bind) / Navigation (IUINavigable+foco)**.
- `MorimonchiDetailInfoUITK` (478) → `.cs` (289, núcleo+Info+Combat) + `.Trees.cs` (196, tabs Linaje/Descendencia). Verificado exacto (327 líneas). **Todos los paneles UI >400 líneas partidos.**

### Fase 2.5 — Otros hotspots World ✅ HECHO (2026-06-19)
- `MoriMochiSpawner` (622) → `.cs` (398, motor: prewarm/sync/pump/spawn) + `.Pool.cs` (62) + `.Ballistics.cs` (66, solve velocity/landing) + `.Debug.cs` (123, dev buttons + gizmos). Mismo patrón partial-class. Verificado exacto (404 líneas), llaves balanceadas.

### Fase 3 — Slim Core/Systems ✅ HECHO (2026-06-20)
**Enfoque (corregido vs. F1/F2): COMPONENTES independientes, NO partial class.** El tooling dev no comparte estado mutable irreducible con el dominio — solo lee el registry (público vía GameManager) y llama API pública. Es el caso exacto donde la regla pide clase aparte. Conexiones por **referencia serializada explícita** (sin static nuevo, sin acceso a SO por `.Current` en las consolas): los datos salen de `GameManager` (su dueño), las acciones del controller.
- `GameManager` (318 → 133 núcleo) → creados `GeneticsLabPreview` (preview de editor, estado aislado) + `DevToolsConsole` (dabloons/clear; ahora persiste por `GameEvents.InventoryChanged`, sin `SaveSystem` directo).
- `BreedingController` (281 → 108, solo dominio) → creado `BreedingDevConsole`. `BreedCreatures` ahora devuelve el ID del hijo. `Instance` se queda (lo usan corrales/NameTag).
- `CombatController` (281 → 77, solo dominio + API) → creado `CombatDevConsole`. Se añadió `Config`, `SimulateLocal`, wrappers async; `EnqueueForAsyncCombat` pasó a `Task`. NO se agregó `Instance` (nadie lo referenciaba).
- PASO MANUAL ✅ HECHO (Juan): los 4 componentes dev agregados con sus refs serializadas. Confirmado en Play.

### Disciplina de partial class — auditoría (2026-06-20)
Regla 11 codificada en `CLAUDE.md`: **partial solo por ventaja física de archivo (Git / código generado); el remedio al tamaño es dividir en clases/componentes independientes; tooling dev y código puro nunca en partial; núcleo con estado mutable irreducible puede quedar partial SOLO documentado aquí como deuda.** Auditoría de los partials creados en F1/F2/F2.5/F5:
**Test aplicado:** una partial es UNA clase para el compilador → "arreglarla" solo aporta si el código puede volverse una **unidad genuinamente independiente** (estado propio o sin estado). Si no, arreglar = exponer estado privado (peor) o fold cosmético (cero ganancia).
- ✅ **Convertidos a unidad independiente:**
  - `MoriMochiSpawner.Ballistics.cs` → `static class SpawnBallistics` (`SolveLaunchVelocity`, `ResolvePlayer` — puros). `RandomLandingPoint`/`ResolveActivationPoint` (leen estado del spawner) movidos al núcleo.
  - `MoriMochiSpawner.Pool.cs` → `class ControllerPool` (dueña de su propia `Queue`; el spawner le pide `Get`/`Return`). `Despawn`/`ClearAll` (spawn-tracking, no pooling) movidos al núcleo.
- ⚠️ **Excepción pragmática justificada (se quedan partial, deuda documentada):**
  - `MoriMochiAgent` (.Brain/.Physics/.Confinement/.Tuning): FSM MonoBehaviour con núcleo mutable irreducible. **Evidencia (auditoría 2026-06-20):** la variable `state` la escriben los TRES concerns; comparten `agent`/`rb`/`col`/`dna`/`profile`/`player`/`holdAnchor`/masks/`currentContainer`/`reservedStation`/`rebakeInProgress` + ~10 timers reseteados juntos en `RestoreNavMeshControl`. Las llamadas cruzan concerns libremente (Physics→`EnterRoaming`/`ReleaseStation`; Confinement→`DetachToPhysics`/`RejoinNavMesh`+`EnterRoaming`; Brain lee `currentContainer` en 5 sitios). No hay costura limpia: los archivos son vistas sobre un mismo cuerpo. Un blackboard/State-pattern haría públicos ~30 campos mutables y dejaría las clases mutuamente dependientes → **relocaliza el acoplamiento, no lo reduce; rompe encapsulamiento y "un dueño por dato" (principios 1-2) + abstracción innecesaria (principio 8/regla 8)**. `.Tuning` son `[SerializeField]` del componente (mover a config object resetearía valores del prefab) + readouts + 2 botones dev atados al FSM. **Decisión: NO decomponer; partial es la forma menos mala.**
  - Paneles UITK (`CombatPanelUITK`, `BreedingPanelUITK`, `MorimonchiDetailInfoUITK`): comparten el árbol de `VisualElement` mutable (refs de elementos, listas de cards, índices).
  - `CloudSyncService` (.Auth/.Sync): comparten estado de sesión de red (`isSignedIn`, `playerID`, `registry`, meta). Sesión cohesiva.
  - `MoriMochiSpawner.Debug.cs`: botones dev atados a la config privada del cañón + `debugShots`; gizmos = visualización propia del componente (convención Unity). Extraer = exponer internals.
- 🔮 **Backlog opcional** (revisitar solo si crecen): tabs de paneles UITK → sub-presenters por pestaña; decomponer FSM de `MoriMochiAgent` vía objeto-blackboard (tarea dedicada con testing en Play). *(→ promovido a Fases 7-8 por la decisión S32, abajo.)*

### 🔄 DECISIÓN S32 (2026-07-02) — la dirección pasa a COMPOSICIÓN; la excepción pragmática se cierra

Juan fijó la dirección definitiva: un script grande se divide en **mini-managers/colaboradores con estado propio** que COMPONEN el script (núcleo delgado coordinador), NO en partial classes. La "excepción pragmática" de arriba deja de ser un estado aceptable: los partials existentes pasan a **deuda activa** con hoja de ruta (Fases 6-9). Regla 11 de `CLAUDE.md` reescrita en consecuencia.

**Patrón canónico aplicado (S32, Systems/Combat):** `CombatService` (513→366 líneas) quedó como orquestador delgado del loop; sus piezas salieron a unidades independientes: `CombatRng` (RNG determinista inyectable), `Combatant`+`ActiveEffect` (modelo runtime), `CombatResolver` (ICombatContext de procs + anti-permastun + stacking), `CombatStats` (stats base+partes; `EffectiveStats` promovido a struct top-level en Data/Combat), `CombatEvolution` (evolución de tiers, dedup con AsyncCombatService). CombatService no era partial — era monolito interno; el patrón de descomposición es el mismo para los partials de abajo.

### Fase 6 — Descomponer `CloudSyncService` (.Auth/.Sync) ✅ HECHO (2026-07-20, S53)
`CloudAuth` (clase plana, dueña única de identidad: `IsSignedIn`/`PlayerID`/`PlayerName`/`AuthMethod`/`ServerOffset`; status por callback `Action<string>`, sign-in completo por `Func<string, Task>`) + `CloudSyncOps` (clase plana, dueña de sync/meta: keys, `SyncMeta`, validate/push/pull/reset/notify; lee identidad, nunca la muta) + núcleo MonoBehaviour de 148 líneas (compone en `Start`, secuencia post-sign-in, fachada pública intacta — `GameManager` sin cambios; `newNameInput` único serializado, quedó en el núcleo). Partials `.Auth.cs`/`.Sync.cs` eliminadas. Verificado en Play por MCP: sign-in resume, pull on login, push+validate (Security OK), flush on quit, y `FetchQueuedIdsAsync` del combate async contra servidor OK (el riesgo auth→async señalado aquí).

### Fase 7 — Paneles UITK → sub-presenters por pestaña ✅ HECHO (S53 piloto + S54 completa, 2026-07-20)
Cada tab = clase propia con sus refs y su foco interno; el panel core compone y coordina. **Piloto `CombatPanelUITK` ✅**: interfaz `ICombatTabPresenter` (`Enter/Navigate/Submit/Cancel/ClearFocus/Rebuild/Teardown`) + 3 presenters (`CombatOnlineTabPresenter`, `CombatResultsTabPresenter` con `Tick()`, `CombatHistoryTabPresenter`); núcleo con `IUINavigable` reducido a `TabBar ⇄ Content` — los sub-estados de foco viven en su presenter, que retorna `false` en `Navigate`/`Cancel` para salir a la tab bar. Presenters hacen sus `Q<>` sobre el root y reciben registry por `Func<>` (no cachean). Partials `.Tabs.cs`/`.Navigation.cs` eliminadas; tab Equipo 3v3 sigue en el sibling `CombatLineupUITK`. Verificado en Play por MCP (contenido + navegación por teclado, paridad). **S54 completó la fase**: la interfaz se generalizó a `ITabPresenter` (renombre, GUID preservado); `BreedingPanelUITK` → `BreedingBreedTabPresenter` (con `Busy` fuera de la interfaz — el núcleo congela input global durante un breed en vuelo) + `BreedingEggsTabPresenter` (con `Tick()`), núcleo 216 líneas; `MorimonchiDetailInfoUITK` → 4 colaboradores planos `Rebuild(dna)` SIN `ITabPresenter` (el panel no tiene navegación interna — implementarla habrían sido métodos muertos, regla 8): `DetailInfoTabPresenter`/`DetailCombatTabPresenter`/`DetailTreesPresenter` (Linaje+Descendencia, un dominio)/`DetailEquipTabPresenter`, núcleo 173 líneas (era 665+199). Ambos verificados en Play por MCP. Regla consolidada: `ITabPresenter` para tabs con foco jerárquico; panel de solo-contenido = colaboradores planos.

### Fase 8 — Descomponer `MoriMochiAgent` (FSM) ✅ HECHO (2026-07-21, S55) — el jefe final cayó
`AgentContext` (clase plana, blackboard: dueño único del estado compartido `State`/`Dna`/`Profile`/`Player`/componentes/`CurrentContainer`/masks/`HoldAnchor`/`RebakeInProgress` + helpers compartidos `SetStopped`/`SetColliderTrigger`/`SetDestinationSafe`/`PlanarDistanceToPlayer`/`IsNavMeshControlled`/`IsMoving`/`IsBreeding`/`RandomPointInBounds`) + 3 mini-managers con estado propio: `AgentBrain` (estados de conducta + needs + reacciones + intent + pet; timers propios), `AgentPhysics` (throw/knock/ragdoll/recovery/handoff NavMesh⇄Rigidbody; `lastVelocity`/`settleTimer`/`getUp*` propios), `AgentConfinement` (pen + cortejo COMPLETO — ticks incluidos, movidos de Brain — + supervivencia a rebake; campos court propios). Núcleo `MoriMochiAgent` (MonoBehaviour, ya NO partial): TODOS los `[SerializeField]` de `.Tuning` absorbidos (pasaron `private`→`internal`, MISMOS nombres → prefab preservó valores), lifecycle, dispatch del Update (orden exacto preservado), fachada pública INTACTA (cero cambios en Spawner/NameTag/containers/escena) y **switchboard interno** (`RequestRoam`/`RequestReleaseStation`/`RequestEnterRagdoll`/`RequestDetachToPhysics`/`RequestRejoinNavMesh`/`RequestReleaseFromPen`): ningún manager llama a otro directamente. Los 4 partials (`.Brain`/`.Physics`/`.Confinement`/`.Tuning`) eliminados. Verificado en Play por MCP (0 errores): pipeline cañón completo, ragdoll forzado→recovery, grab/carried/throw con penalidad de Affect, needs decay + `Condition` + fallback sin estación, rebake 9/9 ragdoll→9/9 recuperadas, RespawnAll con reuso de pool 9/9 on-mesh. NO ejercitado en vivo: confinamiento/cortejo (la GameScene actual no tiene corrales colocados) y pet (requiere posicionamiento físico del jugador).

### Fase 9 — `MoriMochiSpawner.Debug` ✅ HECHO (2026-07-20, S55)
Botones dev extraídos a `SpawnerDevConsole` (componente aparte, patrón F3: ref serializada al spawner + accesores `internal` mínimos — `CreaturePrefab`/`MuzzlePosition`/`LaunchAngleRange`/`SpawnedEntries` + counts/`Sync`/`ClearAll`/`RandomLandingPoint` promovidos a `internal`); `debugShots` ahora es estado del console. Gizmos quedaron en el núcleo (visualización propia del componente, auditoría 2026-06-20). Partial `.Debug.cs` eliminada; el spawner ya no es partial. Componente agregado y wireado en GameScene. Verificado en Play por MCP: los 4 botones (RespawnAll/FireDebugShot/ClearDebugShots/DumpSpawnState) funcionando, 0 errores.

> 🏁 **DEUDA DE PARTIALS CERRADA (S53-S55)**: no queda ningún `partial class` en el código del juego. El experimento spider (S48-S52) fue eliminado por completo en S55 (13 scripts, prefab, escena `MorimonchiNewModel`, assets y materiales; `MonchiAnimationDriver` y la regla 60/30/10 sobreviven).

### Fase 4 — Unificar acceso a SO ✅ HECHO (2026-06-20)
**Decisión de Juan: cascada de responsabilidad.** Cada dominio tiene un apex (controller) DUEÑO de las refs de su dominio; los hijos las piden al apex vía su singleton de servicio runtime `.Instance` (servicio runtime = permitido; el `static Current` de los SO se ELIMINA — perdía trazabilidad de dueño). Apexes cuelgan de GameManager. Eliminados los 5 `static Current`:
- `CombatManagerSO` → dueño **CombatController** (campo serializado `config`; gana `static Instance`). Consumidores (AsyncCombatService, CombatPanelUITK) → `CombatController.Instance.Config`.
- `InheritanceOddsTableSO` → dueño **BreedingController** (serializado; getter `InheritanceOdds`). Consumidores (AsyncBreedingService, BreedingPanelUITK) → `BreedingController.Instance.InheritanceOdds`. Removido también de GameManager.
- `BreedingAffinityTableSO` → ya era serializado en BreedingController; quitado el fallback `.Current`.
- `FurTypeDatabaseSO` → dueño **GameManager** (raíz); ruteado por la cascada `spawner→controller.Initialize(furDb)→visualizer.SetFurDatabase`. El visualizer ya no usa `.Current`.
- `PersonalityProfileSO` → se queda en GameManager; ya llegaba al agent por `Initialize`; quitado el fallback `.Current`.
- `CreatureLifeStageTableSO` ya seguía esta convención (serializado en BreedingController).
- PASO MANUAL ✅ HECHO (Juan): `config`, `inheritanceOddsTable`, `furTypeDatabase` asignados. Confirmado funcionando en Play.

### Fase 5 — Red y reconciliación 🟡 (parcial)
- `CloudSyncService` (568) ✅ partido → `.cs` (133, núcleo+meta) + `.Auth.cs` (246, auth+init+cuenta) + `.Sync.cs` (218, validate+reset+push+pull). Verificado exacto (353 líneas), llaves balanceadas.
- ✅ HECHO (2026-06-20): patrón `CallEndpointAsync<string>` + `Deserialize<T>` deduplicado en `CloudEndpoint` (static, `CallAsync`/`CallAsync<T>`). Lo usan `AsyncCombatService` y `AsyncBreedingService`; la reconciliación queda per-servicio (es distinta). Dedup delgado pero centraliza la llamada+parse.

---

### ✅ RESUELTO — Orden de carga de data + colocación de breeders en frío (Sesión 16 → confirmado por Juan 2026-06-23)

**Estado:** Juan confirma que la 1ª carga en frío del breeding ya coloca y empareja correctamente (cortejo orbita, la cría sale del corral). Deuda cerrada.

**Causa raíz real (según Juan, tentativo):** al cargar/recuperar los breeders AL CORRAL en frío **no se actualizaban/refrescaban sus datos al cargarlos** → la pareja quedaba con estado/ocupancia stale → el cortejo no matcheaba y la cría no salía. Es decir, fue un problema de **refresh de data al reclamar al corral** (`ReclaimBreedingOccupants`/`ReclaimDirect`/`Claim` + `Rebind` tras `OnRegistryReloaded`), NO el timing del bake del NavMesh que era la hipótesis principal de abajo. La hipótesis histórica + fix A-E quedan como registro.

**Síntoma (histórico):** en la PRIMERA carga tras abrir Play, una pareja de breeders queda mal: uno "flotando"/congelado y/o no quedan ambos como ocupantes del MISMO corral → `ManageCourtship` no los empareja (se quedan quietos, sin orbitar) y al eclosionar `OnBreedingCompleted.FindOccupant` falla → la cría NO sale del corral y los padres no salen de cortejo. En la SEGUNDA carga (local==nube, mesh "caliente") carga correcto. Misma familia de carreras que la Sesión 14 (snap / re-instancia).

**Hipótesis de raíz (CONFIRMAR con instrumentación ANTES de tocar — evitar bola de nieve):** la colocación/confinamiento de breeders corre antes de que su pre-requisito esté listo. Pre-requisitos que hoy NO se verifican explícitamente por corral:
1. **Área de NavMesh de cría horneada** en el piso de ESE corral. Hoy se asume del `worldReady` genérico (primer bake); puede haber varios bakes y el área de cría llegar tarde → `EnterConfinement`/`RejoinNavMesh` con `confinedAreaMask` falla.
2. **Pull de nube tardío** (~8s, Sesión 14): `OnRegistryReloaded` reemplaza los DNA y re-encola/re-coloca breeders ya colocados → churn que vuelve a correr la carrera.
3. **Atomicidad de la pareja**: los dos miembros se colocan independientes; si uno confina y el otro difiere/falla, la pareja queda partida → el cortejo nunca matchea.

**Trazar PRIMERO (Sesión 17):** instrumentar con `SpawnDiagnostics` + `logStateTransitions`/`snapWarnThreshold` (ya existen) el orden real en frío: disk load → Start de escena (`pen.Start` registra penKey + `ReclaimBreedingOccupants`) → furniture + bake (¿incluye el área de cría y CUÁNDO?) → sign-in reload → pull de nube → pump del spawner. Confirmar en qué paso exacto el breeder cae a un estado roto y CUÁL pre-requisito falta.

**Solución correcta (implementar+testear DESPUÉS de trazar):**
- **A. Gate único y autoritativo de "listo para poblar"**: poblar breeders solo cuando data asentada (tras reconciliar el pull, no solo el sign-in) Y mundo horneado. Considerar esperar (ventana acotada) a que el pull termine antes de colocar breeders.
- **B. Chequeo de área de cría por corral**: antes de colocar un breeder, samplear el NavMesh de cría en su `pen.Center`; si falla, diferir (no cañón). Atar el éxito de `DeferBreeder` a ese sample, no solo a que el corral esté registrado.
- **C. Colocación atómica de la pareja**: colocar ambos miembros juntos (solo cuando ambos pueden confinar) → la pareja nunca queda partida.
- **D. Re-reclaim tras asentar el pull**: tras el `OnRegistryReloaded` final, re-correr el reclaim del corral para re-penned + re-emparejar lo desplazado.
- **E. Desacoplar la eyección de la cría del censo de ocupantes**: `OnBreedingCompleted` decide dueño por `mother.HomePenKey == penKey` (no `FindOccupant`) y registra el lanzamiento desde ESE corral aunque los agentes padres no estén findables; el `ExitCourtship` lo cubre el safety de `TickCourting`.

**Parche actual (NO resuelve la 1ª carga):** watchdog `RecoverIfStuckOffMesh` (kinematic + off-mesh → re-ancla penned / cae libre). Se queda como red de seguridad pero NO atacó este caso — el breeder roto probablemente está on-mesh pero mal posicionado o la pareja partida, no kinematic+off-mesh. Revisar si mantenerlo o reemplazarlo por la solución correcta.

---

## 📋 Tabla resumen de prioridad

| # | Item | Fase | Impacto | Riesgo |
|---|------|------|---------|--------|
| 1 | Namespacing `MoriMonchiSimulator` + reorg carpetas ✅ | 0 | Organización | Bajo |
| 2 | Código muerto / debug gated | 0 | Limpieza | Bajo |
| 3 | Partir `MoriMochiAgent` en colaboradores | 1 | Arquitectura | Medio |
| 4 | Partir paneles UI (Combat/Breeding) | 2 | Mantenibilidad | Medio |
| 5 | Separar debug/UI de dominio en Controllers ✅ | 3 | Arquitectura | Medio |
| 6 | Adelgazar `GameManager` ✅ | 3 | Arquitectura | Medio |
| 9 | Disciplina partial class (regla 11 + audit) ✅ | 3 | Arquitectura | Bajo |
| 7 | Unificar convención de acceso a SO (cascada) ✅ | 4 | Estabilidad | Medio |
| 8 | Deduplicar Async services (CloudEndpoint) ✅ | 5 | Arquitectura | Medio |
| 10 | ✅ Orden de carga de data + colocación de breeders en frío (resuelto, confirmado Juan 2026-06-23) | — | Estabilidad | Alto |
| 11 | ✅ Descomponer `CloudSyncService` (CloudAuth + CloudSyncOps, S53) | 6 | Arquitectura | Medio |
| 12 | ✅ Paneles UITK → sub-presenters por pestaña (piloto CombatPanelUITK S53; Breeding + DetailInfo S54) | 7 | Arquitectura | Medio |
| 13 | ✅ Descomponer `MoriMochiAgent` (AgentContext + Brain/Physics/Confinement, S55) | 8 | Arquitectura | Alto |
| 14 | ✅ `MoriMochiSpawner.Debug` → `SpawnerDevConsole` (S55) | 9 | Limpieza | Bajo |
