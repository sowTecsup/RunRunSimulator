---
tags: [script, world, expedition, ui, cues]
---

# ArenaCueOverlay.cs

**Ruta:** `World/Expedition/ArenaCueOverlay.cs`

**Responsabilidad:** Presentación de guías visuales (cues) sobre el terreno de la arena. **Solo lee** las fachadas públicas del agente y no muta estado. Dibuja en modo inmediato (`Graphics.RenderMesh` vía `CueDrawer`) por criatura: anillo de percepción giratorio punteado + arcos de atención hacia lo percibido, ruta suavizada Catmull-Rom con dashes fluyendo al destino y marcador pulsante, líneas hacia percepciones teñidas por afinidad **S99:** y rivalidad (Foe/Friend por Team), retícula de 4 arcos sobre el objetivo de expedición, halos radiales bajo minerales, enlaces sociales, y **S100 NUEVO:** flecha roja aditiva pulsante del atacante al ClashTarget. **S101 NUEVO:** dibuja salidas de base (discos + anillos + punteado lento por equipo), arco de minería con progreso. Todo configurado por `CueStyleSO`. Quirk S97: el flujo visual es la vara de **Shapes** (Freya Holmér) sin que Juan lo pida; convención: ángulos en radianes, 0 = +X, crece hacia +Z (Atan2).

## DrawExits S101 (nuevas salidas de base)

```csharp
private void DrawExits()
{
    if (sandbox.Exits == null) return;

    foreach (var exit in sandbox.Exits)
    {
        if (exit == null) continue;

        Vector3 center = exit.transform.position + Vector3.up * style.HeightOffset;
        Color color = exit.Team == ExpeditionTeam.Player ? style.FriendColor : style.FoeColor;

        CueDrawer.Disc(center, exit.Radius, color, style.ExitAlpha, 0f);

        Color ringColor = color;
        ringColor.a = style.ExitAlpha * 2f;
        CueDrawer.Ring(center, exit.Radius, style.ExitRingThickness, ringColor);

        Color dashColor = color;
        dashColor.a = style.ExitAlpha;
        CueDrawer.DashedRing(center, exit.Radius, style.ExitRingThickness, style.RingDashCount, style.RingDashRatio, Time.time * style.RingSpinSpeed * 0.5f, dashColor);
    }
}
```

**Significado:**
- Disco translúcido (ExitAlpha ~0.15): fondo de salida
- Anillo sólido (ExitAlpha × 2): contorno destacado
- Punteado lento (RingSpinSpeed × 0.5): gira media velocidad que minerales (más pausado)
- Color por Team: verde si Player, rojo si Rival
- Iteradas desde `sandbox.Exits` (inyectadas por ArenaSandbox)

**Consumo en LateUpdate (línea 93):**
```csharp
if (showExits) DrawExits();
```

## DrawMining S101 (progreso de minería)

```csharp
private void DrawMining(MoriMonchiController controller, Vector3 origin)
{
    if (controller.Agent.Intent != CreatureIntent.Taking) return;

    float progress = controller.Agent.MiningProgress;
    if (progress <= 0f) return;

    Color trackColor = style.ColorFor(CreatureIntent.Taking);
    trackColor.a = 0.15f;
    CueDrawer.Ring(origin, style.MiningArcRadius, style.MiningArcThickness, trackColor);

    Color arcColor = style.ColorFor(CreatureIntent.Taking);
    arcColor.a = style.MiningArcAlpha;

    float startAngle = Mathf.PI * 0.5f;
    float sweep = progress * Mathf.PI * 2f;
    CueDrawer.Arc(origin, style.MiningArcRadius, style.MiningArcThickness, startAngle, sweep, arcColor, arcColor, true);
}
```

**Significado:**
- Solo dibuja cuando Intent == Taking (minando activamente)
- Anillo base (MiningArcRadius, track color, alpha 0.15): la "pista" completa
- Arco progresivo: empieza en PI/2 (arriba), barre según progress (0–1) × 2π
- Ejemplo: progress=0.5 → arco es media circunferencia (π radianes)
- Color: amarillo (ColorFor Taking) o según CueStyleSO.TakingColor
- Dibuja sobre layer normal (aditivo deshabilitado, a diferencia de Clash/Social Fighting)

