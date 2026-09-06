---
tags: [index, expedition, arena, sandbox, cues]
---

# 23 - Arena Sandbox y Expedición (implementación, S97-S101)

**Responsabilidad:** la escena de pruebas donde se sueltan criaturas para ver comportamientos emergentes (Fase 1), la capa de metas de expedición configurable por ScriptableObjects (Fase 2, iniciada) y las guías visuales sobre el suelo. Es la contraparte de implementación de [[Index/22 - Bajada Nocturna y Linaje (Draft)]] Parte 8. Quirks de herramientas en [[Index/12 - Unity MCP]] (sección S96-S97).

Relacionado: [[Index/06 - Player & World]] (el agente y sus colaboradores), [[Index/05 - UI System]], [[Index/11 - Technical Debt]].

---

## 1 · La escena `ArenaSandbox`

`Assets/RunRunSimulator/Resources/Scenes/ArenaSandbox.unity` (no está en build settings; es sandbox). Se abre por MCP o `eval`; no depende de `GameManager`.

| Objeto | Qué es |
|---|---|
| `Environment/Ground` + `WallN/S/E/W` | Cubo 40×40 con top en y=0 y cuatro muros bajos; materiales URP Lit en `Resources/Materials/Arena/` |
| `Environment/Obstacles` | 7 árboles (cilindro + esfera) y 5 rocas (cubos) sembrados con `System.Random(4242)` fuera del radio de 6 m del centro. Bloqueo, no arte |
| `Directional Light` | Sombras suaves |
| `Main Camera` + `CinemachineBrain` · `ObserverCamera` (`CinemachineCamera`, FOV 42) | Cámara fija en alto; para clips se orbita por script |
| `NavMesh` (`NavMeshSurface`) | Tipo de agente **Morimonchi** (`-1372625422`), `CollectObjects.All`, `RenderMeshes`; **`NavMeshData` persistido** en `Scenes/ArenaSandbox/NavMesh-NavMesh.asset` (el `BuildNavMesh()` por script no lo persiste solo) |
| `SpawnCenter` | Centro de spawn de criaturas y del mineral central |
| `ArenaSandbox` | Componente [[ArenaSandbox]]: refs a prefab de criatura, `RoleWorldProfileTable`, `SocialTuning`, `ExpeditionRules`, `MonchiVisualBank`, `FurTypeDatabase`, `CreatureDatabase`, prefab de mineral; observer = Main Camera; seed 4242, count 3, radio 4 |
| `ArenaCueOverlay` | Componente [[ArenaCueOverlay]]: refs al sandbox, `MonchiCue.mat`, `CueStyle.asset` |

**Assets propios de la arena:** `Resources/Materials/Arena/{ArenaGround,ArenaTrunk,ArenaCanopy,ArenaRock,ArenaWall,ArenaMineral,MonchiCue}.mat` · `Resources/FX/Arena/{FX_DustGround,FX_DustPuff,FX_SmokePuff}.prefab` (variantes de Hovl: `loop=false`, `playOnAwake=false`, `stopAction=Disable`, escala 0,15-0,3) · `Resources/Prefabs/Arena/Mineral.prefab` (cristal emisivo + `Perceivable` de tipo `Material` + [[MaterialPickup]]) · `ScriptableObjects/Expedition/CueStyle.asset` · `ScriptableObjects/Expedition/ExpeditionRules.asset`.

---

## 2 · Scripts

| Script | Ruta | Rol |
|---|---|---|
| [[ArenaSandbox]] | `World/Expedition/ArenaSandbox.cs` | Genera N criaturas al azar con semilla (mint espejo de `GameManager`), las suelta sobre el NavMesh del tipo del prefab, mantiene sus needs llenas, siembra minerales (1 central de valor 5 y escala 2,5 + 4 de esquina), botones Respawn / Reseed. Expone `Spawned`, `Minerals`, `ActiveSeed` |
| [[ArenaCueOverlay]] | `World/Expedition/ArenaCueOverlay.cs` | Presentación: por criatura dibuja anillo punteado girando al `PerceptionRadius`, ruta curva (Catmull-Rom por los corners, tangente inicial = forward) con destino suavizado y fundido, líneas hacia lo percibido teñidas por afinidad; discos bajo minerales. **Solo lee** la fachada del agente |
| [[CueDrawer]] | `World/Expedition/CueDrawer.cs` | Dibujante estático en modo inmediato (`Graphics.RenderMesh` sobre un quad + `MaterialPropertyBlock`): `Ring`, `DashedRing`, `Disc` (+ degradado radial), `Segment` / `Arrow` (+ dos colores), `DashedSegment`, `Arc`; `Configure(material, additiveMaterial)` y `bool additive` por llamada. Cero GameObjects |
| `MonchiCue.shader` | `Shaders/MonchiCue.shader` | URP unlit; SDF en espacio de mundo sobre XZ con anti-aliasing por `fwidth`; `_Shape` 0 Ring · 1 Disc · 2 Segment · 3 Arrow · 4 DashedRing · 5 Arc · 6 DashedSegment; `_ColorB` (degradado a lo largo / angular), `_InnerAlpha/_OuterAlpha` (radial), `_DashLength/_DashGap/_DashOffset`, `_ArcStart/_ArcSweep`, blend por `_SrcBlend/_DstBlend` |
| [[MaterialPickup]] | `World/Expedition/MaterialPickup.cs` | Recolectable: `Value`, `Taken`, `TryTake(out int)`; se desactiva al tomarse (y con eso su `Perceivable` se desregistra) |
| [[AgentExpedition]] | `World/AI/AgentExpedition.cs` | Colaborador del agente: "si veo material voy, lo tomo, vuelvo a vagar". `TryEngage()` puntúa `Percepts × ExpeditionRulesSO.Current.Rules`; `TickExpedition()` repath, give-up y llegada planar; `Collected`, `Target`, `Intent = Collecting` |
| [[CueStyleSO]] | `Data/Expedition/CueStyleSO.cs` | **Gancho de datos**: diccionario Odin `CreatureIntent → Color` + knobs (anillo, curva, percepciones, minerales), `ColorFor`, `PopulateDefaults` |
| [[ExpeditionRuleBase]] | `Data/Expedition/ExpeditionRuleBase.cs` | `ExpeditionGoal`, regla abstracta `Matches(in Percept, self, rules, out score)` y `SeekMaterialRule` (`MaxDistance`, `BoldnessBias`; score `1/(1+dist)`) |
| [[ExpeditionRulesSO]] | `Data/Expedition/ExpeditionRulesSO.cs` | `Current` (patrón `SocialTuningSO`), lista polimórfica Odin de reglas, `ArriveDistance`, `RepathInterval`, `GiveUpSeconds`, `PopulateDefaults` |

**Modificados en S97:** [[MoriMochiAgent]] (compone `AgentExpedition`; despacho `Expedition`; `Intent` prioriza Socializing → Expedition → brain; fachada `Percepts`, `CollectedMaterial`, `ExpeditionTarget`) · [[AgentContext]] (`AgentState.Expedition`, incluido en `IsNavMeshControlled`) · [[MonchiLocomotionAnimator]] (`UnityEvent onTakeOff`, `onFlyLand`) · [[MonchiMoodDriver]] (`Collecting → Emocionado`) · `Core/Enums` (`CreatureIntent.Collecting = 18`, `PerceivableKind.Material = 4`) · prefab `MorimonchiAgent` (hijo `Feedbacks/` con `OnLand`, `OnGetUp`, `OnBounce`, `OnTakeOff`, `OnFlyLand`, cada uno `MMF_Player` + `MMF_ParticlesInstantiation` en Pool, enchufados a los `UnityEvent`) · tabla `Localization/Strings` (`intent.collecting`).

---

## 3 · Flujos

**Spawn** (`ArenaSandbox.Spawn`): `activeSeed` → `UnityEngine.Random.InitState` + `System.Random` → por criatura: `CreatureGenerator.GenerateRandom` + Element/Role/stats/diales/nombre/`Stamp` (mismo orden que `GameManager.MintRandomCreature`) → punto en el círculo muestreado con `NavMeshQueryFilter` del tipo del prefab → `Instantiate` bajo `SpawnHolder` (inactivo) → `areaMask = AllAreas` → reparentar (activa) → `Initialize(dna, table, observer, bank, furDb)` → arranca en Roaming. Después los minerales. `Update` rellena needs si `keepNeedsFull`.

