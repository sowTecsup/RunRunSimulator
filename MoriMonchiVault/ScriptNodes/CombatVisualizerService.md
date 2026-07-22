---
tags: [combat, visualization, replay, ui, 3v3]
---

# CombatVisualizerService

**Ruta:** `Systems/CombatVisualizer/CombatVisualizerService.cs`

**Responsabilidad:** Orquesta visualización local 3v3 de `CombatRecord`, construyendo árbol de nodos doblemente enlazados y generando secuencia de animaciones turno-a-turno. Play() resuelve equipos vía registry. Colaborador `CombatVisualUnits` spawn/lookup. Emite eventos UI (OnActionOrder, OnUnitElement, OnActiveUnit, etc.). **S61b:** Knob `phasePauseSeconds` (0.9s, pausa de lectura entre etapa de pasivas y ataque, solo si hubo pasivas y hay ataque); emite `OnPhase(Passives/Attack/Rest, actorSide)` para sincronizar cámaras Cinemachine vía `CombatCameraDirector`. Loop de procs post-golpe (!BeforeStrike && !PassivePhase) movido dentro del bloque de ataque — se reproduce tras popup de daño y ANTES de esperar attackDone (afinidad/marcas sincronizadas con impacto). **S61:** ReactionLine simplificado — firma sin `who`, formato solo estado negrita + description truncada a 40, emite `LogAppend` tras `UnitElement`. Colores actualizados: PositiveStateColor 6FB7FF (azul), NegativeStateColor FF9090 (rojo). **S58:** Animador retipado a `DragonAnimationDriver` (PlayAttack espera onImpact real, PlayHit, PlayDefeat, PlayVictory, PlayIdle, PlayBuff). Muertes persistentes con CorpseFade (MonchiVisualizer.SetGhost alpha). Facing con TurnTowards/TurnBack alrededor lunges. AnchorOf() público nuevo. **S59:** Anticipación de pasivas con knobs passiveAnticipationSeconds/Pullback. PushHp ganó parámetro animate (Restore pasa false → SnapHp). PushTimeScale propaga Speed a los drivers al spawn y en SetSpeed.

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
| `protectorLine` | `string` | Frase Protector (default: "¡Toma, protégete!") |
| `empaticoLine` | `string` | Frase Empático (default: "¡{0}, te curo!") con {0} para nombre |
| `agresivoLine` | `string` | Frase Agresivo (default: "¡Te comparto mi espíritu de pelea!") |
| `protectorSelfLine` | `string` | Frase Protector sobre sí mismo (default: "¡Me escudaré!") |
| `empaticoSelfLine` | `string` | Frase Empático sobre sí mismo (default: "¡Qué alivio!") |
| `agresivoSelfLine` | `string` | Frase Agresivo sobre sí mismo (default: "¡Me toca a mí!") |
| `attackLine` | `string` | Frase de ataque (default: "¡TOMA, {0}!") con {0} para nombre defensor |
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

## Cambios S61b (Pacing de turnos por etapas, sincronización Cinemachine)

**Knob phasePauseSeconds nuevo:**
```csharp
[SerializeField, MinValue(0f)] private float phasePauseSeconds = 0.9f;
```

**Propósito:**
- Pausa de lectura entre etapa de pasivas y etapa de ataque (solo si hubo ambas)
- Permite que el jugador procese visualmente el cambio de fase
- Knob serializado para tuneable por Juan

**Flujo S61b en ForwardRoutine():**
1. Anima pasivas aliadas (si existen) → emite `Phase(Passives, actorSide)` (cámara tablero actor)
2. **Espera `phasePauseSeconds` segundos** (pausa de lectura)
3. Emite `Phase(Attack, actorSide)` (cámara tablero opuesto/defensor)
4. Anima ataque y procs post-golpe (sincronizados con impacto)
5. Fin: emite `Phase(Rest, A)` (cámaras escena base)

**Emisión de OnPhase:**
```csharp
// Inicio pasivas (si existen)
if (hasPassives)
{
    CombatVisualEvents.Phase(CombatTurnPhase.Passives, atkUnit.Side);
}

// Pausa lectora entre fases
yield return new WaitForSeconds(phasePauseSeconds / Speed);

// Antes de ataque
CombatVisualEvents.Phase(CombatTurnPhase.Attack, atkUnit.Side);

// Anima ataque

// Fin turno
CombatVisualEvents.Phase(CombatTurnPhase.Rest, CombatVisualSide.A);
```

**Consumidor (CombatCameraDirector):**
- Suscriptor a `OnPhase(phase, actorSide)`
- Passives: sube cámara tablero actor (allyCamera si A, enemyCamera si B) a phasePriority=30
- Attack: sube cámara tablero opuesto a phasePriority=30
- Rest: ambas cámaras a prioridad 0 (escena base en 10)

