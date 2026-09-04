---
tags: [script, world, ai, agent, internal]
---

# AgentExpedition.cs

**Ruta:** `World/AI/AgentExpedition.cs`

**Responsabilidad:** Colaborador interno de `MoriMochiAgent` que orquesta "percibo material → lo notice → navego → lo tomo → reacciono". Evaluador de reglas: `TryEngage()` puntúa cada percepto contra cada regla en `ExpeditionRulesSO.Current.Rules` y elige el mejor (máximo score); al enganchar entra en estado `Expedition`. Máquina de estados con 4 fases (Noticing → Moving → Taking → Losing). Sin interfaz pública salvo la fachada de propiedades (`Collected`, `Target`, `Intent`). En la tienda `ExpeditionRulesSO.Current == null` → TryEngage devuelve false → cero impacto.

## Métodos Internos

- `TryEngage() → bool` — **entry point** (llamado desde `MoriMochiAgent.Update` en Idle/Roaming, antes que social). Revisa si hay `ExpeditionRulesSO.Current` y reglas activas; itera todos los percepto × reglas, acumula scores; elige el mejor. Si hay ganador y su `Percept.Source` contiene `MaterialPickup` no tomado, entra en `AgentState.Expedition`, **entra fase Noticing**, emite emote `Curioso`, y devuelve true.
- `TickExpedition()` — **tick de estado** (llamado desde `MoriMochiAgent.Update` caso `Expedition`). Orquesta las 4 fases: Noticing → Moving → Taking → Losing. Chequea si target aún es válido; si no, EnterLosing. Si tiempo > `GiveUpSeconds`, abandona. Avanza fases según `phaseTimer` y logística.
- `ResetForReuse()` — **pooling**: limpia target, timers, fase; llamado en `Initialize` y `RestoreNavMeshControl` para reciclaje.

## Propiedades Públicas (fachada)

- `Collected → int` — contador acumulativo de material tomado (sesión local).
- `Target → MaterialPickup` — el recolectable actual siendo perseguido (null si idle).
- `Intent → CreatureIntent` — devuelve según fase: `Taking` en fase Taking, `Losing` en fase Losing, `Collecting` en resto.

## Campos Internos

- `owner` (MoriMochiAgent) — el agente propietario.
- `ctx` (AgentContext) — contexto compartido (estado, percepciones, NavMesh).
- `target` (MaterialPickup) — objetivo actual.
- `phase` (Phase enum: Noticing/Moving/Taking/Losing) — **S98-S99 NUEVO** fase actual.
- `phaseTimer` — cuenta regresiva dentro de fase (sincronizado con beat timings).
- `repathTimer` — throttle de repath (se decrementa cada frame).
- `elapsed` — tiempo transcurrido en la expedición (para give-up).
- `collected` — acumulador de material (int).
- `blockedTimer` — detector de bloqueo en Moving (si velocity < 0.05 m/s durante ~0.6s, se considera "llegado").
- `lostPoint` — posición recordada en el beat Losing (para giro post-pérdida).

## Máquina de Estados S98 (4 fases)

```
TryEngage() → entra Expedition, fase = Noticing, phaseTimer = ExpeditionRulesSO.NoticeSeconds

[Noticing] (criatura ve mineral, se frena, emote Curioso)
  - SetStopped(true): paraliza NavMesh
  - phaseTimer decrece
  - Si phaseTimer <= 0 o NoticeSeconds <= 0:
    → Fase Moving
    → SetStopped(false): reactiva NavMesh
    → SetDestination(ApproachPoint): navega acercándose

[Moving] (navega hacia mineral, evitando otros agentes)
  - Repath cada RepathInterval segundos
  - ApproachPoint: calcula punto de acercamiento tangencial (desplazamiento angular 2π)
    → Itera todos los percepts Monchi que compitan por el mismo target
    → Desplaza el punto de acercamiento para evitar solapamiento
  - Chequea 2 condiciones de "llegada":
    1. delta.magnitude <= ArriveDistance: distancia planar al punto de acercamiento
    2. blockedTimer: si velocity < 0.05 m/s por >0.6s (detector de bloqueo/estancamiento)
  - Si llegada:
    → Fase Taking
    → SetStopped(true)
    → phaseTimer = ExpeditionRulesSO.TakeSeconds

[Taking] (criatura agarrando/consumiendo mineral)
  - SetStopped(true): paraliza NavMesh
  - Rota body hacia target: Slerp(rotation, LookRotation(to_target), 10*dt)
  - phaseTimer decrece
  - Si phaseTimer <= 0 o TakeSeconds <= 0:
    → TryTake(): llama target.TryTake(out value)
    → Si éxito:
      ✓ collected += value
      ✓ EmitEmote(Feliz)
      ✓ owner.onPickup?.Invoke()
      ✓ Abort() → roaming
    → Si fallo (rival lo tomó):
      ✗ EnterLosing()

[Losing] (reacción a perder el mineral a un rival)
  - SetStopped(true)
  - Rota body hacia lostPoint (posición del mineral)
  - EmitEmote(Molesto)
  - phaseTimer = ExpeditionRulesSO.LoseSeconds (default 1s)
  - phaseTimer decrece
  - Si phaseTimer <= 0 o LoseSeconds <= 0:
    → Abort() → roaming

Timeout global:
  - Si elapsed > ExpeditionRulesSO.GiveUpSeconds (default 12s):
    → Abort() → roaming (sin alcanzar tomar)

Target validation:
  - Si en cualquier fase: target == null || target.Taken || !gameObject.active:
    → EnterLosing() inmediatamente
```

