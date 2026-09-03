---
tags: [script, world, expedition, ui, cues]
---

# ArenaCueOverlay.cs

**Ruta:** `World/Expedition/ArenaCueOverlay.cs`

**Responsabilidad:** Presentación de guías visuales (cues) sobre el terreno de la arena. **Solo lee** las fachadas públicas del agente y no muta estado. Dibuja en modo inmediato (`Graphics.RenderMesh` vía `CueDrawer`) por criatura: anillo de percepción giratorio punteado + arcos de atención hacia lo percibido, ruta suavizada Catmull-Rom con dashes fluyendo al destino y marcador pulsante, líneas hacia percepciones teñidas por afinidad, retícula de 4 arcos sobre el objetivo de expedición, y halos radiales bajo minerales. Todo configurado por `CueStyleSO`. Quirk S97: el flujo visual es la vara de **Shapes** (Freya Holmér) sin que Juan lo pida; convención: ángulos en radianes, 0 = +X, crece hacia +Z (Atan2).

## Métodos Privados (lógica de dibujo)

**Núcleo:**
- `DrawPerception(MoriMonchiController, CueState, Vector3 origin, float radius)` — anillo punteado giratorio redondeado con respiración al entrar percepto nuevo, arcos de atención hacia el más cercano.
- `DrawPath(MoriMonchiController, CueState)` — ruta Catmull-Rom suavizada con Dashed Segment/Arrow, destino pulsante. Gestiona `CueState.HasShown`, `ShownEnd`, `Alpha`, `DestAppear`.
- `DrawPercepts(MoriMonchiController, Vector3 origin, float radius)` — por cada `Percept.Monchi` en la lista, línea punteada fluyente hacia ese monchi, teñida por afinidad y atenuada por distancia.
- `DrawReticle(MoriMonchiController, CueState)` — retícula de 4 arcos girando sobre `ExpeditionTarget`; aparición/desaparición suave.
- `DrawMinerals()` — por cada mineral: disco radial + anillo fino, el central con punteado contrarrotante.
- `DrawSocial(MoriMonchiController)` — enlace punteado entre criaturas que socializan; rojo pulsante aditivo si están peleando.

**Helpers:**
- `DrawDestinationMarker(CueState, Color)` — marcador de destino bajo la ruta.
- `CatmullRom(p0, p1, p2, p3, t) → Vector3` — interpolación cúbica para la ruta.
- `Step(CueAnim, bool visible, float seconds, float dt) → float` — actualiza alfa de animación suave.
- `AppearScale(float alpha, float from) → float` — escala de entrada suave.
- `GetCueState(MoriMonchiController) → CueState` — cache por criatura.
- `GetMineralAnim(MaterialPickup) → CueAnim` — cache por mineral.

## Campos Internos

**Cache:**
- `cueCache` (Dict<MoriMonchiController, CueState>) — estado de animación por criatura.
- `mineralAnims` (Dict<MaterialPickup, CueAnim>) — animaciones por mineral.
- `cornersBuffer` (List<Vector3>) — buffer temporal para puntos de ruta.

**CueState (clase interna):**
- `Nav` (NavMeshAgent) — referencia cacheada del agente.
- `ShownEnd`, `HasShown` — destino suavizado y bandera de inicialización.
- `Alpha` — fade-in/out de la ruta (0–1).
- `Corners` — puntos del path de NavMesh este frame.
- `PerceptionAppear`, `LastPerceptCount`, `PulseElapsed` — respiración del anillo.
- `DestAppear`, `LastDestination`, `HasDestination` — marcador de destino.
- `Reticle`, `LastTargetPosition` — retícula del objetivo.

**CueAnim (clase interna):**
- `Alpha` — opacidad actual (0–1).
- `Visible` — bandera de visibilidad deseada.

## Campos Configurables (Inspector)

**Referencias requeridas:**
- `sandbox` (ArenaSandbox) — referencia al generador de criaturas y minerales.
- `cueMaterial` (Material) — material URP unlit con `MonchiCue.shader` (blend alpha).
- `additiveMaterial` (Material) — material aditivo (blend One/One) para resaltes.
- `style` (CueStyleSO) — todos los colores, espesores, velocidades y timings.