**Consumo en LateUpdate (línea 88):**
```csharp
if (showMining) DrawMining(controller, origin);
```

## DrawMinerals S101 (iteración de todos los Perceivable)

```csharp
private void DrawMinerals()
{
    PerceivableRegistry.QueryInRadius(sandbox.transform.position, 200f, null, mineralQueryBuffer);

    foreach (var p in mineralQueryBuffer)
    {
        if (p == null || p.Kind != PerceivableKind.Material) continue;

        var m = GetMineralPickup(p);
        if (m == null) continue;

        var anim = GetMineralAnim(m);
        float alpha = Step(anim, !m.Taken, style.AppearSeconds, Time.deltaTime);
        if (alpha <= 0.01f) continue;

        Vector3 center = m.transform.position + Vector3.up * style.HeightOffset;
        float radiusScale = m.Value > 0 ? (float)m.Remaining / m.Value : 1f;
        float radius = style.MineralDiscRadius * (m.Value > 1 ? 1.6f : 1f) * Mathf.Lerp(0.5f, 1f, radiusScale);

        CueDrawer.Disc(center, radius, style.MineralColor, style.MineralInnerAlpha * alpha, style.MineralOuterAlpha * alpha);

        Color ringColor = style.MineralColor;
        ringColor.a = style.MineralRingAlpha * alpha;
        CueDrawer.Ring(center, radius, style.MineralRingThickness, ringColor);

        if (m.Value > 1)
            CueDrawer.DashedRing(center, radius, style.MineralRingThickness, style.RingDashCount, style.RingDashRatio, Time.time * -style.RingSpinSpeed, ringColor);
    }
}
```

**Cambios S101:**
- Ahora itera `PerceivableRegistry.QueryInRadius()` directamente en lugar de recorrer Percepts (más alcance: 200m vs perception range)
- Caché `mineralLookup` (Dict<Perceivable, MaterialPickup>) para evitar GetComponent repetido
- Disco escala con `Remaining/Value` (más pequeño conforme se agota el mineral)
- Punteado contrarrotatorio (×-1) para minerales múltiples

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

**Método (líneas 365-379):**
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

## Métodos Privados (lógica de dibujo) S101

**Núcleo:**
- `DrawPerception(MoriMonchiController, CueState, Vector3 origin, float radius)` — anillo punteado giratorio redondeado con respiración al entrar percepto nuevo, arcos de atención hacia el más cercano.
- `DrawPath(MoriMonchiController, CueState)` — ruta Catmull-Rom suavizada con Dashed Segment/Arrow, destino pulsante. Gestiona `CueState.HasShown`, `ShownEnd`, `Alpha`, `DestAppear`. **S97:** colorea por Intent; **S99:** colores nuevos para Taking/Losing.
- `DrawPercepts(MoriMonchiController, Vector3 origin, float radius)` — por cada `Percept.Monchi` en la lista, línea punteada fluyente hacia ese monchi, **S99 NUEVO:** teñida por rivalidad (Foe/Friend) o afinidad, atenuada por distancia.
- `DrawReticle(MoriMonchiController, CueState)` — retícula de 4 arcos girando sobre `ExpeditionTarget`; aparición/desaparición suave.
- `DrawMinerals()` — **S101:** por cada material en PerceivableRegistry (radio 200m): disco radial + anillo fino, escala con mineral restante.
- `DrawSocial(MoriMonchiController)` — enlace punteado entre criaturas que socializan; rojo pulsante aditivo si están peleando.
- `DrawClash(MoriMonchiController)` — flecha roja pulsante del atacante al objetivo de choque.
- **S101 NUEVO:** `DrawExits()` — por cada salida (ExitZone): disco + anillo + punteado lento por Team.
- **S101 NUEVO:** `DrawMining(MoriMonchiController, Vector3 origin)` — arco de progreso si Intent == Taking.

## Campos Internos

