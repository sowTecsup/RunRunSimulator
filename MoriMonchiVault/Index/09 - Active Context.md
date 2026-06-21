---
tags: [index, core]
---

# 09 - Active Context

**Session:** 2026-06-21 (Session 16 — Breeding container: cortejo orientado al AGENTE (orbit/tend) + 3 comportamientos + bug de carga en frío) — **EN PROGRESO, 1ª carga SIN confirmar**
**Focus:** Mejorar el behavior dentro de los corrales: cortejo vivo (no congelado) con sub-estado por sexo, cría sale disparada al nacer, solo adultos crían, padres vuelven a merodear al nacer. **PARCIAL:** el feel + reload (2ª carga) funcionan y gustan; la 1ª carga en frío sigue fallando (pareja congelada + cría no sale del corral) → la raíz es el ORDEN DE CARGA, ver [[Index/11 - Technical Debt]] (🔴 ABIERTO — orden de carga). NO parchear más sin trazar primero.

**Modelo nuevo (contexto vs comportamiento):**
- El **corral** solo provee contexto social: quién está dentro + quién emparejado con quién. Empareja y dispara `EnterCourtship(partner, anchor)` UNA vez por agente; ya NO posa por frame.
- El **agente** es dueño del cortejo: estado `Courting` + sub-estado `CourtRole` por sexo. **Hembra (`Tend`):** darts cortos/rápidos alrededor del slot, mirando al macho. **Macho (`Orbit`):** orbita la posición VIVA de la hembra, mirándola. Velocidad boosteada (`courtSpeedMultiplier`). Visualmente distinto del penned no-emparejado (merodeo calmo).

**CONFIRMADO por Juan (funciona):**
- Feel orbit/tend ("me gusta cómo ha quedado", "está funcionando bien").
- Reload → retoma cortejo (2ª carga). **Fix persistencia:** `HomePenKey`/`HomePenSlot` se seteaban DESPUÉS del `RegistryChanged` de `StartBreedingAsync` → no persistían; ahora `TryRollPair` dispara `RegistryChanged` tras estamparlos. **+ `ManageCourtship` robusto:** `ResolveCourtAnchor` (slot si válido, si no centro del corral) → corteja aunque el slot no sobreviva.

**FALLA (fix + testear Sesión 17):**
- **1ª carga en frío:** pareja queda CONGELADA (no orbita) + cría NO sale del corral. Causa aguas abajo del orden de carga (los breeders no quedan bien como co-ocupantes del corral). Plan A-E en [[Index/11 - Technical Debt]]. **Trazar PRIMERO con SpawnDiagnostics.**
- Watchdog `RecoverIfStuckOffMesh` agregado como red de seguridad — **NO resolvió la 1ª carga** (el breeder roto no cumple kinematic+off-mesh). Revisar si mantener o reemplazar.

**Sin confirmar (dependían de testear hoy):**
- **Cría sale disparada:** `BirthLanding()` (afuera del corral, `birthEjectDistance`) + `RegisterBirthLaunch(child, muzzle, landing)` 3-args. Hoy depende de `FindOccupant` en `OnBreedingCompleted` → si los padres no son ocupantes (bug 1ª carga), no se registra el lanzamiento. **Fix correcto: dueño por `HomePenKey` (item E del debt).**
- **Solo adultos crían:** `AvailableOf` filtra `IsAdult(d)` (`LifeStageTable.GetStage(AgeDays) >= Adult`, incluye Elder); diagnóstico muestra "no adulto". Depende de que el `Life Stage Table` esté asignado en `BreedingController` (si no, no bloquea).
- **Padres a merodear al nacer:** `OnBreedingCompleted` → `ExitCourtship` + safety de `TickCourting` (la pareja deja de estar Breeding → auto-exit). Mismo downstream del bug 1ª carga.

**Files Touched (esta sesión — input para ScriptNodes, PENDIENTE correr agente haiku cuando se confirme en Play):**
- `World/AI/MoriMochiAgent.cs`: `baseSpeed`, enum `CourtRole`, campos de cortejo, `offMeshGrace`, llamada `RecoverIfStuckOffMesh` en Update.
- `World/AI/MoriMochiAgent.Confinement.cs`: `EnterCourtship(partner, anchor)` + `ExitCourtship` reescritos; normalización de rotación en `EnterConfinement`.
- `World/AI/MoriMochiAgent.Brain.cs`: `TickCourting`/`TickOrbit`/`TickTend`/`FacePartner`; `IsRecovering`/`IsBreeding`; restaura `agent.speed` en `EnterRoaming`.
- `World/AI/MoriMochiAgent.Physics.cs`: knock-proof por `IsBreeding`; `RecoverIfStuckOffMesh`.
- `World/AI/MoriMochiAgent.Tuning.cs`: tunables de cortejo (`courtSpeedMultiplier`/`courtOrbitRadius`/`courtAngularSpeed`/`courtLookahead`/`courtRepath`/`courtTendRadius`/`courtTendInterval`) + `offMeshRecoverDelay` + readout `CourtInfo`.
- `World/Containers/BreedingContainer.cs`: `ManageCourtship` solo-contexto + `ResolveCourtAnchor`; regla adultos (`IsAdult`) + diagnóstico; `BirthLanding`/`birthEjectDistance`; `RegistryChanged` tras estampar slot/key; reclaim espera `!IsRecovering`.
- `World/Spawning/MoriMochiSpawner.cs`: `DeferBreeder`/`breederPlaceTimeout`/`breederPlaceDeadline`; `RegisterBirthLaunch` 3-args + `birthLandingPoints`.

**Files Created:** ninguno.

**NEXT SESSION (17):**
1. **Trazar el orden de carga en frío** (SpawnDiagnostics + dev toggles) → confirmar dónde se rompe la colocación de breeders y qué pre-requisito falta. Ver [[Index/11 - Technical Debt]].
2. Implementar la solución correcta (items A-E) y testear: 1ª carga sana (pareja orbita), cría sale del corral, padres merodean, solo adultos crían.
3. Si todo OK → correr agente haiku para ScriptNodes (lista Files Touched arriba) + marcar etapa ✅.

