---
tags: [combat, visualization, replay, ui, 3v3]
---

# CombatVisualizerService

**Ruta:** `Systems/CombatVisualizer/CombatVisualizerService.cs`

**Responsabilidad:** Orquesta visualización local 3v3 de `CombatRecord`, construyendo árbol de nodos doblemente enlazados y generando secuencia de animaciones turno-a-turno. **S41 REWRITE a equipos (1..3 por lado):** Firma nueva `Play(CreatureDNA self, CombatRecord record)` resuelve equipos vía `record.SelfTeamIds/OpponentTeamIds` desde registry (vieja sobrecarga 3-args ELIMINADA). Colaborador `CombatVisualUnits` spawn/lookup. Barras usan stats EXACTOS del snapshot (sin recomputar EquipmentStats). `CombatNode` lee `TeamStateA/TeamStateB` del record, `DiedHereA/DiedHereB` por unidad. Helper `ElementText()` narra eventos elementales (S41); afinidad/energía omitidas de narrativa. **S42 NUEVO:** Popups elementales con ReactionName + OverrideColor, PublishOrder() emite orden de próxima acción, PushElements() renderiza chips a barras, ActionIndex (contador ronda), eventos OnActionOrder/OnUnitAffinity/OnActiveUnit. **S43 NUEVO:** Campos speech tweakeables (protectorLine, empaticoLine, agresivoLine, speechSeconds, stateBeatSeconds), método VCamOf(), PlayProc emite Speech events + FlashReaction, PushShield/PushShieldAll para escudo azul, Negative en ElementChipData. **S45 NUEVO:** Mapa `roundOrders` (Dictionary<int, List<(side,index)>>) precomputado en BuildStates para orden ESTABLE dentro ronda; dead-actors se mantienen en posición, muertos previos van al final. En ForwardRoutine: LUNGE (atacante se desplaza via `MoveOverTime()`), nuevo método privado Lerp-based. En PlayProc: emite `OnUnitElement` por cada proc elemental (MarkApplied/MarkRemoved/Reaction/StateArmed/StateConsumed/StateRemoved). Beat conditions expandidas: MarkApplied, EnergyGained, AffinityGained>=2 ahora tienen beat.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Play(CreatureDNA self, CombatRecord record)` | **S41 FIRMA NUEVA** Inicia replay 3v3 resolviendo equipos via registry |
| `Stop()` | Detiene playback, limpia, despawns unidades |
| `TogglePlay()` | Toggle automático |
| `Next()` | Avanza un turno |
| `Back()` | Retrocede un turno |
| `SetSpeed(float value)` | Setea velocidad playback (0.25–4) |
| `VCamOf(CombatVisualSide side, int index)` | **S43 NEW** Retorna la CinemachineCamera de la unidad (para cortes de cámara) |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `visualizerPrefab` | `MoriMonchiVisualizer` | Prefab a instanciar |
| `boardA` | `Transform` | Board lado A (raíz, 7 hijos anchor: Front0/1, Mid0/1/2, Back0/1) |
| `boardB` | `Transform` | Board lado B (idem) |
| `elementTable` | `ElementTableSO` | **S42 NEW** Para DisplayName + UiColor de elementos (tooltips, log, chips) |
| `windupSeconds` | `float` | Duración windup (÷ Speed) |
| `impactSeconds` | `float` | Duración impacto (÷ Speed) |
| `lungeFraction` | `float` | **S45 NEW** Fracción de distancia recorrida en lunge (0..1, default 0.6) |
| `betweenTurnsSeconds` | `float` | Pausa entre turnos |
| `deathPauseSeconds` | `float` | Pausa al morir |
| `synergyPopupDelay` | `float` | Delay pre-popup reacción (÷ Speed) |
| `stateBeatSeconds` | `float` | **S43 NEW** Pausa en estados armados (narrador) |
| `protectorLine` | `string` | **S43 NEW** Frase Protector: "¡Toma, protégete!" |
| `empaticoLine` | `string` | **S43 NEW** Frase Empático: "¡{0}, te curo!" (formato para nombre aliado) |
| `agresivoLine` | `string` | **S43 NEW** Frase Agresivo: "¡Te comparto mi espíritu de pelea!" |
| `speechSeconds` | `float` | **S43 NEW** Duración globo de habla (default 1.6s) |
| `playbackSpeed` | `float` | Multiplicador velocidad (0.25–4) |

## Estructura: CombatNode (S41 NUEVO, S42: ActionIndex, S45: sin cambios)

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

## Construcción de Estados (S41, S42: ActionIndex + ElementText, S45: roundOrders precomputado)

**BuildStates()** construye árbol `CombatNode` desde `CombatRecord.Turns`:

**Orden por Ronda (S45 NEW):** Mapa `roundOrders` (Dictionary<int, List<(CombatVisualSide Side, int Index)>>) precomputado:
- Por cada turno en record, se agrega `(attackerSide, attackerIndex)` a la lista de su `TurnNumber`
- Si atacante ya está en la lista, no se duplica
- Cuando se publica orden en PublishOrder(), se emite la lista del TurnNumber actual
- Dead-actors se mantienen en su posición en la lista, muertos previos van al final (orden estable dentro ronda)

**Barras (S41 NUEVO):** Stats tomados DIRECTAMENTE del snapshot (`CombatFighterSnapshot`). NO se recomputan mods de equipo en el visualizer — snapshot es fuente de verdad (ya incluye equipo del momento de pelea).

**Procesamiento Turns (S42: ActionIndex incrementa, S45: sin cambios core):**
- Procesa `Turn.Procs` (antes/después golpe)
  - Si `ElementEvent == None`: llama `ProcText(pe, ...)` (clásico)
  - Si `ElementEvent != None`: llama `ElementText(pe, ...)` (S41 NEW)
- Log del golpe (ataque/crítico/dodge)
- Captura `TeamStateA/TeamStateB` del turno (estado full de todos los units tras este turno)
- Detecta muertes por HP ≤ 0 en `DiedHereA/DiedHereB` (por unit, S41)
- **S22 NEW:** Incrementa ActionIndex (totalTurns++)
- **S45:** Actualiza `roundOrders` con `(attackerSide, attackerIndex)` si no existe ya en esa ronda
- Crea `CombatNode` enlazado con ActionIndex

**Helper `ElementText()` (S41 NEW, S42/S43 sin cambios):** Narra eventos elementales solo si `pe.ElementEvent != None`:
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

**Spawn (S41, S42: con elementTable, S43: igual):** Tras BuildStates, `CombatVisualUnits.Spawn(side, dnas, snapshots, board, prefab, elementTable)` instancia modelos en anchors (S42: pasa elementTable para bind de barra).

## Colaborador: CombatVisualUnits (S41, S42, S43: sin cambios API)

Clase privada que maneja spawn/lookup/lifecycle (regla 11, composición):
- `Spawn(side, dnas, snapshots, board, prefab, elements)` — **S22:** con ElementTableSO
- `Get(side, index)` — busca unidad
- `DespawnAll()` — destruye todos
- `TransformOf(side, index)` — retorna transform para popups
- `PosOf(side, index)` — retorna posición
- `SetActive(unit, active)` — muestra/oculta
- **S43:** `units.Get(side, index)?.VCam` accesible via `VCamOf(side, index)`

## Método MoveOverTime() (S45 NEW)

```csharp
private IEnumerator MoveOverTime(Transform tr, Vector3 from, Vector3 to, float duration)
{
    if (tr == null) yield break;
    if (duration <= 0f) { tr.position = to; yield break; }
    float elapsed = 0f;
    while (elapsed < duration)
    {
        if (tr == null) yield break;
        elapsed += Time.deltaTime;
        tr.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
        yield return null;
    }
    if (tr != null) tr.position = to;
}
```

Coroutine que interpola la posición del Transform del atacante durante el lunge. Se usa en ForwardRoutine para:
1. Mover desde posición home hacia `lungePos` (home + 60% de distancia hacia defensor) durante windup
2. Mover de vuelta a home durante impacto

**S45:** Reemplaza movimiento instantáneo con animación fluida; lungeFraction permite tweakear qué tan cerca llega del defensor.

## Eventos Aditivos S42/S43/S45

**PublishOrder():** Emite `OnActionOrder(List<CombatOrderEntry>)` con próximas acciones vivas (atacantes) + todas las unidades vivas + todas las muertas. Orden:
1. Próximos atacantes (desde `roundOrders[currentTurnNumber]`, first appearance each (side, index))
2. Unidades vivas no listadas (self)
3. Unidades muertas (al final, gris)

**PushElements():** Renderiza chips elementales a barras (antes de `PublishOrder()`). Itera `CombatUnitState.ElementMarks` (marcas) y `ArmedStates` (estados), convierte a `ElementChipData`, llama `Bar.SetElementState()`. **S43:** Agrega bool Negative a ElementChipData (true si es estado negativo o marca enemiga). **S45:** Sin cambios.

**PlayProc() S42/S43/S45:** Rama de `CombatProcEvent`:
- **S42:** Si `ElementEvent == Reaction`: Emite `OnPopup()` con Kind = Reaction, Text = ReactionName, OverrideColor = elemento.UiColor, HasOverrideColor = true
- **S43 NUEVO:** Emite `OnSpeech()` para roles (Protector pre-golpe, Empático post-golpe, Agresivo al ganar energía)
- **S43 NUEVO:** Si `ElementEvent == StateArmed`: emite Speech narrador "¡Quedé {stateName}!" (rojo si negativo, verde si positivo) + beat extra con stateBeatSeconds
- **S45 NUEVO:** Emite `OnUnitElement()` por cada ElementEventKind (MarkApplied, MarkRemoved, Reaction, StateArmed, StateConsumed, StateRemoved) — permite que barra actualice en vivo
- **S45 NUEVO:** Beat conditions expandidas: MarkApplied, EnergyGained, AffinityGained>=2, StateArmed, StateConsumed, MarkRemoved, Reaction, y ModifierEffectKind.Shield/Heal ahora tienen pausa stateBeatSeconds

**PushShield() / PushShieldAll() S43 NEW:** Restauran escudo post-turno o mid-golpe. Llaman `Bar.SetShield(shield)` (dibuja barra azul 4px sobre hp-track), emiten popup Shield con valor. **S45:** Sin cambios.

**Restore() S42/S43/S45:** Llama `PublishOrder()` al fin. **S43:** También llama `PushShield()` para sincronizar escudos. **S45:** Sin cambios (roundOrders ya precomputado).

**ForwardRoutine() S42/S43/S45:** 
- S22: Emite `OnActiveUnit()` al inicio, llama `PushElements()` + `PublishOrder()` al fin de turno
- S43: Igual, pero `PushShield()` se llama post-golpe (DefenderShieldAfter en mid-golpe) y post-turno
- **S45 NUEVO:** Lunge animado vía `MoveOverTime()` durante windup/impacto. Posición lunge = home + (targetPos - home) * lungeFraction. Atacante se desplaza hacia el defensor durante windup, vuelve a home durante impacto. Si atkUnit == null, salta MoveOverTime (1v1 legacy).

## Método VCamOf() S43 NEW

```csharp
public Unity.Cinemachine.CinemachineCamera VCamOf(CombatVisualSide side, int index) 
    => units.Get(side, index)?.VCam;
