---
tags: [memory-bank, active, session]
---

# 09 - Active Context

**Session:** 2026-06-18 (Session 9)
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
