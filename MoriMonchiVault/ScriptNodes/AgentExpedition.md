---
tags: [script, world, ai, agent, internal, expedition]
---

# AgentExpedition.cs

**Ruta:** `World/AI/AgentExpedition.cs`

**Responsabilidad:** Colaborador interno de `MoriMochiAgent` que orquesta ocupaciones de tiempo en arena: `Gather` (percibo material → lo notice → navego → lo tomo → reacciono), `Guard` (me planto en un puesto de material y lo vigilo), `Break` (acecharé y golpearé a rival que recolecta/carga), `Decoy` (provoco al rival y huyo). Evaluador de reglas: `TryEngage()` puntúa cada percepto contra cada regla en `ExpeditionRulesSO.Current.Rules` y elige el mejor (máximo score); al enganchar entra en estado `Expedition`. Máquina de estados con fases (Noticing → Moving → Mining → Losing para Gather; Guarding para Guard; Hunting para Break; Decoying para Decoy). Sin interfaz pública salvo la fachada de propiedades (`Collected`, `Target`, `Intent`, `Carried`, `MiningProgress`, `TargetTransform`). En la tienda `ExpeditionRulesSO.Current == null` → TryEngage devuelve false → cero impacto.

## Métodos Internos

- `TryEngage() → bool` — **entry point** (llamado desde `MoriMochiAgent.Update` en Idle/Roaming, antes que social). Revisa si hay `ExpeditionRulesSO.Current`. Según Occupation (traducido a Gather si None/Explore): llama TryGatherEngage, TryGuardEngage, TryBreakEngage o TryDecoyEngage. Si hay matching, entra en `AgentState.Expedition` con fase inicial, emite emote, devuelve true.

**Sub-métodos por ocupación (S101 NUEVO):**
- `TryGatherEngage()` — itera percepto × reglas, elige mejor score. Entra Noticing → Moving → Mining → Returning → Securing.
- `TryGuardEngage()` — busca `GuardPost` inyectado o el MaterialPickup con más valor. Entra Guarding (estático, vigila).
- `TryBreakEngage()` — busca MoriMochiAgent rival que recolecte/carga. Si lo encuentra, entra Hunting y persigue. Fallback: acecha MaterialPickup con más valor y espera rival.
- `TryDecoyEngage()` — busca MoriMochiAgent rival guardián o rompedor, con cooldown de último decoy. Entra Decoying (Approach → Taunt → Flee).

- `TickExpedition()` — **tick de estado** (llamado desde `MoriMochiAgent.Update` caso `Expedition`). Orquesta máquina por fase. Chequea si target válido; si no, EnterLosing/Abort. Si tiempo > `GiveUpSeconds`, abandona. Avanza fases según timers y logística.
- `ResetForReuse()` — **pooling**: limpia target, timers, fase; llamado en `Initialize` y `RestoreNavMeshControl` para reciclaje.

## Propiedades Públicas (fachada)

- `Collected → int` — contador acumulativo de material tomado (sesión local).
- `Target → MaterialPickup` — el recolectable actual siendo perseguido (null si idle).
- `Carried → int` — material actual en mano (0-CarryCapacity).
- `MiningProgress → float` — 0-1 progreso del beat de minería.
- `TargetTransform → Transform` — transform del objetivo (material o exit) o null.
- `Intent → CreatureIntent` — devuelve según fase.

## Fases S101 + S98

**Gather (Ocupación.Gather, default):**
- Noticing → Moving → Mining → Returning → Securing
- Cuando Mining completa: `carried++`, si llegas a capacidad o mineral agotado, comienza Returning hacia HomeExit
- Securing: entra en la salida y deposita material

**Guard (Ocupación.Guard):**
- Guarding: se planta cerca del MaterialPickup de Guardia (GuardPost inyectado o descubierto)
- Rota para vigilar minerales cercanos o rivales

**Break (Ocupación.Break):**
- Hunting: si ve rival recolectando/cargando, lo persigue y golpea (si está en rango, agresión física)
- Fallback: si no hay rival, se planta en MaterialPickup de Guardia y espera

**Decoy (Ocupación.Decoy):**
- Decoying: Approach → Taunt → Flee
- Approach: navega hacia rival guardián/rompedor
- Taunt: se detiene, emota Molesto, hace "ruido" (emote)
- Flee: corre lejos del rival, hacia HomeExit si existe
- Cooldown: no puede Decoy cada 4s (DecoyCooldown)

## Campos Internos (Ejemplos clave)

- `owner` (MoriMochiAgent) — agente propietario.
- `ctx` (AgentContext) — contexto compartido.
- `target` (MaterialPickup) — objetivo recolectable.
- `carried` (int) — material en mano (≤ CarryCapacity).
- `phase` (Phase enum) — Noticing/Moving/Mining/Losing/Returning/Securing/Guarding/Hunting/Decoying.
- `phaseTimer` — cuenta regresiva dentro de fase.
- `exit` (ExitZone) — salida donde depositar (HomeExit).
- `prey` (MoriMochiAgent) — rival siendo perseguido (Break/Decoy).
- `decoyStep` (DecoyStep enum: Approach/Taunt/Flee) — sub-fase de Decoying.
- `decoyCooldownUntil` (float) — Time.time del próximo decoy permitido.

## Máquina de Estados Detallada

