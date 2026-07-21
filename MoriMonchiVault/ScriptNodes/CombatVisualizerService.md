---
tags: [combat, visualization, replay, ui, 3v3]
---

# CombatVisualizerService

**Ruta:** `Systems/CombatVisualizer/CombatVisualizerService.cs`

**Responsabilidad:** Orquesta visualización local 3v3 de `CombatRecord`, construyendo árbol de nodos doblemente enlazados y generando secuencia de animaciones turno-a-turno. Play() resuelve equipos vía registry. Colaborador `CombatVisualUnits` spawn/lookup. Emite eventos UI (OnActionOrder, OnUnitElement, OnActiveUnit, etc.). **S58:** Animador retipado a `DragonAnimationDriver` (PlayAttack espera onImpact real, PlayHit, PlayDefeat, PlayVictory, PlayIdle, PlayBuff). Muertes persistentes con CorpseFade (MonchiVisualizer.SetGhost alpha). Facing con TurnTowards/TurnBack alrededor lunges. AnchorOf() público nuevo. **S59:** Anticipación de pasivas con knobs passiveAnticipationSeconds/Pullback. PushHp ganó parámetro animate (Restore pasa false → SnapHp). PushTimeScale propaga Speed a los drivers al spawn y en SetSpeed. **S59d:** Líneas de reacción del log via helper `ReactionLine` con formato compacto "{quien}: {ElemA}+{ElemB} → {Estado} — {consecuencia}"; elementos coloreados por UiColor, estado en negrita verde/rojo según set NegativeStates, Description truncada a 70 chars en gris.

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

**Log lines reformateadas (S58):**
- Reacción (línea 289): `string reactionText = ReactionLine(pe, who)` — ahora usa helper dedicado
- Muerte (línea 329, 335): `log.Add(Line(CombatVisualLogKind.Death, $"...", true, CombatVisualSide.A, i))` — emit con HasUnit=true

## Cambios S59 (Anticipación pasiva, PushHp animate, TimeScale)

**Anticipación de pasivas:**
- Línea 27–28: nuevos knobs `passiveAnticipationSeconds` y `passiveAnticipationPullback`
- Línea 464–473: al animar lunge pasiva, ejecuta pullback anticipatorio:
  ```csharp
  var pullDir = units.PosOf(groupSide, groupIndex) - atkHome;
  pullDir.y = 0f;
  var pullbackDir = pullDir.sqrMagnitude > 0.0001f ? -pullDir.normalized : Vector3.zero;
  lungeStart = atkHome + pullbackDir * passiveAnticipationPullback;
  yield return StartCoroutine(MoveOverTime(atkUnit.Instance.transform, atkHome, lungeStart, passiveAnticipationSeconds * 0.6f / Speed));
  yield return new WaitForSeconds(passiveAnticipationSeconds * 0.4f / Speed);
  ```
- Propósito: anticipación visual antes de la pasiva (como en DragonAnimationDriver con PlayAttack)

**PushHp con animate:**
- Línea 963: `private void PushHp(CombatVisualSide side, int index, float hp, bool animate = true)`
- Si `animate=true`: llama `unit.Bar?.SetHp(hp, unit.MaxHp)` (anima con juice)
- Si `animate=false`: llama `unit.Bar?.SnapHp(hp, unit.MaxHp)` (inmediato)
- Usado por Restore(): pasa `animate=false` para no mostrar daño/curación falsas al retroceder
- Línea 722: en PlayProc, siempre pasa `true` (default)

**PushTimeScale propaga a animadores:**
- Línea 207–211: 
  ```csharp
  private void PushTimeScale()
  {
      foreach (var unit in units.Team(CombatVisualSide.A)) unit.Anim?.SetTimeScale(Speed);
      foreach (var unit in units.Team(CombatVisualSide.B)) unit.Anim?.SetTimeScale(Speed);
  }
  ```
- Llamado en SetSpeed() (línea 203) y BeginRoutine() (línea 393) para sincronizar animations con playback speed
- DragonAnimationDriver.SetTimeScale(float) escala Anim.speed y todas las esperas (via Scaled())

## Cambios S59d (Líneas de reacción con formato compacto)

