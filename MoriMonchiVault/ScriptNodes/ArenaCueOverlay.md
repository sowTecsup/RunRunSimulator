---
tags: [script, world, expedition, ui, cues]
---

# ArenaCueOverlay.cs

**Ruta:** `World/Expedition/ArenaCueOverlay.cs`

**Responsabilidad:** Presentación de guías visuales (cues) sobre el terreno de la arena. Dibuja en modo inmediato (`Graphics.RenderMesh` vía `CueDrawer`) por criatura: anillo de percepción (dashed giratorio o cono de visión), arcos de atención hacia lo percibido, ruta suavizada (delegada a CuePathDrawer), líneas hacia percepciones teñidas por rivalidad **S99**, retícula de 4 arcos sobre el objetivo de expedición, enlaces sociales, **S100:** flecha roja pulsante del atacante al ClashTarget. **S102 NUEVO:** dibuja cono de visión (sector suavizado, borde, lados, anillo del oído) con rumbo suavizado. **S101 NUEVO:** minerales y salidas delegadas a ArenaRoomCueOverlay. Quirk S97: flujo visual Shapes (Freya Holmér); ángulos en radianes, 0 = +X, crece hacia +Z (Atan2).

## DrawVisionCone S102 NUEVO

```csharp
private void DrawVisionCone(MoriMonchiController controller, CueState state, Vector3 origin, float radius)
{
    float facing = VisionProfile.FacingAngle(controller.transform.forward);
    // Suavizado de giro con exponencial negativa
    if (!state.HasFacing)
    {
        state.FacingAngle = facing;
        state.HasFacing = true;
    }
    else
    {
        float delta = Mathf.DeltaAngle(state.FacingAngle * Mathf.Rad2Deg, facing * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        state.FacingAngle += delta * (1f - Mathf.Exp(-style.VisionTurnSmoothing * Time.deltaTime));
    }

    float sweep = controller.Agent.VisionDegrees * Mathf.Deg2Rad;
    float start = state.FacingAngle - sweep * 0.5f;
    
    // Sector relleno (inner + outer alpha)
    CueDrawer.Sector(origin, radius, start, sweep, tint, VisionFillInnerAlpha, VisionFillOuterAlpha);
    
    // Borde (edge del sector)
    CueDrawer.Arc(origin, radius, ringThickness, start, sweep, rimColor, rimColor);
    
    // Lados (si no es 360°): dos segmentos a los extremos del cono
    if (sweep < 2π - 0.01)
    {
        CueDrawer.Segment(origin, edgeA, thickness * 0.7, nearAlpha=0, farAlpha=VisionSideAlpha);
        CueDrawer.Segment(origin, edgeB, thickness * 0.7, nearAlpha=0, farAlpha=VisionSideAlpha);
    }
    
    // Anillo del oído (audición, dashed fino)
    float nearRadius = controller.Agent.NearSenseRadius;
    if (nearRadius > 0)
        CueDrawer.DashedRing(origin, nearRadius, thickness * 0.8, dashCount/3, NearRingAlpha);
}
```

**Significado:**
- **Sector:** relleno del cono (VisionFillInnerAlpha en center, VisionFillOuterAlpha en edge)
- **Borde:** arc del sector (VisionEdgeAlpha)
- **Lados:** dos segmentos desde origin a extremos del cono (VisionSideAlpha, fade desde near=0 a far=alpha)
- **Anillo del oído:** dashed ring del NearSenseRadius (audición ciega, ignora cono)
- **Rumbo suavizado:** FacingAngle se anima suavemente al forward real (exponencial negativa, VisionTurnSmoothing)

**Integración en DrawPerception:**
```csharp
if (controller.Agent.HasVisionCone)
    DrawVisionCone(controller, state, origin, radius);
else
    CueDrawer.DashedRing(origin, radius, ...);  // fallback dashed ring
```

## Refactorización S102: Delegación

**Antes:** Todo centralizado en ArenaCueOverlay (ruta, minerales, salidas)

**Ahora (S102):**
- **Rutas:** CuePathDrawer (estático, mantiene PathCueState, dibuja Catmull-Rom)
  - `CuePathDrawer.Draw(style, state.Path, body, baseColor, dt)`
- **Minerales + Salidas:** ArenaRoomCueOverlay (componente nuevo, dibuja PerceivableRegistry)
  - `ArenaRoomCueOverlay.DrawMinerals()` (discos animados)
  - `ArenaRoomCueOverlay.DrawExits()` (anillos por team)

**Beneficio:** Separación de responsabilidades — ArenaCueOverlay es solo agentes; visuales estáticas de sala en otro componente.

