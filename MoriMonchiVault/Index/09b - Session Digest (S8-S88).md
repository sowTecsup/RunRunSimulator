---
tags: [index, core, archive]
---

# 09b - Session Digest (S8-S88)

> Digest consolidado de las bitacoras historicas, destilado en S90 (2026-08-31). Reemplaza a `09b - Session Archive.md` (S8-S45, 284KB) y `09b - Active Context Archive.md` (S46-S79, 256KB), borrados del vault en S90 — el texto completo vive en git (commit `cf5ab14` y anteriores). Las entradas completas S80-S88 estan en git (commit `8ac520d`, ruta `Index/09 - Active Context.md`).
> Solo consultar ante busqueda puntual de historia; el estado vivo esta en [[09 - Active Context]].

## Timeline S8-S45 (2026-06-13 → 2026-07-14)

Epoca: tienda/NPCs/breeding + combate autobattler con elementos (ese combate fue REFUNDADO en S80 y su codigo DEMOLIDO en S75; ~~20 - Combat Prototype MVP (Plan)~~ (borrada S93) es la fuente de verdad actual).

- **S8 (06-13)** — Breeding async server-side: cancel-breeding/cancel-all-breeding en Cloud Code; fix desync "already_breeding"; cortejo v1.
- **S9 (06-18)** — LifeStage + edad; HomePenKey/Slot (corral de origen); slots fijos de cria; recien nacido lanzado desde el corral.
- **S10 (06-19)** — FurType + 3 colores geneticos (Base/Shadows/Outline) por MaterialPropertyBlock; `ColorGenetics`; herencia HSV.
- **S11 (06-19)** — 6 scripts monstruo partidos en partials (deuda pagada en S53-S55); regla de arquitectura general en CLAUDE.md.
- **S12 (06-20)** — Dev consoles por composicion (patron F3: refs serializadas), cascada SO sin `static Current`, `CloudEndpoint` estatico.
- **S13 (06-20)** — Namespace raiz `MoriMonchiSimulator` en 116 scripts + reorg de carpetas por dominio.
- **S14 (06-21)** — Spawning: fix churn de `OnRegistryReloaded` (Rebind liviano, no Initialize); gate dataReady; `SpawnDiagnostics`.
- **S15 (06-21)** — Fix desync color: la KEY embebe BaseColor → `ReconcileColors` self-heal en LoadFrom; Reset borra huevos+cola+results.
- **S16 (06-21)** — Cortejo orbit/tend por sexo.
- **S17 (06-21)** — Sistema NPC compradores completo (FSM, cola, negociacion, CashRegister furniture, TransactionPanel).
- **S18 (06-21)** — Combat Visualizer v1 (replay de CombatRecord, bus CombatVisualEvents, hooks Feel). †S75
- **S19 (06-22)** — Precio por NameTag; NpcThoughtTag world-space; use points anti-overlap; StoreDisplayRegistry auto-registro.
- **S20 (06-23)** — Sold=Dead con SaleDate; cola lineal ortogonal; NpcNameBank/DialogueBank; outbid; cerco de areas NavMesh. Sistema NPC CERRADO.
- **S21 (06-24)** — Generalizacion de containers: `IAnchorPlace`+`AnchorRegistry`, ancla `LocationKey/LocationSlot` en DNA, spawn colocacion-primero.
- **S22-S25 (06-24/25)** — Combat Visualizer funcional (lista de nodos, rewind, muerte Pokemon) + pose procedural + stats en HP bar. †S75
- **S26-S28 (06-26)** — Stats point-buy (presupuesto 18); DEF/LCK/EVA + rename HP→CON; Equipamiento Et.1 (EquipmentSO/DB, `Equipped` en DNA); paleta.
- **S29-S31 (06-30 → 07-01)** — Procs polimorficos inline en Effects; decision macro: combate por SEMILLA determinista (el server deja de simular); DamageNumbersPro.
- **S32-S35 (07-02/03)** — `CombatRng` xorshift32 + `SimulateCore` puro; JS = matchmaker; composicion sobre partial (regla 11); sinergias; mochila free-placement; popups DNP; 6 elementos como stacks. †S75 (sobreviven las lecciones, no el codigo)
- **S36 (07-10)** — Unity MCP instalado y validado (nota 12 nueva, paso 8 del protocolo); GameScene reorg 27→14 raices; PlayerInputs 100% action-driven. Decision: pivot autobattler 3v3.
- **S37-S38 (07-10/11)** — Tesis roles/elementos ([[13 - Combat Design Direction]]); sim 3v3 por equipos; tab "Equipo 3v3" drag&drop; recetario UITK (vive en [[05 - UI System]]).
- **S39-S41 (07-11/12)** — Limpieza legacy 1v1; Personality→Role; 12 estados elementales; refactor por composicion de CombatService; paridad SHA256; replay 3v3. †S75
- **S42-S45 (07-12/14)** — Capa visual del replay 3v3: barra de orden, marcas en tiempo real, lunge, globos. Cerro con "el feel no esta logrado". †S75