---

### Sesión 15 (histórico) — 2026-06-21

**Session:** 2026-06-21 (Session 15 — Bugs post-refactor: desync color/genética + migración shader Toon + reset completo)
**Focus:** Tres bugs tras el refactor. (1) Breeding "Mother not found" + (2b) UITK en blanco = **misma causa raíz**. (2a) Migración al shader nuevo (Unity Toon). (3) "Reset All Progress" no borraba todo el estado MM server-side. **Confirmado funcionando en Play por Juan.**

**Causa raíz (1 + 2b):** `UniqueID` = `ToStringID()-Timestamp`, y `ToStringID()` **embebe el `BaseColor`** como RRGGBB. Saves viejos (era `PrimaryColor`, pre-S10) se cargaron sin migrar → `BaseColor` cayó a su default y se re-guardó: **color real en la KEY, blanco/negro en el value**. Cada load → `BaseColor` default → `UniqueID` recalculado ≠ key → `registry.TryGet` falla (breeding) y la UITK pinta el default. Verificado en disco (key `...5918EA...` vs value `FFFFFF`). El color real sobrevive en la genetic string de la KEY → **recuperable**.

**Fix 1 (self-heal):** `CreatureRegistrySO.LoadFrom` → `ReconcileColors` (+ `TryColorFromKey`): parsea el color de la KEY; si difiere del value restaura `BaseColor` y regenera `SecondaryColor`. Único embudo de carga → cubre **local + nube**. Tras el primer load+save queda reparado; queda como blindaje anti-desync permanente.

**Fix 2a (shader):** material nuevo `BaseFurNewTest` = Unity Toon Shader (props `_BaseColor`, `_1st_ShadeColor`, `_2nd_ShadeColor`, `_RimLightColor`). **Nuevo modelo de color**: `BaseColor` (genético, en la key, fuente de verdad) + `SecondaryColor` (slot derivado determinista del base). `ColorGenetics.BuildFurPalette(base, secondary)` orquesta los 4 colores del shader. Eliminados `ShadowsColor`/`OutlineColor` (todo deriva del base → reconstruible). El outline UTS es pass aparte (no lee MPB) → no se maneja por genética; el rim lo reemplaza. **Acople a nombres de shader sigue SOLO en MoriMonchiVisualizer** (ahora 4 PropertyIDs).

**Fix 3 (reset completo):** `CloudSyncService` Reset ahora borra TODO el estado MM: huevos (`cancel-all-breeding`), entries de combate (`dequeue-combat` por criatura `QueuedForCombat`), `combat_results`, además de las 4 keys de siempre. Usa **endpoints DIRECTOS** (`CloudEndpoint.CallAsync`) en vez de los wrappers de cliente para evitar el race `RegistryChanged → Persist → PushToCloud` (que re-subiría lo borrado). No requiere Cloud Code nuevo.

**Files Touched:**
- `Data/Genetics/CreatureRegistrySO.cs`: `ReconcileColors` + `TryColorFromKey` dentro de `LoadFrom`.
- `Data/Genetics/CreatureDNA.cs`: nuevo `SecondaryColor`; eliminados `ShadowsColor`/`OutlineColor`.
- `Core/ColorGenetics.cs`: `DeriveSecondary`, `BuildFurPalette`, `Shade`, struct `FurPalette`; eliminados `DeriveShadow`/`DeriveOutline`.
- `Core/CreatureGenerator.cs`: mint setea `SecondaryColor`.
- `Systems/Breeding/BreedingService.cs`: la cría setea `SecondaryColor` desde el base del hijo.
- `World/Creatures/MoriMonchiVisualizer.cs`: 4 PropertyIDs del Toon + `ApplyFur` usa `BuildFurPalette`.
- `Systems/Cloud/CloudSyncService.cs`: consts `COMBAT_RESULTS_KEY` / `CANCEL_ALL_BREEDING` / `DEQUEUE_COMBAT`.
- `Systems/Cloud/CloudSyncService.Sync.cs`: `ResetProgressAsync` borra huevos + cola de combate + `combat_results`.

**Files Created:** ninguno.

**Unity (hecho por Juan):** `FurTypeDatabase.asset` → materiales con el shader nuevo (`BaseFurNewTest`); recompiló; probó en Play (mint con color, breeding sano, UITK con color, Reset deja todo limpio local+nube).

**NEXT SESSION:** verificar la cadencia del spawner al hatchear varios huevos ("spawnea de golpe", `spawnInterval`/`spawnPerTick`) si reaparece. Seguir con features de gameplay.

---

### Sesión 14 (histórico) — 2026-06-21

**Session:** 2026-06-21 (Session 14 — Spawning: diagnóstico + toolkit de debug + fixes + gate data-ready)
**Focus:** Dos bugs del spawning: (1) al nacer un MM se "re-instanciaban TODOS", (2) al reactivarse el NavMeshAgent las criaturas se pineaban de golpe al piso. Diagnóstico instrumentado + fix raíz + blindaje estructural. Confirmado funcionando en Play por Juan.

**Diagnóstico (cerrado con evidencia del `SpawnDiagnostics`):**
- El snap al piso = `agent.enabled = true` teletransporta el transform al punto más cercano del NavMesh. Pasa en `RestoreNavMeshControl` (lo llama `Initialize`) y en los handoffs de rebake/recovery.
- El "re-instancia todos" = `MoriMochiSpawner.OnRegistryReloaded` llamaba `controller.Initialize()` sobre CADA criatura spawneada → snap masivo. **El gatillo NO es el nacimiento** (Register solo agrega; HatchLocally solo emite `RegistryChanged` → `Sync` quirúrgico). Los únicos que disparan `RegistryReloaded` en runtime son `CloudSyncService.OnSignedInComplete` (reload local) y `PullAsync` (reload nube) — éste último **llega tarde** (8.6s de latencia observada) y al caer después de spawnear, despawnea+re-dispara cuando local≠nube. Confirmado: en el run con local==nube y pull rápido → `Reload reconcile → rebound=0 despawned=0` (cero churn).

