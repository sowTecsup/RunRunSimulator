---
tags: [index, expedition, persistence, plan]
---

# 24 - Puente Tienda-Arena (plan S119, aprobado por Juan)

> Plan detallado para conectar la escena de gameplay `ArenaSandbox` con los MoriMonchis reales del jugador y con la escena de la tienda (`GameScene`), enfrentando por ahora a bots generados al azar. Escrito en S119 tras leer [[Index/23 - Arena Sandbox y Expedicion]], [[Index/22 - Bajada Nocturna y Linaje (Draft)]] Parte 8, [[Index/07 - Persistence & Identity]] y el código. Se ejecuta con subagentes `morimonchi-coder` (un lote por coder) y se verifica en el editor.

---

## 1 · Regla del cambio

> **La tienda es dueña de los datos; la arena recibe lo que necesita por un paso de mano estático y devuelve un resultado por el mismo camino. Nada más cruza entre escenas.**

Decisiones de Juan (S119 ⭐):
1. Carga de escena **Single** con paso de mano estático (no aditiva, no `DontDestroyOnLoad`).
2. Rivales **minteados al azar por semilla de sala** (Role, diales, partes); el roster queda solo como modo de desarrollo.
3. Al volver, hoy **solo entra el material asegurado** al inventario; las criaturas no se tocan (ni energía ni heridas).
4. El equipo se elige hoy con el selector que ya tiene la arena (`ArenaCastPicker`); el panel de tienda va en S120.
5. El plan detallado vive en esta nota.

---

## 2 · Lo que hay hoy (diagnóstico)

| Frente | Estado | Consecuencia |
|---|---|---|
| Elenco propio | `ArenaCastMode.LocalSave` + `ArenaCastSource.LoadLocal()` leen el `creature_database*.json` **más reciente del disco** (no el del scope del jugador) y `ArenaCastPicker` deja elegir 3. | Con dos cuentas o dos PCs puede tomar el save equivocado. El scope (`SaveSystem._userScope`) es estático y sobrevive la carga de escena: hay que usarlo. |
| Rival | Siempre del `ArenaRosterSO`; sin roster no hay rivales **ni salidas** (`BuildRoom` hace `if (Planner.HasRoster) SpawnExits()`). Órdenes rivales por `RivalPlans[|seed| % 6]`. | Para bots al azar hay que mintear rivales por semilla y sacar el gate del roster. |
| Resultado | `ArenaRound.End()` congela `PlayerSecured/RivalSecured`, calcula `Winner` y captura `ArenaRoundSummary` → `ArenaResultPanel`. **Nada se persiste.** | Falta un consumidor externo. |
| Identidad del resultado | `ArenaRoundStat.Name` = `CustomName`; `ArenaCastPlanner.PlanKey` y `ArenaCastPicker.IsPlanned` comparan por nombre. | Los nombres no son únicos. Las copias del save conservan `Timestamp`, así que `DNA.UniqueID` es el mismo que en el registro: usar ese. |
| Escenas | Build settings tiene **solo `GameScene`**. Cero `SceneManager` en el código. `GameManager` es singleton por `Awake` sin `DontDestroyOnLoad`; 18 archivos usan `GameManager.Instance` y 3 lo tienen como ref serializada de escena. | Mantener el núcleo vivo entre escenas rompería las refs serializadas al recargar la tienda: se descarta. La tienda se reinicia entera al volver. |
| Arranque de la tienda | `CloudSyncService.Start` → sign-in → `HandleSignedInAsync`: `SetUserScope` + `LoadInto` (disco) + `RegistryReloaded`/`InventoryReloaded` + **`PullAsync`**, y el pull **pisa el inventario local con el de la nube** (`CloudSyncOps.PullAsync:196-202`). | Si el resultado se aplica antes de que termine el pull, se pierde. El retorno debe esperar a que termine la sincronización de arranque. `InventoryChanged` solo guarda a disco; la nube recibe el inventario en el próximo push (cambio de registro o cierre). |
| Estado global que cruza | `PlayerController` deja el cursor bloqueado; `ArenaClockControl` puede dejar `Time.timeScale ≠ 1`; `ArenaSandbox` pone `runInBackground`; `ExpeditionRulesSO.Activate/Deactivate` ya se limpian en `OnEnable/OnDisable`. `RenderSettings` los repone la escena al cargar. | Al ir: desbloquear cursor. Al volver: `timeScale = 1`. |
| Needs en la tienda | Mutan por frame sin evento; solo se vuelcan en quit/pausa. | Antes de salir a la arena hay que volcarlos (`GameManager.FlushToCloud()` ya hace save + push). |

