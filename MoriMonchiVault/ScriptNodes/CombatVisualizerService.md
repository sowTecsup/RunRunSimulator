---
tags: [combat, visualization, replay, ui, 3v3]
---

# CombatVisualizerService

**Ruta:** `Systems/CombatVisualizer/CombatVisualizerService.cs`

**Responsabilidad:** Orquesta visualización local 3v3 de `CombatRecord`, construyendo árbol de nodos doblemente enlazados y generando secuencia de animaciones turno-a-turno. **S41:** Firma nueva `Play(CreatureDNA self, CombatRecord record)` resuelve equipos vía registry. Colaborador `CombatVisualUnits` spawn/lookup. **S42:** PublishOrder() emite orden de próxima acción, PushElements() renderiza chips a barras, ActionIndex contador. **S43:** Campos speech tweakeables, PlayProc emite Speech events. **S45:** Precomputa mapa `roundOrders`, emite `OnUnitElement` per-proc. **S46:** `PosOf()` público nuevo (consume CombatFeelDirector). `SnapRole()` privado nuevo. Globo Agresivo re-enganchado a MarkApplied ally-sourced + SnapRole=Agresivo. Emisiones de OnUnitAffinity sin energy param. `ReactionName` incluido al emitir OnUnitElement. **S47:** Coreografía especial para procs con PassivePhase=true: agrupa procs pasivos por (targetSide, targetIndex) y ejecuta lunge del atacante a aliado objetivo, anima procs, retorna. Nuevos campos protectorSelfLine/empaticoSelfLine/agresivoSelfLine, attackLine con nombre del defensor.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Play(CreatureDNA self, CombatRecord record)` | **S41** Inicia replay 3v3 resolviendo equipos via registry |
| `Stop()` | Detiene playback, limpia, despawns unidades |
| `TogglePlay()` | Toggle automático |
| `Next()` | Avanza un turno |
| `Back()` | Retrocede un turno |
| `SetSpeed(float value)` | Setea velocidad playback (0.25–4) |
| `VCamOf(CombatVisualSide side, int index)` | **S43** Retorna la CinemachineCamera de la unidad |
| `PosOf(CombatVisualSide side, int index)` | **S46 NEW PUBLIC** Retorna Vector3 posición del unit (consume CombatFeelDirector) |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `visualizerPrefab` | `MoriMonchiVisualizer` | Prefab a instanciar |
| `boardA` | `Transform` | Board lado A (7 hijos anchor) |
| `boardB` | `Transform` | Board lado B (7 hijos anchor) |
| `elementTable` | `ElementTableSO` | **S42** Para DisplayName + UiColor |
| `windupSeconds` | `float` | Duración windup (÷ Speed) |
| `impactSeconds` | `float` | Duración impacto (÷ Speed) |
| `lungeFraction` | `float` | **S45** Fracción de distancia en lunge (default 0.6) |
| `betweenTurnsSeconds` | `float` | Pausa entre turnos |
| `deathPauseSeconds` | `float` | Pausa al morir |
| `synergyPopupDelay` | `float` | Delay pre-popup reacción |
| `stateBeatSeconds` | `float` | **S43** Pausa en estados armados |
| `protectorLine` | `string` | **S43/S47** Frase Protector (default: "¡Toma, protégete!") |
| `empaticoLine` | `string` | **S43/S47** Frase Empático (default: "¡{0}, te curo!") con {0} para nombre |
| `agresivoLine` | `string` | **S43/S47** Frase Agresivo (default: "¡Te comparto mi espíritu de pelea!") |
| `protectorSelfLine` | `string` | **S47 NEW** Frase Protector sobre sí mismo (default: "¡Me escudaré!") |
| `empaticoSelfLine` | `string` | **S47 NEW** Frase Empático sobre sí mismo (default: "¡Qué alivio!") |
| `agresivoSelfLine` | `string` | **S47 NEW** Frase Agresivo sobre sí mismo (default: "¡Me toca a mí!") |
| `attackLine` | `string` | **S47 NEW** Frase de ataque (default: "¡TOMA, {0}!") con {0} para nombre defensor |
| `speechSeconds` | `float` | **S43** Duración globo de habla |
| `playbackSpeed` | `float` | Multiplicador velocidad |

## Estructura: CombatNode (S41+)

```csharp
private class CombatNode
{
    public bool                      HasTurn;
    public CombatTurn                Turn;
    public int                       TurnNumber;
    public int                       ActionIndex;        // **S42** contador acciones
    public List<CombatUnitState>     StateA;            // **S41** team A state
    public List<CombatUnitState>     StateB;            // **S41** team B state
    public bool[]                    DiedHereA;         // **S41** muertes por unit
    public bool[]                    DiedHereB;         // **S41** muertes por unit
    public CombatVisualSide          AttackerSide;
    public int                       AttackerIndex;     // **S41** índice en equipo
    public int                       DefenderIndex;     // **S41** índice en equipo
    public bool                      Crit;
    public List<CombatVisualLogLine> Log;
    public CombatNode                Prev;
    public CombatNode                Next;
    public bool IsEnd => Next == null;
}
```

## Cambios S47

**Coreografía de pasivas:**

En `ForwardRoutine()`, líneas 412–450, la secuencia ahora es:

```csharp
// BeforeStrike procs
if (t.Procs != null)
    foreach (var pe in t.Procs)
        if (pe.BeforeStrike) yield return StartCoroutine(PlayProc(pe, target));