**Fixes (raíz + blindaje):**
- **Rebind (fix raíz):** `OnRegistryReloaded` ahora usa un `Rebind` liviano (re-vincula DNA + `RefreshFur`, SIN tocar nav/estado/posición) en vez de `Initialize`. Cascada nueva: `spawner.OnRegistryReloaded → controller.Rebind → agent.Rebind + visualizer.RefreshFur`. Solo churna el delta real (stale despawn, nuevos enqueue); las coincidencias hacen Rebind sin snap.
- **Anti-snap defensivo:** `RestoreNavMeshControl` hace `Warp` a la posición actual tras habilitar el agente (consistente con `RejoinNavMesh`/`TickRecovering`).
- **Gate data-ready (estructural):** el spawner ya NO puebla desde el `.asset` antes de la data autoritativa. Campo `dataReady` (gate en `EnsurePump`), se setea en el primer `OnRegistryReloaded`; coroutine `DataReadyFallback` (timeout `dataReadyTimeout`, def 6s) cubre offline/sin nube. Puebla UNA sola vez del roster correcto; reloads posteriores reconcilian vía Rebind.

**Toolkit de debug (nuevo):**
- `World/Spawning/SpawnDiagnostics.cs` (componente standalone, solo bus público → patrón F3): loguea cada `RegistryChanged`/`Reloaded`/`BreedingCompleted`/rebake con frame+tiempo + contadores; warning ruidoso en Reloaded.
- `MoriMochiAgent` pestaña Dev (en `.Tuning.cs`, excepción de tooling): readouts `CurrentState`/`NavStatus`, `forceRagdoll` (nunca rejoina), `logStateTransitions` (+ detector de snap por salto de `y`).
- `MoriMochiSpawner.Debug.cs`: botón "Dump Spawn State". Núcleo: log "Reload reconcile → rebound/despawned/enqueued".

**Files Touched:**
- `World/Spawning/MoriMochiSpawner.cs`: `Rebind` en OnRegistryReloaded + log reconcile; gate `dataReady` (campo, `EnsurePump`, `DataReadyFallback`, `dataReadyTimeout`, readout).
- `World/AI/MoriMochiAgent.cs`: método `Rebind` (público); hooks dev en Update (`forceRagdoll` + `DevTrackState`).
- `World/AI/MoriMochiAgent.Tuning.cs`: pestaña Dev (readouts + `forceRagdoll`/`logStateTransitions`/`snapWarnThreshold` + `DevTrackState`).
- `World/AI/MoriMochiAgent.Physics.cs`: guard `if (forceRagdoll) return;` en `TickThrown`.
- `World/AI/MoriMochiAgent.Confinement.cs`: anti-snap Warp en `RestoreNavMeshControl`.
- `World/Creatures/MoriMonchiController.cs`: passthrough `Rebind`.
- `World/Creatures/MoriMonchiVisualizer.cs`: `RefreshFur` público (wrap de `ApplyFur`).
- `World/Spawning/MoriMochiSpawner.Debug.cs`: botón "Dump Spawn State".

**Files Created:**
- `World/Spawning/SpawnDiagnostics.cs`.

**Nota dev:** los `Debug.Log` de "Reload reconcile" y `SpawnDiagnostics` quedan como instrumentación viva (útil mientras se sigue debugeando el spawning). Quitar/gatear cuando se cierre el tema.

**NEXT SESSION:** spawning robusto (sin churn/snap, pobla del autoritativo). Pasar a la sesión de debugs general que Juan mencionó.

---

### Sesión 13 (histórico) — 2026-06-20

**Session:** 2026-06-20 (Session 13 — F0 Higiene: namespacing + reorg de carpetas)
**Focus:** Cerrar Fase 0. Namespace raíz único en los 116 scripts + reorganización de `Scripts/World` y `Scripts/Data` en subcarpetas por dominio. Decidido por Juan pese a recomendación de saltar el namespacing (payoff cosmético con assembly único); ejecutado con auditoría de serialización previa para descartar pérdida de datos.

**Decisiones:**
- **Namespace:** raíz único `MoriMonchiSimulator`, **block-scoped** (`namespace X { }`). File-scoped (`namespace X;`) es C# 10 y Unity 6.3 compila C# 9 → no disponible.
- **NO re-indentado:** se insertan solo 3 líneas por archivo (`namespace` / `{` / `}`); el cuerpo queda en columna 0 dentro del namespace (legal, compila idéntico). Motivo: diff mínimo y revisable + cero riesgo a strings. Indentado bonito = paso opcional posterior con Reformat del IDE (Rider `Ctrl+Alt+L`) como commit cosmético aparte.
- **`#region`:** la auditoría de `Index/11` lo listaba; medido = **0 ocurrencias** en todo el código → ítem moot, tachado.

**Reorg (47 `.cs` movidos con su `.cs.meta` → GUID intacto, cero refs rotas):**
- `World/` (24) → `AI/` (MoriMochiAgent×5), `Spawning/` (MoriMochiSpawner×2, SpawnBallistics, ControllerPool), `Creatures/` (MoriMonchiController, MoriMonchiVisualizer, FurRenderer, BodyPartJoint, NameTag), `Containers/` (MoriMochi/Breeding/Store Container), `Needs/` (NeedStation, NeedStationRegistry, Feeder, PlayZone, RestZone), `Props/` (WorldPropInstance, HotbarController).
- `Data/` (24 sueltos) → por dominio: `Databases/` (+Creature/Furniture/Item/FurType/PartVisualBank), `Genetics/` (CreatureDNA, RarityOdds, PersonalityProfile, CreatureRegistry, Creature/PartNameBank), `Breeding/` (BreedingAffinity, InheritanceOdds, CreatureLifeStage), `Combat/` (CombatManager, CombatLogEntry/Record/Result), `Furniture/` (FurnitureDefinition/Registry, PlacedFurniture), `Items/` (ItemDefinition), `Player/` (PlayerInventory). `NeedsState` queda en raíz.
- **`CreateAssetMenu` reorganizado:** los 25 `menuName` que estaban planos bajo `RunRunSimulator/` ahora cuelgan de subcarpetas que espejan los dominios (`Databases/`, `Parts/`, `Genetics/`, `Breeding/`, `Furniture/`, `Combat/`, `Items/`, `Player/`, `Store/`). Solo cambia el menú Assets→Create; assets existentes intactos (tipo por GUID).