**Expedición** (`MoriMochiAgent.Update`): en `Idle` / `Roaming`, `expedition.TryEngage()` va **antes** que `social.TryEngage()`; si engancha, `ctx.State = Expedition`, destino = el material; `TickExpedition` repath cada `RepathInterval`, abandona a los `GiveUpSeconds`, y al llegar (`ArriveDistance` planar) `TryTake` → emote Feliz → `RequestRoam()`. En la tienda `ExpeditionRulesSO.Current == null` → `TryEngage` devuelve false → cero impacto.

**Guías** (`ArenaCueOverlay.LateUpdate`): `CueDrawer.Configure(material)` en `OnEnable`; por criatura, estado `CueState { Nav, ShownEnd, HasShown, Alpha, Corners }` para suavizar y fundir la ruta; todo se dibuja cada frame en modo inmediato.

**Juice**: los `UnityEvent` del agente y del animador de locomoción disparan `PlayFeedbacks()` de los `MMF_Player` del hijo `Feedbacks/`; el contenido vive en el Inspector (regla de Feel).

---

## 4 · Invariantes y reglas de oro (S97)

- **La presentación solo lee.** Overlay, dibujante y estilo nunca mutan al agente ni al mundo; leen por la fachada pública de `MoriMochiAgent` y por `Perceivable`.
- **Datos por ScriptableObject, lógica mínima.** Una intención nueva = valor de enum + entrada en `CueStyle` + caso en `MonchiMoodDriver` + clave `intent.<nombre>` en la tabla. Una meta nueva = subclase de `ExpeditionRuleBase` + entrada en `ExpeditionRules.asset`. Nada de colores ni umbrales en C#.
- **El prefab de la tienda no cambia por la arena**, salvo el hijo `Feedbacks/` y los dos eventos de vuelo (ambos inertes si nadie los enchufa).
- **`IsNavMeshControlled` debe incluir todo estado con NavMesh activo**: se agregó `Expedition`. `HandFeed` no está (preexistente, sin tocar).
- **Máscara de áreas**: el prefab trae `areaMask = 56` (áreas de tienda). Cualquier escena con NavMesh Walkable debe fijar `AllAreas` antes de activar el agente (ver quirk 5 de `Index/12`).
- **NavMesh por script**: persistir `navMeshData` como asset o se pierde al reiniciar el editor.
- **FX de Hovl**: nunca usar los prefabs de demo directo (loops infinitos); solo variantes con `loop=false` y `stopAction=Disable`.
- **Determinismo**: el layout y el mint son deterministas por semilla; NavMesh y física no lo son entre máquinas. El sandbox es para feel y emergencia; la reproducción bit a bit del rival es asunto de Fase 3.

---

## 5 · Auditoría fija (cómo se verifica cada cambio)

1. `unity command recompile` → `recompile_status` con `failed:false` → `console --tail` sin errores.
2. `editor_play` sobre `ArenaSandbox` → sondas por `eval_file` (posición, `onMesh`, velocidad, intent, clip de animación, `Percepts`, `CollectedMaterial`, minerales restantes, cantidad de `ParticleSystem` vivos) → `capture_game_view --source screen` MIRADAS → `editor_stop`.
3. Números de referencia S97: 3 criaturas `onMesh` desde el frame 0; velocidad 2-7; pool de partículas estable en 4; el más osado toma el mineral central en ~17 s con semilla 4242; 0 errores; únicos warnings "part slots empty" (databases de partes vacías desde S75).
4. Clip: `capture_frames.cs` (delegado en `EditorApplication.update` + `ScreenCapture` fuera de `Assets/`) + ffmpeg (quirk 9 de `Index/12`).

---

## 5b · Lenguaje visual de guías (la vara es Shapes) ⭐

Regla de Juan (S97): las guías tienen que salir con la calidad del asset **Shapes** (Freya Holmér) sin que él lo pida: *"así como mejoré tus clues visuales iniciales, no quiero tener que decírtelo"*. Memoria: `feedback-guias-visuales-vara-shapes`.

**Vocabulario de Shapes que adoptamos** (de su documentación): puntas de línea None / Square / **Round**; uniones de polilínea **Round**; **dashes** Basic / Angled / **Rounded** con tamaño, espaciado y **offset animable** (el offset en la dirección del movimiento es lo que hace que una ruta "fluya"); **snap** End-to-End (un dash en cada extremo) para que los punteados no corten feo; grosor en **metros** para guías de suelo, con **fundido por delgadez** (nunca menos de 1 px, se atenúa por cobertura); **degradados** lineales en líneas (cola → cabeza), **radiales y angulares** en discos, anillos y arcos; **arcos y pies** (anillos parciales con puntas redondas); blend **transparente** para lo ambiental y **aditivo** para resaltes activos; y la regla de dibujo "primero lo opaco, después lo transparente".

**Cómo se ve cada guía nuestra a ese nivel (spec v3, objetivo del próximo lote de guías):**

| Guía | Hoy | Nivel Shapes |
|---|---|---|
| Percepción | anillo punteado redondeado girando | igual + **arco de atención**: el sector del anillo que mira a lo percibido se enciende con degradado angular; "respiración" de escala 1 → 1,05 cuando entra un percepto nuevo |
| Intención (ruta) | curva con fundido, flecha, destino suavizado | igual + **dash que fluye hacia el destino** (offset animado), **degradado de alfa** cola → cabeza, y **marcador de destino**: disco pulsante que aparece con escala 1,4 → 1 al fijar destino |
| Lo percibido | línea fina por afinidad, atenuada por distancia | punteada fina con flujo lento hacia lo percibido, puntas redondas, degradado hacia el otro extremo |
| Objetivo elegido | nada | **retícula**: cuatro arcos alrededor del material elegido que entran con escala 1,4 → 1 y giran despacio; desaparecen al tomarlo |
| Minerales | disco plano | **disco con degradado radial** (alfa 0,35 → 0) + anillo fino; el central además con anillo punteado girando en sentido contrario al de percepción |
| Interacción social | nada | enlace punteado con puntas redondas entre los dos; en pelea, **arco rojo** que pulsa |
| Nervio (Fase 2) | nada | **arco medidor** alrededor de la criatura con degradado angular verde → rojo, se vacía con punta redonda |
| Aparición / salida | corte seco | **toda guía** entra con escala 0,85 → 1 + alfa en 0,25 s y sale con alfa; nada aparece de golpe |

**Hecho en S97 (spec v3 cumplida y verificada en Play, capturas `s97_v3_*.png` y clip `s97_arena_clip3.mp4`):** `MonchiCue.shader` con formas **5 Arc** (puntas redondas, degradado angular `_Color → _ColorB` por `_ArcStart/_ArcSweep`) y **6 DashedSegment** (`_DashLength/_DashGap/_DashOffset`, puntas redondas por dash, recorte a los extremos), degradado de color/alfa a lo largo de Segment y Arrow (`_ColorB`), degradado radial en Disc (`_InnerAlpha/_OuterAlpha`) y blend por propiedad (`_SrcBlend/_DstBlend`) → materiales `MonchiCue.mat` (alpha) y `MonchiCueAdditive.mat` (One/One). `CueDrawer.Configure(material, additiveMaterial)` + `bool additive` en todos los métodos + `Arc`, `DashedSegment` y sobrecargas con dos colores. `ArenaCueOverlay`: `CueAnim` (fundido y escala de entrada/salida) para anillo, marcador de destino, retícula y minerales; arco de atención hacia el percepto más cercano; respiración del anillo al entrar un percepto; ruta punteada con fase continua fluyendo al destino y alfa cola → cabeza; marcador de destino pulsante; retícula de 4 arcos girando sobre `ExpeditionTarget` con salida en la última posición; minerales con halo radial + anillo (+ punteado contrarrotante en el central) y fundido al tomarse; enlace social punteado (`SocialPartner`, rojo pulsante aditivo en pelea). Convención de ángulos compartida: radianes, 0 = +X, crece hacia +Z (`Atan2(z, x)`).

**Pendiente de la vara (siguiente pasada):** medidor de nervio (cuando exista el nervio) · texto/iconos sobre criaturas (hoy solo `NameTag` UITK) · afinar radios y alfas mirando capturas (marcadores de destino un poco blandos a la altura actual de cámara).

---

## 5c · Lote de realismo (S98): gestos, mirar, giros, caminar con motivo

Cumple los puntos 1-3 del plan de realismo de `Index/22` 8.6 ("el asset full mapeado": los 23 clips del pack Suriyun Dragons_SD entran a la conducta).

