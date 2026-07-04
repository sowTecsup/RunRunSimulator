---
tags: [combat, visualization, replay, ui]
---

# CombatVisualizerService

**Ruta:** `Systems/CombatVisualizer/CombatVisualizerService.cs`

**Responsabilidad:** Orquesta visualización local de `CombatRecord`, construyendo árbol de nodos doblemente enlazados y generando secuencia de animaciones turno-a-turno. Playback manual (fwd/back) y automático, mapea datos sim → visual (HP, muerte, popups, estado de efectos). Aplica `EquipmentStats` a stats de visualización. Rastrea StatusA/StatusB y pushea a UI. **S35:** Incorpora delay pre-popup de sinergia (`synergyPopupDelay`) y pushea `TargetStatusAfter` a barra por proc.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Play(CreatureDNA self, CreatureDNA opponent, CombatRecord record)` | Inicia replay de un record |
| `Stop()` | Detiene playback y limpia estado |
| `TogglePlay()` | Toggle automático |
| `Next()` | Avanza un turno (manual) |
| `Back()` | Retrocede un turno (manual) |
| `SetSpeed(float value)` | Setea velocidad de playback |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `windupSeconds` | `float` | Duración del windup de ataque (dividido por Speed) |
| `impactSeconds` | `float` | Duración del impacto post-golpe (dividido por Speed) |
| `betweenTurnsSeconds` | `float` | Pausa entre turnos |
| `deathPauseSeconds` | `float` | Pausa al ocurrir muerte |
| `synergyPopupDelay` | `float` | **(S35)** Delay previo a popup de sinergia (default 0.6s, dividido por Speed) |
| `playbackSpeed` | `float` | Multiplicador de velocidad (0.25–4) |

## Construcción de Estados

**BuildStates()** construye árbol `CombatNode` desde `CombatRecord.Turns`:

**Stats CON Equipment (S32):**
```csharp
statsA = EquipmentStats.Apply(CombatStats.GetEffectiveStats(selfDna, Db), selfDna, EquipDb);
statsB = EquipmentStats.Apply(CombatStats.GetEffectiveStats(oppDna, Db), oppDna, EquipDb);
hpMaxA = statsA.Constitution * CombatStats.BaseHpCombatMultiplier;
hpMaxB = statsB.Constitution * CombatStats.BaseHpCombatMultiplier;
```

Las barras de HP y los stats mostrados (ATK, VEL) incluyen bonificaciones del equipo, paridad con la simulación.

**Procesamiento Turns:**
- Procesa `Turn.Procs`, aplicando before-strike y after-strike
- Calcula HP acumulado tras cada turno
- Rastrea StatusA/StatusB mapeado según `SelfWasA` (A=self o opponent según perspectiva)
- Rastrea muertes (`ADiedHere`, `BDiedHere`)

## Animación de Turnos

**ForwardRoutine()** anima un turno:
1. Procs before-strike
2. Golpe (si !NoAttack): windup → impact
3. Procs after-strike
4. **PushStatus(target)** — actualiza barras de estado
5. Muerte (si ADiedHere/BDiedHere)

**PlayProc()** anima un proc individual:

```csharp
private IEnumerator PlayProc(CombatProcEvent pe)
{
    var side = SimToVisual(pe.TargetIsA);
    float before = side == CombatVisualSide.A ? shownHpA : shownHpB;
    float max    = side == CombatVisualSide.A ? hpMaxA : hpMaxB;
    if (pe.Kind == ModifierEffectKind.Synergy)
        yield return new WaitForSeconds(synergyPopupDelay / Speed);  // S35: delay pre-popup
    RaiseProcPopup(pe, side, pe.TargetHpAfter - before);
    PushHp(side, pe.TargetHpAfter, max);
    if (pe.TargetStatusAfter != null) PushStatusSide(side, pe.TargetStatusAfter);  // S35: sincroniza UI
    yield return new WaitForSeconds(impactSeconds / Speed);
}
```

**Cambios S35:**
- Si es Synergy, espera `synergyPopupDelay / Speed` antes de levantar el popup (efecto dramático)
- Después de popup, pushea `TargetStatusAfter` a la barra del luchador afectado vía `PushStatusSide()`

## Mapeo Sim → Visual

- `SimToVisual(bool simIsA)` — convierte `simIsA` en `CombatVisualSide`
- `FighterPos(CombatVisualSide side)` — posición para popups
- `FighterTransform(CombatVisualSide side)` — retorna Transform del luchador para que popups lo sigan
- `ProcPopupKind(ModifierEffectKind)` — mapea tipo a popup visual
- `RaiseProcPopup()` — dispara evento popup

## RaiseProcPopup (S32 + S34 + S35)

```csharp
private void RaiseProcPopup(CombatProcEvent pe, CombatVisualSide side, float delta)
{
    // Stun: solo texto
    if (pe.Kind == ModifierEffectKind.Stun)
    {
        CombatVisualEvents.Popup(new CombatVisualPopup
        {
            Side = side, Position = FighterPos(side), Follow = FighterTransform(side),
            Kind = CombatPopupKind.Stun, Amount = pe.Amount,
        });
        return;
    }
    // Synergy: solo texto si delta < 0.5
    if (pe.Kind == ModifierEffectKind.Synergy && Mathf.Abs(delta) < 0.5f)
    {
        CombatVisualEvents.Popup(new CombatVisualPopup
        {
            Side = side, Position = FighterPos(side), Follow = FighterTransform(side),
            Kind = CombatPopupKind.Synergy, Amount = 0f,
        });
        return;
    }
    // Otros: ignorar si delta muy pequeño
    if (Mathf.Abs(delta) < 0.5f) return;
    // Mapear y disparar con número
    CombatVisualEvents.Popup(new CombatVisualPopup
    {
        Side = side, Position = FighterPos(side), Follow = FighterTransform(side),
        Kind = ProcPopupKind(pe.Kind), Amount = Mathf.Abs(delta),
    });
}
```

Todos los Popup raises setean `Follow = FighterTransform(side)` para que el número siga al combatiente.

## ProcPopupKind Mapeo — S35

```csharp
private static CombatPopupKind ProcPopupKind(ModifierEffectKind k) => k switch
{
    ModifierEffectKind.Poison       => CombatPopupKind.Poison,
    ModifierEffectKind.Burn         => CombatPopupKind.Burn,
    ModifierEffectKind.ReturnDamage => CombatPopupKind.Thorns,
    ModifierEffectKind.Heal         => CombatPopupKind.Heal,
    ModifierEffectKind.Regen        => CombatPopupKind.Regen,
    ModifierEffectKind.Stun         => CombatPopupKind.Stun,
    ModifierEffectKind.Synergy      => CombatPopupKind.Synergy,
    ModifierEffectKind.Static       => CombatPopupKind.Static,       // S35
    ModifierEffectKind.Pulse        => CombatPopupKind.Pulse,        // S35
    ModifierEffectKind.Steel        => CombatPopupKind.Steel,        // S35
    ModifierEffectKind.Mist         => CombatPopupKind.Mist,         // S35
    ModifierEffectKind.Lifesteal    => CombatPopupKind.Lifesteal,    // S35
    _                               => CombatPopupKind.Hit,
};
```

## Clases Internas

### CombatNode (nodo de árbol replay)

**Campos:**
- `bool HasTurn` — si representa turno real
- `CombatTurn Turn` — el turno (null si !HasTurn)
- `float HpA, HpB` — HP acumulado
- `bool ADead, BDead` — estado actual
- `bool ADiedHere, BDiedHere` — murió EN este turno
- `int TurnNumber` — número de turno
- `List<CombatStatusMark> StatusA, StatusB` — Estado de efectos activos tras este turno
- `CombatNode Prev, Next` — enlaces
- `List<CombatVisualLogLine> Log` — líneas de log acumuladas

**Métodos:**
- `FireWindup()`, `FireImpact()` — animan golpe (si !NoAttack)
- `FireDeath()` — anima muerte

## FighterTransform Helper

```csharp
private Transform FighterTransform(CombatVisualSide side)
{
    var inst = side == CombatVisualSide.A ? instanceA : instanceB;
    if (inst != null) return inst.transform;
    return side == CombatVisualSide.A ? slotA : slotB;
}
```

Retorna el Transform del luchador (visual o slot como fallback). Usado por popups en `RaiseProcPopup()` para seguimiento dinámico.

## PushStatus Method

```csharp
private void PushStatus(CombatNode node)
{
    barA?.SetStatus(node.StatusA);
    barB?.SetStatus(node.StatusB);
}
```

Llamado en `ForwardRoutine()` tras `current = target` para actualizar las barras de estado. También en `Restore()` para sincronizar en saltos de turno (back/forward).

## PushStatusSide Helper — S35

```csharp
private void PushStatusSide(CombatVisualSide side, List<CombatStatusMark> marks)
{
    if (side == CombatVisualSide.A) barA?.SetStatus(marks);
    else barB?.SetStatus(marks);
}
```

Pushea los status marks a la barra correspondiente (A o B). Se llama en `PlayProc()` cuando `pe.TargetStatusAfter` no es null (S35).

## Cambios S32

**EquipmentStats.Apply():** BuildStates ahora aplica mods de equipo a los stats calculados. Las barras HP y stats mostrados (ATK, SPD) reflejan bonificadores de equipment, paridad total con la simulación.

**RaiseProcPopup():** Caso nuevo para `ModifierEffectKind.Synergy`: si delta HP < 0.5, dispara popup textual ("¡Sinergia!") sin número, análogo a Stun.

**ProcPopupKind():** Mapeo agregado `Synergy → CombatPopupKind.Synergy`.

## Cambios S34

**CombatNode.StatusA/StatusB:** Almacena estado de efectos activos de ambos luchadores tras cada turno, mapeado según `SelfWasA` (A=self, B=opponent).

**FighterTransform(side):** Nuevo helper que retorna Transform del luchador para que popups lo sigan.

**PushStatus(node):** Nuevo método que pushea StatusA/StatusB a las barras via `SetStatus()`. Llamado en ForwardRoutine tras avanzar a nuevo turno, y en Restore() para sincronizar en saltos.

**RaiseProcPopup():** Todos los Popup raises setean `Follow = FighterTransform(side)` para que números sigan al combatiente dinámicamente.

## Cambios S35

**synergyPopupDelay:** Nuevo campo serializado (default 0.6s). En `PlayProc()`, si proc es Synergy, espera este delay antes de levantar el popup. Efecto: las sinergias se ven "diferidas" dramáticamente.

**ProcPopupKind():** 5 mapeos nuevos (Static, Pulse, Steel, Mist, Lifesteal).

**PushStatusSide():** Nuevo helper que sincroniza `TargetStatusAfter` a la barra por proc. En PlayProc, tras RaiseProcPopup, si `pe.TargetStatusAfter != null`, pushea a barra para que refleje estado post-proc en tiempo real.

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatRecord]] — lee Turns + StatusA/StatusB
- [[CombatStats]] — calcula stats base
- [[EquipmentStats]] — aplica mods de equipment
- [[EffectiveStats]] — struct stats
- [[CombatService]] — parámetros de sim, genera StatusMarks
- [[CombatVisualEvents]] — publisher de eventos
- [[CombatStatusMark]] — estado de efectos
- [[MoriMonchiVisualizer]] — prefab instanciado
- [[MoriMonchiCombatVisualizerUITK]] — barra de efectos, SetStatus()

## Conexiones

**Entrada:**
- `Play(self, opponent, record)` — llamado desde UI/test

**Salida:**
- `CombatVisualEvents.On{Start,TurnStart,Hit,Popup,Dead}` — eventos visuals
- `MoriMonchiCombatVisualizerUITK.SetStatus(List<CombatStatusMark>)` — actualiza chips de estado
- `CombatDamageNumbers.HandlePopup()` — anima números flotantes

## Notas

- **Árbol nodos:** Doubly linked list permite jump fwd/back eficiente.
- **HP tracking:** `shownHpA/B` para delta en proc text.
- **NoAttack:** Turnos sin golpe saltan windup/impact.
- **S32:** Refactor extrajo `CombatStats` y agregó `EquipmentStats.Apply()` para paridad visual.
- **S34:** Rastrea StatusA/StatusB en nodos; FighterTransform para popups dinámicos; PushStatus sincroniza UI de efectos tras cada turno.
- **S35:** Delay pre-popup de sinergia + PushStatusSide sincroniza `TargetStatusAfter` a barra por proc en tiempo real (no solo fin de turno).
- **Null-tolerance:** StatusA/StatusB nulls deserializan como listas vacías; BuildStates crea listas vacías si no están presentes.
