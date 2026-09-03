---
tags: [index, expedition, arena, sandbox, cues]
---

# 23 - Arena Sandbox y Expedición (implementación, S97)

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

## 6 · Pendientes y deuda

- Fase 2, siguiente lote (plan de realismo en `Index/22` 8.6): fidgets y mirar · locomoción por intención · beats de notar / tomar / perder.
- Reglas que faltan en `ExpeditionRules.asset`: llevar a la salida, confrontar, huir, reagruparse, obedecer; `PerceivableKind` Salida / Peligro; carga y depósito en `AgentExpedition`.
- Capa de rasgos por tipo de parte (`TraitEffectBase` → `PartTraitSO` / `CutieMarkSO`) cuando Juan defina los propósitos.
- Los minerales de esquina quedan fuera del radio de percepción (6 m): decisión de diseño pendiente (8.7).
- `com.unity.recorder` instalado sin uso efectivo; los 3 SO espejo sucios; `HandFeed` fuera de `IsNavMeshControlled`; `Index/02` desactualizada.

---

## Historial

- **2026-09-03 (S97):** nota creada. Fase 1 (escena, Hovl → URP, Feel en el prefab, sandbox) y arranque de Fase 2 (guías visuales, reglas de expedición por SO, colaborador, minerales), todo verificado en Play.