**Namespacing (116 `.cs`):** script PowerShell determinista (no subagentes — transform uniforme, más predecible). Inserta tras el último `using` (o antes del primer tipo si no hay using; 4 casos: Enums, Feeder, PlayZone, RestZone), `}` al EOF. Preserva CRLF + estado de BOM exactos. Guardas: salta si ya tiene namespace, `break` antes del cuerpo de clase (no confunde `using(...)` de método). **Validado: los 116 con exactamente 1 namespace, llaves balanceadas, cierre en `}`.**

**Auditoría de serialización (clave — descarta pérdida de datos):** 0 `SerializeReference` / `TypeNameHandling` / `$type` / binders custom. Newtonsoft serializa por nombre de propiedad (enums por `StringEnumConverter`), deserializa en tipos genéricos explícitos → tipo resuelto en compilación, no del JSON ⇒ saves locales/cloud/DNA string namespace-safe. Odin: Parts son `SerializedScriptableObject` (refs por GUID); dicts autorales con claves enum / tipos concretos (resueltos por firma de campo, no polimórficos) ⇒ namespace-safe. Registry `.asset` es espejo del JSON (re-Sync).

**PASO MANUAL PENDIENTE (Juan, Unity):** reabrir Unity → reimporta (genera `.meta` de las 13 carpetas nuevas) y recompila. **Confirmar 0 errores de `using`/namespace.** De paso, ojear que los assets Odin autorales (FurTypeDatabase, BreedingAffinityTable, InheritanceOddsTable, PersonalityProfileTable, CreatureLifeStageTable, Part DBs) muestren sus entradas — deberían; si alguno saliera vacío, restaurar ese `.asset` de git (no debía cambiar). Opcional: Reformat del IDE para indentar.

**Cierre de vault (mantenimiento, 2026-06-20):**
- **Tags limpiados:** de 54 tags (40+ usados 1 vez) a **10 canónicos**. Cada nota = `[tipo, dominio]` con tipo ∈ {script, index, archive} y dominio ∈ {core, genetics, combat, ui, world, cloud, furniture}. Eliminado `memory-bank` (estaba en 116/120 → sin valor de filtro) y toda la cola. Aplicado por script determinista (vocabulario controlado).
- **ScriptNodes (haiku):** refrescada la línea `**Ruta:**` de los 40 nodos cuyos scripts se movieron (los 5 partials no tienen nodo propio). Creado nodo faltante `FurRenderer.md` y poblado el stub vacío `RarityOddsTableSO.md`. Vault completo: todo script con nodo, rutas al día.
- No se tocó responsabilidad/conexiones del resto (no cambió contrato — solo ubicación + namespace + menú).

**NEXT SESSION:** F0 cerrada + vault sincronizado a la fecha (queda solo el reindentado cosmético opcional del namespace). Volver a features de gameplay / probar en Play el cableado de F3/F4.

---

### Sesión 12 (histórico) — 2026-06-20
**Focus:** F3 (slim Core/Systems) resuelto por **componentes independientes, NO partial** (corrige el enfoque de F1/F2). Codificación de la **regla 11** (disciplina de partial class) en `CLAUDE.md`. Auditoría y corrección de los partials previos: convertir lo puro, documentar el resto como excepción justificada.

**Decisión de arquitectura (Juan):** partial class solo por ventaja física de archivo (Git / código generado). Si la clase creció, el remedio es dividir en clases/componentes independientes. Tooling dev y código puro nunca en partial. Núcleo con estado mutable irreducible: partial SOLO documentado como deuda en `Index/11`. Sin abusar de static: las consolas dev usan **referencias serializadas explícitas** (no `.Instance`, no SO `.Current`) → trazabilidad de quién depende de qué.

**Files Touched:**
- `CLAUDE.md`: principio 3 refinado + regla 11 nueva.
- `Core/GameManager.cs` (318→177): solo persistencia/assets/Mint.
- `Systems/Breeding/BreedingController.cs` (281→108): solo dominio; `BreedCreatures` devuelve childID; header actualizado.
- `Systems/Combat/CombatController.cs` (281→77): solo dominio + API (`Config`, `SimulateLocal`, wrappers async, `EnqueueForAsyncCombat`→`Task`).
- `World/MoriMochiSpawner.cs`: call-sites puros → `SpawnBallistics`; `RandomLandingPoint`/`ResolveActivationPoint` movidos al núcleo.
- `World/MoriMochiSpawner.Debug.cs`: call-sites → `SpawnBallistics`.

**Files Created:**
- `Core/GeneticsLabPreview.cs`, `Core/DevToolsConsole.cs`
- `Systems/Breeding/BreedingDevConsole.cs`
- `Systems/Combat/CombatDevConsole.cs`
- `World/SpawnBallistics.cs` (static, matemática de lanzamiento)
- `World/ControllerPool.cs` (pool independiente con su propia Queue; el spawner pide Get/Return)

**Files Deleted:**
- `World/MoriMochiSpawner.Ballistics.cs` (+ `.meta`)
- `World/MoriMochiSpawner.Pool.cs` (+ `.meta`) — su lógica de pooling salió a `ControllerPool`; `Despawn`/`ClearAll` (spawn-tracking) movidos al núcleo de `MoriMochiSpawner.cs`

**Auditoría de partials:** solo `Ballistics` era convertible (puro). Resto = excepción documentada en `Index/11`: `MoriMochiAgent` (FSM irreducible), paneles UITK (árbol VisualElement), `CloudSyncService` (sesión de red), `MoriMochiSpawner` (Pool/Debug: estado privado + gizmos).