- **Un solo dueño del Animator en gameplay ⭐:** [[MonchiLocomotionAnimator]] suma gestos (`PlayGesture` de un disparo con duración = largo del clip · `HoldGesture` sostenido · `StopGesture` · `IsGesturing` · `IsStill`) y **giros con los 6 clips laterales** (`Walk_L/R`, `Run_L/R`, `Fly_L/R`, estados del `MonchiAnimator.controller` → 23 estados, 0 parámetros; se eligen por tasa de giro suavizada con histéresis, `turnThreshold` 100°/s en el prefab, guardado por `HasState`). Nadie más escribe en el Animator (el `DragonAnimationDriver` de combate manda cuando `IsBusy`).
- **Presentación que solo lee (espejo de `MonchiMoodDriver`):** [[MonchiGestureDriver]] + [[MonchiGestureSetSO]] (asset `ScriptableObjects/Visual/MonchiGestureSet.asset`: gesto al entrar en una intención — Taking→Eat, Fighting→Roar, Losing→No, Dazed→No —, gesto sostenido — Resting/SleepingTogether→Rest, `Condition.Sick`→Sick —, fidgets ponderados con osadía mínima — No 1, Yes 1, Eat 0,6, Roar 0,8 con osadía ≥ 0,55 —, intervalo 4-9 s que solo se consume cuando el fidget realmente sale). [[MonchiGazeDriver]]: quieta, gira el `ModelRoot` (no el agente) hacia `ExpeditionTarget` > `SocialPartner` > percepto más cercano, ±70°, 240°/s; vuelve a 0 al moverse. Ambos ignoran combate y estados de física (Held/Thrown/Recovering).
- **Caminar con motivo:** `RoleWorldProfile.RoamSpeedFactor` (0,35 en `RoleWorldProfileTable.asset` para los 3 roles) y `AgentContext.ApplyGaitSpeed()` cada frame desde `MoriMochiAgent.Update` = **único dueño de `NavMeshAgent.speed`** (Roaming → base × factor; Courting no se toca; el resto → base). `AgentBrain.EnterRoaming` ya no fija velocidad. Hallazgo: la velocidad base es la del prefab (`speed = 7`), nadie lee `RoleWorldProfile.MoveSpeed`; con 0,35 vagan a 2,45 (Walk) y con propósito vuelven a 7 (Run).
- **Beats de notar y tomar (por SO):** `ExpeditionRulesSO.NoticeSeconds` 0,5 / `TakeSeconds` 1,2; [[AgentExpedition]] con fases Noticing (se frena, `?`, el gaze gira el modelo al cristal) → Moving → Taking (`CreatureIntent.Taking = 19`: se frena, encara, Eat, y recién al vencer `TryTake` + Feliz + `UnityEvent onPickup`).
- **Juice solo con Feel:** `MoriMochiAgent.onPickup` → `Feedbacks/OnPickup` (`FX_StonesHit`); `MaterialPickup.onTaken` + `disableDelay` 0,8 → prefab `Mineral` con `Feedbacks/OnTaken` (Shrink del hijo `Crystal` + chispas).
- **Arena vestida con Synty (POLYGON Nature, `Assets/Synty/`, fuera del repo por `.gitignore`):** los 7 árboles y 5 rocas de primitivas se reemplazaron en las mismas posiciones por prefabs del pack + 37 piezas de `Decor`, todo `NavMeshModifier.ignoreFromBuild`; NavMesh rebakeado y persistido. Los prefabs de Synty no se modifican (copiar a `Resources/Prefabs/Arena/` si hiciera falta).

| Script | Cambio S98 |
|---|---|
| [[MonchiGestureDriver]] · [[MonchiGazeDriver]] · [[MonchiGestureSetSO]] | NUEVOS (presentación + set de gestos) |
| [[MonchiLocomotionAnimator]] | + gestos, clips laterales, `ClipLength`/`HasState` cacheados |
| [[AgentExpedition]] · [[ExpeditionRulesSO]] | fases Noticing/Moving/Taking, `NoticeSeconds`/`TakeSeconds`, `onPickup` |
| [[MoriMochiAgent]] · [[AgentContext]] · [[AgentBrain]] | `onPickup`, `ApplyGaitSpeed`, `EnterRoaming` sin velocidad |
| [[RoleWorldProfileSO]] · [[MaterialPickup]] · `CreatureEnums` · [[MonchiMoodDriver]] · [[CueStyleSO]] | `RoamSpeedFactor` · `onTaken`/`disableDelay` · `Taking` · Taking→Feliz · color Taking |

---

## 5d · Pulido en loop de QA, elenco y equipos, cámara de grupo (S99)

Demo "viva" declarada tras dos corridas verdes seguidas (sondeo + capturas miradas), con dos hallazgos de Juan resueltos ("se overlapean en el cristal", "el cristal al explotar los cubre").

- **Borde del cristal ⭐:** `MaterialPickup.Radius` (perezoso, bounds del renderer, porque el sandbox escala después de instanciar; `standoffRadius` como override) y `ApproachPoint(from, margin)`. `AgentExpedition.ApproachPoint(rules)`: punto del borde del lado de llegada (`rim = Radius + Agent.radius + ApproachMargin`) con **reparto angular** entre los que apuntan al mismo cristal (`sep = 2·asin((r+0,1)/rim)`, dos pasadas). Llegada = distancia planar ≤ `ArriveDistance` (0,4) o bloqueado 0,6 s cerca del cristal. `NavMeshObstacle` cápsula sin carve en `Mineral.prefab`. Sondeo: `minToTarget` ≥ 0,95 y `minPair` ≥ 0,79 (antes 0,00).
- **Beat de perder ⭐:** fase `Losing` (el cristal se lo llevó otro o `TryTake` falla): freno, encaro `lostPoint`, Molesto, `LoseSeconds` (1,1) y a vagar. Receta de intención nueva cumplida: `CreatureIntent.Losing = 20` · Mood Triste · `CueStyle` gris-azul · gesto `No` · `intent.losing` ("Se le fue" / "Missed it").
- **Explosión legible sin tapar:** Shrink Additive (`RemapCurveZero/One = 0/1`, quirk 4 de `Index/12` S98-S101), 0,5 s, curva pop; `FX_ShardsWhite` (60 esquirlas, la variante azul era invisible sobre pasto a 21 m); `FX_StonesHit` a 0,3.
- **Piso vestido:** `ArenaGround.mat` con `Ground/Grass_01` de Synty y `Environment/GroundCover` (392 piezas: matas, pasto alto en el borde, flores, 11 parches de tierra bajo cada mineral, piedritas; `ignoreFromBuild`, sin colliders, NavMesh sin rebake).
- **Placas y burbujas en la arena:** `NameTag.ShowDistance` (propiedad) + `ArenaSandbox.tagShowDistance` (120 en la escena) y `tagReferenceDistance` (9): a 21-44 m se leen nombre, intención y burbujas `?`/`!`/`:)`. El prefab de la tienda no cambia de comportamiento.
- **Elenco básico y equipos (Etapa 3 · paso 1 ⭐):** [[ArenaRosterSO]] + `ScriptableObjects/Expedition/ArenaRoster.asset` (Player: Osado 0,25/0,9 · Tímida 0,85/0,15 · Equilibrado 0,5/0,5; Rival: Fiero · Cauta · Templado espejo; `BodyShapeID` por `StableHash % 4`; `BaseColor` alfa 0 = aleatorio). `ExpeditionTeam { None, Player, Rival }` + `ExpeditionTeams.AreRivals/AreAllies` (`WorldEnums`); `Perceivable.Team` (+ `SetTeam`, fijado por el sandbox ANTES de activar) y `Percept.Team` (lo llena `AgentSenses`); fachada `MoriMochiAgent.Team`. `ArenaSandbox.TeamCorner` (Player esquina −,− · Rival +,+ con `teamSpawnInset` y radio 2,5; modo aleatorio conservado con `useRoster`). `AgentSocial`: rivales salteados en `TryEngage` y rechazados en `CanPair`. Guías: rival → `FoeColor`, aliado → `FriendColor`. `NameTag`: `allyNameColor` verde pastel · `rivalNameColor` rojo pastel por `agent.Team` (sin equipo sigue el género), `ScreenSizeReferenceDistance`, USS `.tag__name` con contorno y sombra.
- **Cámara de grupo (Bad North):** `ArenaTargetGroup` (`CinemachineTargetGroup`; el sandbox agrega/quita miembros) + `ObserverCamera` con `CinemachinePositionComposer` (distancia 30, damping 0,8) y `CinemachineGroupFraming` (0,85, damping 0, dolly −10..+24, FOV 30..55), pitch 56°. `Environment/Outskirts` (plano 180 m verde oscuro) tapa el gris fuera de la arena.