## Timeline S46-S79 (2026-07-14 → 2026-08-24)

- **S46-S47 (07-14/15)** — Rediseno energia→marcas + `CombatFeelDirector`; escudo por ronda; primer ragdoll joints. †S75
- **S48-S52 (07-15/20)** — POC aracnido procedural (IK, gait predictivo) → probado en GameScene y DESCARTADO por Juan (repelus en masa); sobreviven `MonchiAnimationDriver` y la regla de color 60/30/10. Consolidacion Notion (17 paginas).
- **S53-S55 (07-20/21)** — FIN DE LOS PARTIALS (fases 6-9 de [[11 - Technical Debt]]): CloudSyncService→CloudAuth+CloudSyncOps; paneles→presenters (`ITabPresenter`); MoriMochiAgent→AgentContext+Brain/Physics/Confinement; spider eliminado entero; primer archivado del Active Context.
- **S56-S57 (07-21)** — PIVOT DE MODELO: Suriyun Dragons_SD como MoriMochi final; 33 FurPatterns tinteables + 25 caras + 4 cuerpos; mint ponderado; shiny 0.5%; fotomaton `MonchiPortraitService`.
- **S58-S62 (07-21/22)** — Replay 3v3 sobre Suriyun; pipeline visual legacy borrado; 18 FX por codigo MCP; pacing por fases; balance Monte Carlo ~10k sims (spread 60pp→23pp). †S75
- **S63-S67 (07-22/25)** — "MoriMonchis vivos": social V1-V2 (Percepcion→Decision→Expresion, `SocialGraphService`, dormir juntos, pelea de gremlins); direccion de UI "Diario del Pet Shop" + `Theme.uss` tokens `--mm-*`; tab Relaciones.
- **S68 (07-25)** — Localization-ready: com.unity.localization, `Loc`/`LocEnumMaps`, ~440 strings a keys, EN fuente / ES traduccion.
- **S69-S70 (07-25 → 08-05)** — Diales geneticos Sociability/Boldness (50/30/20); petting hold-E + comer de la mano; item Snack Monchi.
- **S71-S74 (08-05/10)** — Refundacion del combate en papel: [[15 - Theorycrafting S71 - Autobattler y Marketing]], [[16 - Diagnostico por Frentes]], [[17 - Refundacion del Combate]], [[18 - Pilares del Rediseno (Draft)]] (dia/noche, genes visibles, Cutie Marks, archipielago).
- **S75 (08-11)** — LA GRAN DEMOLICION: combate 3v3 + visualizer + partes Arm/Eye/Mouth borrados (≈55 .cs); genes migrados a Horn/Back/Wing/Face; bases CutieMarkSO; verificacion por MSBuild.
- **S76-S78 (08-11/14)** — Rumbo nuevo: Predictive Tactical Extraction ([[19 - Combate Nuevo - Predictive Tactical Extraction]]); braindump del flujo; propuestas plantilla-define-aterrizaje (absorbidas por la 20).
- **S79 (08-24)** — Research Unity CLI oficial: no migrar, candidato a complemento (documentado en [[12 - Unity MCP]]; el CLI se instalo en S89 y se adopto como complemento en S90).

## Timeline S80-S88 (2026-08-25/28) — era del prototipo (entradas completas en git `8ac520d`)

