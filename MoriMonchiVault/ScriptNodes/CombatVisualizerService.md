---
tags: [combat, visualization, replay, ui, 3v3]
---

# CombatVisualizerService

**Ruta:** `Systems/CombatVisualizer/CombatVisualizerService.cs`

**Responsabilidad:** Orquesta visualización local 3v3 de `CombatRecord`, construyendo árbol de nodos doblemente enlazados y generando secuencia de animaciones turno-a-turno. **S41 REWRITE a equipos (1..3 por lado):** Firma nueva `Play(CreatureDNA self, CombatRecord record)` resuelve equipos vía `record.SelfTeamIds/OpponentTeamIds` desde registry (vieja sobrecarga 3-args ELIMINADA). Colaborador `CombatVisualUnits` spawn/lookup. Barras usan stats EXACTOS del snapshot (sin recomputar EquipmentStats). `CombatNode` lee `TeamStateA/TeamStateB` del record, `DiedHereA/DiedHereB` por unidad. Helper `ElementText()` narra eventos elementales (S41); afinidad/energía omitidas de narrativa. **S42 NUEVO:** Popups elementales con ReactionName + OverrideColor, PublishOrder() emite orden de próxima acción, PushElements() renderiza chips a barras, ActionIndex (contador ronda), eventos OnActionOrder/OnUnitAffinity/OnActiveUnit.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Play(CreatureDNA self, CombatRecord record)` | **S41 FIRMA NUEVA** Inicia replay 3v3 resolviendo equipos via registry |
| `Stop()` | Detiene playback, limpia, despawns unidades |
| `TogglePlay()` | Toggle automático |
| `Next()` | Avanza un turno |
| `Back()` | Retrocede un turno |
| `SetSpeed(float value)` | Setea velocidad playback (0.25–4) |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `visualizerPrefab` | `MoriMonchiVisualizer` | Prefab a instanciar |
| `boardA` | `Transform` | Board lado A (raíz, 7 hijos anchor: Front0/1, Mid0/1/2, Back0/1) |
| `boardB` | `Transform` | Board lado B (idem) |
| `elementTable` | `ElementTableSO` | **S42 NEW** Para DisplayName + UiColor de elementos (tooltips, log, chips) |
| `windupSeconds` | `float` | Duración windup (÷ Speed) |
| `impactSeconds` | `float` | Duración impacto (÷ Speed) |
| `betweenTurnsSeconds` | `float` | Pausa entre turnos |
| `deathPauseSeconds` | `float` | Pausa al morir |
| `synergyPopupDelay` | `float` | Delay pre-popup reacción (÷ Speed) |
| `playbackSpeed` | `float` | Multiplicador velocidad (0.25–4) |

## Estructura: CombatNode (S41 NUEVO, S42: ActionIndex)

```csharp
private class CombatNode
{
    public bool                      HasTurn;           // si es turno real
    public CombatTurn                Turn;              // turno simulado
    public int                       TurnNumber;        // número del turno en record
    public int                       ActionIndex;       // **S42 NEW** contador de acciones/turnos vistos (0 = head)
    public List<CombatUnitState>     StateA;            // estado team A tras turno (S41)
    public List<CombatUnitState>     StateB;            // estado team B tras turno (S41)
    public bool[]                    DiedHereA;         // units de A que murieron (S41)
    public bool[]                    DiedHereB;         // units de B que murieron (S41)
    public CombatVisualSide          AttackerSide;      // A o B
    public int                       AttackerIndex;     // índice dentro equipo (S41)
    public int                       DefenderIndex;     // índice dentro equipo (S41)
    public bool                      Crit;              // crítico
    public List<CombatVisualLogLine> Log;               // log acumulado
    public CombatNode                Prev;
    public CombatNode                Next;
    public bool IsEnd => Next == null;
}
```

**Changes S41:**
- Eliminados: `HpA/HpB`, `ADead/BDead`, `StatusA/StatusB` (1v1 legacy)
- Agregados: `StateA/StateB` (equipos), `DiedHereA/DiedHereB` (muertes por unit), `AttackerIndex/DefenderIndex`

**Changes S42:**
- Agregado: `ActionIndex` — contador de acciones vistas (0 en head, incrementa por cada turno no-head). Usado en `CombatVisualPanelState.TurnNumber` y `PublishOrder()`.

## Flujo de Play() (S41)

```csharp
public void Play(CreatureDNA self, CombatRecord record)
{
    // Validar record 3v3 (no records 1v1 legacy)
    if (record.SelfTeam == null || record.SelfTeamIds == null || record.OpponentTeamIds == null)
    {
        Debug.LogWarning("[CombatVisualizer] record sin datos 3v3");
        return;
    }

    // Resolver IDs desde registry
    var reg = GameManager.Instance.Registry;
    var resolvedSelf = new List<CreatureDNA>();
    foreach (var id in record.SelfTeamIds)
        if (!reg.TryGet(id, out var dna)) { Debug.LogWarning(...); return; }
        resolvedSelf.Add(dna);

    var resolvedOpp = new List<CreatureDNA>();
    foreach (var id in record.OpponentTeamIds)
        if (!reg.TryGet(id, out var dna)) { Debug.LogWarning(...); return; }
        resolvedOpp.Add(dna);

    Stop();
    activeRecord = record;
    teamSelf = resolvedSelf;
    teamOpp = resolvedOpp;
    BuildStates();
    if (head == null) { Debug.LogWarning("[CombatVisualizer] No states built."); return; }
    beginRoutine = StartCoroutine(BeginRoutine());
}
```

**Cambios S41:**
- Firma cambió de `Play(self, opponent, record)` a `Play(self, record)`
- Opponent RESUELTO vía registry desde `record.OpponentTeamIds`
- Validación EXIGE SelfTeam != null (no 1v1 legacy)

## Construcción de Estados (S41, S42: ActionIndex + ElementText)

**BuildStates()** construye árbol `CombatNode` desde `CombatRecord.Turns`:

**Barras (S41 NUEVO):** Stats tomados DIRECTAMENTE del snapshot (`CombatFighterSnapshot`). NO se recomputan mods de equipo en el visualizer — snapshot es fuente de verdad (ya incluye equipo del momento de pelea).

**Procesamiento Turns (S42: ActionIndex incrementa):**
- Procesa `Turn.Procs` (antes/después golpe)
  - Si `ElementEvent == None`: llama `ProcText(pe, ...)` (clásico)
  - Si `ElementEvent != None`: llama `ElementText(pe, ...)` (S41 NEW)
- Log del golpe (ataque/crítico/dodge)
- Captura `TeamStateA/TeamStateB` del turno (estado full de todos los units tras este turno)
- Detecta muertes por HP ≤ 0 en `DiedHereA/DiedHereB` (por unit, S41)
- **S42 NEW:** Incrementa ActionIndex (totalTurns++)
- Crea `CombatNode` enlazado con ActionIndex

**Helper `ElementText()` (S41 NEW, S42 sin cambios):** Narra eventos elementales solo si `pe.ElementEvent != None`:
- `MarkApplied`: "{Name} recibe marca {Element}"
- `Reaction`: "¡{ReactionName}! sobre {Name}" (S42: DisplayName del elemento)
- `StateArmed`: "{Name} queda {State}"
- `StateConsumed`: "{Name} consume {State}"
- `StateRemoved`: "{Name} pierde {State}"
- `MarkRemoved`: "{Name} pierde la marca {Element}"
- `Heal/Damage`: "Cura {Name} +X" o "Daña {Name} -X"
- `ShieldDoubled`: "Escudo duplicado"
- `EnergySpent/EnergyGained`: **sin línea** (omitidas de narrativa visual)

**Helpers S42:** `ElemName(e)` y `StateName(s)` retornan DisplayName desde ElementTableSO (fallback ToString).

**Spawn (S41, S42: con elementTable):** Tras BuildStates, `CombatVisualUnits.Spawn(side, dnas, snapshots, board, prefab, elementTable)` instancia modelos en anchors (S42: pasa elementTable para bind de barra).

## Colaborador: CombatVisualUnits (S41, S42)

Clase privada que maneja spawn/lookup/lifecycle (regla 11, composición):
- `Spawn(side, dnas, snapshots, board, prefab, elements)` — **S42:** con ElementTableSO
- `Get(side, index)` — busca unidad
- `DespawnAll()` — destruye todos
- `TransformOf(side, index)` — retorna transform para popups
- `PosOf(side, index)` — retorna posición
- `SetActive(unit, active)` — muestra/oculta

## Eventos Aditivos S42

**PublishOrder():** Emite `OnActionOrder(List<CombatOrderEntry>)` con próximas acciones vivas (atacantes) + todas las unidades vivas + todas las muertas. Orden:
1. Próximos atacantes (hasta fin record, de current.Next onwards, first appearance each (side, index))
2. Unidades vivas no listadas (self)
3. Unidades muertas (al final, gris)

**PushElements():** Renderiza chips elementales a barras (antes de `PublishOrder()`). Itera `CombatUnitState.ElementMarks` (marcas) y `ArmedStates` (estados), convierte a `ElementChipData`, llama `Bar.SetElementState()`.

**PlayProc() S42 NUEVO:** Rama de `CombatProcEvent` con `ElementEvent == Reaction`:
- Emite `OnPopup()` con Kind = Reaction, Text = ReactionName, OverrideColor = elemento.UiColor, HasOverrideColor = true
- Emite `OnUnitAffinity()` para AffinityGained/EnergyGained/EnergySpent

**Restore() S42 NUEVO:** Llama `PublishOrder()` al fin.

**ForwardRoutine() S42:** Emite `OnActiveUnit()` al inicio, llama `PushElements()` + `PublishOrder()` al fin de turno.

## Determinismo

- Barras animadas desde `CombatUnitState.Hp/Shield` del record (no recomputadas)
- Logs narrativos desde `CombatProcEvent` (clásicos + elementales S41)
- Orden turnos/muertes por unit definido por record
- **S42:** Orden de acción desde record.Turns (doblemente enlazado)

## Cambios S41

- **Firma:** `Play(self, record)` — opponent RESUELTO via registry
- **CombatNode:** TeamStateA/B, DiedHereA/B (por unit), AttackerIndex/DefenderIndex
- **Spawn:** Delegado a `CombatVisualUnits` (composición)
- **Barras:** Stats EXACTOS del snapshot (sin recomputo)
- **ElementText():** Narra eventos elementales (marcas, reacciones, estados); energía omitida
- **Validación:** EXIGE record 3v3 (SelfTeam != null)

## Cambios S42

**Aditivos (append-only):**
- **Campo:** `elementTable` (serializado, para DisplayName + UiColor)
- **CombatNode.ActionIndex:** contador de acciones (0 = head, incrementa por turno jugable)
- **Nuevos eventos:** `OnActionOrder(order)`, `OnUnitAffinity(side, index, affinity, energy)`, `OnActiveUnit(side, index)`
- **Nuevos métodos privados:** `PublishOrder()` (emite orden), `PushElements(node)` (renderiza chips), `PlayProc()` rama elemental, helpers `ElemName(e)` y `StateName(s)`
- **Métodos actualizados:** `Restore()` (llama PublishOrder), `ForwardRoutine()` (emite ActiveUnit, PushElements, PublishOrder), `Publish()` usa ActionIndex
- **PopupElemental:** Kind Reaction con Text custom + OverrideColor (en PlayProc() si ElementEvent == Reaction)
- **Spawn:** Pasa elementTable a CombatVisualUnits

**Invariante:** Eventos 1v1 legacy siguen disparándose, métodos viejos intactos (DespawnAll, CanForward/Back, PlayAttack/HitDealt/Dead, etc.).

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CombatVisualUnits]] — spawn/lookup (S41, S42: con elementTable)
- [[CombatVisualEvents]] — eventos replay (S41: OnUnitHpChanged/OnUnitDead, S42: OnActionOrder/OnUnitAffinity/OnActiveUnit aditivos)
- [[CombatRecord]] — fuente datos (S41: TeamStateA/B con ElementMarks/ArmedStates)
- [[CombatProcEvent]] — logs (S41: ElementEvent para narración, S42: ReactionName)
- [[ElementTableSO]] — **S42 NEW** DisplayName + UiColor para narración + chips

## Conexiones

**Entrada:** `Play(self, record)` desde UI/test

**Salida:**
- `CombatVisualEvents.On{Start,Popup,Dead,ActionOrder,UnitAffinity,ActiveUnit}` — eventos visuales
- `CombatVisualUnits` — instancias de modelos