| Script | Cambio S99 |
|---|---|
| [[ArenaRosterSO]] | NUEVO (elenco por asset) |
| [[AgentExpedition]] · [[MaterialPickup]] · [[ExpeditionRulesSO]] | borde del cristal, reparto angular, `Losing`, `LoseSeconds` |
| [[ArenaSandbox]] | `MintRandom`/`SpawnCreature`/`TeamCorner`, roster, `tagShowDistance`, target group |
| [[Perceivable]] · [[AgentSenses]] · [[AgentSocial]] · [[MoriMochiAgent]] · `WorldEnums` | equipos (`Team`, `SetTeam`, `Percept.Team`, `AreRivals/AreAllies`, sin social entre rivales) |
| [[NameTag]] · [[ArenaCueOverlay]] · [[MonchiMoodDriver]] · [[CueStyleSO]] · [[MonchiGestureSetSO]] · `CreatureEnums` | colores por equipo, `ShowDistance`; guías por equipo; Losing |

---

## 5e · Choque físico v1 (S100) y afinado (S101) — Etapa 3 · paso 3

Decisiones de Juan en `Index/22` 8.9: cuerpo único (sin ragdoll articulado), **sin fuego amigo**, perder no cuesta mientras se prueba, tres movimientos, uno por slot.

**Datos (`Data/Expedition/`):** [[ClashMoveSO]] (`ClashSlot Horn/Wings/Back`; `AnticipationSeconds`, `StrikeSeconds`, `Range`, `HitRadius`, `Impulse`, `UpBias`; cuerno `DashSpeed/DashAcceleration/SelfRecoil`; alas `LaunchAngle`; espalda `SweepRadius`; `TellGesture`, `StrikeGesture`) y [[ClashTuningSO]] (`Current` como `ExpeditionRulesSO`; `Horn/Wings/Back`, `EngageRange`, `MinBoldness` 0,45, `Cooldown` 10, `DiveMinDistance` 4, `SweepMinRivals` 2 / `SweepRange` 2,5, `ResolveSeconds` 0,4, `DazedSeconds` 0,7, `ReengageBoldness` 0,7, `RetreatDistance` 8, `VictimGraceSeconds` 6, `ChainImmunitySeconds` 0,8). Assets en `ScriptableObjects/Expedition/`: `ClashTuning` (**`EngageRange` 6 desde S101**) · `ClashMove_Embestida` (tell Roar; dash 14 m/s; impulso 27; arriba 0,3) · `ClashMove_Picada` (tell FlyUp; impulso 33; **arco 32° y radio de impacto 2,5 desde S101**, antes 40° / 2,0) · `ClashMove_Coletazo` (tell No, golpe Jump; radio 2,4; impulso 18). **La masa del Rigidbody es 3**: impulso 9 daba vuelos de 0,5 m; 27-33 dan 4-8 m.

**Colaborador [[AgentClash]]** (`World/AI/`, estado `AgentState.Clashing`): `TryEngage()` en Idle/Roaming **y en Expedition** (antes que expedición y social; si engancha, `expedition.ResetForReuse()`): rival percibido (`AreRivals`), no en el aire/recuperándose/held, `IsClashTargetable`, distancia planar ≤ `EngageRange`, osadía ≥ `MinBoldness`, sin cooldown. Elección: ≥2 rivales a `SweepRange` → coletazo · rival a ≥ `DiveMinDistance` → picada · si no embestida. Fases **Anticipating** (frena, encara, `onClashTell`, emote Molesto, gesto del movimiento) → **Striking** (cuerno = dash por NavMesh con `speed/acceleration/avoidance` sobrescritos y restaurados; alas = `Launch` balístico con anticipación al blanco (`nav.velocity × tiempo de vuelo`) e impacto en `TickAirborne` al bajar; espalda = gesto + `PerceivableRegistry.QueryInRadius` radial) → `Impact` (`victim.ReceiveClashHit(owner, force)` → `Knock(force, stress:false)`) → **Resolving** → `Finish` (cooldown, `RequestRoam`). Lado víctima: `ReceiveHit` (marca `knockedByClash`, `lastAttacker`, inmunidad al dominó, `onKnocked`) → física → `OnRecovered` (gancho `NotifyRecovered` desde `AgentPhysics.TickRecovering`) → **Dazed** (`Intent.Dazed`, gesto No, cara Mareado, gracia de 6 s) → `Decide`: contraataca si osadía ≥ 0,7 y sin cooldown, si no vaga alejándose `RetreatDistance`. `Cancel` (desde `AgentPhysics.Knock` vía `NotifyKnocked`) restaura el NavMesh si nos tumban a mitad de un golpe. `ForceMove` para el botón de desarrollo (no valida osadía ni cooldown).

**Integración:** [[MoriMochiAgent]] compone `clash`; `Intent` Clashing > Socializing > Expedition; fachada `ClashTarget`, `ClashGesture`, `IsClashTargetable`, `ForceClash`; internos `ReceiveClashHit`, `NotifyKnocked`, `NotifyRecovered`, `IgnoresChainKnock`; `UnityEvent onClashTell/onClashHit/onKnocked` (**internos**: los sondeos los toman por reflexión). [[AgentContext]] (`Clashing` en el enum y en `IsNavMeshControlled`, excluido de `ApplyGaitSpeed`). [[AgentPhysics]]: el dominó por colisión/trigger **no golpea aliados** (`AreAllies`; en la tienda None/None no son aliados → sin cambio) **ni al atacante que acaba de golpearnos** (`IgnoresChainKnock`).

**Presentación (solo lee):** `CreatureIntent.Clashing = 21`, `Dazed = 22`; [[MonchiMoodDriver]] (Clashing → Enojado, Dazed → Mareado); [[MonchiGestureDriver]] lee `agent.ClashGesture` (el tell y el golpe salen del asset del movimiento, no del set); [[CueStyleSO]] (+Clashing naranja, +Dazed violeta); `MonchiGestureSet` (+Dazed → No); `Strings` (`intent.clashing` "Embiste"/"Charging", `intent.dazed` "Mareado"/"Dazed"); [[ArenaCueOverlay]]`.DrawClash` (flecha roja aditiva pulsante al `ClashTarget`, toggle `showClash`). Prefab `MorimonchiAgent`: `Feedbacks/OnClashTell` (`MMF_Scale` aditivo +0,12), `OnClashHit` (`MMF_FreezeFrame` 0,08 s + squash **+0,35/−0,3 desde S101** (antes +0,2/−0,2) + `FX_DustPuff`), `OnKnocked` (squash +0,25/−0,3 + `FX_SmokePuff`). Escena: `MMTimeManager` (lo exige el freeze frame), [[ArenaClashDev]] (botones Embestida/Picada/Coletazo por índice y "par más cercano" con distancia máxima), [[ArenaCameraDirector]] (punto 6 de 8.6 adelantado: pesa `focusWeight` 1 a los involucrados en un choque y `idleWeight` al resto en el `ArenaTargetGroup` durante 2,5 s; **`idleWeight` 0,05 desde S101**, antes 0,15: con 0,15 a 44 m no se leía ningún golpe, con 0,05 la cámara entra hasta ver el squash y las burbujas).

**Invariantes del choque (S100-S101):**
- Sin fuego amigo en ningún camino: `TryEngage`, `Sweep`, el dominó de `AgentPhysics` y `ArenaClashDev` filtran por `AreRivals`/`AreAllies`.
- **Gracia de víctima** (`VictimGraceSeconds` 6): tras levantarse nadie la puede volver a elegir; sin ella dos atacantes alternando la acosaban en bucle (17 tells en 62 s).
- **Inmunidad al dominó** (`ChainImmunitySeconds` 0,8): el atacante no sale volando por el cuerpo de su propia víctima.
- **Cooldown 10 s** por atacante tras cada choque o picada (aunque falle).
- Una picada contra un blanco que ya está en el aire o sostenido se cancela (`diving = false`): dos picadas simultáneas mutuas fallan las dos.
- Ningún gameplay lee la cámara: **la arena no pasa `observer` como `Player`** (S101; antes el director acercaba la cámara al choque y el recién levantado reaccionaba con `Retreating` hacia ella por `ReactIfPlayerNear`). `ctx.Player == null` apaga reacciones, caricias y comida de mano sin tocar código.
- La presentación (mood, gestos, cues, feedbacks, director de cámara) solo lee la fachada.