---

## 3 · Arquitectura del puente

```
GameScene (tienda)                                  ArenaSandbox
──────────────────                                  ────────────
DevToolsConsole "Salir de expedición (DEV)"
   └→ ExpeditionBridge.Depart()
        · GameManager.Instance.FlushToCloud()
        · cursor libre
        · ExpeditionHandoff.GoToArena()  ──────────▶  ArenaSandbox.Start → BuildRoom (LocalSave)
                                                       ArenaPlanPanel → picker (3 propios)
                                                       ¡A LA SALA! → ronda 90 s → End()
                                                       ArenaResultPanel + botón "Volver a la tienda"
ExpeditionBridge (Start, corrutina)                       └→ ExpeditionHandoff.ReturnToStore(result)
   · espera CloudSyncService.StartupSyncDone   ◀──────────     · Time.timeScale = 1
   · result = ExpeditionHandoff.ConsumeResult()                · LoadScene(GameScene)
   · inventory.AddAdventureMaterial(result.PlayerSecured)
   · GameEvents.InventoryChanged(inventory)  → GameManager guarda · InfoOverlay refresca el contador
```

**Capas:** `ExpeditionHandoff` (Core, estático puro: datos + navegación de escenas) · `ExpeditionBridge` (Systems/Expedition, MonoBehaviour de la tienda: salida y retorno) · la arena solo escribe en el handoff y nunca toca `GameManager`, `SaveSystem.Save*` ni la nube (invariante S102 de [[Index/23 - Arena Sandbox y Expedicion]] 5g se mantiene: `ArenaCastSource` lee y nunca escribe).

---

## 4 · Lotes de S119 (en orden; si falta tiempo se corta el 4)

### Lote 1 · Identidad (coder A + coder B)

- `ArenaRoundSummary.cs`: `ArenaRoundStat` gana `string Id` = `agent.DNA.UniqueID`. `Name` se conserva para la UI.
- `ArenaCastPlanner.cs`: `PlanKey(dna)` devuelve `dna.UniqueID` (cae a `CustomName` solo si está vacío).
- `ArenaCastPicker.cs`: `IsPlanned` compara por `UniqueID`, no por nombre.
- `SaveSystem.cs`: nuevo `public static Dictionary<string, CreatureDNA> LoadDatabaseCopy()`: lee `DbPath` (scoped; si no existe y hay scope, el no scoped, igual que `LoadInto`) y devuelve el diccionario deserializado o `null`. No toca el registro ni dispara eventos.
- `ArenaCastSource.cs`: `LoadLocal()` primero intenta `SaveSystem.LoadDatabaseCopy()`; solo si devuelve `null` cae al barrido "archivo más reciente" (flujo de desarrollo, Play directo en la arena). Sigue filtrando `IsDead` y ordenando por `Timestamp`.

### Lote 2 · Bots por semilla (coder A, mismo archivo que el lote 1)

- `ArenaCastPlanner.cs`, `Prepare(roomSeed, castSeed, freeCount)`:
  - Nueva propiedad `HasTeams => Mode == ArenaCastMode.LocalSave || HasRoster`.
  - Sin roster y modo `Roster` → elenco libre sin equipos (comportamiento actual, sandbox viejo).
  - Jugador: como hoy (selección del picker o `Pick` por `castSeed`; si el save está vacío → roster si hay, si no 3 minteados por `castSeed`).
  - **Rival en modo `LocalSave`:** 3 criaturas minteadas con `UnityEngine.Random.InitState(roomSeed)` (misma sala = mismos rivales en cualquier PC, regla 8.1), `Team = Rival`, órdenes de `RivalPlans[|roomSeed| % 6]` con `Clamp`. El `mint` ya asigna partes, color, `Role`, `Element`, diales 0,15-0,85, stats y nombre del banco.
  - Rival en modo `Roster`: como hoy (roster).
  - Después de mintear rivales, volver a `InitState(castSeed)` para no cambiar lo que sigue.