```

Retorna la vcam Cinemachine de la unidad en (side, index), o null si no existe. Usado por CombatCameraDirector para elevar prioridades.

## Determinismo

- Barras animadas desde `CombatUnitState.Hp/Shield` del record (no recomputadas)
- Logs narrativos desde `CombatProcEvent` (clásicos + elementales S41)
- Orden turnos/muertes por unit definido por record
- **S22:** Orden de acción desde `roundOrders` (precomputado en BuildStates, estable dentro ronda)
- **S43:** Speech frases + timing serializados (protectorLine, empaticoLine, agresivoLine, speechSeconds, stateBeatSeconds)
- **S45:** Lunge fraction serializado (lungeFraction, default 0.6); order estable via roundOrders.TurnNumber

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

## Cambios S43

**Aditivos (append-only):**
- **Campos serializados nuevos:** 
  - `stateBeatSeconds` (pausa en narrador estado, default 0.5s)
  - `protectorLine`, `empaticoLine`, `agresivoLine` (frases tweakeables)
  - `speechSeconds` (duración globo, default 1.6s)
- **Nuevo método público:** `VCamOf(side, index)` — retorna CinemachineCamera de unit para CombatCameraDirector
- **PlayProc() rama nueva (S43):**
  - Si atacante es rol Protector: emite Speech pre-golpe con protectorLine + color azul
  - Si defensor es rol Empático post-golpe: emite Speech con empaticoLine (format {0} = nombre aliado sanado) + color verde
  - Si atacante gana energía (AffinityGained/EnergyGained): emite Speech Agresivo con agresivoLine + color rojo
  - Si StateArmed en Procs post-golpe: emite Speech narrador "¡Quedé {stateName}!" con `FlashReaction(text, color)`, beat extra con `yield return WaitForSeconds(stateBeatSeconds / Speed)`
- **Nuevos métodos privados S43:**
  - `PushShield(side, index)` — llamado post-turno, emite popup Shield si escudo > 0
  - `PushShieldAll()` — llama PushShield para todos los units vivos
  - (reemplazan/complementan a PushElements)
- **ElementChipData.Negative:** S42 solo tenía `Label, Color, AllySource`; S43 agrega `bool Negative` (true = rojo, false = verde/aliado)
- **MoriMonchiCombatVisualizerUITK.FlashReaction():** método nuevo en barra UI, llamado por PlayProc cuando StateArmed, muestra texto transient 2s

**Invariante:** Eventos/métodos S42 siguen intactos; S43 agrega Speech + Shield + VCamOf() sin romper compat.

## Cambios S45

**Aditivos (append-only):**
- **Campo serializado nuevo:**
  - `lungeFraction` (Range 0..1, default 0.6) — fracción de distancia lunge durante ataque
- **Campo privado nuevo:**
  - `roundOrders` (Dictionary<int, List<(CombatVisualSide Side, int Index)>>) — mapa TurnNumber → lista de atacantes en esa ronda, precomputado en BuildStates
- **Nuevo método privado:**
  - `MoveOverTime(Transform tr, Vector3 from, Vector3 to, float duration)` — coroutine Lerp para animación lunge
- **PlayProc() rama nueva (S45):**
  - Emite `OnUnitElement(CombatElementEventData)` para cada ElementEventKind (MarkApplied, MarkRemoved, Reaction, StateArmed, StateConsumed, StateRemoved) — permite actualizar marcas/estados de barra en vivo
  - Beat conditions expandidas: MarkApplied, EnergyGained, AffinityGained>=2, StateArmed, StateConsumed, MarkRemoved, Reaction, ModifierEffectKind.Shield, ModifierEffectKind.Heal → todas tienen beat de stateBeatSeconds
- **ForwardRoutine() rama nueva (S45):**
  - Calcula `lungePos = atkHome + (targetPos - atkHome) * lungeFraction`
  - Durante windup: `MoveOverTime(atkUnit.Instance.transform, atkHome, lungePos, windupSeconds / Speed)`
  - Durante impacto: `MoveOverTime(atkUnit.Instance.transform, lungePos, atkHome, impactSeconds / Speed)`
  - Si atkUnit == null o Instance == null, salta MoveOverTime (1v1 legacy fallback)
- **BuildStates() rama nueva (S45):**
  - Itera `activeRecord.Turns`, por cada turno agrega `(attackerSide, attackerIndex)` a `roundOrders[t.TurnNumber]` si aún no existe
  - Permite orden estable dentro ronda: dead-actors se mantienen en posición, muertos previos al final

**Invariante:** Eventos/métodos S43 siguen intactos; S45 agrega Lunge + OnUnitElement + roundOrders sin romper compat.

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CombatVisualUnits]] — spawn/lookup (S41, S42: con elementTable, S43: VCam access)
- [[CombatVisualEvents]] — eventos replay (S41: OnUnitHpChanged/OnUnitDead, S42: OnActionOrder/OnUnitAffinity/OnActiveUnit, **S43: OnSpeech aditivo, S45: OnUnitElement aditivo**)
- [[CombatRecord]] — fuente datos (S41: TeamStateA/B con ElementMarks/ArmedStates)
- [[CombatCameraDirector]] — **S43 NEW** suscriptor OnActiveUnit, llama VCamOf() para vcam priorities
- [[CombatSpeechBubbles]] — **S43 NEW** suscriptor OnSpeech, renderiza globos cómic
- [[MoriMonchiCombatVisualizerUITK]] — **S43:** FlashReaction() para narrador estados + SetShield() para escudo
- [[CombatOrderBarUITK]] — **S45 NEW** suscriptor OnUnitElement, actualiza marcas/estados por-proc

## Notas Implementación S43 / S45

- Globos de habla (Speech) emitidos por PlayProc en 4 escenarios: Protector pre-ataque, Empático post-curación, Agresivo al gastar energía, narrador en estados armados
- Vcam priorities: 0 (inactivo default), 10 (scene cam), 20 (active unit — CombatCameraDirector)
- Shield popups solo si Amount > 0; AnimateHp en barra tweenea a máximo en estateBeatSeconds
- Todos los timings (speechSeconds, stateBeatSeconds, windupSeconds, impactSeconds, etc.) dividen por Speed para playback variable
- Frases son tweakeables en inspector para localization futura
- **S45:** Lunge animado permite visualizar "movimiento" del atacante hacia defensor; lungeFraction=0.6 = 60% de la distancia. Si lungeFraction=0, atacante se queda en posición inicial (desactiva lunge). lungeFraction=1 sería al lado del defensor (exagerado)
- **S45:** OnUnitElement dispara por cada proc elemental DURANTE PlayProc, permitiendo que CombatOrderBarUITK actualice Marks/States lists en vivo sin esperar a fin de turno