**Auditoría S100 (`s100_probe.cs`, `s100_clip.cs`):** corridas desde el spawn (150-230 s): los equipos tardaban ~145 s en cruzarse (arena de 40 m, vagan a 0,35), 1 picada autónoma por corrida (vuelos de la víctima 6-7 m, mareo 0,7 s), 0,3-0,5 choques/min; forzados: embestida conecta, picada 3 de 5, coletazo sin verificar. Clip `s100_clash_clip.mp4`.

**Auditoría S101 (`s101_probe.cs` autónomo · `s101_probe_b.cs` coletazo con dos rivales por `Warp` · `s101_probe_c.cs` cinco picadas forzadas a 6 m):** corrida A (solo `EngageRange` 6 + `teamSpawnInset` 13): primer contacto a ~3,5 min desde el spawn, 1 picada (conectó, vuelo 7,5 m), `Retreating` 0 (antes aparecía tras levantarse). Corrida A2 (**`PerceptionRadius` 6 → 9 en `SocialTuning.asset`** — knob global, también rige en la tienda — y **`teamSpawnInset` 14**): los dos equipos ven el cristal central desde el spawn (8,5 m), se cruzan a los ~10 s y en los primeros 50 s hubo 6 tells y 2 impactos (vuelos de la víctima 7,5-8,4 m, mareo, 0 `Retreating`); todos los enganches autónomos son picadas porque el primer rival entra al radio a 5,4-6 m (≥ `DiveMinDistance`): las embestidas solo aparecen como contraataque o forzadas. Resultados completos de A2, B y C en `09 - Active Context` (S101).

| Script | Cambio S100-S101 |
|---|---|
| [[AgentClash]] · [[ClashMoveSO]] · [[ClashTuningSO]] · [[ArenaClashDev]] · [[ArenaCameraDirector]] | NUEVOS |
| [[MoriMochiAgent]] · [[AgentContext]] · [[AgentPhysics]] | compone `clash`, estado `Clashing`, ganchos `NotifyKnocked/NotifyRecovered`, dominó sin aliados ni atacante inmune |
| [[MonchiMoodDriver]] · [[MonchiGestureDriver]] · [[CueStyleSO]] · [[MonchiGestureSetSO]] · [[ArenaCueOverlay]] · `CreatureEnums` | Clashing/Dazed en presentación y cues |
| [[ArenaSandbox]] | `clashTuning` (S100); `observer` vacío, `teamSpawnInset` 14 (S101, solo datos de escena) |

| Script | Cambio S101 (ocupaciones, loop hasta gameplay) |
|---|---|
| [[ExitZone]] · [[ArenaRound]] · [[ArenaRoundHud]] | NUEVOS (salida por equipo, ronda, marcador) |
| [[AgentExpedition]] | minado por unidades, carga y depósito, drop al ser tumbado, fases Guarding / Hunting / Decoying, `GuardPost` inyectado |
| [[AgentClash]] | gating por ocupación, preferencia de Break por recolectores y de todos por el provocador |
| [[MaterialPickup]] | `Remaining`, `TryMineUnit`, `Taken` derivado (sin `TryTake`) |
| [[ArenaSandbox]] · [[ArenaRosterSO]] · [[ExpeditionRulesSO]] | salidas, `cornerMineralValue`, orden salidas → minerales → criaturas, `Occupation` por entrada, knobs de ocupaciones |
| [[MoriMochiAgent]] · [[AgentContext]] | `Occupation`, `HomeExit`, `GuardPost`, `Carried`, `MiningProgress`, `NotifyKnocked` → expedición |
| [[ArenaCueOverlay]] · [[CueStyleSO]] · [[MonchiMoodDriver]] · [[MonchiGestureSetSO]] · `CreatureEnums` · `WorldEnums` | salidas y arco de minado; 5 intenciones nuevas; `Occupation`; `PerceivableKind.Exit` |

---

## 5f · Ocupaciones con tiempo y sala de 90 s (S101, loop hasta gameplay) — Etapa 3 · paso 2 reformulado

Implementa la Parte 8.10 de `Index/22` (Juan ⭐: "que no se pueda hacer todo": recolectar toma tiempo, uno vigila, otro distrae). Orden ejecutado: (1) minado con canal + capacidad de cristal + salida + ronda con marcador → (2) Vigilar y Romper sobre el choque existente → (3) Distraer. Todo verificado en Play con siete rondas sondeadas (`s101_round_probe.cs`) y capturas miradas.

**Datos (`ExpeditionRules.asset`, sección "Ocupaciones"):** `MiningSecondsPerUnit` 3 · `CarryCapacity` 3 · `DepositSeconds` 0,8 · `DropPrefab` = `Mineral.prefab` · `DropScale` 0,6 · `GuardRadius` 4 · `HuntRepathInterval` 0,4 · `DecoyRange` 4,5 · `TauntSeconds` 0,8 · `DecoyFleeDistance` 8 · `DecoyFleeSeconds` 5 · `DecoyCooldown` 4. Escena: `centerMineralValue` 40, `cornerMineralValue` 8 (con 20/4 el pozo se agotaba a los 40 s y media ronda quedaba muerta), `exitInset` 4 (salidas a (±16, ±16), 22 m del centro), `teamSpawnInset` 14. Roster: cada `Entry` tiene `Occupation` (Gather / Guard / Break / Decoy; None y Explore cuentan como Gather).

**Enums:** `Occupation { None, Gather, Guard, Break, Decoy, Explore }` (`WorldEnums`) · `PerceivableKind.Exit = 5` · `CreatureIntent` `Carrying 23`, `Securing 24`, `Guarding 25`, `Hunting 26`, `Taunting 27` (receta completa: color en `CueStyle`, ánimo en `MonchiMoodDriver`, gesto en `MonchiGestureSet` (Taunting → Roar, Securing → Yes), claves `intent.*` en `Strings`).

**Piezas nuevas (`World/Expedition/`):** [[ExitZone]] (prefab `Resources/Prefabs/Arena/ExitZone.prefab`: `Perceivable` kind Exit + disco dorado `ArenaExit.mat`; dueño de `Secured` por equipo; `Contains`, `Deposit`, `onDeposit`) · [[ArenaRound]] (cronómetro de 90 s, marcador leído de `sandbox.Exits`, `Winner`, `Restart`) · [[ArenaRoundHud]] (UI Toolkit por código sobre `StandartPanelSettings`: material propio · tiempo · material rival, la línea "quién hace qué" por equipo leída de `ArenaRound.Sandbox.Spawned` (nombre + verbo de la ocupación), y "Gana tu equipo / Gana el rival / Empate"; textos en castellano fijo porque es HUD de sandbox).

**Agente:** `AgentContext.Occupation` / `HomeExit` / `GuardPost` (los inyecta [[ArenaSandbox]] al aparecer: ocupación del roster, salida del equipo y el cristal central como puesto conocido, porque a 10 m nadie lo percibe). [[AgentExpedition]] fases `Noticing → Moving → Mining → (Returning → Securing) | Losing`, más `Guarding`, `Hunting` (con presa o acechando el puesto) y `Decoying` (Approach → Taunt → Flee). `Mining` es un canal: cada `MiningSecondsPerUnit` mina una unidad (`MaterialPickup.TryMineUnit`, `Remaining`), `onPickup` por unidad; al llenar `CarryCapacity` o vaciar el cristal vuelve a `HomeExit` y deposita. **Tumbado con carga = suelta un cristal chico** (`Drop`: instancia `DropPrefab` con `Value = carried`; se percibe y se mina como cualquier otro). [[AgentClash]] gating por ocupación: Gather y Decoy nunca inician; Break prefiere rivales con intención Taking/Carrying/Securing/Collecting y usa picada o embestida (nunca coletazo); **cualquier otro prefiere al rival que está provocando** (`Taunting`), que es lo que hace funcionar al señuelo. `MoriMochiAgent.NotifyKnocked` avisa también a la expedición (drop + reset). Fachada nueva: `Occupation`, `Carried`, `MiningProgress`, `ExpeditionTarget` (material, salida, puesto o presa según fase), `SetOccupation`, `SetHomeExit`, `SetGuardPost`.