**Cambio en loop de procs (S61b específico):**
- **Antes S61b:** Procs post-golpe (!BeforeStrike && !PassivePhase) se ejecutaban al final del turno (fuera del bloque de ataque)
- **S61b:** Movidos **dentro** del bloque de ataque → se reproducen tras popup de daño y ANTES de esperar `attackDone`
- **Impacto:** Afinidad y marcas aplicadas se sincronizan con el impacto visual del golpe (no desconectadas al final)

**Propósito:**
- Sincronización visual: cambio de cámara genera pausa natural para lectura de pasivas
- Pacing: juego más respirable entre etapas
- Futuro: permite tutoriales/explicaciones entre fases

## Cambios S61 (ReactionLine simplificada)

**ReactionLine(CombatProcEvent pe) firma actualizada:**
```csharp
private string ReactionLine(CombatProcEvent pe)
{
    bool parsed = System.Enum.TryParse<ElementalState>(pe.ReactionName, out var st);
    string state = parsed
        ? Colored($"<b>{StateName(st)}</b>", NegativeStates.Contains(st) ? NegativeStateColor : PositiveStateColor)
        : pe.ReactionName;
    if (!parsed) return state;
    string desc = elementTable != null ? elementTable.GetState(st).Description : null;
    if (string.IsNullOrEmpty(desc)) return state;
    return $"{state} — {Colored(Truncate(desc, 40), DescriptionColor)}";
}
```

**Cambios vs S59d:**
- **Firma:** Antes `ReactionLine(CombatProcEvent pe, string who)`, ahora `ReactionLine(CombatProcEvent pe)` (sin `who`)
- **Formato:** Eliminado prefijo `"{who}: {ElemA}+{ElemB} →"` → SOLO `"{estado negrita} — {descripción}"`
- **Colores:**
  - `PositiveStateColor = "6FB7FF"` (azul cielo, antes verde "86E3A0")
  - `NegativeStateColor = "FF9090"` (rojo claro, sin cambio)
- **Truncate:** `maxLength = 40` (antes 70)
- **Propósito:** Visualización compacta, sin redundancia de nombres/elementos (ya mostrados en speech globos)

**Ejemplo S61 vs S59d:**
- S59d: `"Juan: Fuego+Hielo → Debilidad — Estado que reduce defensa en un 30%"`
- S61:   `"**Debilidad** — Estado que reduce defensa…"` (40 chars max)

**Consumo en PlayProc():**
```csharp
if (pe.ElementEvent == ElementEventKind.Reaction)
{
    CombatVisualEvents.UnitElement(new CombatElementEventData { /* ... */ });
    CombatVisualEvents.LogAppend(Line(CombatVisualLogKind.Proc, ReactionLine(pe), true, side, pe.TargetIndex));  // S61 NEW
}
```

**Impacto S61:**
- ReactionLine ahora genera línea independiente (sin contexto de quién/qué elementos)
- LogAppend emite DESPUÉS de UnitElement — sincronización visual en beat exacto del proc
- Panel log actualiza incrementalmente (AddCard) en lugar de rebuild total

## Cambios S58 (Migración Suriyun + Retiro Pipeline Visual Legacy)

**Animador retipado:**
- `atkUnit?.Anim?.PlayAttack()` ahora es `MonchiAnimationDriver`
- **Firma nueva:** `PlayAttack(Vector3 target, Action onImpact, Action onDone)` — espera callbacks en lugar de hardcoded
- Otros métodos: `PlayHit(intensity)`, `PlayDefeat()`, `PlayVictory()`, `PlayIdle()`, `PlayBuff(buffName)`
- Línea 480: `atkUnit?.Anim?.PlayBuff(null)` anima pasivas (BuffName = null para genérico)

**Muertes persistentes (no destroy, fade):**
- Línea 572: `unit.Bar?.gameObject.SetActive(false)` — oculta barra antes de fade
- Línea 572: `corpseFades.Add(StartCoroutine(CorpseFade(unit)))` — inicia fade del cadáver
- CorpseFade (línea 616–627): espera `corpseFadeDelay`, luego lerp SetGhost(1→corpseAlpha) durante `corpseFadeSeconds`
- **Nunca** SetActive(false) en la unidad — el cadáver sigue visible (transparente) en el tablero

**Facing con TurnTowards/TurnBack:**
- Línea 462: `yield return StartCoroutine(TurnTowards(atkUnit.Instance.transform, units.PosOf(groupSide, groupIndex), turnSeconds / Speed))` — gira hacia defensor
- Línea 488: `yield return StartCoroutine(TurnBack(atkUnit.Instance.transform, passiveRot, turnSeconds / Speed))` — vuelve a rotación anterior
- TurnTowards (línea 629): calcula yaw hacia objetivo y lerpa Quaternion.LookRotation
- TurnBack (línea 648): lerpa a rotación guardada (para lunges de pasivas y ataques)

