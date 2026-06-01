# RunRunSimulator — MoriMonchis — CLAUDE.md

## 📚 Fuente de Verdad — Leer SIEMPRE primero

> **Antes de cualquier sesión de trabajo, consultar el Notion Wiki.** Contiene el GDD completo, todas las decisiones de diseño tomadas, el roadmap actualizado y las preguntas de diseño abiertas. Este CLAUDE.md es un resumen de referencia rápida; el Notion es el documento vivo y autoritativo.

| Recurso | URL |
|---------|-----|
| 🟣 **Notion Wiki (Hub)** | https://www.notion.so/36cac10136a781819b74e176ed7c00d9 |

**Estructura del Notion** — el hub se divide en dos pilares + dos páginas transversales. Regla mental: **diseño/mecánicas → Gameplay; cómo está construido → Arquitectura.**

- 🎮 **Gameplay (GDD)** — el QUÉ: Concepto y Pilares · Sistema Genético (Diseño) · Breeding · Combate, Venganza y Bidding · Evolución y Ciclo de Vida · Honorarios / Liga del Cielo · Tienda, Economía y Onboarding
- 🏗️ **Arquitectura (Dev)** — el CÓMO: Arquitectura General · Genética — Implementación · Identidad y Persistencia · Breeding — Implementación · Combate Local — Implementación · Combate Async + UGS · UGS CLI & Scheduler
- 📋 **Decisiones de Diseño** — registro consolidado de decisiones cerradas, agrupadas por sistema
- ❓ **Preguntas Abiertas** — solo lo aún sin resolver

**Qué sub-página abrir según la tarea:**
- Genética/DNA/partes → *Sistema Genético (Diseño)* (diseño) + *Genética — Implementación* (código)
- Breeding → *Breeding* (diseño) + *Breeding — Implementación* (código)
- Batalla/combate → *Combate, Venganza y Bidding* (diseño) + *Combate Local — Implementación* + *Combate Async + UGS* (código)
- UGS/cloud/scheduler → *Combate Async + UGS* + *UGS CLI & Scheduler*
- Tienda/economía/onboarding → *Tienda, Economía y Onboarding*
- Persistencia/save/registry → *Identidad y Persistencia*
- Lore → *Concepto y Pilares* + *Honorarios / Liga del Cielo*
- Decisiones ya tomadas → *Decisiones de Diseño*
- Preguntas aún abiertas → *Preguntas Abiertas*

> Al resolver una pregunta de *Preguntas Abiertas*, moverla a *Decisiones de Diseño*. Al cambiar diseño, actualizar la sub-página de Gameplay correspondiente; al cambiar implementación, la de Arquitectura.

---

## Selección de modelo

Antes de comenzar cualquier tarea, evaluar si el modelo actual (Sonnet) es adecuado. **Avisar al usuario si se recomienda cambiar a Opus** antes de proceder.

Cambiar a Opus cuando la tarea implique:
- Diseño de sistemas nuevos desde cero con muchas decisiones interconectadas (economía, tienda, meta-game)
- Arquitectura que afecte múltiples etapas del roadmap simultáneamente
- Análisis de trade-offs complejos sin respuesta obvia

Sonnet es suficiente para:
- Implementación de features concretas (scripts, refactoring, bugfixes)
- Trabajo dentro de sistemas ya diseñados
- Tareas con requisitos claros y acotados

---

## Qué es este proyecto
Simulador de tienda retro 3D ambientado en los años 80s. El jugador es el dueño de una tienda que se sumó a la tendencia "MoriMonchis": criaturas biológicas del tamaño de la palma de la mano (estética Gremlins + Furby + Tamagotchi). En la trastienda opera un club de peleas clandestino asíncrono. Referencia de género: los *Simulator games* del mercado actual (PowerWash Simulator, etc.) pero con genética y muerte permanente.

## Nombre oficial de las criaturas
- **Singular**: MoriMochi
- **Plural**: MoriMonchis
- En código interno usar `Creature` / `CreatureDNA` para generalidad. En UI, logs visibles al jugador y naming de assets usar MoriMochi/MoriMonchis.