## Campos Configurables (Inspector)

**Referencias requeridas:**
- `sandbox` (ArenaSandbox)
- `cueMaterial` (Material, MonchiCue.shader)
- `additiveMaterial` (Material, aditivo)
- `style` (CueStyleSO) — **S102 NUEVO:** incluye VisionFillInnerAlpha, VisionFillOuterAlpha, VisionEdgeAlpha, VisionSideAlpha, NearRingAlpha, VisionTurnSmoothing

**Toggles de presentación:**
- `showPerception` (bool, default true) — anillo o cono
- `showPath` (bool, default true) — ruta (delegada a CuePathDrawer)
- `showPercepts` (bool, default true) — líneas a percepciones
- `showReticle` (bool, default true) — retícula del objetivo
- `showSocial` (bool, default true) — enlaces sociales
- `showClash` (bool, default true) — flecha de choque
- ~~showMinerals, showExits~~ — **S102:** delegados a ArenaRoomCueOverlay
- ~~showMining~~ — **S102:** delegado a ArenaRoomCueOverlay

## Métodos Privados (S102)

**Núcleo (sin cambios grandes):**
- `DrawPerception(MoriMonchiController, CueState, Vector3 origin, float radius)` — **S102:** llama DrawVisionCone si HasVisionCone, else dashed ring
- `DrawVisionCone(...)` — **S102 NUEVO:** sector + borde + lados + anillo del oído + rumbo suavizado
- `DrawPercepts(...)` — per Percept, línea punteada (sin cambios conceptuales)
- `DrawReticle(...)` — retícula del objetivo (sin cambios)
- `DrawSocial(...)` — enlaces de socialización (sin cambios)
- `DrawClash(...)` — flecha atacante → objetivo (sin cambios)

**Removidas (delegadas):**
- ~~DrawMinerals()~~ → ArenaRoomCueOverlay
- ~~DrawExits()~~ → ArenaRoomCueOverlay
- ~~DrawMining()~~ → ArenaRoomCueOverlay

**Removidas (delegadas):**
- ~~DrawPath()~~ → CuePathDrawer.Draw()

## Campos Internos

**Cache:**
- `cueCache` (Dict<MoriMonchiController, CueState>) — estado por criatura
- CueState ahora incluye:
  - `Path` (PathCueState) — estado delegado a CuePathDrawer
  - `FacingAngle`, `HasFacing` — **S102 NUEVO:** ángulo suavizado para cono

## LateUpdate Flow S102

```csharp
foreach controller in sandbox.Spawned:
  1. Calcula perceptionRadius (HasVisionCone ? VisionRadius : global)
  2. DrawPerception(cono o ring)
     → Si HasVisionCone: DrawVisionCone() (sector + anillo)
  3. CuePathDrawer.Draw() (ruta, delegado)
  4. DrawPercepts() (líneas a percepciones)
  5. DrawReticle() (retícula)
  6. DrawSocial() (enlaces)
  7. DrawClash() (flecha choque)
  
// Salidas + Minerales dibujados por ArenaRoomCueOverlay (fuera del loop)
```

## Invariantes S102

- **Cono vs Ring:** HasVisionCone determina si dibuja sector o dashed ring
- **Rumbo suavizado:** FacingAngle no salta (exponencial negativa)
- **Audición aparte:** NearSenseRadius dibuja como anillo dashed independiente (ignora cono)
- **Delegación clara:** cada componente (CuePathDrawer, ArenaRoomCueOverlay) mantiene su estado (PathCueState, MineralAnim)
- **Read-only fachadas:** nunca muta agentes, solo lee Agent properties
- **VisionDegrees = 360:** cono omnidireccional (sin lados, sector llena círculo)

## Conexiones

**Entrada (lectura de fachadas):**
- [[MoriMochiAgent]] — HasVisionCone, VisionRadius, VisionDegrees, NearSenseRadius (fachadas)
- [[VisionProfile]] (FacingAngle para suavizado)
- [[CueStyleSO]] — **S102:** VisionFillInnerAlpha, VisionFillOuterAlpha, VisionEdgeAlpha, VisionSideAlpha, NearRingAlpha, VisionTurnSmoothing
- [[ExpeditionRulesSO]] (valores base de visión)

**Delegadas:**
- [[CuePathDrawer]] — ruta (estático)
- [[ArenaRoomCueOverlay]] — minerales y salidas

**Núcleo (sin cambios):**
- [[ArenaSandbox]], [[CueDrawer]], [[Percept]], [[ExpeditionTeam]], [[ExpeditionTeams]], [[AgentClash]]

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