- `ArenaSandbox.cs`: `if (Planner.HasRoster) SpawnExits()` → `if (Planner.HasTeams) SpawnExits()`. Expone `HasTeams` si el HUD o el panel lo necesitan. `ManualSupers` sigue solo en el equipo del jugador.
- Escena `ArenaSandbox.unity` (**mutación, con OK**): `castMode` = `LocalSave`. `useRoster` queda `true` para poder volver al modo dev desde el panel.

### Lote 3 · Puente de escenas (coder C)

- **NUEVO** `Core/ExpeditionHandoff.cs` (estático):
  - `public struct ExpeditionResult { int Seed; ExpeditionTeam Winner; int PlayerSecured; int RivalSecured; List<ArenaRoundStat> Stats; }`
  - `bool CameFromStore`, `bool HasResult`, `ExpeditionResult Result`.
  - `GoToArena()`: `CameFromStore = true`, `HasResult = false`, `SceneManager.LoadScene("ArenaSandbox")`.
  - `ReturnToStore(ExpeditionResult? result)`: guarda el resultado si lo hay, `Time.timeScale = 1`, `SceneManager.LoadScene("GameScene")`.
  - `bool TryConsumeResult(out ExpeditionResult r)`: devuelve y limpia (`HasResult = false`, `CameFromStore = false`).
  - Los nombres de escena viven solo acá.
- **NUEVO** `Systems/Expedition/ExpeditionBridge.cs` (MonoBehaviour en `GameScene`, **mutación de escena con OK**): `[SerializeField] CloudSyncService cloudSync` (opcional).
  - `Depart()`: `GameManager.Instance.FlushToCloud()`, `Cursor.lockState = None`, `Cursor.visible = true`, `ExpeditionHandoff.GoToArena()`.
  - `Start()`: si `ExpeditionHandoff.HasResult`, corrutina: espera `cloudSync == null || cloudSync.StartupSyncDone` (con tope de 20 s por si el sign-in falla), luego `TryConsumeResult`, `inventory.AddAdventureMaterial(result.PlayerSecured)` (solo si > 0), `GameEvents.InventoryChanged(inventory)`, `Debug.Log` con sala, marcador y material.
- `CloudSyncService.cs`: `public bool StartupSyncDone { get; private set; }`, `true` al final de `HandleSignedInAsync` (después de `PullAsync`). Sin otros cambios.
- `DevToolsConsole.cs`: botón Odin `"Salir de expedición (DEV)"` en un `BoxGroup("Expedition (DEV)")`, con `[SerializeField] ExpeditionBridge bridge` → `bridge.Depart()`. Solo en Play.
- `ProjectSettings/EditorBuildSettings.asset` (**mutación con OK**): agregar `ArenaSandbox.unity` habilitada después de `GameScene`.

### Lote 4 · Retorno desde la arena (coder D)

- `ArenaPlanPanel.cs`: botón `btn-return` ("Volver a la tienda") visible solo si `ExpeditionHandoff.CameFromStore`. Al hacer clic: si hubo ronda terminada arma `ExpeditionResult` con `sandbox.ActiveSeed`, `pendingWinner`, `pendingMine`, `pendingTheirs` y una copia de `round.Summary`; si no, `null`. Llama `ExpeditionHandoff.ReturnToStore(result)`. El resultado de una ronda se entrega una sola vez (tras volver al plan, el pendiente se limpia).
- `UI Toolkit/ArenaPlanPanel.uxml` + `ArenaPlanPanelStyle.uss`: el botón en la barra de herramientas con la clase `.action` existente.
- `ArenaResultPanel.cs`: sin cambios (sigue mostrando `Name`).

### Fuera de alcance hoy (queda para S120+)

- Selector de equipo en la tienda, mueble/puerta con `PanelTrigger`, `UIPanelType.Expedition`, `SelectedIds` en el handoff.
- Costo de energía, heridas, muerte, material gastable (`SpendAdventureMaterial`).
- Localización del HUD y del panel de plan.