**Guías:** [[ArenaCueOverlay]] dibuja las salidas (disco + anillo + punteado lento en el color del equipo), el **arco de minado** que se llena alrededor de la que mina (`MiningArcRadius/Thickness/Alpha` en `CueStyle`), y los minerales desde `PerceivableRegistry` (incluye los caídos), con el disco encogiéndose según `Remaining/Value`.

**Resultados (una ronda por combinación, ruido alto; Player a la izquierda):**

| Player | Rival | Marcador | Lectura |
|---|---|---|---|
| 3 Recolectar | Romper + 2 Recolectar | 10-10 (R3) | el rompedor solo compensa al tercer minero |
| Vigilar + 2 | 3 Recolectar | 12-9 (R4) | el vigía niega el centro (7 golpes) |
| Vigilar + 2 | Romper + 2 | 8-7 (R2, con 20/4) · R7 abajo | la pareja canónica: 14 impactos, 2 drops, remontada final |
| Distraer + 2 | Vigilar + 2 | 6-12 (R5, sin prioridad) → **12-12 (R6, con prioridad)** | el señuelo come todos los golpes del vigía (2 de 2 sobre Osado, 0 sobre mineras) |

**Invariantes S101 (ocupaciones):**
- Una ocupación por criatura, fijada por el sandbox antes de activarla; la tienda no las usa (`ExpeditionRulesSO.Current == null`).
- Solo cuenta lo depositado en la salida propia; `carried` sobrevive a `Abort` pero se pierde al ser tumbado (cae al suelo como pickup, nunca desaparece).
- Guard y Break se plantan en el `GuardPost` inyectado si no perciben nada mejor; Break persigue solo presas con intención de recolección y vuelve al acecho si la pierde.
- El señuelo nunca golpea; provoca a `DecoyRange`, huye `DecoyFleeSeconds` hacia su salida y descansa `DecoyCooldown`.
- Presentación solo lee (`ArenaRoundHud`, overlay); `ExitZone` es el único dueño del material asegurado; `ArenaRound` solo lee y congela al final.

**Cámara (segunda pasada S101, pedido de Juan: "muy exagerado el movimiento… una cámara dinámica con Cinemachine apuntando a la acción pero con cierto control"):** `ObserverCamera` pasa de `PositionComposer` a **`CinemachineOrbitalFollow`** (esfera, radio 30, pitch 56°, binding WorldSpace, damping 1,2) + `CinemachineRotationComposer` (damping 0,6) + `CinemachineGroupFraming` en modo **ZoomOnly** (tamaño 0,75, damping 2, FOV 32-58: el encuadre automático solo toca el FOV, así no pelea con el zoom del usuario). [[ArenaCameraDirector]] más calmo: `idleWeight` 0,35, `blendSpeed` 0,7, `focusHoldSeconds` 4 y **histéresis `minSwitchSeconds` 3** (no cambia de foco más de una vez cada 3 s; los dos involucrados en un choque entran juntos en el mismo frame), `Suspend(seconds)` y `OnDisable` que devuelve todos los pesos a 1. Nuevo [[ArenaCameraControl]] (lee `Mouse.current` / `Keyboard.current` del Input System): **rueda = zoom** (`RadialAxis` 0,55-1,7), **botón derecho arrastrando = orbitar** (yaw libre, pitch 25-80°), **F = alternar el director** (seguir la acción / vista general), **R = volver a la pose inicial**; cualquier input suspende el director 3 s. Rig cambiado por `eval_file` en modo edición y guardado con `manage_scene save` (con Synty presente la escena ya se puede guardar desde el editor).

**Sala por semilla (S101, Juan ⭐: "la semilla debería concentrarse en la distribución de la arena"):** nuevo [[ArenaLayoutBuilder]] (`World/Expedition/`, objeto `Environment/ArenaLayout`): con la semilla de la sala genera en Play los **obstáculos** (pools `treePrefabs`/`rockPrefabs` de Synty: 6 árboles + 6 rocas con colliders; 6 árboles y 4 rocas por sala, `obstacleSpacing` 3,5, escala aleatoria) y las **vetas** (`veins` 4, capacidad 4-8, `veinMinFromCenter` 7, `veinSpacing` 8, muestreadas sobre el NavMesh), con **simetría central** (`mirror`: cada obstáculo y cada veta tienen su espejo por el centro con la misma capacidad, así los dos equipos ven la misma sala) y zonas libres alrededor del centro (6 m) y de las cuatro esquinas (6 m: spawns y salidas). Orden: `Clear` → desactivar el grupo estático `Environment/Obstacles` → instanciar bajo `GeneratedLayout` → **`NavMeshSurface.BuildNavMesh()` en runtime** → vetas. Todo el azar sale de `System.Random(seed)` (misma sala en cualquier máquina). [[ArenaSandbox]]: `seed` gobierna SOLO la sala; el aspecto de las criaturas pasa a `castSeed` (1); `layout.Build` corre entre las salidas y los minerales, y `SpawnMinerals` usa las vetas (fallback a las esquinas si no hay builder). `NavMeshSurface` pasó a **`PhysicsColliders`** (`m_UseGeometry: 1`): el horneado por mallas de render avisaba "does not allow read access" por las mallas combinadas del pasto y los árboles; los rocks de Synty usan `MeshCollider` y siguen avisando en el editor (funciona en Play; en build harían falta mallas legibles o colliders primitivos). El HUD muestra `sala NNNN`. Verificado con `s101_layout_test.cs`: semillas 4242, 777 y 2026 dan 10 obstáculos y 4 vetas distintas cada una (capturas `s101_sala_*.png`).

**Sondeo:** `s101_round_probe*.cs` (scratchpad, se pierde): transiciones de intención, `carried`, marcador, drops (conteo de `Perceivable` Material), tells/hits por reflexión sobre `onClashHit`, capturas automáticas en los primeros minado/carga/depósito/golpe/mareo/acecho/vigilancia y cada 15 s.

---

## 5g · Semilla que reparte la sala, cono de visión, paleta del mapa, elenco desde el save y pantalla de plan (S102, loop hasta gameplay)

Pedido de Juan (S102 ⭐): *"la semilla influencia cómo se distribuye el mapa, rangos de visión en cono marcados con nuestro sistema de UI, lo único fijo es el cristal principal al medio; después una palette shader para el mapa; probar con 3 de mis MoriMonchis; y es momento de hacer la UI"*. Todo verificado en Play con capturas miradas (`Assets/Screenshots/s102_*.png`) y una ronda completa grabada con los MoriMonchis del save local de la PC2 (`s102_gameplay_local.mp4`).

**Semilla ([[ArenaLayoutBuilder]], reescrito):** `System.Random(seed)` decide, en este orden fijo, (1) el **eje de entrada** entre cuatro (`diagonal`, `diagonal inversa`, `norte-sur`, `este-oeste`; `EntryDirection` unitario apunta al lado Rival, `EntryScale` = √2 en las diagonales y 1 en los ejes, `EntryPoint(team, inset)` = centro ± dir · (half − inset) · escala, `ExitPoint(team)` con `exitInset` 4, `SpawnPoint(team)` a `spawnDistance` 8,5 fijo), (2) las **cantidades** por rango (`treeCount` 4-9, `rockCount` 2-6, `veinCount` 2-5, `decorClusters` 6-12 con `decorPerCluster` 3-7 en radio 2,2), (3) obstáculos, decorado y vetas con **simetría central**. Zonas libres: centro (`clearCenterRadius` 6) y los dos spawns + las dos salidas (`clearEntryRadius` 5), ya no las cuatro esquinas. El **decorado por semilla** (`decorPrefabs`: 29 prefabs Synty de flores, arbustos, helechos, hongos, pasto, parches de tierra y piedritas) se instancia sin colliders (`DestroyImmediate` antes del horneado; con `Destroy` diferido los parches entraban al NavMesh y avisaban) y apaga los grupos estáticos `staticDecor` (`GroundCover/Flowers`, `Patches`, `Pebbles`; `Grass` y `EdgeGrass` siguen fijos). [[ArenaSandbox]] pide `layout.SpawnPoint` / `layout.ExitPoint` (fallback a las esquinas diagonales sin builder). El cristal central sigue fijo en `SpawnCenter`. Semillas probadas: 4242 = este-oeste (12 obstáculos, 8 racimos, 6 vetas), 777 = norte-sur.

