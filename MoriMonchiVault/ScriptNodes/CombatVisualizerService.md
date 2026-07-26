---
tags: [combat, visualization, replay, ui, 3v3]
---

# CombatVisualizerService

**Ruta:** `Systems/CombatVisualizer/CombatVisualizerService.cs`

**Responsabilidad:** Orquesta visualización local 3v3 de `CombatRecord`, construyendo árbol de nodos doblemente enlazados y generando secuencia de animaciones turno-a-turno. Play() resuelve equipos vía registry. Colaborador `CombatVisualUnits` spawn/lookup. Emite eventos UI (OnActionOrder, OnUnitElement, OnActiveUnit, etc.). **S61b:** Knob `phasePauseSeconds` (0.9s, pausa de lectura entre etapa de pasivas y ataque, solo si hubo pasivas y hay ataque); emite `OnPhase(Passives/Attack/Rest, actorSide)` para sincronizar cámaras Cinemachine vía `CombatCameraDirector`. Loop de procs post-golpe (!BeforeStrike && !PassivePhase) movido dentro del bloque de ataque — se reproduce tras popup de daño y ANTES de esperar attackDone (afinidad/marcas sincronizadas con impacto). **S61:** ReactionLine simplificado — firma sin `who`, formato solo estado negrita + description truncada a 40, emite `LogAppend` tras `UnitElement`. Colores actualizados: PositiveStateColor 6FB7FF (azul), NegativeStateColor FF9090 (rojo). **S58:** Animador retipado a `DragonAnimationDriver` (PlayAttack espera onImpact real, PlayHit, PlayDefeat, PlayVictory, PlayIdle, PlayBuff). Muertes persistentes con CorpseFade (MonchiVisualizer.SetGhost alpha). Facing con TurnTowards/TurnBack alrededor lunges. AnchorOf() público nuevo. **S59:** Anticipación de pasivas con knobs passiveAnticipationSeconds/Pullback. PushHp ganó parámetro animate (Restore pasa false → SnapHp). PushTimeScale propaga Speed a los drivers al spawn y en SetSpeed. **S68:** Voice lines eliminados — ahora keys Loc.Tr en BuildStates() + PlayProc().

## Cambios S68 (Localization-ready)

**Eliminados los [SerializeField] de voice lines:**
- `protectorLine` (e.g., "¡Toma, protégete!")
- `empaticoLine` (e.g., "¡{0}, te curo!")
- `agresivoLine` (e.g., "¡Te comparto mi espíritu de pelea!")
- `protectorSelfLine` (e.g., "¡Me escudaré!")
- `empaticoSelfLine` (e.g., "¡Qué alivio!")
- `agresivoSelfLine` (e.g., "¡Me toca a mí!")
- `attackLine` (e.g., "¡TOMA, {0}!")

**Líneas de localización agregadas:**
- Línea 226: `Loc.Tr("combat.log.versus", NamesColored(snapsA, SelfColor), NamesColored(snapsB, OppColor))` (inicio log)
- Línea 303: `Loc.Tr("combat.log.crit", atk, def, dmg)` (log golpe crítico)
- Línea 304: `Loc.Tr("combat.log.hit", atk, def, dmg)` (log golpe normal)
- Línea 323: `Loc.Tr("combat.log.death", Colored(snapsA[i].Name, SelfColor))` (log muerte)
- Línea 329: `Loc.Tr("combat.log.death", Colored(snapsB[i].Name, OppColor))` (log muerte)