---

## 5 · Verificación en el editor (paso 8 del protocolo)

1. Compilar: `read_console` con 0 errores.
2. Play en `GameScene` → esperar sign-in → botón `Salir de expedición (DEV)` → carga `ArenaSandbox` con cursor libre; consola: `elenco=LocalSave` y `salidas=2` sin roster en uso.
3. En el panel de plan: `Mis MoriMonchis` muestra los del scope del jugador (contar contra `RegistryCount`); rivales con nombres del banco, 3, misma sala → mismos rivales al repetir la semilla (`Otra sala` cambia, `SetSeed` fija).
4. Jugar una ronda (reloj acelerado permitido) → resultado → `Volver a la tienda`.
5. En la tienda: `player_inventory_{scope}.json` con `AdventureMaterial` incrementado en lo asegurado; el overlay muestra el contador nuevo; consola con el log del puente; 0 errores; los MoriMonchis de la tienda reaparecen (spawner) y el registro no cambió (`RegistryCount` igual, sin `RegistryChanged` disparado por el puente).
6. Repetir ida y vuelta dos veces seguidas (sin duplicar `GameManager`, sin material duplicado por el pull).
7. Flujo de desarrollo intacto: Play directo en `ArenaSandbox` sin tienda sigue funcionando (sin scope → barrido del archivo más reciente; sin `CameFromStore` → sin botón de volver).

---

## 6 · Hoja de ruta de las sesiones siguientes

| Sesión | Objetivo | Piezas |
|---|---|---|
| **S120** | Elegir el equipo desde la tienda y salir por un lugar físico | `UIPanelType.Expedition` + `ExpeditionPanelUITK` (grid reutilizando `CreatureDisplay`/`MonchiPortraitUI`, hasta 3, excluye `IsDead`/`BusyState`), `ExpeditionHandoff.SelectedIds`, la arena salta el picker si vienen ids, mueble o puerta con `PanelTrigger` (patrón `Furniture3x3_Ring`), gate de energía mínima para salir. Aviso de vuelta ("Volviste de la sala NNNN: +N material") en el overlay. |
| **S121** | Que bajar cueste y que el rival se lea | Energía gastada al volver (mutación por `RegistryChanged`), decidir la pregunta ⭐ de 8.7 (¿un choque perdido cuesta la criatura o solo el material?), bots con partes y `Role` variados (bases S109) para que la lectura por tipos de parte tenga sentido, `SpendAdventureMaterial` y un primer uso del material en la tienda. |
| **S122+** | Lo que quedó de S118 | Bases por personalidad (`Role` abre dos de tres bases, panel de plan con dos píldoras), tres observaciones del clip (tarjetas simples en combate, anillos de la zonal, salto con impacto), sesión de feel a 1× de Juan. |
| Después | Rival real | Snapshots de otros jugadores por Cloud Save (patrón `project_async_combat`), ferales, botín genético (8.2, no decidido). |

---

## 6b · Estado al cierre de la ejecución S119 ✅

Los cuatro lotes quedaron implementados y verificados en Play: la tienda arrancó con 10 criaturas y 3 de material; la arena leyó esos 10 del scope del jugador, minteó 3 rivales y creó 2 salidas; la ronda terminó 30-18 y la vuelta dejó el material en 33, en memoria y en disco, con un solo `GameManager` y `timeScale` 1. Una segunda ida y vuelta sin jugar no duplicó nada. La arena abierta sola sigue en modo desarrollo, sin botón de volver. Corrección propia tras la prueba: `ReturnToStore(null)` apaga `CameFromStore`. Los rivales minteados de una misma sala comparten `Timestamp`, así que su `UniqueID` solo difiere por genes.

## 6c · Estado de S120-S122 (misma sesión, ejecutado de corrido) ✅