**Wiring Unity ✅ HECHO (Juan):** los 4 dev consoles agregados al GameObject con sus refs `[Required]` (`GeneticsLabPreview`, `DevToolsConsole`, `BreedingDevConsole`, `CombatDevConsole`). Confirmado compilando y funcionando en Play.

**ScriptNodes:** el agente Deepseek/opencode se colgó; se rehízo con un **subagente Claude (haiku)** que SÍ funcionó → 4 nodos actualizados (GameManager, BreedingController, CombatController, MoriMochiSpawner) + 6 creados (GeneticsLabPreview, DevToolsConsole, BreedingDevConsole, CombatDevConsole, SpawnBallistics, ControllerPool). Nodos al día. **Nota de proceso:** para futuros cierres, usar subagente haiku en vez de opencode/Deepseek (este último colgó sin output).

**MoriMochiAgent (auditoría 2026-06-20):** se evaluó decomponer el FSM y se DESCARTÓ con evidencia dura (ver `Index/11`): los 3 concerns comparten `state` + ~30 campos mutables + ~10 timers y se llaman entre sí; un blackboard relocalizaría el acoplamiento a un contexto público (peor). Queda como excepción documentada de la regla 11.

**F4 + F5 HECHAS (2026-06-20, modelo cascada de Juan):** cada dominio = pirámide con apex dueño de sus refs; hijos piden al apex por `.Instance` (servicio runtime, permitido); SO `static Current` eliminados (perdían trazabilidad de dueño).
- F4 — 5 `Current` fuera: CombatManagerSO→CombatController (config serializado + nuevo `Instance`); InheritanceOddsTableSO→BreedingController (serializado, getter `InheritanceOdds`, sacado de GameManager); BreedingAffinityTableSO→BreedingController (ya serializado, quitado fallback); FurTypeDatabaseSO→GameManager (raíz) ruteado spawner→`controller.Initialize(furDb)`→`visualizer.SetFurDatabase`; PersonalityProfileSO→queda en GameManager, ya llegaba por Initialize.
- F5 — `CloudEndpoint` (static, `CallAsync`/`CallAsync<T>`) deduplica call+deserialize en AsyncCombat/AsyncBreeding (la reconciliación queda per-servicio).
- Files: GameManager, CombatController, AsyncCombatService, CombatPanelUITK, CombatManagerSO, BreedingController, AsyncBreedingService, BreedingPanelUITK.Content, InheritanceOddsTableSO, BreedingAffinityTableSO, MoriMonchiController, MoriMonchiVisualizer, MoriMochiAgent, FurTypeDatabaseSO, PersonalityProfileSO (mod) + CloudEndpoint (nuevo). Verificado: 0 `.Current` residuales, llaves OK.
- Wiring Unity ✅ HECHO (Juan): `config` en CombatController, `inheritanceOddsTable` en BreedingController, `furTypeDatabase` en GameManager asignados. Confirmado funcionando en Play (cascada SO operativa, sin nulls).

**NEXT SESSION:** todas las F del roadmap de saneamiento cerradas (F0 namespacing saltado por decisión). Volver a features de gameplay / probar en Play el nuevo cableado (cascada SO + dev consoles de F3). Backlog opcional vivo en Index/11: tabs UITK→sub-presenters; FSM MoriMochiAgent vía blackboard (descartado con evidencia).

---

### Sesión 11 (histórico) — 2026-06-19
**Session:** 2026-06-19 (Session 11 — Mantenimiento / Refactor)
**Focus:** Auditoría de arquitectura + saneamiento. Mapa por capas, hoja de ruta viva en `Index/11 - Technical Debt`, regla de arquitectura general codificada en `CLAUDE.md`. F0 (higiene): sin código muerto (CombatManagerSO vivo, CloudCodeTester es dev tool). F1 (partir `MoriMochiAgent`): hecho vía `partial class` (no componentes — el FSM comparte núcleo mutable; separarlo empeoraría el acoplamiento).

**Files Touched:**
- `CLAUDE.md`: nueva sección "Regla de arquitectura general" antes de las 10 reglas (4 principios: capas sin saltos de 2 niveles, comunicación solo por bus/servicio, límite ~400 líneas/1 dominio, singleton=servicio / SO=data).
- `World/MoriMochiAgent.cs`: reducido a núcleo (~243 líneas) — campos, lifecycle, dispatch, helpers NavMesh compartidos, gizmos. Clase ahora `partial`. Cero cambio de comportamiento ni de API pública (verificado: diff de contenido exacto contra el original de 1189 líneas).

**Files Created:**
- `World/MoriMochiAgent.Tuning.cs` (~201): `[SerializeField]` Odin + readouts + dev buttons.
- `World/MoriMochiAgent.Brain.cs` (~358): estados + needs + reacciones + intent + queries.
- `World/MoriMochiAgent.Physics.cs` (~274): colisión/knock/throw/ragdoll/recovery/handoff.
- `World/MoriMochiAgent.Confinement.cs` (~136): pen + courtship + rebake survival + pooling.

F1 compilado OK por Juan. **F2 (partir paneles UI gigantes) HECHO** con el mismo patrón partial-class (los paneles comparten estado UI mutable → componentes empeorarían el acoplamiento):
- `UI/CombatPanelUITK.cs` (849→241 núcleo) + creados `UI/CombatPanelUITK.Tabs.cs` (390) + `UI/CombatPanelUITK.Navigation.cs` (234). Verificado exacto (563 líneas), sin cambio de comportamiento.
- `UI/BreedingPanelUITK.cs` (637→170 núcleo) + creados `UI/BreedingPanelUITK.Content.cs` (273) + `UI/BreedingPanelUITK.Navigation.cs` (210). Verificado exacto (418 líneas).
- Patrón UITK establecido: **Core (lifecycle+wiring+data) / Content|Tabs (build+bind) / Navigation (IUINavigable+foco)**.
- `UI/MorimonchiDetailInfoUITK.cs` (478→289 núcleo+Info+Combat) + creado `UI/MorimonchiDetailInfoUITK.Trees.cs` (196, tabs Linaje/Descendencia). Verificado exacto (327 líneas). **Todos los paneles UI >400 líneas partidos.**
- `World/MoriMochiSpawner.cs` (622→398 motor) + creados `.Pool.cs` (62), `.Ballistics.cs` (66), `.Debug.cs` (123, dev+gizmos). Verificado exacto (404 líneas).
- `Systems/Cloud/CloudSyncService.cs` (568→133 núcleo+meta) + creados `.Auth.cs` (246), `.Sync.cs` (218, validate+reset+push+pull). Verificado exacto (353 líneas).