```
[Gather]
TryGatherEngage() → Noticing, phaseTimer=NoticeSeconds
Noticing → Moving (repath hacia MaterialPickup)
Moving → Mining (al llegar, phaseTimer=MiningSecondsPerUnit)
Mining → `carried++` cada phaseTimer vencido
       si carried >= CarryCapacity o material.Taken → BeginReturn(exit)
       sino → siguiente Mining cycle
Returning → navega a HomeExit, repath cada RepathInterval
          si Contains(exit) → Securing
Securing → rota hacia exit, phaseTimer=DepositSeconds
         → exit.Deposit(carried), carried=0, Abort()

[Guard]
TryGuardEngage() → Guarding, target=GuardPost/MaterialPickup
Guarding → si target.Taken → Abort()
         → si rival acerca → rota hacia rival más cercano o target
         → mantener distancia GuardRadius

[Break]
TryBreakEngage() → Hunting (si hay rival) o Hunting con target=MaterialPickup (si no)
Hunting → si prey != null → persigue a prey
        → si prey en rango de choque → AgentClash.TryEngage() (automático, no muta aquí)
        → si prey desaparece → Abort() o switch a MaterialPickup fallback

[Decoy]
TryDecoyEngage() → Decoying, decoyStep=Approach, phaseTimer=0
  DecoyStep.Approach → navega hacia rival, huntTimer=HuntRepathInterval
                    → si en rango DecoyRange → decoyStep=Taunt, phaseTimer=TauntSeconds
                    → si elapsed > GiveUpSeconds → EndDecoy (cooldown)
  DecoyStep.Taunt → rota hacia rival, emota Molesto
                 → phaseTimer decrece
                 → si phaseTimer <= 0 → decoyStep=Flee, phaseTimer=DecoyFleeSeconds
                                      → calcula dirección away from rival (hacia HomeExit si existe)
  DecoyStep.Flee → navega lejos, phaseTimer decrece
                → si phaseTimer <= 0 → EndDecoy (suma cooldown, Abort())
```

## Ocupación Mapping (S101 NUEVO)

```csharp
// En TryEngage():
var occ = ctx.Occupation;
if (occ == Occupation.None || occ == Occupation.Explore) occ = Occupation.Gather;

switch (occ)
{
    case Occupation.Guard:  return TryGuardEngage(rules);
    case Occupation.Break:  return TryBreakEngage(rules);
    case Occupation.Decoy:  return TryDecoyEngage(rules);
    default:                return TryGatherEngage(rules);
}
```

## Ciclo de Operación

```
Idle/Roaming (en MoriMochiAgent.Update):
  if (! expedition.TryEngage()) social.TryEngage()
    → Si hay ocupación y matching, entra Expedition
    → Si sin ocupación en tienda, devuelve false

Expedition (en MoriMochiAgent.Update):
  expedition.TickExpedition()
    → Orbesta máquina de fases según ocupación
    → Vuelve a Roaming en Abort()
```

## Evaluación de Reglas (TryGatherEngage)

Itera `ctx.Percepts` (poblada por `AgentSenses`) vs `ExpeditionRulesSO.Current.Rules` (lista polimórfica). Cada regla implementa `Matches(Percept, self, rules, out score)`. Ganador = máximo score.

## Invariantes S101 + S98 + S99

- **Ocupaciones discretas:** Gather/Guard/Break/Decoy definen estrategias sin solapamiento (TryEngage elige una según Occupation actual).
- **Gather es default:** si Occupation.None o Occupation.Explore → traducir a Gather.
- **Guard es estático:** se planta en MaterialPickup, vigila sin interactuar (permite que rivales lleguen pero alertar al equipo).
- **Break es agresivo:** busca y golpea rivales que recolectan, o acecha esperando.
- **Decoy es táctica:** provoca rivales guardián/rompedor para distraerlos, luego huye. Cooldown evita spam.
- **Carried vs Secured:** `carried` es local a agente (en mano), `exit.Secured` es acumulador de equipo (depositado).
- **Percepts read-only:** solo lectura de `AgentSenses`, sin mutación.
- **No persist:** `collected` es sesión local; al poolear se resetea.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]] (sección 8.10: Ocupaciones)

## Conexiones

**Componentes:**
- [[MoriMochiAgent]] (owner, Intent, Occupation, EmitEmote, RequestRoam)
- [[AgentContext]] (contexto, Percepts, SetDestinationSafe, SetStopped, Occupation, HomeExit, GuardPost)

**Datos y servicios:**
- [[ExpeditionRulesSO]] (Current, Rules, timings: NoticeSeconds, MiningSecondsPerUnit, DepositSeconds, etc.)
- [[Occupation]] (Gather, Guard, Break, Decoy, Explore)
- [[MaterialPickup]] (target, Taken, TryMineUnit)
- [[ExitZone]] (exit, Deposit, Secured)
- [[MoriMochiAgent]] (prey para Break/Decoy, Intent de rival)

**Visuals (S101-S99):**
- [[CreatureIntent]] (Collecting, Taking, Losing, Guarding, Hunting, Taunting, Fleeing)
- [[MonchiGestureDriver]] (sincroniza con intent)
- [[MonchiMoodDriver]] (reacciona a intent)
- [[ArenaCueOverlay]] (dibuja rutas coloreadas por intent, retícula de objetivo)
- [[ArenaCameraDirector]] (enfoca en conflictos)

**Combate (S101-S100 integración):**
- [[AgentClash]] (si Break y rival en rango, TryEngage() es automático desde MoriMochiAgent)
- [[ClashTuningSO]] (tuning de combate)
