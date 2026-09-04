---
tags: [script, world, expedition, ui, cues]
---

# ArenaCueOverlay.cs

**Ruta:** `World/Expedition/ArenaCueOverlay.cs`

**Responsabilidad:** Presentación de guías visuales (cues) sobre el terreno de la arena. **Solo lee** las fachadas públicas del agente y no muta estado. Dibuja en modo inmediato (`Graphics.RenderMesh` vía `CueDrawer`) por criatura: anillo de percepción giratorio punteado + arcos de atención hacia lo percibido, ruta suavizada Catmull-Rom con dashes fluyendo al destino y marcador pulsante, líneas hacia percepciones teñidas por afinidad **S99:** y rivalidad (Foe/Friend por Team), retícula de 4 arcos sobre el objetivo de expedición, halos radiales bajo minerales, enlaces sociales, y **S100 NUEVO:** flecha roja aditiva pulsante del atacante al ClashTarget. Todo configurado por `CueStyleSO`. Quirk S97: el flujo visual es la vara de **Shapes** (Freya Holmér) sin que Juan lo pida; convención: ángulos en radianes, 0 = +X, crece hacia +Z (Atan2).

## DrawPercepts S99 (filtro de Team)

```csharp
private void DrawPercepts(MoriMonchiController controller, Vector3 origin, float perceptionRadius)
{
    foreach (var p in controller.Agent.Percepts)
    {
        if (p.Kind != PerceivableKind.Monchi || p.Source == null) continue;

        var mine = controller.Agent.Team;  // S99 NUEVO: leer Team de la fachada
        Color color = ExpeditionTeams.AreRivals(mine, p.Team) ? style.FoeColor
                    : ExpeditionTeams.AreAllies(mine, p.Team) ? style.FriendColor
                    : Color.Lerp(style.FoeColor, style.FriendColor, (p.Affinity + 1f) * 0.5f);

        // Resto del dibujo (línea punteada, flujo, atenuación)
    }
}
```

**Significado:**
- Si rivales: color rojo (FoeColor)
- Si aliados: color verde (FriendColor)
- Si neutral/Mixed affinity: interpolación rojo-verde según afinidad (-1 → rojo, 0 → mixto, 1 → verde)

## DrawClash S100 (flecha de atacante → objetivo)

**Nuevo método (líneas 365-379):**
```csharp
private void DrawClash(MoriMonchiController controller)
{
    var target = controller.Agent.ClashTarget;
    if (target == null) return;

    Vector3 a = controller.transform.position + Vector3.up * style.HeightOffset;
    Vector3 b = target.transform.position + Vector3.up * style.HeightOffset;

    Color head = style.FightColor;
    head.a = 0.55f + 0.45f * Mathf.Sin(Time.time * style.FightPulseSpeed);
    Color tail = head;
    tail.a *= style.PathTailAlpha;

    CueDrawer.Arrow(a, b, style.PathThickness * 1.5f, style.HeadLength, style.HeadWidth, tail, head, true);
}
```

**Significado:**
- Flecha gruesa (PathThickness × 1.5) roja (FightColor, igual que Social Fighting)
- Cabeza pulsante (sin atenuación, 0.55–1.0 alpha vía sine)
- Cola atenuada (PathTailAlpha)
- Dibuja sobre aditivo (último parámetro `true`) para resalte brillante

**Consumo en LateUpdate (línea 82):**
```csharp
if (showClash) DrawClash(controller);
```

**Razón:** Visualiza el emparejamiento atacante-objetivo durante Clashing/Striking fases de combate. Similar a Social Fighting pero dinámico (aparece al TryEngage, desaparece al Finish).

## Métodos Privados (lógica de dibujo) S100

**Núcleo:**
- `DrawPerception(MoriMonchiController, CueState, Vector3 origin, float radius)` — anillo punteado giratorio redondeado con respiración al entrar percepto nuevo, arcos de atención hacia el más cercano.
- `DrawPath(MoriMonchiController, CueState)` — ruta Catmull-Rom suavizada con Dashed Segment/Arrow, destino pulsante. Gestiona `CueState.HasShown`, `ShownEnd`, `Alpha`, `DestAppear`. **S97:** colorea por Intent; **S99:** colores nuevos para Taking/Losing.
- `DrawPercepts(MoriMonchiController, Vector3 origin, float radius)` — por cada `Percept.Monchi` en la lista, línea punteada fluyente hacia ese monchi, **S99 NUEVO:** teñida por rivalidad (Foe/Friend) o afinidad, atenuada por distancia.
- `DrawReticle(MoriMonchiController, CueState)` — retícula de 4 arcos girando sobre `ExpeditionTarget`; aparición/desaparición suave.
- `DrawMinerals()` — por cada mineral: disco radial + anillo fino, el central con punteado contrarrotante.
- `DrawSocial(MoriMonchiController)` — enlace punteado entre criaturas que socializan; rojo pulsante aditivo si están peleando.
- **S100 NUEVO:** `DrawClash(MoriMonchiController)` — flecha roja pulsante del atacante al objetivo de choque.

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
- `style` (CueStyleSO) — todos los colores, espesores, velocidades y timings. **S100:** incluye colores para Clashing/Dazed.

**Toggles de presentación:**
- `showPerception` (bool, default true) — anillo y arcos de atención.
- `showPath` (bool, default true) — ruta y destino.
- `showPercepts` (bool, default true) — líneas a lo percibido. **S99:** coloreadas por Team.
- `showMinerals` (bool, default true) — halos bajo minerales.
- `showReticle` (bool, default true) — retícula del objetivo expedición.
- `showSocial` (bool, default true) — enlaces de socialización.
- **S100 NUEVO:** `showClash` (bool, default true) — flecha de choque (atacante → objetivo).

## Invariantes S100 + S99

- **Team-aware coloring:** `DrawPercepts()` filtra percepto Monchi por rivalidad usando `ExpeditionTeams.AreRivals/AreAllies()`.
- **Afinidad como fallback:** si neutral (ambos son None o ambos misma alianza sin rivalidad), usa afinidad social para interpolar rojo-verde.
- **Clash flecha dinámica:** `DrawClash()` solo dibuja si `ClashTarget != null` (presente durante Anticipating/Striking, desaparece en Resolving/Finish).
- **Read-only fachada:** nunca muta estado del agente; solo lee `Agent.Percepts`, `Agent.Team`, `Agent.Intent`, `Agent.ExpeditionTarget`, **S100:** `Agent.ClashTarget`.
- **Coloración consistente:** colores `Clashing/Dazed` en `CueStyleSO.PopulateDefaults()` usan naranja / violeta para distinguir combate de recolección.

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

**Entrada (lectura de fachadas):**
- [[MoriMonchiController]], [[MoriMochiAgent]] — fachadas de estado (Percepts, Team, Intent, ExpeditionTarget, SocialPartner, **S100:** ClashTarget)
- **S99:** [[Percept]] — incluye Team field
- **S99:** [[ExpeditionTeam]], [[ExpeditionTeams]] (filtro AreRivals/AreAllies)
- [[ArenaSandbox]] — referencia a sandbox.Spawned para iterar criaturas
- [[CueStyleSO]] — todos los knobs de presentación (colores, espesores, velocidades, **S100:** Clashing/Dazed colors)
- [[CueDrawer]] — motor de renderizado inmediato
- **S100:** [[AgentClash]] (popula ClashTarget durante combate)

**Salida (visual):**
- Gizmos en Game view (solo Play mode): anillos, rutas, líneas, retículas, halos, **S100:** flechas de choque
