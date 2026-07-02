---
tags: [combat, visualization, replay, ui]
---

# CombatVisualizerService

**Ruta:** `Systems/CombatVisualizer/CombatVisualizerService.cs`

**Responsabilidad:** Orquesta visualización local de `CombatRecord`, construyendo árbol de nodos doblemente enlazados y generando secuencia de animaciones turno-a-turno. Playback manual (fwd/back) y automático, mapea datos sim → visual (HP, muerte, popups). **S32:** Aplica `EquipmentStats` a stats de visualización para que barras y ATK/VEL reflejen equipo.

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

**Procesamiento Turns:**
- Procesa `Turn.Procs`, aplicando before-strike y after-strike
- Calcula HP acumulado tras cada turno
- Rastrea muertes (`ADiedHere`, `BDiedHere`)

## Animación de Turnos

**ForwardRoutine()** anima un turno:
1. Procs before-strike
2. Golpe (si !NoAttack): windup → impact
3. Procs after-strike
4. Muerte (si ADiedHere/BDiedHere)

**PlayProc()** anima un proc individual: popup + HP delta + wait.

## Mapeo Sim → Visual

- `SimToVisual(bool simIsA)` — convierte `simIsA` en `CombatVisualSide`
- `FighterPos(CombatVisualSide side)` — posición para popups
- `ProcPopupKind(ModifierEffectKind)` — mapea tipo a popup visual
- `RaiseProcPopup()` — dispara evento popup

## RaiseProcPopup (S32)

```csharp
private void RaiseProcPopup(CombatProcEvent pe, CombatVisualSide side, float delta)
{
    // Stun: solo texto
    if (pe.Kind == ModifierEffectKind.Stun)
    {
        CombatVisualEvents.Popup(new CombatVisualPopup
        {
            Side = side, Position = FighterPos(side),
            Kind = CombatPopupKind.Stun, Amount = pe.Amount,
        });
        return;
    }
    // Synergy: solo texto si delta < 0.5
    if (pe.Kind == ModifierEffectKind.Synergy && Mathf.Abs(delta) < 0.5f)
    {
        CombatVisualEvents.Popup(new CombatVisualPopup
        {
            Side = side, Position = FighterPos(side),
            Kind = CombatPopupKind.Synergy, Amount = 0f,
        });
        return;
    }
    // Otros: ignorar si delta muy pequeño
    if (Mathf.Abs(delta) < 0.5f) return;
    // Mapear y disparar con número
    CombatVisualEvents.Popup(new CombatVisualPopup
    {
        Side = side, Position = FighterPos(side),
        Kind = ProcPopupKind(pe.Kind), Amount = Mathf.Abs(delta),
    });
}
```

**Lógica S32:** Si `Kind == Synergy` y delta HP < 0.5, dispara popup textual sin número (paridad con efectos Stun). Mapeo en `ProcPopupKind()` incluye `Synergy → CombatPopupKind.Synergy`.

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
    ModifierEffectKind.Synergy      => CombatPopupKind.Synergy,  // S32
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
- `CombatNode Prev, Next` — enlaces

**Métodos:**
- `FireWindup()`, `FireImpact()` — animan golpe (si !NoAttack)
- `FireDeath()` — anima muerte

## Cambios S32

**EquipmentStats.Apply():** BuildStates ahora aplica mods de equipo a los stats calculados. Las barras HP y stats mostrados (ATK, SPD) reflejan bonificadores de equipment, paridad total con la simulación.

**RaiseProcPopup():** Caso nuevo para `ModifierEffectKind.Synergy`: si delta HP < 0.5, dispara popup textual ("¡Sinergia!") sin número, análogo a Stun.

**ProcPopupKind():** Mapeo agregado `Synergy → CombatPopupKind.Synergy`.

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatRecord]] — lee Turns + Procs
- [[CombatStats]] — calcula stats base
- [[EquipmentStats]] — aplica mods de equipment (S32)
- [[EffectiveStats]] — struct stats
- [[CombatService]] — parámetros de sim
- [[CombatVisualEvents]] — publisher de eventos
- [[MoriMonchiVisualizer]] — prefab instanciado

## Conexiones

**Entrada:**
- `Play(self, opponent, record)` — llamado desde UI/test

**Salida:**
- `CombatVisualEvents.On{Start,TurnStart,Hit,Popup,Dead}` — eventos visuals

## Notas

- **Árbol nodos:** Doubly linked list permite jump fwd/back eficiente.
- **HP tracking:** `shownHpA/B` para delta en proc text.
- **NoAttack:** Turnos sin golpe saltan windup/impact.
- **S32:** Refactor extrajo `CombatStats` y agregó `EquipmentStats.Apply()` para paridad visual. RaiseProcPopup maneja Synergy textual.