// PassivePhase procs (S47 NEW)
if (t.Procs != null)
{
    var passiveProcs = t.Procs.Where(pe => !pe.BeforeStrike && pe.PassivePhase).ToList();
    int gi = 0;
    while (gi < passiveProcs.Count)
    {
        var groupSide  = SimToVisual(passiveProcs[gi].TargetIsA);
        var groupIndex = passiveProcs[gi].TargetIndex;
        var group = new List<CombatProcEvent> { passiveProcs[gi] };
        int gj = gi + 1;
        while (gj < passiveProcs.Count && SimToVisual(passiveProcs[gj].TargetIsA) == groupSide && passiveProcs[gj].TargetIndex == groupIndex)
        {
            group.Add(passiveProcs[gj]);
            gj++;
        }
        gi = gj;

        bool isSelf = groupSide == target.AttackerSide && groupIndex == target.AttackerIndex;
        if (isSelf)
        {
            // Pasiva sobre el atacante mismo: anima en posición sin movimiento
            foreach (var pe in group) yield return StartCoroutine(PlayProc(pe, target));
        }
        else
        {
            // Pasiva sobre aliado distinto: atacante viaja, anima, retorna
            var dest = atkHome + (units.PosOf(groupSide, groupIndex) - atkHome) * lungeFraction;
            if (atkUnit?.Instance != null)
                yield return StartCoroutine(MoveOverTime(atkUnit.Instance.transform, atkHome, dest, windupSeconds / Speed));
            else
                yield return new WaitForSeconds(windupSeconds / Speed);

            foreach (var pe in group) yield return StartCoroutine(PlayProc(pe, target));

            if (atkUnit?.Instance != null)
                yield return StartCoroutine(MoveOverTime(atkUnit.Instance.transform, dest, atkHome, impactSeconds / Speed));
            else
                yield return new WaitForSeconds(impactSeconds / Speed);
        }
    }
}

// Golpe y sus procs post-strike
if (!t.NoAttack) { ... golpe animado ... }

// Procs post-strike (no-pasivos)
if (t.Procs != null)
    foreach (var pe in t.Procs)
        if (!pe.BeforeStrike && !pe.PassivePhase) yield return StartCoroutine(PlayProc(pe, target));
```

**Procs pasivos se agrupan por (targetSide, targetIndex):**
- Si target = atacante mismo (isSelf), anima en posición sin lunge
- Si target = aliado distinto, lunge a posición, anima, retorna

**Frases de pasivas (S47):**

En `PlayProc()`, líneas 603–621:

```csharp
else if (pe.ElementEvent == ElementEventKind.None && pe.Kind == ModifierEffectKind.Shield)
{
    if (esSelf)
        EmitSpeech(node.AttackerSide, node.AttackerIndex, protectorSelfLine, side, pe.TargetIndex, false);
    else
        EmitSpeech(node.AttackerSide, node.AttackerIndex, string.Format(protectorLine, SnapName(side, pe.TargetIndex)), side, pe.TargetIndex, true);
}
else if (pe.ElementEvent == ElementEventKind.None && pe.Kind == ModifierEffectKind.Heal)
{
    if (esSelf)
        EmitSpeech(node.AttackerSide, node.AttackerIndex, empaticoSelfLine, side, pe.TargetIndex, false);
    else
        EmitSpeech(node.AttackerSide, node.AttackerIndex, string.Format(empaticoLine, SnapName(side, pe.TargetIndex)), side, pe.TargetIndex, true);
}
else if (pe.ElementEvent == ElementEventKind.MarkApplied && pe.AllySource && pe.PassivePhase
      && SnapRole(node.AttackerSide, node.AttackerIndex) == Role.Agresivo && esSelf)
    EmitSpeech(node.AttackerSide, node.AttackerIndex, agresivoSelfLine, side, pe.TargetIndex, false);