**RESUMEN SESIÓN 11:** 6 monstruos partidos vía partial-class (Agent 1189, CombatPanel 849, BreedingPanel 637, DetailInfo 478, Spawner 622, CloudSync 568). **14 partials nuevos**, 6 núcleos reducidos. Todos verificados byte-exactos + llaves balanceadas + GUIDs de prefab intactos. Cero cambio de comportamiento/API. **Compilado OK por Juan** (Unity generó los 14 `.meta`).

**CIERRE SESIÓN 11 (2026-06-20):** agente externo Deepseek ejecutado OK → 6 ScriptNodes actualizados con sección "Organización (partial class)" (MoriMochiAgent, CombatPanelUITK, BreedingPanelUITK, MorimonchiDetailInfoUITK, MoriMochiSpawner, CloudSyncService), sin tocar responsabilidad/conexiones. Hoja de ruta viva en `Index/11`.

**NEXT SESSION (arranque):** F3 — adelgazar `GameManager` + separar debug/dominio en `BreedingController`/`CombatController` (refactor de LÓGICA, no split). Splits menores pendientes: `BreedingContainer` (468), `AsyncCombatService` (462), `BuildModeController` (418). F0-namespacing aún pendiente (decisión de Juan: vale la churn o no).

**PASO MANUAL PENDIENTE (Juan, Unity):** reimportar para que Unity genere los `.meta` de los partials nuevos (MoriMochiAgent ×4, CombatPanelUITK ×2, BreedingPanelUITK ×2) y recompile. Las referencias de componentes en prefabs/escena NO cambian (mismos `.cs.meta`/GUID de los archivos núcleo). Confirmar compilación sin errores de `using`.

**Next Session Goal:** F3 (adelgazar GameManager + separar debug/UI de dominio en Controllers) o F2-extra (`MorimonchiDetailInfoUITK` 478 con el mismo patrón). Hoja de ruta completa en `Index/11`.

---

### Sesión 10 (histórico) — 2026-06-19
**Session:** 2026-06-19 (Session 10)
**Focus:** FurType + sistema de 3 colores genéticos (BaseColor / ShadowsColor / OutlineColor). Cada MoriMochi tiene un FurType (enum) que mapea a un Material CartoonShader vía FurTypeDatabaseSO; las partes del cuerpo comparten ese material y los 3 colores se aplican per-criatura por MaterialPropertyBlock. Herencia: los 3 colores se cruzan de los padres con variación HSV; el FurType se hereda 50/50.

**Files Touched:**
- `Core/Enums.cs`: nuevo enum `FurType { Smooth, Fluffy, Spiky, Shaggy, Scaly }` (metadata, NO parte del DNA string; mapea 1:1 a un material en FurTypeDatabaseSO).
- `Data/CreatureDNA.cs`: `PrimaryColor` → renombrado a `BaseColor`; nuevos `ShadowsColor`, `OutlineColor` (Color, `[ColorUsage(false)]`) y `FurType FurType`. El genetic string NO cambia de formato (BaseColor sigue ocupando la posición RRGGBB en `ToStringID()`/`FromID()`; los otros 3 son metadata JSON como Gender/Personality → no rompen UniqueIDs ni lineage refs).
- `Core/CreatureGenerator.cs`: mint genera `BaseColor` aleatorio (`ColorGenetics.RandomBase`) + `ShadowsColor`/`OutlineColor` derivados, y FurType aleatorio (mismo patrón Enum.GetValues que RandomPersonality).
- `Systems/Breeding/BreedingService.cs`: el hijo hereda los 3 colores vía `ColorGenetics.Inherit(mother.X, father.X)` (blend + jitter HSV) y FurType 50/50 vía `ColorGenetics.Inherit(furM, furF)`. Reemplaza el color full-random anterior.
- `World/MoriMonchiVisualizer.cs`: PropertyIDs cacheados ahora son los del CartoonShader (`_Base_Color`, `_Shadows_Color`, `_Outline_Color`). Eliminados `ApplyColor`, el campo `bodyRenderer` y `ApplyPersonalityTint`. Nuevo `ApplyFur(dna)`: resuelve material vía `FurTypeDatabaseSO.Current.GetMaterial(dna.FurType)`, recolecta TODOS los renderers bajo modelRoot, asigna ese material a cada uno (si existe) y aplica UN MaterialPropertyBlock con los 3 colores. El color ya no es solo del body: todas las partes lo heredan.
- `World/MoriMonchiController.cs`: en `Initialize` eliminadas las líneas del personality tint (resolución del profile + `ApplyPersonalityTint`). El `Tint` de PersonalityProfileSO queda intacto, reservado para colorear los OJOS a futuro (fuera de alcance hoy).
- UI (rename `PrimaryColor`→`BaseColor`, sin cambio de lógica): `UI/MorimonchiDetailInfoUITK.cs`, `UI/CreatureVisualUI.cs`, `UI/CreatureGridView.cs`, `UI/CreatureGridUITK.cs`, `UI/CombatPanelUITK.cs`, `UI/BreedingPanelUITK.cs`.

**Files Created:**
- `Core/ColorGenetics.cs`: helper estático centralizado de genética de color. `RandomBase()`, `DeriveShadow(base)` (V×0.55), `DeriveOutline(base)` (V×0.25), `Inherit(Color,Color)` (lerp aleatorio + jitter HSV ±0.04 H / ±0.05 S,V), `Inherit(FurType,FurType)` (50/50). Lo usan CreatureGenerator y BreedingService.
- `Data/FurTypeDatabaseSO.cs`: `SerializedScriptableObject` con singleton `Current` (auto-registro en OnEnable, patrón BreedingAffinityTableSO). Dict Odin `FurType→Material` + `GetMaterial(type)` (fallback null + warning) + botón "Populate from Enum".