## Ciclo de Operación

```
Idle/Roaming (en MoriMochiAgent.Update):
  if (! expedition.TryEngage()) social.TryEngage()
    → Si no hay reglas (tienda) o no matching, devuelve false; social toma.
    → Si hay matching, entra Expedition, emote Curioso.

Expedition (en MoriMochiAgent.Update):
  expedition.TickExpedition()
    → Orbesta máquina de 4 fases
    → Emote Feliz al tomar, Molesto si pierde
    → Vuelve a Roaming en Abort()
```

## Evaluación de Reglas (TryEngage)

Itera `ctx.Percepts` (poblada por `AgentSenses.Tick()`, throttled cada ~2-4s) vs `ExpeditionRulesSO.Current.Rules` (lista polimórfica). Cada regla implementa `Matches(Percept, self, rules, out score)`:
- Si no match (ej: `Kind != Material`), devuelve false.
- Si match, calcula score y devuelve true.

Ganador = máximo score. Si hay empate, se elige el primero encontrado (orden de iteración).

**Ejemplo:** `SeekMaterialRule` chequea `p.Kind == Material`, no null, activo; score = `1/(1+dist) * (1 + boldnessBias*(boldness-0.5)*2)` (distancia inversa modulada por osadía).

## Invariantes S98

- **4 fases discretas:** Noticing (beat visual), Moving (navegación), Taking (beat consumo), Losing (beat reacción). Cada una con su lógica y duración (timings en ExpeditionRulesSO).
- **Intents por fase:** `Taking` e `Losing` son intents nuevos que permiten sincronización con gestos/emotes/HUD.
- **ApproachPoint desplazamiento:** evita apiñamiento cuando múltiples agentes van por el mismo mineral (angular separation).
- **Percepts read-only:** lista poblada por `AgentSenses`, solo lectura de `AgentExpedition` y `AgentSocial`.
- **TryEngage antes de social:** prioridad de intenciones en Idle/Roaming: Expedición > Social > default roaming brain.
- **Target validation:** si target es tomado por rival (u otro evento), entra Losing inmediatamente.
- **No persist:** `collected` es sesión local del agente; al poolear se resetea. Persistencia de stats es responsabilidad de `CreatureDNA` y `GameManager`.
- **Timeout give-up:** evita stalling si el recolectable se vuelve inaccesible; `GiveUpSeconds` default 12 s.
- **Escena tienda:** `ExpeditionRulesSO.Current` es null → TryEngage always false → expedición nunca activa, social en control.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

**Componentes:**
- [[MoriMochiAgent]] (owner, lector de Intent, fachada pública, EmitEmote, RequestRoam)
- [[AgentContext]] (contexto de estado, Percepts, SetDestinationSafe, SetStopped)

**Datos y servicios:**
- [[ExpeditionRulesSO]] (Current singleton, reglas polimórficas, **beat timings S98-S99**: NoticeSeconds, TakeSeconds, LoseSeconds, navegación)
- [[ExpeditionRuleBase]] / [[SeekMaterialRule]] (evaluadores de score)
- [[MaterialPickup]] (target, TryTake)

**Percepciones:**
- [[Perceivable]] (cada percepto tiene `.Source` que puede contener MaterialPickup)
- [[PerceivableKind.Material]] (filtro en reglas)

**Comportamiento:**
- [[AgentBrain]] (RequestRoam en abort, coordina states)
- **S98-S99:** [[CreatureIntent]] (Taking, Losing)
- **S98:** [[MonchiGestureDriver]] (sincroniza gestos con intent)
- **S98-S99:** [[MonchiMoodDriver]] (reacciona a intent Taking/Losing)
- **S98:** [[ArenaCueOverlay]] (dibuja rutas de expedición coloreadas por intent)