```

**Nuevo atacLine (S47):**

En `ForwardRoutine()`, línea 457:
```csharp
EmitSpeech(target.AttackerSide, target.AttackerIndex, string.Format(attackLine, t.DefenderName), defSide, target.DefenderIndex, true);
```

Suenan las frases:
- Pasiva sobre sí mismo: solo la frase self (sin destino visualizado)
- Pasiva sobre aliado: frase + nombre aliado + arrow visual
- Ataque: frase + nombre defensor

**Marcos visuales en barras (S47):**

- `SetActiveFrames()` línea 401: marco dorado al atacante (inicio de turno)
- `SetTargeted(true)` línea 403: marco rojo al defensor antes del ataque
- `SetTargeted(false)` línea 496: quita marco rojo del defensor después del ataque

## Cambios S46

**PosOf() ahora público:**
- Línea 623: `public Vector3 PosOf(CombatVisualSide side, int index) => units.PosOf(side, index);`
- Consumido por CombatFeelDirector para reproducir MMFeedbacks en la posición del unit

**SnapRole() privado nuevo:**
- Línea 625: `private Role SnapRole(CombatVisualSide side, int index)` — obtiene Role del snapshot
- Usado para re-enganchar globo de Agresivo

**Globo Agresivo re-enganchado (línea 558-561):**
- Antes: colgaba de `ElementEventKind.EnergyGained` (que ya no se emite)
- Ahora: dispara si `d.Kind == ElementEventKind.MarkApplied && d.AllySource && SnapRole(...) == Role.Agresivo && !(same unit)`
- Condición: marca aliada aplicada por Agresivo a distinto aliado

**OnUnitAffinity sin energy:**
- Línea 568: `CombatVisualEvents.UnitAffinity(side, pe.TargetIndex, (int)pe.Amount);` — sin parámetro 4 (energy)

**ReactionName en OnUnitElement:**
- Línea 581: `ReactionName = pe.ReactionName,` — incluido al emitir

**Enums inertes:**
- `ElementEventKind.EnergyGained` y `EnergySpent` siguen existiendo pero nadie los emite
- Append-only: backward compat con records viejos

## Flujo de Play() (S41)

```csharp
public void Play(CreatureDNA self, CombatRecord record)
{
    // Validar 3v3
    if (record.SelfTeam == null || record.SelfTeamIds == null) return;

    // Resolver IDs desde registry
    var reg = GameManager.Instance.Registry;
    var resolvedSelf = new List<CreatureDNA>();
    foreach (var id in record.SelfTeamIds)
        if (!reg.TryGet(id, out var dna)) return;
        resolvedSelf.Add(dna);

    // Similiar para opponent team
    var resolvedOpp = new List<CreatureDNA>();
    foreach (var id in record.OpponentTeamIds)
        if (!reg.TryGet(id, out var dna)) return;
        resolvedOpp.Add(dna);

    Stop();
    activeRecord = record;
    teamSelf = resolvedSelf;
    teamOpp = resolvedOpp;
    BuildStates();
    if (head == null) return;
    beginRoutine = StartCoroutine(BeginRoutine());
}
```

## Métodos Privados Clave (S41 + S45 + S46 + S47)

| Método | Descripción |
|--------|-------------|
| `BuildStates()` | Construye árbol de CombatNode doblemente enlazado desde CombatRecord.Turns. **S45:** Precomputa mapa roundOrders (orden estable por ronda). **S41:** PopulaTeamStateA/B. |
| `BeginRoutine()` | Spawns unidades vía CombatVisualUnits, emite OnVisualCombatStart |
| `ForwardRoutine()` | Anima un turno: **S47:** Filtra procs pre-strike, procs pasivos (con lunge/coreografía), golpe, procs post-strike. **S45:** LUNGE vía MoveOverTime. **S46:** PlayProc emite OnUnitElement con ReactionName. **S47:** Emite attackLine dirigida al defensor. |
| `PlayProc()` | Anima un proc (shield/heal/reacción/estado). **S43:** Emite Speech. **S45:** Emite OnUnitElement para cada evento elemental. **S46:** Incluye ReactionName. **S47:** Emite frases self vs aliado según destino. |
| `Restore()` | Vuelve a estado de un nodo (retroceso). Restaura Hp/Shield/Status + ElementMarks/ArmedStates. |
| `Publish()` | Emite OnPanelState (info control replay) |
| `PublishOrder()` | **S42** Emite OnActionOrder (orden de próxima acción, usado por CombatOrderBarUITK) |
| `PushElements()` | **S42** Emite OnUnitElement por cada unit (dibuja chips a barras) — NOTA: S46 incluye ReactionName |
| `SnapRole()` | **S46 NEW** Helper privado — obtiene Role del snapshot |
| `SetActiveFrames(side, index)` | **S47** Setea marco dorado al unit (début de turno) |
| `SetTargeted(side, index, value)` | **S47** Setea marco rojo al unit (es objeto del ataque) |

## Consumo de OnUnitElement (S46+S47)

En PlayProc, para cada evento elemental:
```csharp
if (pe.ElementEvent == ElementEventKind.MarkApplied
 || pe.ElementEvent == ElementEventKind.MarkRemoved
 || pe.ElementEvent == ElementEventKind.Reaction
 || pe.ElementEvent == ElementEventKind.StateArmed
 || pe.ElementEvent == ElementEventKind.StateConsumed
 || pe.ElementEvent == ElementEventKind.StateRemoved)
    CombatVisualEvents.UnitElement(new CombatElementEventData
    {
        Side = side, Index = pe.TargetIndex, Kind = pe.ElementEvent,
        Element = pe.Element, ElementB = pe.ElementB, AllySource = pe.AllySource, State = pe.State,
        ReactionName = pe.ReactionName,  // S46 NEW
    });