**Cache:**
- `cueCache` (Dict<MoriMonchiController, CueState>) — estado de animación por criatura.
- `mineralAnims` (Dict<MaterialPickup, CueAnim>) — animaciones por mineral.
- **S101 NUEVO:** `mineralLookup` (Dict<Perceivable, MaterialPickup>) — caché de GetComponent para minerales.
- **S101 NUEVO:** `mineralQueryBuffer` (List<Perceivable>) — buffer temporal para query de PerceivableRegistry.
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
- `style` (CueStyleSO) — todos los colores, espesores, velocidades y timings. **S100:** incluye colores para Clashing/Dazed. **S101:** incluye ExitAlpha, MiningArcRadius, MiningArcThickness, MiningArcAlpha.

**Toggles de presentación:**
- `showPerception` (bool, default true) — anillo y arcos de atención.
- `showPath` (bool, default true) — ruta y destino.
- `showPercepts` (bool, default true) — líneas a lo percibido. **S99:** coloreadas por Team.
- `showMinerals` (bool, default true) — halos bajo minerales.
- `showReticle` (bool, default true) — retícula del objetivo expedición.
- `showSocial` (bool, default true) — enlaces de socialización.
- `showClash` (bool, default true) — flecha de choque (atacante → objetivo).
- **S101 NUEVO:** `showExits` (bool, default true) — salidas de base (discos + anillos + punteado).
- **S101 NUEVO:** `showMining` (bool, default true) — arco de progreso de minería.

## Invariantes S101 + S100 + S99

- **Team-aware coloring:** `DrawPercepts()` filtra percepto Monchi por rivalidad usando `ExpeditionTeams.AreRivals/AreAllies()`.
- **Afinidad como fallback:** si neutral (ambos son None o ambos misma alianza sin rivalidad), usa afinidad social para interpolar rojo-verde.
- **Clash flecha dinámica:** `DrawClash()` solo dibuja si `ClashTarget != null` (presente durante Anticipating/Striking, desaparece en Resolving/Finish).
- **Read-only fachada:** nunca muta estado del agente; solo lee `Agent.Percepts`, `Agent.Team`, `Agent.Intent`, `Agent.ExpeditionTarget`, `Agent.ClashTarget`, **S101:** `Agent.Carried`, `Agent.MiningProgress`.
- **Coloración consistente:** colores `Clashing/Dazed` en `CueStyleSO.PopulateDefaults()` usan naranja / violeta para distinguir combate de recolección.
- **S101:** Salidas son globales (fuera de loop de criaturas), dibujadas una sola vez por frame.
- **S101:** Minería es per-criatura, solo si Intent == Taking (no dibuja si esperando o lost).
- **S101:** Minerales iteran desde PerceivableRegistry (200m) no desde Percepts (4-8m perception), así que dibuja todos los que existan en arena.

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]] (sección 5f: Arena Sandbox y Expedicion)

## Conexiones

**Entrada (lectura de fachadas):**
- [[MoriMonchiController]], [[MoriMochiAgent]] — fachadas de estado (Percepts, Team, Intent, ExpeditionTarget, SocialPartner, **S100:** ClashTarget, **S101:** Carried, MiningProgress)
- **S99:** [[Percept]] — incluye Team field
- **S99:** [[ExpeditionTeam]], [[ExpeditionTeams]] (filtro AreRivals/AreAllies)
- [[ArenaSandbox]] — referencia a sandbox.Spawned para iterar criaturas, **S101:** sandbox.Exits para salidas
- **S101:** [[ExitZone]] — describe salida con Team, Radius, transform
- [[PerceivableRegistry]] — consulta minerales en radio 200m
- [[MaterialPickup]] — describe mineral con Value, Remaining, Taken
- [[CueStyleSO]] — todos los knobs de presentación (colores, espesores, velocidades, **S100:** Clashing/Dazed colors, **S101:** Exit/Mining style)
- [[CueDrawer]] — motor de renderizado inmediato
- **S100:** [[AgentClash]] (popula ClashTarget durante combate)
- **S101:** [[AgentExpedition]] (popula MiningProgress, Carried)

**Salida (visual):**
- Gizmos en Game view (solo Play mode): anillos, rutas, líneas, retículas, halos, flechas de choque, **S101:** salidas, arcos de minería