- **S80 (08-25)** — Plan del MVP aprobado: ~~20 - Combat Prototype MVP (Plan)~~ (borrada S93) creada (beats, ticks, juggle, enemigos reactivos); gate de codigo levantado SOLO para el prototipo.
- **S81 (08-25)** — Fases 1-4 ejecutadas: prototipo jugable (29 scripts, `CombatPrototype.unity`), paridad proyeccion==ejecucion verificada 7 veces; feedback de Juan → §10 de la 20.
- **S82 (08-26)** — El idioma nuevo: plantilla = anclaje + aterrizaje, enemigos activadores automaticos, movimiento ajedrez de golpeados, presupuesto 2 acciones, celdas-hueco, isla 12x12, orbita de camara.
- **S83 (08-26)** — Legibilidad post-playtest: vibracion en TODO impacto (evento Impact), seleccion con anillo + guia por estado, zoom de rueda, clic selecciona dragon propio.
- **S84 (08-27)** — Semilla Nocturna: pivote a defensa de objetivo (plantar semilla, oleadas, germinacion=victoria); rotar-hacia-atacante; idioma drag; cerro sin protocolo (reconstruida en S85).
- **S85 (08-27)** — Auditoria data-driven + remediacion (prefab `UnitView` + `Feedbacks/OnHit`, tunables serializados); spawn por bordes con fase `Spawning`; poda del Active Context; auditoria QoL visual → §12 de la 20.
- **S86+S87 (08-28)** — QoL §12 completa (HUD por franjas, camara ORTO re-encuadrada, billboards, FIZZLE visible, highlights por prioridad, clic-a-traves resuelto) + kit unificado §13 (Quake/Pierce/Lift; disparo ⇔ IgnoresHeight ⇔ proyectil) + pulido de giros (`SelectionFacingPreview`, `baseYawOffset` 0).
- **S88 (08-28)** — Ciclo de turnos v2 (§14b: gasto por PODER, ciclo fijo 3, combo libre, fase Reacting) + camara Bad North (perspectiva FOV 30, pitch 38) + UI x1.5 (`CombatPrototypePanelSettings`) + /loop de QA proactivo; 4 hallazgos QA anotados (contraste dragones, TurnLogPanel vs zoom, presion de oleadas, alturas de cards).

## Aun vigente — sistemas de tienda/mundo (el juego base)

- `IAnchorPlace`/`AnchorRegistry` (S21): Admit estampa ancla+persiste · Release limpia+persiste · TryReclaim NO persiste · DetachOccupant silencioso.
- Identidad del corral/mueble = CellKey del `PlacedFurnitureMarker`, estampada DESPUES del Instantiate → los containers resuelven su key en `Start()`, nunca en Awake (S9).
- NPCs: `walkableAreaNames` case-sensitive contra Navigation; `NavMeshAgent.stoppingDistance` ≈ 0 en el prefab NPC o el frente de la cola nunca llega a la caja (S20).
- `UniqueID` = `ToStringID()-Timestamp`; la genetic string EMBEBE `BaseColor`: la key es fuente de verdad del color; `ReconcileColors` en `CreatureRegistrySO.LoadFrom` es self-heal permanente (S15).
- Quirk carga en frio post-wipe: mints hechos apenas arranca Play pueden borrarse cuando el pull de CloudSync aterriza tarde (S39).
- Social: `SocialGraphService.EffectiveAffinity` = semilla genetica + delta de historia (clamp ±0.5); persistencia SOLO local (regla NeedsState); diales `Sociability`/`Boldness` desplazan umbrales de las 5 reglas.
- Genetica visual: FurType `Pattern00..32` (mint ponderado, herencia 50/50) · `IsShiny` 0.5% (materiales `MonchiGem_*` por hash) · armonia de color determinista por hash del BaseColor · cuerpo = `MonchiVisualBankSO.GetBody` FNV-1a % 4. **Deuda latente**: `bodyOverrides` vacio — un 5.o cuerpo remapearia TODAS las criaturas; llenar overrides antes.
- Caras = mood, no gen: `MonchiMoodSetSO` mapea 12 moods → materiales; caras excluidas 12/16/20/21/22 esperan veredicto de Juan.
- Fotomaton: `MonchiPortraitService` cachea por UniqueID y NUNCA invalida; capas reservadas `PortraitStudio` 9, `MonchiFocus` 10.
- Localization: `Loc.Tr`/`LocEnumMaps` (unico dueno de keys de enums); EN fuente; `Loc.Tr` fuera de Play devuelve la key by design. **Fase 2 pendiente**: textos en SOs + pase editorial de `es`.
- Equipment sigue vivo A PROPOSITO (las Cutie Marks lo reemplazan recien con UI propia); `IsDead` no tiene escritor desde la demolicion (el permadeath sera el dueno nuevo).
- `Theme.uss`: tokens `--mm-*`; popups instanciados fuera del arbol tematizado cargan Theme.uss + clase `mm-theme`; UITK runtime NO soporta box-shadow/gradient.
- Sobrevivientes del spider: `MonchiAnimationDriver` + regla de color 60/30/10. Escena `SuriyunSimTest.unity` = paleta de diseno autocontenida.

## Aun vigente — quirks tecnicos transversales