## Stack Técnico
- **Motor**: Unity 3D (C#)
- **Inspector**: Odin Inspector — SIEMPRE usar `SerializedScriptableObject`, `[OdinSerialize]` para Diccionarios, y atributos de Odin para UI de editor (`[Title]`, `[BoxGroup]`, `[Button]`, `[TableList]`, `[Searchable]`, etc.)
- **Serialización**: Newtonsoft.Json — package `com.unity.nuget.newtonsoft-json` requerido para persistencia de criaturas
- **Backend**: Unity Gaming Services (UGS) — Authentication (Player Accounts), Cloud Save (Player Data + Custom Data), Cloud Code (JS scripts), Scheduler (cron triggers)
- **Dev tooling**: UGS CLI (`ugs`) — binario standalone para desplegar schedules. Service Account con roles `Unity Environments Admin` + `Cloud Code Editor/Viewer/Publisher` a nivel project, y `Owner` a nivel organization
- **Arte**: 3D, partes como FBX, ensamblaje con anchor points

---

## Arquitectura actual

```
CloudCode/                            # Server-side scripts y schedules (fuera del build Unity, se suben al Dashboard via CLI o manualmente)
├── enqueue-combat.js                 # Cloud Code: append entry a matchmaking_pool. Returns {status,poolSize}
├── dequeue-combat.js                 # Cloud Code: remueve una criatura del pool por creatureId+playerId. Returns {status:"dequeued"|"not_found"}
├── get-queue-status.js               # Cloud Code: devuelve los creatureIds del caller que están realmente en el pool. 1 read, sin params. Para reconciliar "fantasmas" (local Queued pero ausente del pool)
├── process-matchmaking.js            # Cloud Code (scheduled): drena pool, empareja, simula, escribe combat_results en cada player
├── run-combat.js                     # Cloud Code (legacy modo Instant): enqueue + match + simulate en una sola llamada
├── start-breeding.js                 # Cloud Code: appendea huevo con server-time al array breeding_eggs_<playerId>. BREED_DURATION_MS hardcoded. Varias parejas en paralelo
├── hatch-breeding.js                 # Cloud Code: chequea reloj server vs readyAt para el par (motherId,fatherId). Returns {status:"ready"|"not_ready"|"no_egg"}
├── test-random.js                    # Diagnostic: returns random 1-4
├── test-customdata.js                # Diagnostic: read/write/read isolated en Custom Data
├── matchmaking-tick.sched            # UGS Scheduler: cron "0 * * * *" → emite evento "process-matchmaking.v1"
└── matchmaking-trigger.tr            # UGS Trigger: escucha el evento del scheduler → invoca el script process-matchmaking

Assets/RunRunSimulator/Scripts/
├── Core/                             # Infraestructura transversal: bus, persistencia, generación, tipos base
│   ├── GameManager.cs                # Lab: Generate / Mint + SOURCE OF TRUTH de los assets compartidos (getters Registry/Database/RarityOddsTable/InheritanceOddsTable/CombatConfig). ÚNICO dueño de persistencia: escucha OnRegistryChanged → Persist (save+push)
│   ├── GameEvents.cs                 # static: bus de eventos. OnRegistryChanged(registry) / OnRegistryReloaded(registry) / OnCreatureMinted / OnCombatCompleted / OnBreedingCompleted. Los eventos transportan la data
│   ├── SaveSystem.cs                 # static: SaveDatabase / LoadInto / Serialize (scoped por playerId, migración automática)
│   ├── CreatureGenerator.cs          # static: GenerateRandom(db, oddsTable?)
│   ├── Enums.cs                      # Rarity, PartSet, CreatureGender, PartRole, Tier, BusyReason, UIPanelType (None/CreatureGrid/MorimonchiDetail), PlayerStateType (None/Exploring/Menu)
│   └── Interfaces.cs                 # IThrowable (grab/hold/throw) · IInteractable (tap E para interactuar) · IUINavigable (OnUINavigate/OnUISubmit — panel que recibe input ruteado mientras es el tope del stack)
│                                     # (UIEvents.cs ELIMINADO — sus eventos viven ahora en UIManager)
├── Systems/                          # Mecánicas por feature (cada una desacoplada vía GameEvents)
│   ├── Breeding/
│   │   ├── BreedingService.cs        # static: Breed() — traversal árbol genealógico
│   │   ├── BreedingController.cs     # MonoBehaviour: UI breeding (Fill Random Breeders + Breed local + Breed Timer + Hatch). Referencia GameManager. Espejo de CombatController
│   │   └── AsyncBreedingService.cs   # MonoBehaviour: StartBreedingAsync / HatchAsync (timer server-side). Referencia GameManager. Espejo de AsyncCombatService
│   ├── Combat/
│   │   ├── CombatService.cs          # static: Simulate() — combate local por turnos, evolución, muerte
│   │   ├── CombatController.cs       # MonoBehaviour: UI local combat + Async Combat (Instant + Timer buttons). Referencia GameManager
│   │   └── AsyncCombatService.cs     # MonoBehaviour: EnqueueInstantAsync / EnqueueScheduledAsync / PollResultsAsync. Referencia GameManager
│   └── Cloud/
│       ├── CloudSyncService.cs       # MonoBehaviour: Unity Player Account auth + auto-pull on login + Cloud Save push/pull/reset + SyncMeta. Referencia GameManager
│       └── CloudCodeTester.cs        # MonoBehaviour DEV: TestRandom / TestCustomData / ForceMatchmakingTick
├── UI/                               # Capa visual (Canvas/uGUI + UI Toolkit) + vistas de inspector. Solo display, se nutre del payload de eventos
│   ├── UIManager.cs                  # SerializedMonoBehaviour (Odin): Dictionary<UIPanelType, GameObject> editable. HUB de eventos UI (static) + único que togglea paneles. Oculta UITK por `display` (no SetActive) para que sigan actualizándose; uGUI por SetActive. Todos arrancan ocultos en Start. Emite OnUIFocusChanged. STACK ordenado de paneles (tope=foco) + ROUTER de input: único suscriptor de UIInputs, despacha Navigate/Submit al tope (IUINavigable) y ESC=pop. RegisterNavigable/UnregisterNavigable. Vive en escena
│   ├── UIInputs.cs                   # MonoBehaviour: dueño ÚNICO del action map "UI" (espejo de PlayerInputs). Static events NavigatePressed (stepped) / SubmitPressed / CancelPressed. Habilita el mapa UI solo con foco (escucha OnUIFocusChanged). Vive en el objeto del UIManager. Gamepad gratis
│   ├── CreatureGridUI.cs             # MonoBehaviour (uGUI): grilla in-game (Canvas). Escucha OnRegistryChanged/Reloaded → instancia una CreatureVisualUI por criatura bajo un GridLayoutGroup. NO referencia GameManager
│   ├── CreatureVisualUI.cs           # MonoBehaviour: card de UN MoriMochi (prefab uGUI). Bind(dna) → nombre (TMP) + icono/sprite (Image, teñido con PrimaryColor) + estado (TMP). Vista pura
│   ├── CreatureGridUITK.cs           # MonoBehaviour (UI Toolkit) + IUINavigable: gemelo de CreatureGridUI. Vive en el objeto del UIManager (siempre activo), referencia un UIDocument (separado) y un VisualTreeAsset (card). Clona cards, las hace clicables (→ UIManager.SelectCreature) y cablea el botón cerrar. cardSize configurable. Navegación 2D por teclado/mando (columnas medidas del layout) + highlight (.card--selected) + auto-scroll (ScrollTo) + Enter abre el detalle de la card seleccionada
│   ├── MorimonchiDetailInfoUITK.cs   # MonoBehaviour (UI Toolkit) + IUINavigable: ventana de detalle modal (estilo FireRed). Escucha UIManager.OnCreatureSelected → puebla (stats efectivos vía CombatService.GetEffectiveStats, partes vía CreatureDatabaseSO) → RequestPanelSet(true). sortingOrder alto + backdrop full-screen = modal sobre la grilla. A/D cambia de tab. Siempre abre en Info. Tabs Info/Combate/Breed/Linaje/Equipo (Info activa, resto placeholder)
│   └── CreatureGridView.cs           # MonoBehaviour: grilla read-only de inspector (Odin TableList) de todo el registro. Se suscribe a OnRegistryChanged/Reloaded → auto-refresh desde el payload (NO referencia GameManager)
├── Player/                           # Jugador primera persona — responsabilidades separadas, comunicadas por static events (sin referencias cruzadas)
│   ├── PlayerInputs.cs               # Dueño del action map "Player" (UIInputs es dueño del "UI"). Dispara static events: MoveChanged / Jumped / InteractPressed / InteractReleased / ThrowPressed. Deshabilita el mapa Player mientras hay foco UI (escucha OnUIFocusChanged) → en menú no llega input de gameplay ni se puede cerrar con E
│   ├── PlayerController.cs           # Solo lógica: move FP (CharacterController) + grab/throw + state machine (PlayerStateType). Referencia una CinemachineCamera: de ahí saca el transform (forward) y el CinemachineInputAxisController (lo desactiva para congelar la cámara en Menu). Escucha UIManager.OnUIFocusChanged → Menu (cursor libre, sin control) / Exploring. Tap E = interactuar / Hold E = agarrar / press E cargando = soltar / Click = lanzar. NO referencia PlayerInputs
│   ├── PlayerAnimator.cs             # Solo animación: se suscribe a los static events. Inerte hasta asignar un Animator
│   ├── FirstPersonController.cs      # (referencia de proyecto viejo, sin usar)
│   └── ThirdPersonController.cs      # (referencia de proyecto viejo, sin usar)
├── Interactables/                    # Objetos del mundo con comportamiento componible (drop-a-script). Requieren Collider para el raycast
│   ├── ThrowableObject.cs           # IThrowable: Rigidbody que el player sostiene (follow por velocidad) y lanza. RequireComponent(Rigidbody)
│   └── PanelTrigger.cs              # IInteractable: al hacer tap E dispara UIManager.RequestPanelToggle(su UIPanelType) (static). No conoce instancia del UIManager
├── Data/
│   ├── CreatureDNA.cs                # Genética + Identidad + Linaje + Progresión + Tier/slot + Stats + IsDead
│   ├── CreatureRegistrySO.cs         # SO registry: Dictionary<string, CreatureDNA> — InfoBox warning + Sync btn
│   ├── CreatureDatabaseSO.cs         # SO orquestador: refs sub-DBs + validación de IDs
│   ├── CreaturePartData.cs
│   ├── PartNameBank.cs               # static: pools de nombres por (PartSet, PartRole)
│   ├── RarityOddsTableSO.cs          # SO: pesos por Rarity → Roll() independiente por slot
│   ├── InheritanceOddsTableSO.cs     # SO singleton: odds breeding — pesos configurables en inspector (sin JSON)
│   ├── CombatManagerSO.cs            # SO singleton: EvolutionChance, DeathChance, CritChance, MaxRounds, MaxFightCount(5)
│   ├── CombatResult.cs               # Data class: WinnerID, LoserID, Log, LoserDied, WinnerEvolved, IsDraw
│   ├── Parts/
│   │   ├── BodyPart.cs               # abstract SO: ID[ReadOnly], Name, Rarity, Tier, Set + HP/Attack/Speed
│   │   ├── ArmPart.cs                # GetPartRole() = Arm
│   │   ├── EyePart.cs                # GetPartRole() = Eye
│   │   ├── MouthPart.cs              # GetPartRole() = Mouth
│   │   └── BodyShapePart.cs          # GetPartRole() = Body
│   └── Databases/
│       ├── PartDatabaseSO.cs         # abstract generic: IDPrefix + Sync All IDs + Roll All Names
│       ├── ArmDatabaseSO.cs          # IDPrefix = "A"  → A0, A1, A2…
│       ├── EyeDatabaseSO.cs          # IDPrefix = "E"  → E0, E1, E2…
│       ├── MouthDatabaseSO.cs        # IDPrefix = "M"  → M0, M1, M2…
│       └── BodyShapeDatabaseSO.cs    # IDPrefix = "BS" → BS0, BS1, BS2…
```

---

## Roadmap

| Etapa | Sub-etapa | Estado |
|-------|-----------|--------|
| 1 | 1.1 Arquitectura genética + DNA string + Databases de partes | ✅ Completo |
| 1 | 1.2 Visualizador de criaturas | 🔶 Grilla de inspector ✅ (`CreatureGridView` — nombre, color, género, stats, fights/breeds, padres por nombre, estado, nacimiento). Falta el visualizador 3D (leer DNA → ensamblar Prefab) |
| 1 | 1.3 Sistema de Breeding (herencia, linaje, registro, persistencia) | 🔶 En progreso |
| 2 | 2.1 Sistema de Estadísticas (HP, Fuerza, Velocidad desde partes) | 🔶 Iniciado — BaseStats en DNA + stats por pieza en BodyPart SO |
| 2 | 2.2 Simulador de Batalla local → Battle Log | ✅ — CombatService completo: turnos, empate, límite de peleas, log detallado por turno |
| 2 | 2.3 Integración Unity Services (async battles) | ✅ — Auth + Cloud Save (push/pull/auto-sync) + Cloud Code (enqueue/dequeue/process-matchmaking) + Scheduler+Trigger (cron 1h funcionando) + modo Instant + Busy persistente |
| 2 | 2.4 Breeding Async (timer server-side) | ✅ — start-breeding/hatch-breeding (Game Data) + AsyncBreedingService + Breed Timer/Hatch en BreedingController. Cría minteada local (checkpoint) |
| 3 | 3.1 Tienda Local (NPCs, inventario, vitrinas) | 🔲 Pendiente |
| 3 | 3.2 Mercado Online (P2P via Unity Services) | 🔲 Pendiente |

**Etapa 1.3 — estado detallado:**

| Feature | Estado |
|---------|--------|
| `BreedingService.Breed()` con traversal genealógico | ✅ |
| `InheritanceOddsTableSO` SO puro (pesos configurables en inspector) | ✅ |
| `CreatureRegistrySO` registry visual [ReadOnly] + JSON source of truth | ✅ |
| `SaveSystem` persistencia JSON completa | ✅ |
| `GameManager.MintRandomCreature()` y `BreedCreatures()` | ✅ |
| Validación límite máximo de crías (4) — `BreedingService.MaxBreedCount` | ✅ |
| Validación límite máximo de combates (5) — `CombatManagerSO.MaxFightCount` | ✅ |
| `IsDead` bloquea breed y combate (validado en BreedingService y CombatService) | ✅ |
| Herencia de stats en Breed: 50/50 madre/padre + delta ±1, mínimo 1 | ✅ |
| GameManager: Fill Random Breeders + Fill Random Fighters con info de límites | ✅ |
| Breeding async con timer server-side (start-breeding/hatch-breeding) | ✅ (Etapa 2.4) |
| `IsBusy` bloquea breed y combate (padre incubando no disponible) | ✅ |
| Género por battle-index del padre (actualmente 50/50) | 🔲 Pendiente (Etapa 2) |
| Bonus de rareza en la 4ª cría (última posible) | 🔲 Pendiente |
| Herencia del nivel Tier de las partes | 🔲 Pendiente |

---

## Reglas de código

1. **Desacoplamiento estricto vía eventos**: cada sistema (genética, batalla, tienda) es independiente. La comunicación cross-sistema pasa por `GameEvents` (bus estático), nunca por referencias directas ni llamadas a singletons del otro sistema. **Regla de oro de eventos: el evento transporta la data.** Un suscriptor recibe el `registry` (u otro payload) en el evento y trabaja sobre él — NO vuelve a buscarlo con `GameManager.Instance.Registry`. Si un suscriptor necesita el dato, va en el payload.
2. **Persistencia solo por evento**: ningún script de gameplay llama `SaveSystem.SaveDatabase` ni `PushToCloud` directamente. Disparan `GameEvents.RegistryChanged(registry)` y `GameManager` (único dueño de persistencia) hace el save+push. Excepción: `CloudSyncService` (es la capa de sync) y el flush final en `GameManager.OnApplicationQuit`. Reload externo (cloud pull/reset) usa `OnRegistryReloaded` → solo UI, sin re-push.
3. **No comentar el QUÉ**: solo comentar el POR QUÉ cuando hay un invariante no obvio.
4. **Sin features adelantadas**: no implementar UGS ni mecánicas de batalla hasta Etapa 2. La persistencia local JSON es válida desde Etapa 1.3.
5. **DNA como string ligero**: `CreatureDNA.ToStringID()` / `FromID()` son el contrato de red — no romperlo. El timestamp es metadata de registro, no forma parte del genetic string.
6. **IDs de partes**: nunca pueden contener el carácter `-` (es el separador del DNA string).
7. **Odin siempre**: cualquier ScriptableObject con Diccionarios hereda de `SerializedScriptableObject`. Usar `[OdinSerialize]` explícitamente.
8. **Sin complejidad innecesaria**: no añadir campos, abstracciones ni features que no hayan sido pedidos explícitamente. Tres líneas similares son mejor que una abstracción prematura.
9. **Desuscribir siempre**: todo MonoBehaviour que se suscribe a un `GameEvents` lo hace en `OnEnable` y se desuscribe en `OnDisable`. Un `event static` mantiene vivo al suscriptor (leak + excepción al disparar sobre un objeto destruido).

---

## Arquitectura orientada a eventos (GameEvents)

Bus estático central (`GameEvents.cs`, namespace global). Publicadores y suscriptores dependen **solo del bus**, nunca uno del otro. Razón clave: un `event` de C# solo lo puede disparar la clase que lo declara → un bus neutral permite que *cualquiera* dispare y *cualquiera* escuche, y el suscriptor no necesita referenciar al publicador.

**Filosofía: los eventos transportan la data.** El payload lleva lo que el suscriptor necesita (el `registry`, el `CombatResult`, etc.) para que no tenga que volver a buscarlo en un singleton.

| Evento | Payload | Quién dispara | Quién escucha |
|--------|---------|---------------|---------------|
| `OnRegistryChanged` | `CreatureRegistrySO` | toda mutación de gameplay (mint, breed, combate, enqueue/dequeue, hatch) | `GameManager.Persist` → save+push · `CreatureGridView` → refresh |
| `OnRegistryReloaded` | `CreatureRegistrySO` | `CloudSyncService` tras pull/reset | `CreatureGridView` → refresh (**solo UI, sin push** — la data vino del cloud) |
| `OnCreatureMinted` | `CreatureDNA` | `GameManager.MintRandomCreature` | (hook libre: logging/UI futuro) |
| `OnCombatCompleted` | `CombatResult` | `CombatController` (combate local) | (hook libre: battle-log UI) |
| `OnBreedingCompleted` | `mother, father, child` | `BreedingController` + `AsyncBreedingService.HatchLocally` | (hook libre) |

- **Helper estático por evento**: `RegistryChanged(so) => OnRegistryChanged?.Invoke(so)` — call site corto, sin `?.Invoke` repetido.
- **Un solo evento de mutación con payload** (no dos en paralelo): evita disparar dos veces por mutación.
- **Desuscribir en `OnDisable`** (ver regla 9).
- **Cambio de comportamiento**: el combate local ahora también pushea al cloud (antes solo guardaba local), al pasar por `OnRegistryChanged`. El push es no-op si no hay sesión.
- **Gap conocido**: el path async (`PollResultsAsync` aplica `CloudCombatResult`) NO dispara `OnCombatCompleted`. Un futuro battle-log que escuche ese evento se perdería los combates async — habría que mapear `CloudCombatResult`→`CombatResult` o agregar un evento dedicado.

---

## Sistema de Nombres de Criaturas (CreatureNameBank)

- `CreatureNameBank.cs` — clase estática. Nombre = adjetivo + sustantivo ("Fuzzy Blob"), estética gross/retro (Gremlins/Furby).
- Pools: **50 adjetivos × 50 sustantivos = 2500 combinaciones**.
- `GetRandomName()` se usa en Mint y Breed. El `CustomName` resultante es editable por el usuario.

---

## Sistema de IDs de Partes

- Los IDs son **auto-generados** por la database. **No se editan manualmente** — `BodyPart.ID` es `[ReadOnly]`.
- Formato por tipo: `BS0`, `BS1`… / `A0`, `A1`… / `E0`, `E1`… / `M0`, `M1`…
- El botón **Sync All IDs** en cada database renumera TODO desde 0 y escribe el valor de vuelta en `part.ID`. Usar en setup inicial, nunca con DNA strings ya distribuidos en red.

---

## Sistema de Nombres de Partes (PartNameBank)

- `PartNameBank.cs` — clase estática, pools de 5 palabras por cada `(PartSet, PartRole)`.
- Botón **Roll Name** en cada `BodyPart` SO: genera nombre individual.
- Botón **Roll All Names** en cada database SO: genera nombres en bulk para todas las partes.
- Nombre de criatura = `"{body} {arm} {eye} {mouth}"` → `CreatureDNA.GetDisplayName(db)`.
- Las palabras están temáticamente ligadas al `PartSet` (GooGang = pegajoso, ZapZone = eléctrico, etc.).

---

## Identidad de Criaturas (CreatureDNA)

```
ToStringID() = "BS0-A3-E1-M2-FF00AA"              // genetic string — contrato de red (inmutable)
UniqueID     = "BS0-A3-E1-M2-FF00AA-{Ticks}"      // clave en el registro
BirthDate    = DateTime (UTC)
Stamp()      → setea Timestamp + BirthDate de forma atómica antes de registrar
```

- `CustomName` — nombre editable, auto-asignado en Mint y Breed via `CreatureNameBank.GetRandomName()`. Formato: adjetivo + sustantivo ("Fuzzy Blob"). Editable por el usuario.
- `MotherID`, `FatherID`, `ChildrenIDs` — referencias por `UniqueID` (no genetic strings)
- `Gender` — `Unknown` hasta mintearse. Se asigna 50/50 en `Mint` y en `Breed`.
- `FightCount`, `WinCount`, `BreedCount` — progresión, escritos por CombatService y BreedingService
- `BodyTier`, `ArmTier`, `EyeTier`, `MouthTier` — Tier por slot, independiente por instancia (Tier1 al nacer)
- `BaseHP`, `BaseAttack`, `BaseSpeed` — stats base aleatorios 1–10, asignados en Mint
- `IsDead` — muerte permanente; bloquea combate y breeding si es `true`

---

## Sistema de Breeding (InheritanceOddsTableSO)

Probabilidades por defecto — configurables, normalizadas internamente:

| Origen | Peso por defecto |
|--------|-----------------|
| Padres directos | 40 |
| Abuelos | 20 |
| Bisabuelos | 10 |
| Mutación aleatoria | 20 |
| Base / entorno | 10 |

- Cada slot (body, arm, eye, mouth) hace su **propio roll independiente**.
- Mutación y Base → parte aleatoria del pool completo (sin filtro de rarity ni set).
- Si un ancestro no existe en el registro, cae automáticamente al fallback random.
- Pesos editables directo en el inspector del SO asset — la serialización es la de Unity (no JSON).
- Singleton: `InheritanceOddsTableSO.Current` (se setea en `OnEnable` del SO).
- **Validaciones en `BreedingService.Breed()`**: `IsDead`, género correcto, `BreedCount < MaxBreedCount (4)`. `BreedCount` se incrementa en ambos padres dentro del servicio.
- **Stats del hijo**: cada stat (HP, ATK, SPD) hereda 50/50 de madre o padre, luego aplica delta aleatorio de -1, 0 o +1. Mínimo garantizado: 1.
- `GameManager`: botón **Fill Random Breeders** — selecciona hembra + macho vivos bajo el límite. Muestra info de breeds restantes en inspector.

---

## Sistema de Rareza (RarityOddsTableSO)

- Pesos relativos configurables por `Rarity`. Por defecto: Common 60 / Uncommon 25 / Rare 10 / Epic 4 / Legendary 1.
- `CreatureGenerator.GenerateRandom(db, oddsTable)` → cada uno de los 4 slots hace su propio `oddsTable.Roll()`.
- Si no se asigna tabla, el generador elige sin filtro de rareza.

---

## Part Sets (PartSet enum)

Cada `BodyPart` tiene `Set` que agrupa partes en un tema visual/lore. 10 sets: `GooGang`, `BogBrigade`, `FuzzFactory`, `CosmicCreeps`, `NeonNightmares`, `CrunchCrew`, `GrimGlobs`, `SpudSquad`, `MoldMob`, `ZapZone`. Colores dinámicos en inspector con `[GUIColor]`.

---

## Tier (enum)

`Tier1 = 1`, `Tier2 = 2`, `Tier3 = 3`. Campo en `BodyPart`. Las partes nacen en Tier1. La evolución de Tier durante combate es Etapa 2.

---

## Gender (CreatureGender enum)

`Unknown` (sin registrar), `Male`, `Female`. **NO forma parte del DNA string.** GDD target: género basado en battle-index del padre — pendiente Etapa 2.

---

## Persistencia (SaveSystem)

| Archivo | Contenido | Formato |
|---------|-----------|---------|
| `creature_database.json` | Registro completo de criaturas + árbol genealógico | Newtonsoft.Json |

- `SaveDatabase(registry)` ya **no** se llama directo desde gameplay: las mutaciones disparan `GameEvents.RegistryChanged(registry)` y `GameManager.Persist` hace el save+push (ver "Arquitectura orientada a eventos"). El único save directo es el flush de `OnApplicationQuit` y los de `CloudSyncService` (capa de sync).
- `LoadInto(registry)` se llama en login (`CloudSyncService.OnSignedInComplete`) — popula el SO desde JSON antes del pull.
- `UnityEngine.Color` → hex string via custom `UnityColorConverter`.
- **Dependencia**: package `com.unity.nuget.newtonsoft-json` `3.2.1` en Package Manager (namespace `Newtonsoft.Json`, **no** `Unity.Plastic.Newtonsoft.Json`).
- `CreatureRegistrySO` es el SO asset asignado en GameManager → Setup. JSON es la única fuente de verdad; el SO es vista visual [ReadOnly].

---

## Anchor Points Estándar (Visualizador — Etapa 1.2)

- Estándar fijo: **2 arm anchors + 2 eye anchors + 1 mouth anchor** (formato 2-2-1)
- Partes = hijos del prefab con Transform propio (sin merge de mesh) — intercambiables en runtime
- Se requiere **preview en editor** (editor-time assembly al seleccionar un DNA)

---

## Convenciones de CreateAssetMenu

```
RunRunSimulator/Parts/Arm
RunRunSimulator/Parts/Eye
RunRunSimulator/Parts/Mouth
RunRunSimulator/Parts/Body Shape
RunRunSimulator/Databases/Arm Database
RunRunSimulator/Databases/Eye Database
RunRunSimulator/Databases/Mouth Database
RunRunSimulator/Databases/Body Shape Database
RunRunSimulator/Creature Database (Orchestrator)
RunRunSimulator/Creature Registry
RunRunSimulator/Rarity Odds Table
RunRunSimulator/Inheritance Odds Table
RunRunSimulator/Combat Manager
```

---

## Sistema de Combate (CombatService)

- Stats efectivos = `BaseStat (DNA) + Σ(part.Stat + (tier-1))` por slot. Calculados en runtime, no almacenados.
- Orden por `Speed`; empates aleatorios. 20% crit = ×3 daño. Safety cap: `MaxRounds = 50`.
- **Límite de peleas**: `MaxFightCount = 5` (en `CombatManagerSO`). `CombatService.Simulate()` valida que `FightCount < MaxFightCount` antes de simular.
- **Empate**: si ninguno llega a 0 HP antes de `MaxRounds` → `IsDraw = true`, `FightCount++` en ambos, sin evolución ni muerte.
- **Log por turno**: cada round loguea quién ataca primero, daño, si fue crit, y HP restante del defensor. La línea final siempre incluye nombre, UniqueID y parte evolucionada.
- **Evolución (ganador)**: el ganador **siempre** evoluciona una parte aleatoria elegible (< Tier3). Si todas están en Tier3, se loguea que no hay más evolución posible.
- **Evolución futura (pendiente)**: la parte a evolucionar se elegirá por peso según su tier actual — las partes en Tier1 tienen más probabilidad de ser seleccionadas que las de Tier2, y éstas más que las de Tier3. Regla diseñada: **70% peso Tier1 → Tier2 / 20% peso Tier2 → Tier3 / 10% peso Tier3** (tier máximo, sin efecto). Si un pool está vacío se excluye y los pesos restantes se renormalizan. `CombatManagerSO.EvolutionChance` queda reservado para reglas futuras.
- **Muerte (perdedor)**: probabilidad configurable `DeathChance` en `CombatManagerSO`.
- `CombatManagerSO.Current` — singleton configurable. Asignar en `GameManager → Setup`.
- `GameManager`: botón **Fill Random Fighters** — selecciona 2 criaturas vivas con peleas disponibles. Muestra fights restantes en inspector.

## UGS Cloud Save (CloudSyncService)

- Adjuntar al mismo GameObject que `GameManager`. Asignar `CreatureRegistrySO`.
- Requiere en Unity Dashboard: **Authentication** (Anonymous + **Unity Player Accounts**) + **Cloud Save**.
- **Autenticación**: Unity Player Account via `PlayerAccountService.StartSignInAsync()` (browser). Primer login abre browser; siguientes launches reanudan sesión silenciosamente via `SessionTokenExists`.
- **Player name**: visible en inspector `[Status]`. Botón `Update Name` en bloque `[Account]`.
- **Sign Out**: cierra la sesión actual; el session token se conserva (el próximo launch auto-reanuda).
- **Sesión persistente**: en `Start`, si `AuthenticationService.Instance.SessionTokenExists` → `SignInAnonymouslyAsync()` reutiliza el token cacheado sin browser.
- **Auto-pull en login**: `OnSignedInComplete` ejecuta `SaveSystem.SetUserScope(playerID)` → `LoadInto` (cache local) → `await PullAsync()` (override desde cloud si hay data).
- **Auto-push en Mint/Breed**: `GameManager.TryPushToCloud()` se invoca después de `SaveDatabase` en `MintRandomCreature` y `BreedCreatures` — fire-and-forget. Requiere asignar el `CloudSyncService` en el inspector del `GameManager`.
- **Save local scoped por playerId**: el archivo pasa de `creature_database.json` a `creature_database_<playerId>.json` después del sign-in. Si existe el unscoped pero no el scoped, hay **migración automática** la primera vez. Permite testing con múltiples cuentas/instancias sin que se pisen.
- Push/Pull manual también disponibles via botones del inspector (`EnableIf(_isSignedIn)`).
- `sync_meta.json` local registra timestamps de seguridad para detección de rollback/edición manual.
- **Reset All Progress (DEV)**: borra keys de Cloud Save + vacía JSON local + borra sync_meta.
- **Dev mode**: CHEAT ALERT solo imprime en consola — activar bloqueo en post-Etapa 2.3 con Cloud Code firmando tokens.

---

## Sistema de Combate Async (AsyncCombatService + Cloud Code)

Arquitectura **dual** con dos modos coexistentes que comparten el mismo pool en Cloud Save Custom Data.

### Modo Instant — `run-combat.js`

Botón naranja "Enqueue for Combat (Instant)". El cliente llama `run-combat`, que en un mismo request:
1. Lee el pool. Si hay opponent → simula y escribe resultados a ambos players. Returns `{status: "matched"}` y el cliente hace `PollResultsAsync()` inmediato.
2. Si no hay opponent → enqueue y returns `{status: "waiting"}`.

Use case: testing rápido entre dos cuentas activas.

### Modo Timer/Scheduled — `enqueue-combat.js` + `process-matchmaking.js`

Botón morado "Enqueue for Combat (Timer)". Flow:
1. Cliente llama `enqueue-combat` → solo agrega al pool, returns `{status: "queued"}` inmediato. Cliente puede cerrar el juego.
2. Unity Scheduler dispara `process-matchmaking` cada hora UTC (cron `0 * * * *`). El script drena el pool, hace shuffle, empareja evitando self-match, simula cada par y escribe `combat_results` en el Player Data de cada jugador. Leftover odd-one-out vuelve al pool.
3. Cliente vuelve al juego, presiona "Check Pending Results" → `PollResultsAsync()` aplica los resultados localmente.

### Custom Data — quirks descubiertos

- `setCustomItem` firma correcta: **3 args** `(projectId, customId, body)` donde body es `{key, value}`. La firma de 4 args silently corrompe el body.
- `value` **NO acepta arrays top-level** — siempre envolver en `{ entries: [...] }`. Empírico, la doc no lo dice.
- El método se llama `getCustomItems` (plural con array de keys), NO `getCustomItem` singular.
- Auth via `accessToken: context.serviceToken` en el constructor del `DataApi`.

### Identificación de oponentes en logs

`CombatResult` incluye `OpponentName` (criatura), `OpponentPlayerId` (UUID), `OpponentPlayerName` (display name de `AuthenticationService.GetPlayerNameAsync()`). El log final (resuelto en `AsyncCombatService.ApplyResult`, que también fetchea el `playerName` local) se ve:

```
[AsyncCombat]  Sowtank => "Fuzzy Blob"  vs  "Slimy Goo" <= Manolito  ——  ¡Ganaste!  |  Evolved: Arm
```

El `OpponentPlayerId` ya **no** se imprime en el log (es ruido visual). Se conserva en el `CombatResult` por si se necesita para futuras features (venganza, perfiles).

### Estado Busy persistente (encolado entre sesiones)

`CreatureDNA.BusyState` (`BusyReason.QueuedForCombat`) marca una criatura como ocupada. **Debe persistir en Cloud Save** para sobrevivir logout/login, si no la criatura reaparece libre al reconectarse. Reglas:

- `AsyncCombatService.EnqueueInternal` → tras setear `BusyState`, llama `SaveSystem.SaveDatabase` **+ `GameManager.Instance.PushToCloud()`**. Sin el push, `PullAsync` en el próximo login baja el estado viejo (no-busy) y pisa el local.
- `PollResultsAsync` y `DequeueAsync` → tras limpiar `BusyState`, también pushean.
- `CombatService.Simulate` y `BreedingService.Breed` validan `IsBusy` (además de `IsDead`): una criatura encolada no puede pelear localmente ni criar.
- `dequeue-combat.js` filtra el pool por `creatureId + playerId` (solo desencola criaturas propias). El cliente limpia `BusyState` local **siempre**, aunque el server responda `not_found` (ya fue matcheada → el resultado llegará por `PollResultsAsync`).

### Reconciliación de fantasmas (Queued local sin estar en el pool)

**Síntoma**: una criatura aparece `QueuedForCombat` en local aunque no está en el pool ni tiene resultado pendiente (dequeue da `not_found`, check results no muestra nada). Causa: el `BusyState=Queued` se setea y pushea **antes** de confirmar el enqueue server-side, y los pushes fire-and-forget pueden llegar fuera de orden (el "Queued" pisa al "None" en el cloud → revive en el próximo `PullAsync`). El caso `default` de `EnqueueInternal` tampoco hace rollback.

**Fix (self-healing, no elimina la causa raíz)**: `PollResultsAsync` ahora hace **apply-then-reconcile** — aplica resultados pendientes y SIEMPRE llama `ReconcileGhostsAsync`, que vía `get-queue-status` (1 read) limpia el `BusyState` de toda criatura Queued que el pool no tiene. Salvaguardas: si el server no responde (`null`) no toca nada; salta las que están `inFlightEnqueues` (en la ventana de 5s del enqueue). `Show Queued MoriMonchis` ahora consulta el pool + resultados y rotula cada una: **In Queue / Result Ready / GHOST / ?(offline)**. → El usuario ya no necesita Dequeue para fantasmas. Costo: 1 Cloud Code call + 1 read por botón, solo on-demand.

### Scheduler — arquitectura de 3 piezas (CRÍTICO)

El Scheduler de Unity **NO invoca el script de Cloud Code directamente**. Emite un evento al servicio **Triggers**, y un Trigger separado redirige ese evento al script. Faltaba esta pieza → el schedule existía (`ugs sched list` lo mostraba) pero nunca ejecutaba (`progress: 0`, logs vacíos).

```
matchmaking-tick.sched  →  emite evento  →  Triggers service  →  matchmaking-trigger.tr  →  process-matchmaking.js
   cron "0 * * * *"         "process-matchmaking.v1"                (el enlace)               (el script)
```

**El `eventType` del Trigger sigue un patrón obligatorio:**
```
com.unity.services.scheduler.<eventName>.v<payloadVersion>
```
Con `eventName: process-matchmaking` + `payloadVersion: 1` → `com.unity.services.scheduler.process-matchmaking.v1`.

**Formatos de archivo CLI (¡difieren entre sí!):**
- `.sched` — `Configs` es un **objeto** `{ "nombre": { ... } }`, campos PascalCase (`EventName`, `Type`, `Schedule`, `PayloadVersion`, `Payload`).
- `.tr` — `Configs` es un **array** `[ { ... } ]`, cada item con `Name`, `EventType`, `ActionType: "cloud-code"`, `ActionUrn: "urn:ugs:cloud-code:<script>"`. (Para módulos C#: `urn:ugs:cloud-code:<modulo>/<funcion>`.)

Setup minimal:
1. Bajar `ugs-windows-x64.exe`, renombrar a `ugs.exe`, agregar al PATH
2. Crear Service Account en Dashboard → Organization → Administration → Service Accounts → generar Keys
3. Asignar roles: project (`Cloud Code Editor/Viewer/Publisher`, `Unity Environments Admin`) + organization (`Owner`)
4. `ugs login` + `ugs config set project-id <id>` + `ugs config set environment-name production`
5. Editar `CloudCode/matchmaking-tick.sched` (cron) y `CloudCode/matchmaking-trigger.tr` (enlace)
6. Deployar **ambos**: `ugs deploy CloudCode/matchmaking-tick.sched` + `ugs deploy CloudCode/matchmaking-trigger.tr`
7. Verificar con `ugs sched list` (el CLI NO lista triggers; verlos vía REST API o esperar logs de ejecución)

**Gestión vía REST API** — el CLI solo tiene `sched list` y `new-file` (sin `delete`/`enable`). Para borrar/listar schedules usar la Scheduler Admin API con Basic Auth (`base64(<KEY_ID>:<SECRET_KEY>)`):
```
GET/DELETE https://services.api.unity.com/scheduler/v1/projects/<PROJECT_ID>/environments/<ENV_ID>/configs[/<CONFIG_ID>]
```
Project ID: `14ef2aa0-ac88-457a-be73-9164939d87b0` · Environment `production`: `6f9c7d83-1396-4de7-ba1c-ba01cec186df`.

⚠️ **Restricción de Unity**: schedule mínimo cada 1 hora, siempre UTC. Para testing inmediato hay un botón **"Force Matchmaking Tick (DEV)"** en `CloudCodeTester.cs` que llama directo a `process-matchmaking` (bypasea scheduler+trigger).

### Service Account ≠ Project Secrets

- **Service Account Keys** (Organization → Administration → Service Accounts → Keys): para autenticar la CLI/herramientas externas.
- **Project Secrets** (Proyecto → Cloud Code → Secrets): variables de entorno para que los scripts JS accedan a APIs externas en runtime.

NO confundirlas — son cosas distintas.

## Sistema de Breeding Async (timer) — AsyncBreedingService + Cloud Code

Breeding con timer **server-authoritative**. El cliente nunca decide cuándo termina: el timestamp se stampa y se valida server-side. La cría sí se mintea localmente y se pushea (igual que hoy) — checkpoint de diseño para mover la generación server-side en una etapa futura.

### Almacenamiento — Game Data (Custom Data)

Los huevos viven en Custom Data como un **array** por jugador, key `breeding_eggs_<playerId>`, **solo escribible vía Cloud Code** (service token). El cliente no puede falsificar el tiempo. **Varias parejas pueden incubar en paralelo**; una pareja (o cualquier padre) solo puede estar en un huevo a la vez — lo garantiza `BusyState.Breeding` (un padre ocupado no inicia otro breed) + validación server-side.

```js
breeding_eggs_<playerId> = { entries: [ { motherId, fatherId, startedAt, readyAt }, ... ] }  // readyAt = startedAt + BREED_DURATION_MS, server-side
```

> Un huevo se identifica por su par `(motherId, fatherId)` — único entre huevos activos porque un padre no puede estar en dos.

### Flujo

1. **Breed Timer** (botón morado en `BreedingController`): valida padres localmente → `start-breeding` rechaza si alguno de los dos padres ya está en un huevo, si no stampa server-time y appendea al array → cliente marca ambos padres `BusyReason.Breeding`, cachea `BreedReadyAt`/`BreedPartnerID` (display) → push. El juego se puede cerrar.
2. **Show Eggs** (botón, sin server): lista TODOS los huevos con índice (`[0] "Mamá" x "Papá" — 12:34 left`) leyendo `BreedReadyAt` de las madres. NO es autoritativo — solo display.
3. **Hatch Egg** (botón + campo "Hatch Index"): toma el huevo en ese índice → `hatch-breeding(motherId, fatherId)` compara el reloj real del server vs `readyAt`. Si `ready` → quita ese huevo del array (overwrite `{ entries }`), devuelve la pareja; el cliente limpia el Busy de ambos padres → `BreedingService.Breed()` mintea la cría local (+`BreedCount++`) → registra + push. Si `not_ready` → muestra el tiempo restante real del server.

### Anti-cheat — por qué el server stampa

Si el cliente escribiera el timestamp de inicio, podría atrasar el reloj del PC, iniciar el breed con timestamp viejo, restaurar la hora y el huevo estaría "listo" al instante. Por eso `start-breeding` (no el cliente) pone `startedAt`/`readyAt`, y viven en Custom Data (inaccesible para escritura del cliente). El hatch valida contra el reloj real del server.

- `CreatureDNA.BreedReadyAt` (long, epoch ms del server; 0 = no breeding) y `BreedPartnerID` — cache local para display, se persisten con el registry.
- `CombatService.Simulate` y `BreedingService.Breed` validan `IsBusy` → un padre incubando no puede pelear ni iniciar otro breed.
- El botón **Breed** (local, instantáneo) se conserva para testing — bypasea el timer por completo.

### Quirks Cloud Code (además de los de combate)

- `hatch-breeding` **no usa `deleteCustomItem`** (firma no verificada) — hace `splice` del huevo en el array y reescribe `{ entries }` (igual patrón que el matchmaking pool).
- El array se envuelve en `{ entries: [...] }` — Custom Data rechaza arrays top-level (mismo quirk que `matchmaking_pool`).
- Params de ambos: `motherId`, `fatherId` (camelCase). `hatch-breeding` también los necesita para identificar qué huevo abrir.

## Sistema de Jugador (Player — primera persona)

Responsabilidades separadas en tres scripts que **NO se referencian entre sí**; se comunican por **static events** en `PlayerInputs` (mismo patrón que `GameEvents`: el evento transporta la data, el listener cachea el payload). Suscribir en `OnEnable`, desuscribir en `OnDisable` (regla 9).

- **`PlayerInputs`** — única clase que toca `InputSystem_Actions`. Traduce callbacks crudos a static events: `MoveChanged(Vector2)`, `Jumped`, `InteractPressed` (E key-down), `InteractReleased` (E key-up), `ThrowPressed` (Attack). El `Look` NO está acá — lo maneja Cinemachine.
- **`PlayerController`** — solo lógica. Move FP con `CharacterController` (relativo a la cámara), jump, grab/throw vía interfaces, y **state machine** (`PlayerStateType`). NO referencia `PlayerInputs` (se suscribe a sus static events). NO maneja la cámara: lee el `forward` para mover/agarrar/lanzar.
- **`PlayerAnimator`** — solo animación. Se suscribe a los mismos static events. Inerte hasta asignar un `Animator` (todo guardado por null). Seam para cuando existan clips.

**Cámara**: Cinemachine primera persona (Position Control = *Hard Lock to Target* sobre un `Head`; Rotation Control = *Pan Tilt*; + *Cinemachine Input Axis Controller* leyendo la acción `Look`). `PlayerController` referencia una **`CinemachineCamera`** (no la Main Camera): de ella deriva en `Awake` el `cameraTransform` (su `forward` = mirada) y el `CinemachineInputAxisController` (vía `TryGetComponent`) que **desactiva para congelar la cámara** en estado `Menu`. `using Unity.Cinemachine;` (sin asmdef, `Assembly-CSharp` ya referencia el package).

### Estado del Player (`PlayerStateType`: None / Exploring / Menu)

El player conmuta de estado escuchando `UIManager.OnUIFocusChanged(bool)` (true cuando hay ≥1 panel abierto):
- **Exploring** — control normal: cursor bloqueado/oculto, `CinemachineInputAxisController` activo, mapa `Player` habilitado.
- **Menu** — un panel está abierto: cursor libre/visible, cámara congelada, y se **suspende todo el input de gameplay** porque `PlayerInputs` **deshabilita el mapa `Player`** en `OnUIFocusChanged` (no solo por los guards `if (state != Exploring) return;`, que quedan como red de seguridad). Ya **NO se cierra con E**: en menú el mapa `Player` está apagado → el interact del mundo no puede dispararse (esto además mata el bug del "interact togglea el panel de atrás"). Se cierra con **ESC** (ver router de UI).
- `SetState` centraliza cursor + freeze de cámara. Estado inicial `Exploring` en `Start`.

**Dos action maps mutuamente excluyentes** (`InputSystem_Actions`): `Player` (`Move`/`Jump`/`Interact`/`Attack`, dueño `PlayerInputs`) y `UI` (`Navigate`/`Submit`/`Cancel`, dueño `UIInputs`). Solo uno está habilitado a la vez, conmutado en el borde `OnUIFocusChanged` (gameplay ↔ menú). ⚠️ La acción `Interact` debe tener la interacción **Hold desactivada** (Press) — el hold-vs-tap lo decidimos nosotros con un timer (`grabHoldDuration`).

### Grab / Interact / Throw (semántica de E + Click)

`E` significa distinto según contexto:

| Estado | Input | Acción |
|--------|-------|--------|
| Libre | **tap E** (press < `grabHoldDuration`) | Interactuar con un `IInteractable` |
| Libre | **hold E** (≥ `grabHoldDuration`) | Agarrar un `IThrowable` |
| Cargando | **press E** | Soltar en el sitio |
| Cargando | **Click (Attack)** | Lanzar |

- Raycast genérico: `TryFindInView<T>` busca `T` (interface o clase) en el collider O en su `attachedRigidbody`. Lo usan grab (`IThrowable`) e interact (`IInteractable`).
- **Throw**: la fuerza va **desde la posición del objeto (holdAnchor) hacia un punto de mira sobre el eje de la cámara** (`throwAimDistance`), así converge a donde mirás (el holdAnchor está a un lado, como una mano).

## Sistema de Interacción y UI (IInteractable / IThrowable / UIManager)

**Comportamientos componibles "drop-a-script"** sobre objetos del mundo (requieren **Collider** para el raycast; las primitivas ya lo traen):
- **`IThrowable`** (en `Interfaces.cs`) — `IsHeld`, `OnGrab(anchor)`, `OnRelease()`, `OnThrow(force)`. Implementación: `ThrowableObject` (RequireComponent Rigidbody; mientras se sostiene sigue al anchor por **velocidad** → choca en vez de clippear). Agarrar = hold E.
- **`IInteractable`** (en `Interfaces.cs`) — `Interact()`. Implementación: `PanelTrigger`. Interactuar = tap E. Un objeto puede implementar **ambas** (tap interactúa, hold agarra).

**Paneles del Canvas** (desacoplado por static events, sin referencias directas):

```
PanelTrigger (mundo, IInteractable) --Interact()--> UIManager.RequestPanelToggle(UIPanelType)
                                                            │  (el evento lleva el enum)
                                                            ▼
UIManager (escena) --Dictionary<UIPanelType,GameObject>--> muestra/oculta el panel
```

- **`UIManager` = hub de eventos UI + gestor de paneles** (`SerializedMonoBehaviour` de Odin, en escena). Los eventos UI viven **acá como `static event Action`** (NO en `GameEvents`, que queda solo para gameplay; NO en un `UIEvents` aparte — eliminado). Patrón idéntico a los static events de `PlayerInputs`. Eventos:
  - `OnPanelToggleRequested(UIPanelType)` / `RequestPanelToggle` — toggle (lo usa la E).
  - `OnPanelSetRequested(UIPanelType, bool)` / `RequestPanelSet` — show/hide explícito (idempotente), para acciones que no son flip (p. ej. abrir el detalle aunque ya esté abierto).
  - `OnCreatureSelected(CreatureDNA, CreatureRegistrySO)` / `SelectCreature` — una card fue clicada; el evento lleva la criatura **y** el registry (para resolver padres).
  - `OnUIFocusChanged(bool)` — true al abrirse el primer panel, false al cerrarse el último. Se dispara solo en el borde 0↔1 (cuenta el `stack`). Lo escuchan el **player** (suspende control), `PlayerInputs` (deshabilita el mapa `Player`) y `UIInputs` (habilita el mapa `UI`).
  - `OnNavigableRegistered(UIPanelType, IUINavigable)` / `RegisterNavigable` + `OnNavigableUnregistered(UIPanelType)` / `UnregisterNavigable` — cada panel focusable registra su handler de input ruteado (lo llaman en `Start`/`OnDestroy`).
- **STACK + router de teclado/mando** (resuelve el viejo "sistema de prioridad de UI"): los paneles abiertos viven en una **lista ordenada** (`stack`, el último = tope con foco). El `UIManager` es el **único suscriptor** de `UIInputs` y **despacha solo al tope**: `Navigate`/`Submit` → `IUINavigable` del tope; `Cancel` (ESC) → **pop del tope** (atrás universal, cierra en orden LIFO: detalle → grilla → gameplay). Un panel no implementa Cancel: lo maneja el manager. `Push` mueve un panel al tope (re-abrir lo re-enfoca).
- **`UIInputs`** (en el objeto del UIManager) — dueño único del action map `UI`, espejo de `PlayerInputs`. Static events `NavigatePressed` (stepped: 1 paso por pulsación, con debounce de tecla/stick sostenido), `SubmitPressed`, `CancelPressed`. Habilita el mapa `UI` solo con foco. **Gamepad gratis** (Navigate=stick/dpad, Cancel=B, Submit=A). El detalle clickeable sigue por los punteros del UITK (no por este mapa).
- **Dos estrategias de ocultar** (para que los paneles sigan actualizándose ocultos):
  - **UI Toolkit** (el GameObject tiene `UIDocument`) → togglea `rootVisualElement.style.display`, dejando el `UIDocument` **ACTIVO**. ⚠️ NUNCA `SetActive(false)` un `UIDocument`: sin objeto activo no hay `rootVisualElement` y deja de poblarse.
  - **uGUI** → `SetActive` normal.
- **Todos los paneles arrancan ocultos** en `UIManager.Start` (por `display` los UITK, por `SetActive` los uGUI).
- **`UIPanelType`** (enum en `Enums.cs`) — `None / CreatureGrid / MorimonchiDetail`. Convención: sufijo `Type` + `None = 0` (los enums viejos NO se renombran).
- **`PanelTrigger`** (`IInteractable`, en `Interactables/`) — campo `UIPanelType panel`; al tap E dispara `UIManager.RequestPanelToggle(panel)` (static). No conoce instancia del `UIManager`.

## Capa UI Toolkit (UXML/USS) — grilla + detalle

Assets UXML/USS/PanelSettings viven en `Assets/RunRunSimulator/UI Toolkit/` (carpeta normal, **NO Resources** — se referencian por inspector, no por ruta). Unity 6.3; `TabView`/`Tab` y el autosize de texto (`-unity-text-generator: advanced; -unity-text-auto-size: best-fit <min> <max>`, requiere activar **Project Settings → UI Toolkit → Advanced Text Generator**) son nativos.

- **`CreatureGridUITK`** (gemelo UITK de `CreatureGridUI`, **implementa `IUINavigable`**) — vive en el **objeto del UIManager** (siempre activo) y referencia un `UIDocument` separado (`CreatureGridUITK.uxml`) + un `VisualTreeAsset` (`CreatureCardUITK.uxml`). Por estar en un objeto activo, sigue suscrito a `OnRegistryChanged/Reloaded` y **repuebla la grilla aunque el panel esté oculto** (resuelve el gap de `OnDisable`). Clona una card por criatura (saca la card del `TemplateContainer` para no romper el wrap, guarda el `CreatureDNA` en `card.userData`), las hace clicables (`ClickEvent → Select(idx) + UIManager.SelectCreature`), y cablea el botón cerrar (`Start`) → `RequestPanelToggle`. Grilla = flexbox `flex-wrap` + `ScrollView`; margen 10% en los 4 bordes (`position:absolute` con `left/right/top/bottom:10%`); cards 20%/60%/20% (nombre/icono/estado) con `cardSize` configurable.
  - **Navegación teclado/mando**: ←/→ mueven 1 card, ↑/↓ saltan una fila completa (el nº de columnas se **mide del layout real** — cuenta las cards que comparten la `y` de la primera fila). Highlight `.card--selected` (la `.card` lleva un borde transparente de 3px fijo → al seleccionar **no hay reflow**). **Auto-scroll** vía `ScrollView.ScrollTo` (la barra sigue a la selección). **Enter/A** (`OnUISubmit`) → abre el detalle de la card seleccionada. Mouse y teclado quedan sincronizados (el clic también fija el índice).
- **`MorimonchiDetailInfoUITK`** (`MorimonchiDetailInfoUITK.uxml/.uss`, **implementa `IUINavigable`**) — ventana de detalle **modal**, estilo resumen de Pokémon FireRed. Vive en el objeto del UIManager, referencia su `UIDocument` + la `CreatureDatabaseSO`. Escucha `OnCreatureSelected` → `Populate(dna)` → `selectedTabIndex = 0` (**siempre abre en Info**) → `RequestPanelSet(true)`. **Modalidad**: `document.sortingOrder` alto (encima de la grilla, misma `PanelSettings`) + backdrop full-screen que captura los clicks → no se puede tocar la grilla detrás hasta cerrar con la **X** o **ESC**. **A/D** (`OnUINavigate`) cambia de tab (clamp). `TabView` con **Info** activa (retrato cuadrado teñido con PrimaryColor + nombre grande autosize + stats coloreados `FINAL (base + bonus)` vía `CombatService.GetEffectiveStats` · identidad · partes con swatch del color del set `Nombre · Set · Rareza · TierN` vía `BodyPart.SetColor/RarityColor` públicos · progresión) + tabs **Combate** (historial) / **Breed** (hijos) / **Linaje** (árbol genealógico) / **Equipo**, todas placeholder. El **linaje se movió a su propia tab** (ya no en Info). Las **flechas de scroll del header de tabs se ocultan** vía `.unity-scroller--horizontal { display:none }` dentro de `.detail-tabs` (no afecta la barra vertical del panel Info).

## Bugs conocidos (pendientes de fix)

- **Fantasma de cola (causa raíz, mitigado)**: el `BusyState=Queued` se setea/pushea antes de confirmar el enqueue server-side, y los pushes fire-and-forget pueden llegar fuera de orden; el caso `default` de `EnqueueInternal` no hace rollback. Hoy está **auto-curado** por `ReconcileGhostsAsync` (ver sección de combate async), pero la causa raíz sigue: para eliminarla habría que secuenciar los pushes y agregar rollback en `default`.
- `DeathChance` está hardcoded a 15% en `process-matchmaking.js` y `run-combat.js`. Cambiar el `CombatManagerSO.DeathChance` solo afecta el combate local. Para sincronizar habría que pasar el valor como param o duplicarlo manualmente.
- `BREED_DURATION_MS` está hardcoded a 30 min en `start-breeding.js`. `InheritanceOddsTableSO.BreedDurationMinutes` solo afecta el display local — misma limitación que `DeathChance`.
- No hay race-condition handling en el matchmaking pool — dos llamadas simultáneas pueden pisarse. Aceptable en testing; para producción con tráfico real, agregar `writeLock` del SetItemBody.
- **Sistema de prioridad de UI (RESUELTO)**: ahora el `UIManager` mantiene un **stack ordenado** de paneles (tope = foco) y rutea el input solo al tope; ESC hace pop en orden LIFO. Además los action maps `Player`/`UI` son mutuamente excluyentes, así que en menú el interact del mundo (E) no puede dispararse → el edge del "tap E togglea la grilla de atrás" desaparece. (Ver "STACK + router" en la sección de UI.) Pendiente menor: la grilla navega 1 paso por pulsación, sin **auto-repetición** al mantener la dirección (cómodo de añadir con un timer si una grilla grande lo pide).

## Checkpoints de diseño — Breeding Async (pendientes para etapas futuras)

- **Busy-lock server-enforced**: hoy el flag `BusyState = Breeding` lo escribe el CLIENTE (espejo local en Player Data), sincronizado con eventos del server pero no impuesto por él. El timer SÍ es infalsificable (vive en Custom Data), pero un tramposo podría limpiar el `BusyState` local de un padre incubando y usarlo en combate/otro breed mientras el huevo sigue server-side. Fix futuro: que `start-breeding` y `enqueue-combat` cross-checkeen server-side contra el array de huevos / pool antes de aceptar la acción. Aplica igual al `BusyReason.QueuedForCombat`.
- **Generación de la cría server-side**: hoy se mintea local + push. Mover a Cloud Code cuando se endurezca el anti-cheat (igual que `process-matchmaking` portó `CombatService`).
- **Cross-device**: el countdown local (`BreedReadyAt`) no viaja entre dispositivos; el huevo autoritativo (Custom Data) sí. Resolver con un `get-breeding` peek si hace falta mostrar el timer en otro device.
- **Crash entre hatch-ready y crear la cría**: riesgo bajo de perder el breed (el huevo ya se borró server-side). Mitigar con borrado en dos fases más adelante.