**PASO MANUAL PENDIENTE (Juan, en Unity):**
- Crear el asset `FurTypeDatabase` (menú RunRunSimulator/Databases/Fur Type Database), pulsar "Populate from Enum", crear un Material CartoonShader por FurType (variando solo el look, colores en neutro) y asignarlos en el dict. Sin materiales, las criaturas igual reciben sus 3 colores por MPB sobre el material que tengan los prefabs.

**Nota de shader (Juan avisó cambio inminente):** el shader es intercambiable. El ÚNICO acople a nombres de propiedad del shader vive en `MoriMonchiVisualizer.cs` (3 `Shader.PropertyToID`: `_Base_Color`, `_Shadows_Color`, `_Outline_Color`). Al cambiar de shader, actualizar SOLO esos 3 IDs para que coincidan con las propiedades del nuevo shader; el resto del pipeline (DNA, herencia, FurTypeDatabaseSO) no se toca.

**Next Session Goal:** crear el asset FurTypeDatabase + materiales, probar en Play que cría y mint pintan base/sombra/contorno y que las crías heredan colores+furtype. Posible swap de shader.

---

### Sesión 9 (histórico) — 2026-06-18
**Focus:** BreedingContainer — edad/etapa de vida en NameTag, corral de origen (HomePenKey), orden de carga (ocupados directos + libres por cañón), recién nacido lanzado desde su corral, contador de crías 0/MaxBreedCount.

**Files Touched:**
- `Core/Enums.cs`: nuevo enum `LifeStage { Newborn, Child, Teen, Adult, Elder }` (display-only, no parte del DNA string).
- `Data/CreatureDNA.cs`: `HomePenKey` (string, corral donde cría; persiste); `AgeDays` (int, días vividos desde BirthDate UTC).
- `World/MoriMonchiController.cs`: expone `public MoriMochiAgent Agent` (lo usa el spawner para colocación directa).
- `World/NameTag.cs`: línea `stage-label` = "{etapa} · {días}d" (ambos layouts, consulta `CreatureLifeStageTableSO.Current`); `breed-label` = "{BreedCount}/{MaxBreedCount}" (solo layout corral). Helper `StageText`.
- `UI Toolkit/NameTagUITK.uxml` + `NameTagUITKStyle.uss`: labels `stage-label` (.tag__stage) y `breed-label` (.tag__breed).
- `World/BreedingContainer.cs`: `penKey` resuelto en Start vía `PlacedFurnitureMarker.AnchorCell` ("x_y"); registro estático `byKey` + `TryGet`; stamp `HomePenKey` en ambos padres al aceptar el server; `ReclaimBreedingOccupants` filtra por `HomePenKey == penKey`; `ReclaimDirect(agent)` (expone Claim); `LaunchPoint` = Center + up*launchHeight; `ClearBreed` limpia HomePenKey. Dueño del nacimiento: suscribe `OnBreedingCompleted` y si la pareja es ocupante de ESTE corral → `MoriMochiSpawner.Instance.RegisterBirthLaunch(child, LaunchPoint)` + `ExitCourtship()` en ambos padres (vuelven a deambular; antes quedaban posados mirándose tras nacer la cría).
- `World/MoriMochiSpawner.cs`: singleton `Instance`; `breederQueue` (prioridad) drena antes que `spawnQueue`; `SpawnOne` coloca breeders directo en su corral (`TryPlaceInPen` → ReclaimDirect, fallback a cañón si rechaza); helper `Acquire` (unifica prewarm/cold). El lanzamiento del recién nacido lo decide el corral: `RegisterBirthLaunch(childId, worldPoint)` rellena `birthLaunchPoints`; en `SpawnOne` el muzzle del recién nacido = ese punto. (Ya NO suscribe `OnBreedingCompleted` ni depende de `HomePenKey` para el lanzamiento.)

**Files Created:**
- `Data/CreatureLifeStageTableSO.cs`: SerializedScriptableObject (SIN singleton — un SO nunca es singleton), dict Odin `LifeStage→umbral días`, `GetStage(ageDays)`, `Label(stage)` (español), botón Seed Defaults (0/1/3/7/20). La referencia vive en `BreedingController.lifeStageTable` (getter `LifeStageTable`); el NameTag la lee vía `BreedingController.Instance`.

**PASO MANUAL PENDIENTE (Juan, en Unity):**
- Crear el asset `CreatureLifeStageTable` (menú RunRunSimulator/Life Stage Table), pulsar "Seed Defaults", y **asignarlo en el campo `Life Stage Table` del componente BreedingController** (mismo GameObject que GameManager). Sin asignarlo, el NameTag muestra solo "{días}d" sin etiqueta de etapa.
- Verificar que los corrales (BreedingFurnitureNew / ContainerBase) entren por el sistema de muebles (tienen PlacedFurnitureMarker); si hay corrales puestos a mano en escena, el penKey caerá al `name` (warning en consola).

**Quirk técnico:** identidad estable del corral = CellKey del mueble (`"x_y"`), no el GameObject. El `PlacedFurnitureMarker` lo estampa FurnitureSpawner DESPUÉS del Instantiate, por eso el corral resuelve su key en `Start()`, no en Awake/OnEnable.

**Fixes de feedback (mismo bloque de sesión):**
- `World/MoriMochiAgent.cs`: (a) `ReactIfPlayerNear` — estando penned se restringe SOLO el acercarse al jugador (Approach/Follow se omiten dentro del corral); el resto de estados (flee/retreat, roaming, idle, needs) se mantienen. (Primero se gateó toda reacción, pero quedaban estáticos/encimados; corregido a restricción quirúrgica.) (b) `Knock` preserva `thrownTimer` si ya estaba `Thrown` → un cúmulo de criaturas golpeándose en vuelo ya no se resetea el safety timeout mutuamente (antes quedaban "por los aires" indefinidamente; ahora recuperan a más tardar en `maxThrownTime`).
- `World/NameTag.cs`: layout de corral más alto + compacto. Campos `penRaise` (def 0.6) y `penScale` (def 0.8); cachea localPosition/localScale base en Awake y los aplica en LateUpdate solo cuando `agent.IsPenned` (antes el tag de cría clipeaba el piso).

