---
tags: [memory-bank, active, session]
---

# 09 - Active Context

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
