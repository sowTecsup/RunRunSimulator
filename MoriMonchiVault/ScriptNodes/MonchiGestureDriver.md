---
tags: [script, creatures, animation, realismo]
---

# MonchiGestureDriver.cs

**Ruta:** `World/Creatures/MonchiGestureDriver.cs`

**Responsabilidad:** Orquestador de gestos (animaciones discretas) basado en intención y condición. Maneja transiciones suave de gestos (enter → hold), fidgets periodicos cuando está idle, y override por enfermedad. Consulta `MonchiGestureSetSO` para mapping Intención → Gesto.

## Propiedades

**Serializado (Inspector):**
- `agent` (MoriMochiAgent) — referencia requerida; consume `agent.Intent` y `agent.DNA.Boldness`
- `locomotion` (MonchiLocomotionAnimator) — referencia requerida; orquestación de animaciones (PlayGesture, HoldGesture, StopGesture, IsGesturing, IsStill)
- `set` (MonchiGestureSetSO) — referencia requerida; SO de mapping intención → gesto
- `combatDriver` (DragonAnimationDriver) — opcional; si existe y `IsBusy`, descarta gestos (combate tiene su propio anim)

**Internos:**
- `lastIntent` (CreatureIntent) — intención en el frame anterior; detector de cambio
- `currentHold` (string) — gesto de mantención actualmente activo
- `pendingEnter` (string) — gesto de entrada esperando espacio de animación
- `nextFidget` (float) — timestamp del próximo fidget (Time.time)

## Ciclo de Vida

**OnEnable:**
```csharp
nextFidget = Time.time + Random.Range(0, set.FidgetInterval.x)
lastIntent = agent.Intent
currentHold = ""
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

4. Ejecutar gesto de entrada:
   if (pendingEnter != "" && locomotion.PlayGesture(pendingEnter)):
     pendingEnter = "" (consumido)

5. Determinar gesto de mantención deseado:
   desiredHold = (CreatureCondition == Sick) ? set.SickGesture : (set.TryHoldGesture(intent) ? state : "")

6. Si locomotion deja de gesticular, limpiar currentHold:
   if (currentHold != "" && !locomotion.IsGesturing):
     currentHold = ""

7. Transitar a nuevo hold si cambió:
   if (desiredHold != currentHold):
     if (string.IsNullOrEmpty(desiredHold)):
       locomotion.StopGesture()
       currentHold = ""
     else if (locomotion.HoldGesture(desiredHold)):
       currentHold = desiredHold

8. Fidgets periódicos (solo Idle/Wandering, quieto):
   if (Time.time >= nextFidget
       && (intent == Idle || Wandering)
       && locomotion.IsStill && !IsGesturing && currentHold == ""):
     nextFidget = Time.time + Random.Range(set.FidgetInterval.x, set.FidgetInterval.y)
     boldness = agent.DNA != null ? agent.DNA.Boldness : 0.5f
     fidget = set.PickFidget(boldness)
     if (fidget != null):
       locomotion.PlayGesture(fidget)
```

## Invariantes S98

- **Prioridad enter > hold:** gesto de entrada ocupa espacio de animación (breve, trigger); hold es loop continuo. Transición suave: enter se consume, luego hold toma control.
- **Sick override:** `CreatureCondition.Sick` fuerza `SickGesture` sin respetar intención. Toma prioridad máxima.
- **Fidgets sociales:** solo durante Idle/Wandering, cuando creature no está haciendo nada. Ponderados por Boldness (bravos hacen gestos más agresivos).
- **Combate aislado:** si `DragonAnimationDriver.IsBusy`, descarta toda lógica de gestos (combate maneja su propia anim).
- **Held/Airborne/Recovering:** descarta gestos cuando creature está siendo pettead, cargado, o saltando (transiciones autom. con OnDisable).

## Conexiones

[[MoriMochiAgent]], [[MonchiLocomotionAnimator]], [[MonchiGestureSetSO]], [[CreatureIntent]], [[CreatureCondition]], [[CreatureDNA]], [[DragonAnimationDriver]]

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]] (Parte 8: "Gestos y miradas")
