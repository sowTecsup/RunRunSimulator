---
tags: [combat, visualization, replay, ui]
---

# CombatVisualizerService

**Ruta:** `Systems/CombatVisualizer/CombatVisualizerService.cs`

**Responsabilidad:** Orquesta visualización local de `CombatRecord`, construyendo árbol de nodos doblemente enlazados y generando secuencia de animaciones turno-a-turno. Playback manual (fwd/back) y automático, mapea datos sim → visual (HP, muerte, popups, estado de efectos). **S32:** Aplica `EquipmentStats` a stats de visualización. **S34:** Rastrea StatusA/StatusB y pushea a UI.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Play(CreatureDNA self, CreatureDNA opponent, CombatRecord record)` | Inicia replay de un record |
| `Stop()` | Detiene playback y limpia estado |
| `TogglePlay()` | Toggle automático |
| `Next()` | Avanza un turno (manual) |
| `Back()` | Retrocede un turno (manual) |
| `SetSpeed(float value)` | Setea velocidad de playback |

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

**Procesamiento Turns (S34):**
- Procesa `Turn.Procs`, aplicando before-strike y after-strike
- Calcula HP acumulado tras cada turno
- **Rastrea StatusA/StatusB** mapeado según `SelfWasA` (A=self o opponent según perspectiva)
- Rastrea muertes (`ADiedHere`, `BDiedHere`)

```csharp
StatusA = (activeRecord.SelfWasA ? t.StatusA : t.StatusB) ?? new List<CombatStatusMark>(),
StatusB = (activeRecord.SelfWasA ? t.StatusB : t.StatusA) ?? new List<CombatStatusMark>(),
```

## Animación de Turnos

**ForwardRoutine()** anima un turno:
1. Procs before-strike
2. Golpe (si !NoAttack): windup → impact
3. Procs after-strike
4. **PushStatus(target)** — actualiza barras de estado ← S34
5. Muerte (si ADiedHere/BDiedHere)

**PlayProc()** anima un proc individual: popup + HP delta + wait.

## Mapeo Sim → Visual

- `SimToVisual(bool simIsA)` — convierte `simIsA` en `CombatVisualSide`
- `FighterPos(CombatVisualSide side)` — posición para popups
- **`FighterTransform(CombatVisualSide side)`** ← **S34** Retorna Transform del luchador para que popups lo sigan
- `ProcPopupKind(ModifierEffectKind)` — mapea tipo a popup visual
- `RaiseProcPopup()` — dispara evento popup

## RaiseProcPopup (S32 + S34)

```csharp
private void RaiseProcPopup(CombatProcEvent pe, CombatVisualSide side, float delta)
{
    // Stun: solo texto
    if (pe.Kind == ModifierEffectKind.Stun)
    {
        CombatVisualEvents.Popup(new CombatVisualPopup
        {
            Side = side, Position = FighterPos(side), Follow = FighterTransform(side),  // S34
            Kind = CombatPopupKind.Stun, Amount = pe.Amount,
        });
        return;
    }
    // Synergy: solo texto si delta < 0.5
    if (pe.Kind == ModifierEffectKind.Synergy && Mathf.Abs(delta) < 0.5f)
    {
        CombatVisualEvents.Popup(new CombatVisualPopup
        {
            Side = side, Position = FighterPos(side), Follow = FighterTransform(side),  // S34
            Kind = CombatPopupKind.Synergy, Amount = 0f,
        });
        return;
    }
    // Otros: ignorar si delta muy pequeño
    if (Mathf.Abs(delta) < 0.5f) return;
    // Mapear y disparar con número
    CombatVisualEvents.Popup(new CombatVisualPopup
    {
        Side = side, Position = FighterPos(side), Follow = FighterTransform(side),  // S34
        Kind = ProcPopupKind(pe.Kind), Amount = Mathf.Abs(delta),
    });
}
```

**S34:** Todos los Popup raises setean `Follow = FighterTransform(side)` para que el número siga al combatiente.

## ProcPopupKind Mapeo

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
- `List<CombatStatusMark> StatusA, StatusB` — **S34** Estado de efectos activos tras este turno
- `CombatNode Prev, Next` — enlaces
- `List<CombatVisualLogLine> Log` — líneas de log acumuladas

**Métodos:**
- `FireWindup()`, `FireImpact()` — animan golpe (si !NoAttack)
- `FireDeath()` — anima muerte

## FighterTransform Helper — S34

```csharp
private Transform FighterTransform(CombatVisualSide side)
{
    var inst = side == CombatVisualSide.A ? instanceA : instanceB;
    if (inst != null) return inst.transform;
    return side == CombatVisualSide.A ? slotA : slotB;
}
```

Retorna el Transform del luchador (visual o slot como fallback). Usado por popups en `RaiseProcPopup()` para seguimiento dinámico.

## PushStatus Method — S34

```csharp
private void PushStatus(CombatNode node)
{
    barA?.SetStatus(node.StatusA);
    barB?.SetStatus(node.StatusB);
}
```

Nuevamente llamado en `ForwardRoutine()` tras `current = target` para actualizar las barras de estado. También en `Restore()` para sincronizar en saltos de turno (back/forward).

## Cambios S32

**EquipmentStats.Apply():** BuildStates ahora aplica mods de equipo a los stats calculados. Las barras HP y stats mostrados (ATK, SPD) reflejan bonificadores de equipment, paridad total con la simulación.

**RaiseProcPopup():** Caso nuevo para `ModifierEffectKind.Synergy`: si delta HP < 0.5, dispara popup textual ("¡Sinergia!") sin número, análogo a Stun.

**ProcPopupKind():** Mapeo agregado `Synergy → CombatPopupKind.Synergy`.

## Cambios S34

**CombatNode.StatusA/StatusB:** Almacena estado de efectos activos de ambos luchadores tras cada turno, mapeado según `SelfWasA` (A=self, B=opponent).

**FighterTransform(side):** Nuevo helper que retorna Transform del luchador para que popups lo sigan.

**PushStatus(node):** Nuevo método que pushea StatusA/StatusB a las barras via `SetStatus()`. Llamado en ForwardRoutine tras avanzar a nuevo turno, y en Restore() para sincronizar en saltos.

**RaiseProcPopup():** Todos los Popup raises setean `Follow = FighterTransform(side)` para que números sigan al combatiente dinámicamente.

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatRecord]] — lee Turns + StatusA/StatusB
- [[CombatStats]] — calcula stats base
- [[EquipmentStats]] — aplica mods de equipment (S32)
- [[EffectiveStats]] — struct stats
- [[CombatService]] — parámetros de sim, genera StatusMarks
- [[CombatVisualEvents]] — publisher de eventos
- [[CombatStatusMark]] — estado de efectos (S34)
- [[MoriMonchiVisualizer]] — prefab instanciado
- [[MoriMonchiCombatVisualizerUITK]] — barra de efectos, SetStatus() (S34)

## Conexiones

**Entrada:**
- `Play(self, opponent, record)` — llamado desde UI/test

**Salida:**
- `CombatVisualEvents.On{Start,TurnStart,Hit,Popup,Dead}` — eventos visuals
- `MoriMonchiCombatVisualizerUITK.SetStatus(List<CombatStatusMark>)` — actualiza chips de estado (S34)

## Notas

- **Árbol nodos:** Doubly linked list permite jump fwd/back eficiente.
- **HP tracking:** `shownHpA/B` para delta en proc text.
- **NoAttack:** Turnos sin golpe saltan windup/impact.
- **S32:** Refactor extrajo `CombatStats` y agregó `EquipmentStats.Apply()` para paridad visual.
- **S34:** Rastrea StatusA/StatusB en nodos; FighterTransform para popups dinámicos; PushStatus sincroniza UI de efectos tras cada turno.
- **Null-tolerance:** StatusA/StatusB nulls deserializan como listas vacías; BuildStates crea listas vacías si no están presentes.