```

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CombatRecord]] — fuente de datos (turnos, snapshots, team ids)
- [[CombatProcEvent]] — PassivePhase flag S47, usado para filtrar y agrupar procs
- [[CombatVisualEvents]] — publisher de eventos (S46: OnUnitAffinity sin energy, OnUnitElement con ReactionName)
- [[CombatVisualUnits]] — spawn/lookup/PosOf units
- [[CombatOrderBarUITK]] — suscriptor **S42** (OnActionOrder, OnUnitAffinity, OnActiveUnit, OnUnitElement S46)
- [[CombatFeelDirector]] — **S46 NEW** consumidor de PosOf para reproducir feedbacks
- [[CombatSpeechBubbles]] — **S43** suscriptor OnSpeech, emite globos (S47: nuevas frases de pasivas)
- [[CombatCameraDirector]] — **S43** suscriptor OnActiveUnit (VCamOf)
- [[CombatService]] — genera CombatRecord con Procs.PassivePhase=true en fase 14

## Notas S47

- Coreografía de pasivas: procs con PassivePhase=true se agrupan por objetivo y se animan durante el step 14, antes del golpe
- Si la pasiva afecta al atacante mismo, se anima sin movimiento; si afecta a un aliado, el atacante hace lunge+retorno
- Las frases protectorSelfLine, empaticoSelfLine, agresivoSelfLine suenan cuando la pasiva es sobre el actor mismo
- SetActiveFrames/SetTargeted marcan visualmente al actor y al defensor con marcos dorado/rojo

## Notas S46

- `PosOf()` público permite que CombatFeelDirector acceda a posiciones de units
- `SnapRole()` privado localiza Role desde snapshot (no desde Combatant, que ya no existe post-simulación)
- Globo Agresivo cambió: de EnergyGained (obsoleto) a MarkApplied ally-sourced + rol check
- OnUnitAffinity emite sin energy (Affinity 0-2 solamente)
- ReactionName en OnUnitElement permite parseando a ElementalState en CombatOrderBarUITK