**S120 · Panel de bajada.** `ExpeditionPanelUITK` (`UIPanelType.Expedition` = 8, tema nocturno, textos en la tabla `Strings` en/es): lista el registro sin muertos ni vendidos, elegible = libre y energía ≥ 30, hasta 3, teclado y clic. La terminal `WORLD/Props/PanelUI (2)` de `GameScene` (antes abría el combate Dragon RPS fallido, que sigue en la consola dev) abre la bajada. `ExpeditionBridge.RequestDeparture(ids)` por evento estático → `ExpeditionHandoff.SelectedIds` → la arena bloquea ese elenco (oculta Elenco/Elegir/Otros 3) y fuerza sala nueva por bajada. Rivales minteados con `Timestamp + i + 1` (IDs únicos).
**S121 · Costo de bajar.** Al volver, cada criatura propia gasta `20 + 5 × tumbadas` de energía (tope 40), un solo `RegistryChanged`; `GameEvents.OnExpeditionReturned(ExpeditionReturn)` → aviso de 6 s en `InfoOverlayUITK` ("Volviste de la sala N · a-b · +M material · −E energía"). Supuesto sin respuesta ⭐ de 8.7: perder no cuesta la criatura. Sin uso del material en la tienda todavía (decisión de diseño de Juan).
**Medido:** 10 criaturas (6 elegibles, 4 cansadas bloqueadas) → 3 elegidas → arena con esas 3 y sala 20234078 → 5-5 con 6/1/5 tumbadas → material 33 → 38 y energía 46/40/30 → 4/14/0, aviso con texto correcto, sin excepciones.

**S122 · Observaciones del clip S118.** (a) `ArenaHudCard` estado `hud-card--fighting` (choque, tell o mareado, con 1,2 s de gracia): opacidad 0,7 y sin órdenes, carga ni etiquetas de poder; los radiales siguen clickeables. (b) Golpes zonales (Back/Wings) ya no dibujan `ImpactRing` centrado en la víctima, y `AbilityBursts` omite las habilidades de daño: queda solo el destello del borde de la plantilla. (c) `MoriMochiAgent.onDiveLaunch` (despegue de la picada) y `onDiveSlam` (toca el suelo, pegue o no); prefab `MorimonchiAgent/Feedbacks/OnDiveLaunch` (humo) y `OnDiveSlam` (polvo de suelo + piedras), conectados por `PlayFeedbacks`. Medido con picada forzada: 1 despegue, 1 golpe.

**S122 · Bases por personalidad (diseño S118).** `ArenaBases` (estática) + `enum ArenaBase`. El `Role` abre dos bases; la variante sale del Role y se ejecuta con las órdenes existentes:

| Role | Base | Variante | Órdenes |
|---|---|---|---|
| Protector | Territorio | Dominante | guardián del centro |
| Protector | Rebusque | Custodio (por defecto) | recolector de la veta cercana |
| Agresivo | Territorio | Invasor (por defecto) | cazador del centro |
| Agresivo | Oportunismo | Hiena | señuelo de las vetas |
| Empático | Rebusque | Compañera (por defecto) | recolectora del centro |
| Empático | Oportunismo | Gaviota | cazadora de las vetas |

`ArenaOrderRules.Clamp` ajusta a una base abierta del Role; los diales ya no bloquean (`IsLocked`, `UnlockRead`, `LockReason` borrados; los diales siguen afinando visión, choque y social). Panel de plan: una fila BASE con tres píldoras (12 px) y la cerrada con su razón; rival leído como "Protector · Territorio o Rebusque". Rivales por semilla entre sus dos bases. `ArenaRosterSO.Entry.Role` (Osado/Fiero Agresivo, Tímida/Cauta Empático, Equilibrado/Templado Protector). `ArenaMatrixDev` asigna el Role que corresponde a las órdenes del plan.

**Pendiente y abierto para Juan:** (1) el mapeo de variantes a órdenes es v1 del orquestador; (2) regresión de balance con `ArenaMatrixDev` sin correr (cada ronda tarda ~1 min real en el editor); (3) en dos rondas naturales no salió ninguna picada: la súper exige rival entre `MinDistance` 4 y el alcance del movimiento, ajuste previo a esta sesión; (4) glifo de base bajo la criatura sin hacer; (5) uso del material en la tienda.

## 6d · Revisión S123 del rango `ca9fead..bfbe258` (solo lectura, sin código)