**Accessor AnchorOf() público (S58):**
- Línea 781: `public Transform AnchorOf(CombatVisualSide side, int index) => units.Get(side, index)?.Anchor;`
- Consumido por CombatPedestalHighlighter para aplicar shine al pedestal del unit activo

## Cambios S59 (Anticipación pasiva, PushHp animate, TimeScale)

**Anticipación de pasivas:**
- Línea 27–28: nuevos knobs `passiveAnticipationSeconds` y `passiveAnticipationPullback`
- Línea 464–473: al animar lunge pasiva, ejecuta pullback anticipatorio
- Propósito: anticipación visual antes de la pasiva (como en DragonAnimationDriver con PlayAttack)

**PushHp con animate:**
- Línea 963: `private void PushHp(CombatVisualSide side, int index, float hp, bool animate = true)`
- Si `animate=true`: llama `unit.Bar?.SetHp(hp, unit.MaxHp)` (anima con juice)
- Si `animate=false`: llama `unit.Bar?.SnapHp(hp, unit.MaxHp)` (inmediato)
- Usado por Restore(): pasa `animate=false` para no mostrar daño/curación falsas al retroceder

**PushTimeScale propaga a animadores:**
- Línea 207–211: propaga Speed a todos los drivers vía SetTimeScale()
- Llamado en SetSpeed() (línea 203) y BeginRoutine() (línea 393)

## Métodos Privados Clave

| Método | Descripción |
|--------|-------------|
| `BuildStates()` | Construye árbol de CombatNode desde CombatRecord.Turns. Precomputa mapa roundOrders. |
| `BeginRoutine()` | Spawns unidades vía CombatVisualUnits, emite OnVisualCombatStart, **S59** llama PushTimeScale() |
| `ForwardRoutine()` | **S61b:** Emite OnPhase(Passives/Attack/Rest, actorSide), espera phasePauseSeconds. Mueve procs post-golpe dentro bloque ataque. **S58:** Anima turno con TurnTowards/TurnBack alrededor lunges, PlayAttack espera onImpact, PlayHit(intensity), CorpseFade en muertes. **S59:** anticipación pasiva, PushHp(animate=true en proc, false en Restore). **S61:** PlayProc emite LogAppend |
| `PlayProc()` | **S61** Anima proc (shield/heal/reacción/estado). Emite OnUnitElement, luego LogAppend (solo reacciones). Usa ReactionLine(pe) sin `who`. |
| `CorpseFade()` | **S58 NEW** Corrutina que espera delay, luego lerp SetGhost(alpha) |
| `TurnTowards()` | **S58 NEW** Lerpa rotación hacia objetivo (yaw) |
| `TurnBack()` | **S58 NEW** Lerpa rotación a valor guardado |
| `PushTimeScale()` | **S59 NEW** Propaga Speed a todos los animadores vía SetTimeScale |
| `ReactionLine(pe)` | **S61 SIMPLIFICADA** Construye línea `"**{Estado}** — {Description}"` (máx 40 chars), coloreada azul/rojo por positivo/negativo. SIN prefijo "{quien}: {ElemA}+{ElemB}" |
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
- [[ElementTableSO]] — DisplayName, UiColor, Description para reacciones; **S61b** ShortDescription
- [[CombatSpeechBubbles]] — OnSpeech

## Notas S61b

- OnPhase emite en ForwardRoutine(): Passives (inicio), Attack (post-pausa), Rest (fin)
- phasePauseSeconds solo tiene efecto si hubo pasivas armadas Y hay ataque próximo
- Procs post-golpe ahora sincronizados: dentro bloque ataque, tras popup daño, antes attackDone
- Cámaras conmutan automáticamente vía CombatCameraDirector (sin lógica manual en Service)

## Notas S61

- ReactionLine ahora sin `who` — contexto ya en speech globo + ReactionName en evento elemental
- Formato compacto: solo estado negrita + descripción truncada (40 chars max)
- Colores: PositiveStateColor azul claro (6FB7FF), NegativeStateColor rojo claro (FF9090)
- LogAppend emite DESPUÉS de UnitElement en PlayProc (sincronización beat)
- Panel log ahora actualiza incrementalmente vía AddCard (eficiente)

## Notas S59

- Anticipación pasiva: pullback hacia atrás antes de lunge (visual feedback)
- PushHp animate control: Restore() snapea (false), proc/hit anima (true)
- TimeScale centralizado: SetSpeed propagates a todos los drivers

## Notas S58

- Animador nuevo: DragonAnimationDriver con callbacks onImpact y onDone
- Muertes son **persistentes** — cadáveres transparentes quedan en tablero
- Facing dinámico: gira hacia objetivo, retorna después
- AnchorOf() público para shine de pedestal