- Bug del EDITOR de Unity: `MissingReferenceException: UIDocument destroyed` al salir de Play con un GO con UIDocument seleccionado en el Inspector — deseleccionar antes de parar (S45/S46, probado por experimento controlado).
- Quirk UITK world-space: un UIDocument world-space auto-genera un BoxCollider de picking (Fixed 1920x1080 ≈ caja de 20 m) — nacer `Dynamic` y silenciar el collider con `enabled=false` (S64).
- UnityEvent NO serializa argumentos enum → solo metodos sin args cableables; cuidado con colisiones de nombres entre componentes del mismo GO (S24).
- DamageNumbersPro: el "+" en curas exige `enableLeftText = true` + `UpdateText()`; asmdef autoReferenced (S34). Relevante para popups §10.6 del prototipo.
- Quirk Odin: campo nuevo con initializer agregado a una clase YA serializada deserializa en 0/null, no en el default del codigo (S57).
- Truco MSBuild (verificar compilacion sin editor): csproj clonado con Reference a `Library/ScriptAssemblies/*.dll`, borrar despues (S75; hoy lo cubre `unity recompile` del CLI).
- Flags de debug que QUEDARON activados: `hideForDebug=true` en CombatSpeechBubbles (escena CombatVisualizerMM) y prefab MoriMonchiVisualizer (S45 — escena/prefab legacy); acciones vestigiales `Previous`/`Next` en InputSystem_Actions (S36).
- Metodo de paridad reutilizable (S40): fixtures sobre CLONES + semillas fijas + SHA256 del log antes/despues de refactors.

## Aun vigente — combate async/base (codigo demolido S75, decisiones que renacen)

- Server = matchmaker + oraculo de seed (S32): los JS de Cloud Code NO simulaban; ambos clientes corrian `SimulateCore` con la misma seed. El patron vale para el async futuro del rediseno.
- `CombatHistory` ilimitado dentro de snapshots infla blobs de Cloud Save (S32) — vigilar en el sistema nuevo.

## Arrastres historicos CON DETALLE (lo que las agendas citan cripticamente)

### "Pendiente editor S75" (obligatorio, sigue abierto)
1. Crear los .asset nuevos: `HornDatabase`/`BackDatabase`/`WingDatabase`/`FaceDatabase` + sus partes (5 por slot) + `CutieMarkDatabase`.
2. Rewirear `CreatureDatabase.asset` (campos Horns/Backs/Wings/Faces) y `GameManager` (cutieMarkDatabase).
3. Limpiar `GameScene.unity`: 5 componentes con script muerto (GOs CombatPanelUITK, CombatLineupUITK, AsyncCombatService, CombatController, CombatDevConsole), el PanelTrigger con `panel: 4` y la entrada Combat del dict de UIManager.
4. Consola 0 errores + Play.

### "Endpoints Cloud Code" (limpieza de dashboard UGS)
Despublicar los 5 endpoints ya borrados del repo (`run-combat`, `enqueue`, `dequeue`, `process-matchmaking`, `get-queue-status`) y borrar schedule+trigger `matchmaking-tick`. Va por Admin API (referencia en ClaudeOld.md). El cliente ya no los llama.

### "Keys huerfanas" (Localization)
Borrar de la tabla Strings: `outcome.*`, `ui.detail.combat.*`, `status.queued`. Mejor desde el editor de Localization.

### "Reescritura de Index/02"
[[02 - Genetics & Breeding]] sigue describiendo ArmPart/EyePart/MouthPart. Real desde S75: partes **Horn/Back/Wing** (mecanicas) + **Face** (visual); string `BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB` (FromID exige 5 tokens); `PartRole {Body=0,Horn=1,Back=2,Wing=3,Face=4}`; prefijos H/BK/W/FC (CM para CutieMarks).

### Backlog congelado (~S70, reactivable)
- Pataleo en ragdoll (clips Suriyun gateados por IsAirborne en MonchiLocomotionAnimator).
- Eyeball de Juan al petting hold-E / hand-feed con input real.
- Arte pixel: Juan dibuja el marco 48x48 9-slice en Aseprite → sesion de enchufe sobre Theme.uss (prompts en [[14 - Art Prompts]]).
- Remocion del building ⏸️ (el rediseno podria dar uso nuevo a la tienda).
- Draft compartible del modo aventura (pedido original S75).
- `CombatHpBar.uxml` parece huerfano — confirmar y borrar.
- Deuda de la nota 16: divergencias vault↔codigo.

## Donde esta el detalle

- S8-S45 completo: git, ruta original `MoriMonchiVault/Index/09b - Session Archive.md` (borrado S90).
- S46-S79 completo: git commit `cf5ab14`, ruta original `MoriMonchiVault/Index/09b - Active Context Archive.md` (borrado S90).
- S80-S88 completo: git commit `8ac520d`, ruta `MoriMonchiVault/Index/09 - Active Context.md` (podado S90).