**Lo que está bien (verificado en el diff y en las firmas que consume):**
- `ExpeditionHandoff`: estado estático con reset por `SubsystemRegistration`, `timeScale` 1 al volver, `CameFromStore` apagado al volver sin resultado y al consumir. `ExpeditionBridge`: espera `StartupSyncDone` antes de tocar inventario y registro (el pull pisa ambos), un solo `RegistryChanged` y un `InventoryChanged`, costo `min(40, 20 + 5·tumbadas)`, salta muertas e ids ajenos. Regla 2 respetada: `FlushToCloud` es API pública del dueño de persistencia.
- `ExpeditionPanelUITK`: suscribe/desuscribe en `OnEnable`/`OnDisable`, botones desconectados en `OnDestroy`, navegable registrado. `UIManager` muestra paneles por `display` del `UIDocument` (no `SetActive`), así que `OnPanelSet` sí llega y `Rebuild` corre en cada apertura; el diccionario Odin solo necesita la entrada `Expedition` que ya se cableó en S120.
- Identidad: `PlanKey`/`IsPlanned`/`ArenaRoundStat.Id` por `UniqueID`; `UniqueID` se calcula en caliente desde `Timestamp`, así que los rivales con `Timestamp + i + 1` sí tienen IDs distintos (cierra la nota S119.1).
- `ArenaBases`: 6 combos de órdenes únicos, `RoleFor` no es ambiguo, `RivalRead` conjuga "u" bien. Sin comentarios, sin partials, Odin correcto.

**Hay que cambiar:**
1. **Bug (regresión de matriz sesgada).** `ArenaMatrixDev` fija el `Role` solo si `RoleFor` acierta y luego `Sandbox.SetOrders` → `Planner.SetOrders` → `ArenaOrderRules.Clamp`, que cae a la base por defecto cuando las órdenes no son una variante del Role. Los 2 combos sin variante (Big·Flee·Aggressive y Small·Fight·Protect) se cambian en silencio: la matriz mediría otra cosa. Arreglo: que el camino dev (`SetOrders`) no clampee (solo `SetPlayerOrders`), o que la matriz descarte esos combos explícitamente.
2. **Riesgo de datos al salir.** `Depart` llama `FlushToCloud` (push fire-and-forget: validar + guardar, dos viajes) y carga la escena en el mismo cuadro; `PullAsync` al volver hace `LoadFrom` directo (la nube gana sin comparar fechas). Si ese push falla (offline o `ValidateBeforePush` falso por otro dispositivo), la vuelta pisa lo hecho en la tienda desde el último push bueno. Arreglo chico: `FlushToCloud` devuelve la `Task` y el bridge la espera con tope (~5 s) antes de `GoToArena`.
3. **Feel sin nube.** `StartupSyncDone` solo se enciende dentro de `HandleSignedInAsync`; sin sesión el bridge espera los 20 s del tope y el aviso llega tarde. Encenderlo también cuando `InitializeAsync` termina sin sesión.
4. **Limpieza.** `ExpeditionRulesSO` `BoldFightLock`/`ShyFleeLock`/`SocialProtectLock`/`LonerAggressiveLock` ya no tienen lector; `ArenaOrderRules.Clamp(dna, rules, o)` y `ArenaOrderCatalog.PersonalityName(dna, rules)` cargan `rules` sin usarlo. `ArenaCastPlanner.Prepare` re-siembra `Random` con `castSeed` después de los rivales (reinicia la secuencia, no la restaura: inofensivo hoy). `ArenaBases` suma ~20 strings en español a la deuda de localización (igual que `ArenaOrderCatalog`).
5. **Menor.** `ExpeditionPanelUITK` no escucha `RegistryReloaded`: si el pull llega con el panel abierto, la lista queda vieja hasta reabrir.

**Decisiones de Juan:**
1. ¿Una ronda por bajada (ocultar Play tras la ronda si `CameFromStore`) o varias? Hoy `lastResult` se pisa en cada `Play` y la energía se cobra una sola vez al volver: jugar 3 rondas cuesta lo mismo que 1.
2. Mapeo v1: Gaviota (Empático·Oportunismo) = Small·Fight·Aggressive es un Empático que pelea con postura agresiva, y Compañera = Big·Flee·Protect. ¿Se sostiene o se invierte con Hiena (Small·Flee·Aggressive)?
3. ¿Los 2 combos sin variante desaparecen del juego (y de la matriz) o alguna variante los toma?

