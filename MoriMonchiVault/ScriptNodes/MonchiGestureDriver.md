---
tags: [script, creatures, animation, realismo]
---

# MonchiGestureDriver.cs

**Ruta:** `World/Creatures/MonchiGestureDriver.cs`

**Responsabilidad:** Orquestador de gestos (animaciones discretas) basado en intención y condición. Maneja transiciones suave de gestos (enter → hold), fidgets periodicos cuando está idle, y override por enfermedad. Consulta `MonchiGestureSetSO` para mapping Intención → Gesto. **S100 NUEVO:** Lee `agent.ClashGesture` y lo dispara cuando cambia, permitiendo que [[AgentClash]] injecte gestos de aviso/golpe (TellGesture/StrikeGesture).

## Propiedades

**Serializado (Inspector):**
- `agent` (MoriMochiAgent) — referencia requerida; consume `agent.Intent`, `agent.DNA.Boldness`, **S100:** `agent.ClashGesture`
- `locomotion` (MonchiLocomotionAnimator) — referencia requerida; orquestación de animaciones (PlayGesture, HoldGesture, StopGesture, IsGesturing, IsStill)
- `set` (MonchiGestureSetSO) — referencia requerida; SO de mapping intención → gesto
- `combatDriver` (DragonAnimationDriver) — opcional; si existe y `IsBusy`, descarta gestos (combate tiene su propia anim)

**Internos:**
- `lastIntent` (CreatureIntent) — intención en el frame anterior; detector de cambio
- `currentHold` (string) — gesto de mantención actualmente activo
- `pendingEnter` (string) — gesto de entrada esperando espacio de animación
- `lastClashGesture` (string, S100 NUEVO) — gesto de clash en el frame anterior; detector de cambio
- `nextFidget` (float) — timestamp del próximo fidget (Time.time)

## Ciclo de Vida

**OnEnable:**
```csharp
nextFidget = Time.time + Random.Range(0, set.FidgetInterval.x)
lastIntent = agent.Intent
currentHold = ""
lastClashGesture = ""  // S100 NUEVO
```

**Update:**
```
1. Si combatDriver.IsBusy: descarta gestos, retorna (combate maneja anim)

2. Si IsHeld || IsAirborne || IsRecovering:
   Detener gesto, limpiar pendingEnter/currentHold, retorna

3. Detectar cambio de intención:
   intent = agent.Intent
   if (intent != lastIntent):
     pendingEnter = set.TryEnterGesture(intent, out enterState) ? enterState : ""
     lastIntent = intent

4. S100 NUEVO: Detectar cambio de gesto de clash:
   clashGesture = agent.ClashGesture ?? ""
   if (clashGesture != lastClashGesture):
     lastClashGesture = clashGesture
     if (clashGesture != ""):
       pendingEnter = clashGesture  // prioridad: clash gesto > intent gesto

5. Ejecutar gesto de entrada:
   if (pendingEnter != "" && locomotion.PlayGesture(pendingEnter)):
     pendingEnter = "" (consumido)

6. Determinar gesto de mantención deseado:
   desiredHold = (CreatureCondition == Sick) ? set.SickGesture : (set.TryHoldGesture(intent) ? state : "")

7. Si locomotion deja de gesticular, limpiar currentHold:
   if (currentHold != "" && !locomotion.IsGesturing):
     currentHold = ""

8. Transitar a nuevo hold si cambió:
   if (desiredHold != currentHold):
     if (string.IsNullOrEmpty(desiredHold)):
       locomotion.StopGesture()
       currentHold = ""
     else if (locomotion.HoldGesture(desiredHold)):
       currentHold = desiredHold

9. Fidgets periódicos (solo Idle/Wandering, quieto):
   if (Time.time >= nextFidget
       && (intent == Idle || Wandering)
       && locomotion.IsStill && !IsGesturing && currentHold == ""):
     nextFidget = Time.time + Random.Range(set.FidgetInterval.x, set.FidgetInterval.y)
     boldness = agent.DNA != null ? agent.DNA.Boldness : 0.5f
     fidget = set.PickFidget(boldness)
     if (fidget != null):
       locomotion.PlayGesture(fidget)
```

## Cambios S100: Clash Gestures

**Líneas 46-51 (nuevas en Update):**
```csharp
string clashGesture = agent.ClashGesture ?? "";
if (clashGesture != lastClashGesture)
{
    lastClashGesture = clashGesture;
    if (clashGesture != "") pendingEnter = clashGesture;
}
```

**Razón:** Permite que [[AgentClash]] injecte gestos visuales sin modificar Intent. Durante Anticipating, AgentClash expone `move.TellGesture` (p.ej. "Roar"); MonchiGestureDriver lo lee y lo dispara. Durante Striking, expone `move.StrikeGesture` (p.ej. ""); al volver a Resolving, retorna "" y detiene gesto.

**Prioridad:** ClashGesture > Intent gesture. Si hay gesto de clash pendiente, lo dispara aunque el Intent no haya cambiado. Permite que animaciones de combate ocurran **dentro del mismo estado de Intent** (ej. Clashing intent con gesto Roar → gesto Strike → back to Roar).

**Ejemplo de flujo:**
1. Agent en Idle, ClashGesture = ""
2. TryEngage, Begin → Anticipating, ClashGesture = "Roar"
3. MonchiGestureDriver detecta cambio, dispara "Roar"
4. Anticipating timer cuenta down, StartStrike → Striking, ClashGesture = ""
5. Detección de cambio "Roar" → "", pendingEnter queda "", gesto se detiene
6. Impact, Resolve, Finish → Clashing intent termina, vuelve a Intent anterior

## Invariantes S100 + S98

- **Prioridad enter > hold:** gesto de entrada ocupa espacio de animación (breve, trigger); hold es loop continuo. Transición suave: enter se consume, luego hold toma control.
- **Clash override:** ClashGesture es inyección directa desde [[AgentClash]]; no es parte de MonchiGestureSetSO. Permite gestos custom del combate sin hardcodear en SO.
- **Sick override:** `CreatureCondition.Sick` fuerza `SickGesture` sin respetar intención. Toma prioridad máxima (aún mayor que ClashGesture).
- **Fidgets sociales:** solo durante Idle/Wandering, cuando creature no está haciendo nada. Ponderados por Boldness (bravos hacen gestos más agresivos).
- **Combate aislado:** si `DragonAnimationDriver.IsBusy`, descarta toda lógica de gestos (combate antiguo maneja su propia anim). **S100:** AgentClash NO interfiere (su estado es Clashing, no legacy DragonRpsBrain).
- **Held/Airborne/Recovering:** descarta gestos cuando creature está siendo pettead, cargado, o saltando (transiciones autom. con OnDisable).

## Conexiones

- [[MoriMochiAgent]] (**S100:** lee ClashGesture)
- [[MonchiLocomotionAnimator]]
- [[MonchiGestureSetSO]]
- [[CreatureIntent]]
- [[CreatureCondition]]
- [[CreatureDNA]]
- [[DragonAnimationDriver]]
- **S100:** [[AgentClash]] (fuente de ClashGesture)
- **S100:** [[ClashMoveSO]] (contiene TellGesture/StrikeGesture)

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]] (Parte 8: "Gestos y miradas")