**Breed points fijos + datos de pareja persistidos (sub-feature):**
- `Data/CreatureDNA.cs`: `HomePenSlot` (int, −1 = sin asignar) = slot de cría preferido; persiste con el DNA (local+cloud) junto a `HomePenKey`+`BreedPartnerID` → el dato de pareja (qué MM↔qué MM, corral, slot) queda completo y reconstruible del registro.
- `World/BreedingContainer.cs`: reemplazado el spacing dinámico (`courtGap`/`BodyRadius`) por **slots fijos configurables**: `BreedingSlot[] breedingSlots` (cada slot = 2 anclas hijas spotA/spotB, mirándose). `FindFreeSlot()` asigna el primer slot libre al emparejar (escribe `HomePenSlot` en ambos padres). `ManageCourtship` reescrito multi-pareja: posa cada pareja breeding en su slot (hembra→spotA, macho→spotB). `ClearBreed` limpia `HomePenSlot`. Manager-awareness: `static All` (registro de corrales), `PenKey`, `ActivePairs()`.
- `World/MoriMochiAgent.cs`: quitado `BodyRadius` (sin uso con slots fijos). `EnterCourtship(pos, lookAt)` ahora recibe los Transforms del slot.
- `Systems/Breeding/BreedingController.cs`: readout Odin (BoxGroup "Corrales") `ActivePenCount` + `ActivePairsInfo` leyendo `BreedingContainer.All`/`ActivePairs()` — el manager conoce los corrales activos y sus parejas sin refs serializadas.

**PASO MANUAL PENDIENTE (Juan, Unity):** en el prefab del corral, crear los empties spotA/spotB por slot (1 par para 1 pareja, 2 pares en esquinas para 2) y asignarlos al array `Breeding Slots`. Sin slots, las parejas no se posan (merodean).

**Next Session Goal:** testear en Play. Confirmar: pose en slots fijos (sin variar al recargar), 2 parejas en 2 slots, readout de parejas en BreedingController, padres vuelven a merodear al nacer cría.

---

### Sesión 8 (histórico) — 2026-06-13
**Focus:** BreedingContainer feedback + visual cortejo. BreedingController como única fuente de servicios. Cancel async server-side. NameTag corral. Recuperación de huevos huérfanos.

**Files Touched:**
- `World/BreedingContainer.cs`: implementa `IInteractable` (tap E → hatch); restore pasivo de needs; `ForceRoll` (botón Odin) que ignora cooldown; `TryRollPair(verbose, ignoreCooldown)` ahora es `async` y reporta éxito SOLO si el server inició la incubación (no miente: si el server rechaza con `already_breeding` lo dice y no quema cooldown); `ManageCourtship` posa la pareja incubando enfrentada en slots fijos (`courtGap`); `CancelBreeding` al retirar del corral (limpia local + llama server); `ReclaimBreedingOccupants` reengancha al corral los breeders mid-incubación tras recargar escena; `PairDiagnostics` + `lastRollInfo` para debug en inspector
- `World/MoriMochiContainer.cs`: `OccupantInfo` ahora {Name, Gender, Personality} (sin dump de DNA); `Claim()` compartido (Admit + reclaim); `Release()` public virtual; warning de NavMesh si confinamiento falla
- `World/MoriMochiAgent.cs`: estado `Courting` + `EnterCourtship`/`ExitCourtship`/`IsCourting`/`IsPenned`
- `World/NameTag.cs`: layout de corral (glifo género ♂/♀ + nombre + personalidad; ❤ + countdown si incubando) vs layout libre (status/intent/petHint)
- `Systems/Breeding/BreedingController.cs`: singleton `Instance`; dueño de `AsyncBreedingService` + `BreedingAffinityTableSO`; wrappers `GetAffinity`/`StartBreedingAsync`/`HatchAsync`/`CancelBreedingAsync`/`CancelAllBreedingAsync`; botón "Cancel All Eggs" en BoxGroup Breed Timer
- `Systems/Breeding/AsyncBreedingService.cs`: `CancelBreedingAsync(mother,father)` (endpoint cancel-breeding) y `CancelAllBreedingAsync()` (endpoint cancel-all-breeding, vacía la cola entera + `ClearAllLocalBreeding`)

**Cloud Code (nuevos, desplegar):**
- `CloudCode/cancel-breeding.js`: borra un huevo puntual (motherId+fatherId)
- `CloudCode/cancel-all-breeding.js`: vacía la key `breeding_eggs_<playerId>` entera (recupera huérfanos que el cliente no rastrea)

**Quirk técnico resuelto:** el bug de "¡Emparejados! pero no pasa nada" era desync con el server — un cancel cuyo endpoint no estaba desplegado dejaba el huevo vivo en el server; al reusar esos IDs, `start-breeding` devolvía `already_breeding` y el `BusyState` local nunca se seteaba. Fix doble: (1) `TryRollPair` async ahora verifica `BusyState` real antes de cantar éxito; (2) botón Cancel All Eggs + endpoint para limpiar huérfanos.

**Next Session Goal (diseño + testing):**
- **Corral de origen**: las parejas deben recargar en el MISMO corral donde breedearon. Crear data que empareje `furnitureId` ↔ MoriMonchis (hoy `ReclaimBreedingOccupants` reengancha a cualquier corral disponible, no al de origen).
- **Bug en testing (no reproducido)**: error "unknown" raro, posiblemente al pulsar "Cancel All Eggs" repetidas veces (sospecha: `async void` sin guard re-entrante → llamadas solapadas a CallEndpointAsync). Intentar reproducir; si confirma, agregar guard (estilo `isHatching`).
- Verificar visualmente la pose de cortejo y el reclaim tras recarga de escena.