## 7 · Riesgos conocidos

- El pull de la nube al volver tarda unos segundos: el material aparece cuando termina (`StartupSyncDone`). Si el sign-in falla, el tope de 20 s aplica igual y el push queda para el próximo cambio de registro o el cierre.
- `Resources.UnloadUnusedAssets` al cambiar de escena puede descargar SOs sin referencia; el registro y el inventario los re-referencia `GameManager` al recargar la tienda, y la arena no los usa (lee del disco).
- El NavMesh de la arena se hornea en runtime con `PhysicsColliders`; en build harían falta colliders primitivos (pendiente viejo, no bloquea el editor).
- `ArenaMatrixDev` asume roster: correr las matrices en modo `Roster` como hasta ahora.

## 8 · Bajada por pisos (diseño en `Index/22` Parte 9, S123)

> **Plan ejecutable detallado (contratos, lotes, verificación) en [[Index/26 - Plan H0 - Bajada por pisos]].** Esta sección es el resumen.
>
> **✅ S124: lotes A-E implementados y verificados en Play** (bajada ganada de 3 pisos con buffo y bajada perdida). Desvíos: ver `Index/26` §8; mediciones y pendientes en `09 - Active Context` S124.

**Regla del cambio:** la arena deja de ser una ronda y pasa a ser una run de pisos; el puente aplica el total de la run, no el de una ronda.

| Lote | Responsabilidad | Piezas |
|---|---|---|
| **A · Run** | Estado de la bajada dentro de la arena | `ArenaRun` (clase pura, `Data/Expedition`): semilla base, piso actual, `FloorSeed(n)`, `FloorKind(n)` (buffo cada 3), botín acumulado, energía por `UniqueID`, `Lost`. `ExpeditionResult` gana `Floors`, `Lost` y `Energy` por id; `ExpeditionReturn` gana `Floors`/`Lost`. |
| **B · Piso de buffo** | Sala sin rival | `ArenaSandbox` con `ArenaFloorKind`: en `Buff` no mintea rivales ni salidas rivales, vetas solo del jugador, un `MaterialPickup` de energía en el centro (`EnergyCrystal`, reusa el pickup con un `Kind`); la ronda termina al vaciar la sala o por tiempo. |
| **C · Panel entre pisos** | Decisión seguir / retirarse | `ArenaPlanPanel`: tras la ronda muestra resultado del piso, acumulado, vista previa del siguiente (`FloorKind` + lectura del rival) y botones **Seguir** (nueva sala con `FloorSeed(n+1)`) y **Retirarse**; al perder, solo **Volver** con "Perdiste en el piso N". `Play` desaparece cuando `CameFromStore`. |
| **D · Puente** | Aplicar la run | `ExpeditionBridge`: material = acumulado si retiró, 0 si perdió; energía por id desde la run (buffo resta); `permadeathEnabled` (false) mata la terna al perder; aviso del overlay con pisos y resultado. |
| **E · Arreglos §6d** | Antes de todo | Quitar los 2 combos huérfanos de `ArenaMatrixDev` y que el camino dev no clampee; `FlushToCloud` devuelve `Task` y `Depart` la espera (tope 5 s); `StartupSyncDone` también sin sesión. |

Si falta tiempo se corta **B** (la run funciona con solo pisos de enemigos). Muta fuera de código: `ArenaPlanPanel.uxml/uss` (botones), `Strings` en/es (vista previa, perdiste), escena `ArenaSandbox` (cristal de energía), tabla del overlay.

**Verificación:** bajada de 3 pisos desde la tienda (enemigos · enemigos · buffo) y retirada → material = suma, energía = 2 costos − 30; bajada perdida en el piso 2 → material +0, energía cobrada, terna viva (flag apagado); matriz `ArenaMatrixDev` con 6 combos sin cambios silenciosos.

