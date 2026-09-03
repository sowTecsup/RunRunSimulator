---
tags: [script, world, ai, agent, internal]
---

# AgentExpedition.cs

**Ruta:** `World/AI/AgentExpedition.cs`

**Responsabilidad:** Colaborador interno de `MoriMochiAgent` que orquesta "si veo material recolectable, voy, lo tomo, vuelvo a vagar". Evaluador de reglas: `TryEngage()` puntúa cada percepto contra cada regla en `ExpeditionRulesSO.Current.Rules` y elige el mejor (máximo score); al enganchar entra en estado `Expedition`. Tick: repath cada `RepathInterval`, abandona pasados `GiveUpSeconds`, y al llegar a `ArriveDistance` (planar) intenta tomar y vuelve a roaming. Sin interfaz pública salvo la fachada de propiedades (`Collected`, `Target`, `Intent`). En la tienda `ExpeditionRulesSO.Current == null` → TryEngage devuelve false → cero impacto.

## Métodos Internos

- `TryEngage() → bool` — **entry point** (llamado desde `MoriMochiAgent.Update` en Idle/Roaming, antes que social). Revisa si hay `ExpeditionRulesSO.Current` y reglas activas; itera todos los percepto × reglas, acumula scores; elige el mejor. Si hay ganador y su `Percept.Source` contiene `MaterialPickup` no tomado, entra en `AgentState.Expedition`, resetea timers, emite emote `Curioso`, y devuelve true.
- `TickExpedition()` — **tick de estado** (llamado desde `MoriMochiAgent.Update` caso `Expedition`). Chequea si target aún es válido (existe, no tomado, activo); si no, abort. Si tiempo > `GiveUpSeconds`, abandon. Repath si timer <= 0. Al llegar a destino (distancia planar <= `ArriveDistance`), intenta tomar, emite emote `Feliz`, y abort a roaming.
- `ResetForReuse()` — **pooling**: limpia target, timers; llamado en `Initialize` y `RestoreNavMeshControl` para reciclaje.

## Propiedades Públicas (fachada)

- `Collected → int` — contador acumulativo de material tomado (sesión local).
- `Target → MaterialPickup` — el recolectable actual siendo perseguido (null si idle).
- `Intent → CreatureIntent` — siempre devuelve `CreatureIntent.Collecting` si en expedición.

## Campos Internos

- `owner` (MoriMochiAgent) — el agente propietario.
- `ctx` (AgentContext) — contexto compartido (estado, percepciones, NavMesh).
- `target` (MaterialPickup) — objetivo actual.
- `repathTimer` — throttle de repath (se decrementa cada frame).
- `elapsed` — tiempo transcurrido en la expedición (para give-up).
- `collected` — acumulador de material (int).

## Ciclo de Operación

```
Idle/Roaming (en MoriMochiAgent.Update):
  if (! expedition.TryEngage()) social.TryEngage()
    → Si no hay reglas (tienda) o no matching, devuelve false; social toma.
    → Si hay matching, entra Expedition, emote Curioso.

Expedition (en MoriMochiAgent.Update):
  expedition.TickExpedition()
    → Valida target, chequea timeout, repath.
    → Si target muere/se toma por otro, abort → Roaming.
    → Si timeout, abandon → Roaming.
    → Si llega, TryTake + emote Feliz → Roaming.
```

## Evaluación de Reglas (TryEngage)

Itera `ctx.Percepts` (poblada por `AgentSenses.Tick()`, throttled cada ~2-4s) vs `ExpeditionRulesSO.Current.Rules` (lista polimórfica). Cada regla implementa `Matches(Percept, self, rules, out score)`:
- Si no match (ej: `Kind != Material`), devuelve false.
- Si match, calcula score y devuelve true.

Ganador = máximo score. Si hay empate, se elige el primero encontrado (orden de iteración).

**Ejemplo:** `SeekMaterialRule` chequea `p.Kind == Material`, no null, activo; score = `1/(1+dist) * (1 + boldnessBias*(boldness-0.5)*2)` (distancia inversa modulada por osadía).

## Invariantes S97

- **Percepts read-only:** lista poblada por `AgentSenses`, solo lectura de `AgentExpedition` y `AgentSocial`.
- **TryEngage antes de social:** prioridad de intenciones en Idle/Roaming: Expedición > Social > default roaming brain.
- **Target validation:** `TryTake()` devuelve bool; si falla (material ya tomado), se abort sin error.
- **No persist:** `collected` es sesión local del agente; al poolear se resetea. Persistencia de stats es responsabilidad de `CreatureDNA` y `GameManager`.
- **Timeout give-up:** evita stalling si el recolectable se vuelve inaccesible; `GiveUpSeconds` default 12 s.
- **Emotes:** `Curioso` al enganchar (se lo ve pensando), `Feliz` al tomar (éxito).
- **Escena tienda:** `ExpeditionRulesSO.Current` es null → TryEngage always false → expedición nunca activa, social en control.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]], [[Index/06 - Player & World]]

## Conexiones

**Componentes:**
- [[MoriMochiAgent]] (owner, lector de Intent, fachada pública)
- [[AgentContext]] (contexto de estado, Percepts, SetDestinationSafe)

**Datos y servicios:**
- [[ExpeditionRulesSO]] (Current singleton, reglas polimórficas)
- [[ExpeditionRuleBase]] / [[SeekMaterialRule]] (evaluadores de score)
- [[MaterialPickup]] (target, TryTake)

**Percepciones:**
- [[Perceivable]] (cada percepto tiene `.Source` que puede contener MaterialPickup)
- [[PerceivableKind.Material]] (filtro en reglas)

**Comportamiento:**
- [[AgentBrain]] (RequestRoam en abort, coordina states)