**Cono de visión (Etapa 3 · paso 6 adelantado):** [[ExpeditionRulesSO]] sección "Visión": `VisionDegrees` 150, `VisionRadius` 11, `NearSenseRadius` 3 (oído: todo lo que está a menos de 3 m se percibe sin importar el ángulo), `BoldnessVisionSkew` 0,25. [[VisionProfile]] (NUEVO, estático, matemática pura): `Resolve(dna, rules)` → radio × (1 + skew) y grados × (1 − skew) con skew = 0,25·(osadía − 0,5)·2 (osado: más largo y más estrecho; tímida: más corto y más ancho, "mira su espalda"); `CanSense(forward, from, target, radius, degrees, near)`; `FacingAngle`. [[AgentSenses]] filtra con el cono **solo si `ExpeditionRulesSO.Current != null`** (la tienda sigue con el radio global, que vuelve a **6 m** en `SocialTuning.asset`; los 9 de S101 eran para la arena). Fachada en [[MoriMochiAgent]]: `HasVisionCone`, `VisionRadius`, `VisionDegrees`, `NearSenseRadius`. **Guía:** `MonchiCue.shader` gana `_Shape` **7 = Sector** (disco recortado por `_ArcStart/_ArcSweep`, degradado radial por `_InnerAlpha/_OuterAlpha`, bordes rectos con AA); [[CueDrawer]] `Sector(...)`; [[ArenaCueOverlay]] dibuja el sector relleno (0,09 → 0), el arco del borde (0,5), los dos lados radiales (0 → 0,3) y el anillo punteado corto del oído, con el rumbo suavizado (`VisionTurnSmoothing` 9) para que el cono no tiemble al girar; los arcos de atención siguen sobre el radio. Knobs nuevos en [[CueStyleSO]] "Cono de visión". Al ser seis conos superpuestos, el relleno tiene que ser casi nulo: con 0,16 la arena se lavaba de color.

**Paleta del mapa (cómo funciona el palette shader):** los assets Synty son mallas cuyas UV apuntan a **parches de color planos de un atlas** (`PolygonNature_01.png`, `Generic_01_A`, `Leaves_*`); el color no vive en la malla sino en la textura. `Shaders/ArenaPalette.shader` (URP, HLSL a mano, 3 pases: ForwardLit + ShadowCaster + DepthOnly) lee el atlas original, saca la **luminancia** del texel y con ella busca el color en una **rampa de 256×1** (`_Ramp`): los tonos oscuros del atlas caen en `Dark`, los medios en `Mid`, los claros en `Light`. Así una sola textura sirve para cualquier paleta y se conservan las diferencias de sombra entre parches. Además: Lambert con luz principal + sombras (`_ShadowStrength`), ambiente por SH, fog, `_AlphaClip` con `_Cutoff` para las hojas recortadas, viento por vértice (`_WindStrength/_WindSpeed/_WindScale`, proporcional a la altura local) para follaje y pasto, `_Cull` heredado. [[ArenaPaletteSO]] (NUEVO, `Data/Expedition/`): seis rampas `Ramp { Dark, Mid, Light }` por **slot** (`ArenaPaletteSlot`: Ground, Grass, Foliage, Trunk, Rock, Wall) + luz (`SunColor`, `SunIntensity`), `AmbientColor`, `FogColor`/`FogDensity`, `SkyColor`. Cuatro assets: `ArenaPalette_Pradera` (verdes), `_Otono` (ocres y rojos), `_Crepusculo` (violetas y rosas, fog denso), `_Nevado` (azules y blancos). [[ArenaPaletteApplier]] (NUEVO, objeto `Environment/ArenaPalette`): `Apply(palette)` genera las seis rampas como `Texture2D`, recorre los `Renderer` bajo `roots` (= `Environment`, que incluye `GeneratedLayout`) y **clasifica cada material original por nombre** (`Trunk` → Trunk; `Leaves`/`Tree`/`Plants` → Foliage; `Moss`/`Rock`/`Pebble`/`PolygonNature_0x` → Rock; `Generic_0x`/`Grass`/`Flower` → Grass; `ArenaGround`/`ArenaOutskirts` → Ground; `ArenaWall` → Wall), crea **una instancia** de `ArenaPalette.mat` por material original (copia la textura base buscando `_BaseMap`/`_Main_Texture`/`_Albedo_Map`/`_MainTex`/`_Texture` con su tiling, `_AlphaClip`, `_Cutoff`/`_Alpha_Clip_Threshold`, `_Cull`, viento según slot) y la recuerda (`instanceByOriginal` / `originalByInstance`) para reaplicar sin duplicar; después fija sol, ambiente plano, fog y color de fondo de la cámara. `ApplyIndex(i)` / `IndexForSeed(seed)` = `|seed| % paletas`. [[ArenaSandbox]]: `paletteIndex` −1 = por semilla; `SetPaletteIndex` / `CyclePalette` cambian en vivo. Lo que se pierde: el viento propio de Synty y el color propio de las flores (toda la vegetación baja cae en la rampa Grass). Los cristales, las salidas y las criaturas no entran en la paleta.

**Elenco desde el save (Juan: "tengo muchos más MoriMonchis"):** [[ArenaCastSource]] (NUEVO, estático, solo lectura): toma el `creature_database*.json` **más reciente** de `persistentDataPath` (en la PC2: 19 vivos del scope `PugP4…`), lo deserializa con `SaveSystem.Deserialize` (mismo serializador, sin tocar `GameManager` ni persistir nada), filtra `IsDead` y `Pick(pool, n, seed)` baraja por `castSeed`. `ArenaCastMode { Roster, LocalSave }` en `WorldEnums`. En `LocalSave` el equipo Player son 3 del save (nombre, cuerpo, colores y diales tal cual; `keepNeedsFull` los mantiene sanos sobre la copia en memoria) y el Rival sigue saliendo del roster. Si no hay save, cae al roster (`LocalCastAvailable`).

**Sandbox partido en sala + elenco ([[ArenaSandbox]], reescrito):** `BuildRoom()` (semilla → `layout.Build` → paleta → salidas → minerales → `PrepareCast()`) y `SpawnCast()` separados; `PlannedCast` (`ArenaCastEntry { Dna, Team, Occupation, Site }`, NUEVO struct) es el plan editable; `SetPlayerPlan(index, occupation, site)` (recordado por nombre entre salas), `SetCastMode`, `ShuffleCast` (castSeed++), `ResetRoom(newSeed)`, `ClearCast`; `Respawn` = `ResetRoom(false)` + `SpawnCast`. **Sitio** (`ArenaSite { Center, NearVein, FarVein }`) = el `GuardPost` que se inyecta: centro, la veta más cercana a la salida propia o la más cercana a la salida rival (siempre un `MaterialPickup`, porque `AgentExpedition.GuardPoint` y `InjectedPost` lo exigen); Distraer lo ignora. **Plan rival por semilla:** cinco combinaciones (`Guard+Gather+Gather`, `Break+Gather+Gather`, `Decoy+Guard+Gather`, `Gather×3`, `Break+Decoy+Gather`) elegidas por `|seed| % 5`; los recolectores rivales reparten Centro / Veta cercana / Veta lejana. `autoSpawnCast` false en la escena (la pantalla de plan lanza).

**Pantalla de plan ([[ArenaPlanPanel]] NUEVO + `UI Toolkit/ArenaPlanPanel.uxml` + `ArenaPlanPanelStyle.uss`, objeto `ArenaPlanPanel` con `UIDocument` sobre `StandartPanelSettings`, orden 20):** tema `.mm-theme` de día (papel) reutilizando `.panel`, `.panel__header`, `.action` de `TransactionPanel.uss`; columna izquierda de 440 px sobre la **vista previa de la sala** (obstáculos, vetas y salidas ya generados, sin criaturas): cabecera "PLAN DE BAJADA · sala NNNN · paleta · entrada X"; una tarjeta por criatura propia (swatch con `BaseColor`, nombre, diales) con dos filas de píldoras: **HACE** (Recolecta · Vigila · Rompe · Distrae) y **DÓNDE** (Centro · Veta cercana · Veta lejana, apagadas para Distrae); herramientas `Elenco básico / Mis MoriMonchis`, `Otros 3`, `Paleta ▸` (cicla en vivo), `Otra sala` (reseed); línea "Rival: nombres · entra por el lado opuesto" (no revela su plan: leerlo es la habilidad de 8.1); botón **¡A LA SALA!** → `SpawnCast` + `round.Begin`. Al terminar la ronda espera `resultHoldSeconds` 4, hace `ResetRoom(false)` + `round.ResetRound()` y vuelve con "Ganaste / Perdiste / Empate N-M" conservando el plan. [[ArenaRound]] `autoStart` false + `ResetRound()`; [[ArenaRoundHud]] oculta marcador, línea de ocupaciones y resultado hasta que la ronda corre (la etiqueta `sala NNNN` queda siempre). Textos en castellano fijo como el HUD.