**Toggles de presentación:**
- `showPerception` (bool, default true) — anillo y arcos de atención.
- `showPath` (bool, default true) — ruta y destino.
- `showPercepts` (bool, default true) — líneas a lo percibido.
- `showMinerals` (bool, default true) — halos de minerales.
- `showReticle` (bool, default true) — retícula del objetivo.
- `showSocial` (bool, default true) — enlaces sociales.

## Ciclo de Actualización

```csharp
OnEnable():
  CueDrawer.Configure(cueMaterial, additiveMaterial)

LateUpdate():
  if (sandbox == null || style == null) return
  perceptionRadius = SocialTuningSO.Current?.PerceptionRadius ?? 0
  
  foreach criatura en sandbox.Spawned:
    if (showPerception) DrawPerception(...)
    if (showPath) DrawPath(...)
    if (showPercepts) DrawPercepts(...)
    if (showReticle) DrawReticle(...)
    if (showSocial) DrawSocial(...)
  
  if (showMinerals) DrawMinerals()
```

Notas:
- Todos los dibujos ocurren en LateUpdate() después de que Update() haya completado su tick de física.
- El dibujante usa modo inmediato: nada de GameObjects, solo `Graphics.RenderMesh` con `MaterialPropertyBlock`.
- Ángulos: `Atan2(z, x)` convención (radianes, 0 = +X hacia +Z).

## Especificación Visual v3 S97 (cumplida y verificada)

| Guía | Características |
|---|---|
| **Percepción** | Anillo punteado redondeado girando; respiración 1→1.05 al entrar percepto; arco de atención al más cercano con degradado angular hacia los lados. |
| **Intención (ruta)** | Catmull-Rom suavizada con dashes que fluyen hacia destino (offset animado), degradado de alfa cola→cabeza, marcador pulsante 1→1.4. |
| **Lo percibido** | Línea punteada fina teñida por afinidad (amigo→foe), atenuada por distancia, flujo lento hacia lo percibido. |
| **Objetivo (expedición)** | Retícula de 4 arcos alrededor del target elegido, escala 1.4→1, rotación lenta, desaparición en ubicación final. |
| **Minerales** | Disco radial con degradado (alfa 0,35→0) + anillo fino; central con punteado contrarrotante. |
| **Interacción social** | Enlace punteado con puntas redondas; rojo pulsante aditivo si peleando. |

## Invariantes S97

- **Presentación read-only:** nunca muta agente, Percepts, ni Perceivable; solo lee y dibuja.
- **Fachada pública:** `MoriMochiAgent.Percepts`, `.Intent`, `.ExpeditionTarget`, `.SocialPartner`; todo inmutable.
- **Height offset:** se suma `style.HeightOffset` (0.03) a todas las posiciones Y para evitar z-fighting con el terreno.
- **Convención de ángulos:** `Atan2(posZ - originZ, posX - originX)` radianes; 0 = +X, π/2 = +Z.
- **Catmull-Rom suavizado:** inicio/fin virtuales para tangentes naturales; `style.StartTangent` controla extensión de inicio.
- **Fade path:** `state.Alpha` transiciona suave cuando la ruta desaparece/reaparece; `style.PathFadeSeconds` = 0.35 s.
- **Respiración:** si `perceptCount > lastCount`, `pulsElapsed` se resetea a 0; ciclo de `style.PulseSeconds` (0.35 s).

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]], [[Index/12 - Unity MCP]] (captura de pantalla)

## Conexiones

- [[ArenaSandbox]] (referencia, lee `.Spawned` y `.Minerals`)
- [[MoriMochiAgent]] / [[MoriMonchiController]] (lee fachada: `.Percepts`, `.Intent`, `.ExpeditionTarget`, `.SocialPartner`)
- [[CueDrawer]] — dibujante estático inmediato
- [[CueStyleSO]] — configuración de colores y geometría
- [[MaterialPickup]] — recolectables que se dibujan
- [[SocialTuningSO]] — perception radius global
- [[MonchiCue.shader]] — shader URP unlit con SDF (contrato: `_Shape` 0-6, `_Color`, `_ColorB`, etc.)