**Helper ReactionLine (S59d NEW):**
- Línea 916–931: `private string ReactionLine(CombatProcEvent pe, string who)`
- Construye formato: `"{who}: {ElemA}+{ElemB} → {Estado} — {Description}"`
- Elementos coloreados via `ElemNameColored()` (hexadecimal UiColor del elementTable)
- Estado parseado como `ElementalState`: negrita, rojo si `NegativeStates.Contains(st)`, verde si positivo
- Description truncada a 70 chars max, coloreada gris (DescriptionColor #B8B8B8)

**Set NegativeStates (S59d NEW):**
- Línea 896–900: `private static readonly HashSet<ElementalState> NegativeStates`
- Contiene: Boiling, Debilidad, Confuso, Leech, Mareado, PisoTierra
- Todos otros estados parseados se consideran positivos (verde)

**Helpers de coloreo (S59d):**
- Línea 906–911: `private string ElemNameColored(Element e)` — retorna nombre elemento con color hexadecimal de UiColor
- Línea 890–891: `private string ElemName(Element e)` — DisplayName de elementTable
- Línea 893–894: `private string StateName(ElementalState s)` — DisplayName parseado
- Línea 913–914: `private static string Truncate(string text, int maxLength)` — trunca con "…"

**Colores definidos (S59d):**
- `PositiveStateColor = "86E3A0"` — verde claro
- `NegativeStateColor = "FF9090"` — rojo claro
- `DescriptionColor = "B8B8B8"` — gris neutro

**Llamada en BuildStates():**
- Línea 288–290: cuando `pe.ElementEvent == ElementEventKind.Reaction`:
  ```csharp
  string reactionText = ReactionLine(pe, who);
  log.Add(Line(CombatVisualLogKind.Proc, reactionText, true, unitSide, pe.TargetIndex));
  ```
- Emite log con HasUnit=true para que CombatVisualizerPanelUITK lo renderice

**Ejemplo de log S59d:**
```
Juan: Fuego+Hielo → Debilidad — Estado que reduce defensa en un 30%
```

## Métodos Privados Clave

| Método | Descripción |
|--------|-------------|
| `BuildStates()` | Construye árbol de CombatNode desde CombatRecord.Turns. Precomputa mapa roundOrders. **S59d:** usa ReactionLine() para reacciones elementales. |
| `BeginRoutine()` | Spawns unidades vía CombatVisualUnits, emite OnVisualCombatStart, **S59** llama PushTimeScale() |
| `ForwardRoutine()` | **S58:** Anima turno con TurnTowards/TurnBack alrededor lunges, PlayAttack espera onImpact, PlayHit(intensity), CorpseFade en muertes, AnchorOf para shine. **S59:** anticipación pasiva, PushHp(animate=true en proc, false en Restore) |
| `PlayProc()` | Anima proc (shield/heal/reacción/estado). Emite OnUnitElement con ReactionName. |
| `CorpseFade()` | **S58 NEW** Corrutina que espera delay, luego lerp SetGhost(alpha) |
| `TurnTowards()` | **S58 NEW** Lerpa rotación hacia objetivo (yaw) |
| `TurnBack()` | **S58 NEW** Lerpa rotación a valor guardado |
| `PushTimeScale()` | **S59 NEW** Propaga Speed a todos los animadores vía SetTimeScale |
| `ReactionLine(pe, who)` | **S59d NEW** Construye línea de reacción "{who}: {ElemA}+{ElemB} → {Estado} — {Description}" con colores y formato compacto |
| `ElemNameColored(e)` | **S59d NEW** Retorna nombre elemento coloreado por UiColor hexadecimal |
| `Restore()` | Vuelve a estado de un nodo. SetGhost(1) para cadáveres si no está muerto ahora. **S59:** PushHp pasa animate=false |
| `Publish()` | Emite OnPanelState |
| `PublishOrder()` | Emite OnActionOrder |
| `SetActiveFrames(side, index)` | Setea marco dorado al unit (début de turno) — probablemente deprecated con CombatPedestalHighlighter S58 |

## Consumo de eventos UI (S58–S59–S59d)

- `OnActiveUnit` → CombatPedestalHighlighter.HandleActiveUnit() llama AnchorOf() para shine
- `OnHit/OnCrit/OnPopup` → CombatDamageNumbers renderiza popups
- `OnUnitHpChanged` → CombatRadialHealthBar.SetHp() (antes: barras legacy)
- `OnUnitDead` → registra log (antes: animaba barra)
- `OnUnitElement` → CombatOrderBarUITK.HandleUnitElement() dibuja chips (ReactionName parseado a ElementalState)
- `OnActionOrder` → CombatOrderBarUITK.HandleOrder() reordena cartas
- `OnPanelState` → CombatVisualizerPanelUITK.HandleState() renderiza log filtrado (HasUnit=true || Kind=Result)
- `OnVisualCombatEnd` → CombatPedestalHighlighter.HandleVisualCombatEnd() limpia shine, UI muestra "Final"

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CombatRecord]] — fuente de datos
- [[CombatVisualEvents]] — publisher de eventos
- [[CombatVisualUnits]] — spawn/lookup units
- [[CombatPedestalHighlighter]] — **S58 NEW** shine pedestal (AnchorOf + shine materials)
- [[CombatRadialHealthBar]] — **S58** barras radiales world-space; **S59** PushHp(animate) control; **S59d** siempre visible
- [[MonchiAnimationDriver]] — **S58** animar ataques/hits/defeat/victory/buff; **S59** SetTimeScale
- [[DragonAnimationDriver]] — **S59** implementa SetTimeScale(float), escala Anim.speed y Scaled()
- [[MonchiVisualizer]] — **S58** SetGhost(alpha) para fade persistente
- [[CombatOrderBarUITK]] — OnActionOrder, OnUnitElement, OnActiveUnit; **S59** emite OnUnitHover
- [[CombatVisualizerPanelUITK]] — OnPanelState, log filtrado; **S59d** renderiza ReactionLine con colores
- [[CombatDamageNumbers]] — OnPopup
- [[CombatCameraDirector]] — OnActiveUnit, VCamOf
- [[ElementTableSO]] — DisplayName, UiColor, Description para reacciones

## Notas S58–S59–S59d

- Animador nuevo: DragonAnimationDriver con callbacks onImpact y onDone (espera real en lugar de duration fija)
- Muertes son **persistentes** — cadáveres transparentes quedan en tablero, no desaparecen
- Facing dinámico: gira hacia objetivo en lunge, retorna después
- AnchorOf() público permite que efectos de pedestal (shine) accedan al modelo raíz
- Log lines de reacción/muerte llevan HasUnit=true para filtrado en "Eventos" UI
- **S59:** Anticipación pasiva: pullback hacia atrás antes de lunge (visual feedback)
- **S59:** PushHp animate control: Restore() snapea (false), proc/hit anima (true)
- **S59:** TimeScale centralizado: SetSpeed propagates a todos los drivers (animaciones en sync)
- **S59d:** Reacciones elementales tienen formato compacto, legible, coloreado (elemento + estado + descripción). Facilita feedback visual del combo elemental.
