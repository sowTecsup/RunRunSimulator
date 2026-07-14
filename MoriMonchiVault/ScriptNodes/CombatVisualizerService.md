---
tags: [combat, visualization, replay, ui, 3v3]
---

# CombatVisualizerService

**Ruta:** `Systems/CombatVisualizer/CombatVisualizerService.cs`

**Responsabilidad:** Orquesta visualización local 3v3 de `CombatRecord`, construyendo árbol de nodos doblemente enlazados y generando secuencia de animaciones turno-a-turno. **S41:** Firma nueva `Play(CreatureDNA self, CombatRecord record)` resuelve equipos vía registry. Colaborador `CombatVisualUnits` spawn/lookup. **S42:** PublishOrder() emite orden de próxima acción, PushElements() renderiza chips a barras, ActionIndex contador. **S43:** Campos speech tweakeables, PlayProc emite Speech events. **S45:** Precomputa mapa `roundOrders`, emite `OnUnitElement` per-proc. **S46:** `PosOf()` público nuevo (consume CombatFeelDirector). `SnapRole()` privado nuevo. Globo Agresivo re-enganchado a MarkApplied ally-sourced + SnapRole=Agresivo. Emisiones de OnUnitAffinity sin energy param. `ReactionName` incluido al emitir OnUnitElement.

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
| `protectorLine` | `string` | **S43** Frase Protector |
| `empaticoLine` | `string` | **S43** Frase Empático (con {0} para nombre) |
| `agresivoLine` | `string` | **S43** Frase Agresivo |
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

## Métodos Privados Clave (S41 + S45 + S46)

| Método | Descripción |
|--------|-------------|
| `BuildStates()` | Construye árbol de CombatNode doblemente enlazado desde CombatRecord.Turns. **S45:** Precomputa mapa roundOrders (orden estable por ronda). **S41:** PopulaTeamStateA/B. |
| `BeginRoutine()` | Spawns unidades vía CombatVisualUnits, emite OnVisualCombatStart |
| `ForwardRoutine()` | Anima un turno: windup/lunge/impacto/procs/muertes. **S45:** LUNGE vía MoveOverTime. **S46:** PlayProc emite OnUnitElement con ReactionName. |
| `PlayProc()` | Anima un proc (shield/heal/reacción/estado). **S43:** Emite Speech (Protector/Empático/Agresivo/narrador). **S45:** Emite OnUnitElement para cada evento elemental. **S46:** Incluye ReactionName. |
| `Restore()` | Vuelve a estado de un nodo (retroceso). Restaura Hp/Shield/Status + ElementMarks/ArmedStates. |
| `Publish()` | Emite OnPanelState (info control replay) |
| `PublishOrder()` | **S42** Emite OnActionOrder (orden de próxima acción, usado por CombatOrderBarUITK) |
| `PushElements()` | **S42** Emite OnUnitElement por cada unit (dibuja chips a barras) — NOTA: S46 incluye ReactionName |
| `SnapRole()` | **S46 NEW** Helper privado — obtiene Role del snapshot |

## Consumo de OnUnitElement (S46)

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
- [[CombatRecord]] — fuente de datos (turnos, snapshots, team ids)
- [[CombatVisualEvents]] — publisher de eventos (S46: OnUnitAffinity sin energy, OnUnitElement con ReactionName)
- [[CombatVisualUnits]] — spawn/lookup/PosOf units
- [[CombatOrderBarUITK]] — suscriptor **S42** (OnActionOrder, OnUnitAffinity, OnActiveUnit, OnUnitElement S46)
- [[CombatFeelDirector]] — **S46 NEW** consumidor de PosOf para reproducir feedbacks
- [[CombatSpeechBubbles]] — **S43** suscriptor OnSpeech
- [[CombatCameraDirector]] — **S43** suscriptor OnActiveUnit (VCamOf)

## Notas S46

- `PosOf()` público permite que CombatFeelDirector acceda a posiciones de units
- `SnapRole()` privado localiza Role desde snapshot (no desde Combatant, que ya no existe post-simulación)
- Globo Agresivo cambió: de EnergyGained (obsoleto) a MarkApplied ally-sourced + rol check
- OnUnitAffinity emite sin energy (Affinity 0-2 solamente)
- ReactionName en OnUnitElement permite parseando a ElementalState en CombatOrderBarUITK