**Impacto:**
- Voice lines de ataque/pasiva ahora se guardan SOLO en la String Table Collection "Strings" con keys como `"combat.voice.protector"`, `"combat.voice.agresivo"`, etc.
- Los overrides de escena (si había alguno) se pierden — las frases vienen solo de la localización.
- Log del visualizador completamente localizado (versus / hit / crit / death messages).

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Play(CreatureDNA self, CombatRecord record)` | Inicia replay 3v3 resolviendo equipos via registry |
| `Stop()` | Detiene playback, limpia, despawns unidades |
| `TogglePlay()` | Toggle automático |
| `Next()` | Avanza un turno |
| `Back()` | Retrocede un turno |
| `SetSpeed(float value)` | Setea velocidad playback (0.25–4), **S59** propaga a drivers vía PushTimeScale() |
| `VCamOf(CombatVisualSide side, int index)` | Retorna la CinemachineCamera de la unidad |
| `PosOf(CombatVisualSide side, int index)` | Retorna Vector3 posición del unit |
| `AnchorOf(CombatVisualSide side, int index)` | **S58 NEW** Retorna Transform del anchor del unit |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `visualizerPrefab` | `MonchiVisualizer` | **S58** Prefab Suriyun a instanciar |
| `boardA` | `Transform` | Board lado A (7 hijos anchor) |
| `boardB` | `Transform` | Board lado B (7 hijos anchor) |
| `elementTable` | `ElementTableSO` | Para DisplayName + UiColor + Description |
| `windupSeconds` | `float` | Duración windup (÷ Speed) |
| `impactSeconds` | `float` | Duración impacto (÷ Speed) |
| `lungeFraction` | `float` | Fracción de distancia en lunge (default 0.6) |
| `passiveAnticipationSeconds` | `float` | **S59 NEW** Duración anticipación antes de lunge pasiva (default 0.25) |
| `passiveAnticipationPullback` | `float` | **S59 NEW** Distancia pullback en anticipación (default 0.3) |
| `betweenTurnsSeconds` | `float` | Pausa entre turnos |
| `deathPauseSeconds` | `float` | Pausa al morir |
| `synergyPopupDelay` | `float` | Delay pre-popup reacción |
| `stateBeatSeconds` | `float` | Pausa en estados armados |
| `phasePauseSeconds` | `float` | **S61b NEW** Pausa entre etapa de pasivas y ataque (default 0.9) — solo si hubo pasivas y hay ataque próximo |
| `corpseFadeDelay` | `float` | **S58** Delay antes de fade (default 2.5) |
| `corpseFadeSeconds` | `float` | **S58** Duración fade (default 1.5) |
| `corpseAlpha` | `float` | **S58** Alpha final del cadáver (default 0.35) |
| `speechSeconds` | `float` | Duración globo de habla |
| `playbackSpeed` | `float` | Multiplicador velocidad |

## Estructura: CombatNode

```csharp
private class CombatNode
{
    public bool                      HasTurn;
    public CombatTurn                Turn;
    public int                       TurnNumber;
    public int                       ActionIndex;
    public List<CombatUnitState>     StateA;
    public List<CombatUnitState>     StateB;
    public bool[]                    DiedHereA;
    public bool[]                    DiedHereB;
    public CombatVisualSide          AttackerSide;
    public int                       AttackerIndex;
    public int                       DefenderIndex;
    public bool                      Crit;
    public List<CombatVisualLogLine> Log;
    public CombatNode                Prev;
    public CombatNode                Next;
    public bool IsEnd => Next == null;
}
```

## Métodos Privados Clave

| Método | Descripción |
|--------|-------------|
| `BuildStates()` | Construye árbol de CombatNode desde CombatRecord.Turns. **S68:** Usa Loc.Tr para log lines (versus, hit, crit, death). Precomputa mapa roundOrders. |
| `BeginRoutine()` | Spawns unidades vía CombatVisualUnits, emite OnVisualCombatStart, **S59** llama PushTimeScale() |
| `ForwardRoutine()` | **S61b:** Emite OnPhase(Passives/Attack/Rest, actorSide), espera phasePauseSeconds. Mueve procs post-golpe dentro bloque ataque. **S58:** Anima turno con TurnTowards/TurnBack alrededor lunges, PlayAttack espera onImpact, PlayHit(intensity), CorpseFade en muertes. **S59:** anticipación pasiva, PushHp(animate=true en proc, false en Restore). **S61:** PlayProc emite LogAppend |
| `PlayProc()` | **S61** Anima proc (shield/heal/reacción/estado). Emite OnUnitElement, luego LogAppend (solo reacciones). Usa ReactionLine(pe) sin `who`. |
| `CorpseFade()` | **S58 NEW** Corrutina que espera delay, luego lerp SetGhost(alpha) |
| `TurnTowards()` | **S58 NEW** Lerpa rotación hacia objetivo (yaw) |
| `TurnBack()` | **S58 NEW** Lerpa rotación a valor guardado |
| `PushTimeScale()` | **S59 NEW** Propaga Speed a todos los animadores vía SetTimeScale |
| `ReactionLine(pe)` | **S61 SIMPLIFICADA** Construye línea `"**{Estado}** — {Description}"` (máx 40 chars), coloreada azul/rojo por positivo/negativo. |
| `Restore()` | Vuelve a estado de un nodo. SetGhost(1) para cadáveres si no está muerto ahora. **S59:** PushHp pasa animate=false |
| `Publish()` | Emite OnPanelState |
| `PublishOrder()` | Emite OnActionOrder |
| `SetActiveFrames(side, index)` | Setea marco dorado al unit (début de turno) |

## Consumo de eventos UI (S58–S59–S61–S61b)

- `OnPhase` → **S61b NEW** CombatCameraDirector.HandlePhase() conmuta cámaras por etapa
- `OnActiveUnit` → CombatPedestalHighlighter.HandleActiveUnit() llama AnchorOf() para shine
- `OnHit/OnCrit/OnPopup` → CombatDamageNumbers renderiza popups
- `OnUnitHpChanged` → CombatRadialHealthBar.SetHp()
- `OnUnitDead` → registra log
- `OnUnitElement` → CombatOrderBarUITK.HandleUnitElement() dibuja chips (ReactionName parseado a ElementalState)
- `OnActionOrder` → CombatOrderBarUITK.HandleOrder() reordena cartas
- `OnLogAppend` → **S61 NEW** CombatVisualizerPanelUITK.HandleLogAppend() agrega línea incremental
- `OnPanelState` → CombatVisualizerPanelUITK.HandleState() renderiza log filtrado (HasUnit=true || Kind=Result)
- `OnVisualCombatEnd` → CombatPedestalHighlighter.HandleVisualCombatEnd() limpia shine

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[Index/14 - Localization]]
- [[CombatRecord]] — fuente de datos
- [[CombatVisualEvents]] — **S61b** emite OnPhase; **S61** emite OnLogAppend
- [[CombatVisualUnits]] — spawn/lookup units
- [[CombatPedestalHighlighter]] — **S58 NEW** shine pedestal
- [[CombatRadialHealthBar]] — **S58** barras radiales; **S59** PushHp(animate)
- [[MonchiAnimationDriver]] — **S58** animar ataques; **S59** SetTimeScale
- [[MonchiVisualizer]] — **S58** SetGhost(alpha) para fade
- [[CombatOrderBarUITK]] — OnActionOrder, OnUnitElement, OnActiveUnit; **S61b** accede ShortDescription de estado
- [[CombatVisualizerPanelUITK]] — **S61** OnLogAppend handler nuevo; OnPanelState
- [[CombatDamageNumbers]] — OnPopup
- [[CombatCameraDirector]] — **S61b NEW** suscriptor OnPhase (gestiona prioridades vcam por etapa)
- [[ElementTableSO]] — DisplayName, UiColor, Description para reacciones
- [[CombatSpeechBubbles]] — OnSpeech
- [[Loc]] — **S68** resolución voice lines + log messages
- [[LocEnumMaps]] — resolución enums a strings localizados (indirecto)

## Notas S68

- Voice lines eliminadas de campos [SerializeField] — completamente sustituidas por Loc.Tr
- Log del visualizador ahora completamente localizado (versus / hit / crit / death via Loc.Tr)
- BuildStates() accede Loc para generar log lines (sin dependencia de campos serializados)

## Notas S61b

- OnPhase emite en ForwardRoutine(): Passives (inicio), Attack (post-pausa), Rest (fin)
- phasePauseSeconds solo tiene efecto si hubo pasivas armadas Y hay ataque próximo
- Procs post-golpe ahora sincronizados: dentro bloque ataque, tras popup daño, antes attackDone

## Notas S61

- ReactionLine ahora sin `who` — contexto ya en speech globo + ReactionName en evento elemental
- Formato compacto: solo estado negrita + descripción truncada (40 chars max)
- Colores: PositiveStateColor azul claro (6FB7FF), NegativeStateColor rojo claro (FF9090)
- LogAppend emite DESPUÉS de UnitElement en PlayProc (sincronización beat)

## Notas S59

- Anticipación pasiva: pullback hacia atrás antes de lunge (visual feedback)
- PushHp animate control: Restore() snapea (false), proc/hit anima (true)
- TimeScale centralizado: SetSpeed propagates a todos los drivers

## Notas S58

- Animador nuevo: DragonAnimationDriver con callbacks onImpact y onDone
- Muertes son **persistentes** — cadáveres transparentes quedan en tablero
- Facing dinámico: gira hacia objetivo, retorna después
- AnchorOf() público para shine de pedestal