**Ronda de referencia (sala 4242 este-oeste, Crepúsculo, Mis MoriMonchis: Snotty Pudge vigila el centro, Dizzy Niblet recolecta la veta cercana, Dizzy Nibble recolecta el centro vs plan rival Decoy+Guard+Gather):** 6-0 a los 28 s, 9-5 a los 53 s, 12-10 a los 78 s, **12-10 final** ("Ganaste"); Snotty Pudge tumbó a Cauta en el primer cruce y Templado aseguró 2 por la veta lejana. Los diales de los MoriMonchis del save están casi todos en 0,5 (no se crían todavía), así que sus conos son iguales; el elenco básico sigue siendo el que muestra la diferencia osado / tímida.

**Partición por composición (revisión de cierre S102):** para respetar el límite de ~400 líneas y una responsabilidad por archivo, el plan del elenco vive en [[ArenaCastPlanner]] (clase plana sin `MonoBehaviour`: `Prepare(roomSeed, castSeed, freeCount)`, `SetMode`, `SetPlayerPlan`, `Planned`, `LocalAvailable`; recibe el roster y un `Func<CreatureDNA>` para mintear; carga el save una sola vez), [[ArenaSandbox]] se queda con sala + spawn + sitios y le delega (`PlannedCast`, `CastMode`, `SetPlayerPlan`, `SetCastMode`, `ShuffleCast`), las guías de la sala (minerales y salidas) pasan a [[ArenaRoomCueOverlay]] (segundo componente sobre el mismo objeto `ArenaCueOverlay`, mismas refs) y la ruta curva con su marcador a [[CuePathDrawer]] (estático sobre `PathCueState`); [[ArenaCueOverlay]] conserva las guías por criatura. [[ArenaRound]] es el único dueño del flujo: `Launch()` (spawn del elenco + `Begin`) y `Reset(newSeed)` (sala nueva o la misma + cronómetro a cero); `ArenaPlanPanel` llama al round para el flujo y al sandbox solo para el plan.

**Revisión de cierre (`/code-review medium`, S102):** cinco correcciones que cambian invariantes: `ResetRoom` barre también los cristales caídos (`Drop` los instancia en la raíz; antes sobrevivían a la sala nueva y se minaban en la ronda siguiente); el HUD vuelve a leer la línea de ocupaciones cada vez que se muestra; `ExpeditionRulesSO.Current` **ya no se fija solo en `OnEnable` del asset**: `Activate(rules)` / `Deactivate(rules)` estáticos, y `ArenaSandbox` lo activa en `OnEnable` y lo suelta en `OnDisable` (antes bastaba con haber cargado el asset en el editor para que el cono y el gating por ocupación se filtraran a la tienda); `ArenaLayoutBuilder.Clear` usa `DestroyImmediate` para que el horneado del mismo frame no vea los colliders de la sala anterior; `AgentExpedition.ApproachPoint` solo reparte lugares del borde entre quienes tienen intención `Collecting`/`Taking` (los vigías plantados en el puesto ya no empujan a las mineras a girar).

**Invariantes S102:**
- La semilla gobierna eje de entrada, cantidades, posiciones, decorado y (si `paletteIndex` −1) paleta; el cristal central es lo único fijo. Misma semilla = misma sala en cualquier PC.
- El cono existe solo mientras `ArenaSandbox` está habilitado (`ExpeditionRulesSO.Activate/Deactivate`); la tienda no cambia. Todo lo que está a menos de `NearSenseRadius` se percibe siempre (nadie queda ciego a lo que lo toca).
- La paleta solo toca renderers bajo `Environment` y nunca los assets originales (instancias en memoria, destruidas con el aplicador); sol, ambiente, fog y fondo son estado de presentación.
- `ArenaCastSource` lee y nunca escribe; los DNAs cargados son copias vivas solo en la arena.
- `PlannedCast` es el único plan; `SpawnCast` lo materializa y `ArenaPlanPanel` solo lo edita por la fachada del sandbox (`SetPlayerPlan`).

---

## 6 · Pendientes y deuda

- Etapa 3 (`Index/22` 8.9): la UI de decisión ya existe como pantalla de plan por ocupación + sitio (5g); falta el catálogo `ExpeditionObjective/Role/DirectiveSO` (objetivo → rol → 3 indicaciones) si Juan lo sigue queriendo, y las 2 respuestas ⭐ de 8.7; después paso 4 nervio y memoria corta, paso 5 utilidad con curvas. El cono de visión (paso 6) ya está (5g); falta el oído como sentido aparte y el pizarrón de equipo.
- Paleta: las flores pierden su color propio (rampa Grass); los cristales y las salidas no siguen la paleta; el viento propio de Synty se reemplaza por el del shader; `RenderSettings` quedan como los dejó la última paleta (solo en la arena).
- Elenco desde el save: se eligen por `castSeed` (`Otros 3`), sin selector manual; los diales del save están casi todos en 0,5.
- Choque: variedad de movimientos autónomos (hoy casi siempre picada: la elección es solo por distancia; la capa de tipos de parte de 8.3 debería sesgarla), picadas mutuas simultáneas que fallan las dos, coletazo solo verificado forzado (nunca hay dos rivales a 2,4 m de forma autónoma), perder no cuesta nada (8.7), ragdoll articulado como v2 opcional.
- Reglas que faltan en `ExpeditionRules.asset`: llevar a la salida, confrontar, huir, reagruparse, obedecer; `PerceivableKind` Salida / Peligro; carga y depósito en `AgentExpedition`.
- Capa de rasgos por tipo de parte (`TraitEffectBase` → `PartTraitSO` / `CutieMarkSO`) cuando Juan defina los propósitos.
- Percepción: desde S102 la arena usa `VisionRadius` 11 con cono (5g) y `SocialTuning.PerceptionRadius` volvió a 6 para la tienda; la pregunta de 8.7 (¿conocen el mapa?) sigue abierta, hoy se resuelve con el sitio elegido en el plan.
- Sonido (pack de criaturas por elegir); `Rest` es el único clip sin loop; `com.unity.recorder` instalado sin uso efectivo; los 3 SO espejo sucios; `HandFeed` fuera de `IsNavMeshControlled`; `Index/02` desactualizada; PC2 sin Synty (quirk 8 de `Index/12` S98-S101).

---

## Historial

- **2026-09-05 (S102, `/loop` "hasta que tengas un gameplay para mostrarme"):** semilla que reparte la sala (eje de entrada, densidades, decorado), cono de visión con sector en el overlay, palette shader con 4 paletas, elenco desde el save local y pantalla de plan previa a la ronda (5g). Verificado en Play con capturas y una ronda grabada con los MoriMonchis de Juan.
- **2026-09-05 (S101, segunda mitad, `/loop` "hasta que tengas gameplay"):** ocupaciones con tiempo y sala de 90 s (5f): minado por unidades, salidas, ronda con marcador y HUD, Vigilar / Romper / Distraer, siete rondas sondeadas con la matriz de contras. Escena y assets editados en disco con la escena cerrada; sin commit.
- **2026-09-05 (S101):** afinado del choque por datos (`EngageRange` 6, picada 32°/2,5, squash +0,35/−0,3, `idleWeight` 0,05, `observer` vacío, `teamSpawnInset` 14, `PerceptionRadius` 9) con loop de QA (sondeos A/A2/B/C + capturas miradas + clip); contratos S98-S100 bajados (5c, 5d, 5e).
- **2026-09-04 (S100):** choque físico v1 (5e) verificado en Play.
- **2026-09-04 (S99):** pulido en loop de QA, elenco y equipos, cámara de grupo (5d).
- **2026-09-04 (S98):** lote de realismo (5c) y arena vestida con Synty.
- **2026-09-03 (S97):** nota creada. Fase 1 (escena, Hovl → URP, Feel en el prefab, sandbox) y arranque de Fase 2 (guías visuales, reglas de expedición por SO, colaborador, minerales), todo verificado en Play.
