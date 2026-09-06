---
tags: [script, world, agent, internal, expedition]
---

# AgentClash.cs

**Ruta:** `World/AI/AgentClash.cs`

**Responsabilidad:** Máquina de estados interna de choque/combate físico para cada MoriMochiAgent. Maneja ciclo completo: enganche automático (TryEngage), combate manual (ForceMove), fases (Anticipating, Striking, Resolving, Dazed), impacto en rivales, knockback en cadena, counter-attack, cooldowns y gracia post-golpe. Expone fachada pública a MoriMochiAgent: Target, Gesture, Intent, IsTargetable. Inyecta estado Clashing en [[AgentContext.State]].

## Estados internos

```csharp
private enum Phase { None, Anticipating, Striking, Resolving, Dazed }
```

- **None:** sin choque activo
- **Anticipating:** mostrando intención, durando move.AnticipationSeconds
- **Striking:** golpeando, durando move.StrikeSeconds
- **Resolving:** post-golpe, durando tuning.ResolveSeconds
- **Dazed:** golpeado, durando tuning.DazedSeconds (solo si knockedByClash=true)

## Métodos públicos (llamados desde MoriMochiAgent)

- `TryEngage() → bool` — **S101:** intenta iniciar choque automático contra rival válido si:
  - Cooldown vencido (Time.time >= cooldownUntil)
  - **S101 NUEVO:** Gating por `ctx.Occupation`: rechaza Gather y Decoy (no inician choques)
  - Boldness >= tuning.MinBoldness
  - Rival dentro de EngageRange, no sostenido/volando/recuperándose, targetable
  - **S101 NUEVO:** Elige rival según ocupación:
    - `Break`: prefiere rivales con Intent `Taking/Carrying/Securing/Collecting` (los ladrones), fallback a más cercano
    - Otros: prefiere rival con Intent `Taunting` (señuelos provocan), fallback a más cercano
  - Elige movimiento según distancia/rivales: **S101:** nunca Back (evita barrida), prefiere Wings o Horn
- `ForceMove(ClashMoveSO move, MoriMochiAgent rival) → bool` — fuerza un movimiento específico (dev tools, no validación de cooldown)
- `TickClashing()` — cada frame en estado Clashing, avanza fase:
  - **Anticipating:** gira hacia rival, cuenta down. Si timer <= 0, StartStrike.
  - **Striking:** según slot:
    - **Horn:** navega hacia rival, impacta si distancia <= HitRadius, sino timeout
    - **Wings:** ya está en picada (Launch), TickAirborne controla impacto
    - **Back:** cuenta down, al vencer llama Sweep
  - **Resolving:** cuenta down, luego Finish
  - **Dazed:** gira hacia atacante, cuenta down, luego Decide
- `TickAirborne()` — mientras el atacante vuela (Wings dive), detecta impacto por distancia + velocidad descendente
- `ReceiveHit(MoriMochiAgent attacker)` — llamado desde [[MoriMochiAgent.ReceiveClashHit()]] cuando es golpeado:
  - Marca knockedByClash=true
  - Guarda attacker para counter-attack potencial
  - Arma chain immunity (no se puede golpear 2x en cadena del mismo atacante)
  - Emite onKnocked
- `IsTargetable → bool` — si !Dazed y Time.time >= targetableAt (gracia post-golpe)
- `IgnoresChainKnock(MoriMochiAgent other) → bool` — retorna true si other es lastAttacker y chain immunity activa
- `Cancel()` — detiene choque (p.ej. al ser lanzado fuera de alcance)
- `OnRecovered()` — llamado desde [[AgentPhysics.TickRecovering()]] cuando se levanta tras ragdoll:
  - Si estaba en Dazed, restaura control normal y decide counter-attack o retrete
  - Si estaba en picada (diving), suma cooldown
- `ResetForReuse()` — limpia estado al recyclar el agente

## Métodos internos (flujo)

- `Begin()` — inicia choque: RequestReleaseStation, entra Anticipating, emite onClashTell
- `StartStrike()` — transición a Striking, configura por slot:
  - **Horn:** override NavMeshAgent (speed, acceleration), habilita rotación automática, SetDestination
  - **Wings:** calcula velocidad de lanzamiento con Lead prediction, llama owner.Launch()
  - **Back:** detiene agente (solo gira in-place)
- `Impact()` — golpea rival:
  - Calcula dirección hacia víctima
  - Aplica impulso con UpBias
  - Si Horn con SelfRecoil, el atacante recibe knock suave hacia atrás
  - Llama victim.ReceiveClashHit(owner, force)
- `Sweep()` — busca rivales en SweepRadius, golpea todos los que cumplan:
  - No son aliados
  - No están volando/sostenidos/recuperándose
  - Targetable
- `Resolve()` — transición a Resolving, restaura NavMesh control
- `Finish()` — termina choque, suma cooldown, RequestRoam
- `Decide()` — post-Dazed, decide si contra-atacar:
  - Si lastAttacker está cerca, no sostenido/volando, Boldness >= ReengageBoldness, llama Begin contra-ataque
  - Sino, retrocede RetreatDistance
- `ChooseMove(t, rival, dist, occ) → ClashMoveSO` — **S101 NUEVO:** elige movimiento según ocupación:
  - `Break`: prefiere Wings (dive) si distancia >= DiveMinDistance, fallback Horn, nunca Back
  - Otros: Back si muchos rivales en SweepRange, Wings si dist >= DiveMinDistance, Horn como último recurso

## Fachada pública (propiedades)

- `Intent → CreatureIntent` — retorna Clashing si en Anticipating/Striking, Dazed si en Dazed, else None
- `Target → MoriMochiAgent` — retorna target actual si en Anticipating/Striking, else null
- `Gesture → string` — retorna gesto según fase (move.TellGesture en Anticipating, move.StrikeGesture en Striking, "" en otros)

## State internals

- `phase, phaseTimer` — máquina de estados
- `target, move, diving` — choque actual
- `cooldownUntil` — Time.time del próximo intento
- `knockedByClash` — si fue golpeado y en fase de Dazed
- `lastAttacker` — para counter-attack y retraite
- `targetableAt` — Time.time del fin de gracia post-golpe
- `chainImmuneUntil` — Time.time del fin de inmunidad al dominó
- `navOverridden, savedSpeed/Acceleration/Avoidance` — para restaurar NavMeshAgent

## S101: Cambios detallados

**Línea 45-47: Gating por Occupation en TryEngage()**

```csharp
var occ = ctx.Occupation;
if (occ == Occupation.None) occ = Occupation.Gather;
if (occ == Occupation.Gather || occ == Occupation.Decoy) return false;
```

- Gather y Decoy nunca inician choques automáticos (solo pueden ser atacados)
- Break, Guard, Explore (→Gather) sí pueden intentar choque

**Línea 69-80: Selección de rival por Occupation**

```csharp
if (occ == Occupation.Break)
{
    var intent = other.Intent;
    bool isThief = intent == CreatureIntent.Taking || intent == CreatureIntent.Carrying ||
                   intent == CreatureIntent.Securing || intent == CreatureIntent.Collecting;
    if (isThief && dist < preferredDist) { preferredDist = dist; preferred = other; }
}
else if (other.Intent == CreatureIntent.Taunting && dist < preferredDist)
{
    preferredDist = dist;
    preferred     = other;
}
```

- Break: busca ladrones (Taking/Carrying/Securing/Collecting)
- Otros (Guard, Explore): buscan rivales Taunting (señuelos)
- Fallback: rival más cercano

**Línea 256-269: ChooseMove elige Wings o Horn (nunca Back)**

```csharp
private ClashMoveSO ChooseMove(ClashTuningSO t, MoriMochiAgent rival, float dist, Occupation occ)
{
    if (occ == Occupation.Break)
    {
        if (t.Wings != null && dist >= t.DiveMinDistance && dist <= t.Wings.Range) return t.Wings;
        if (t.Horn != null && dist <= t.Horn.Range) return t.Horn;
        return null;
    }

    if (t.Back != null && dist <= t.Back.Range && CountRivalsWithin(t.SweepRange) >= t.SweepMinRivals) return t.Back;
    if (t.Wings != null && dist >= t.DiveMinDistance && dist <= t.Wings.Range) return t.Wings;
    if (t.Horn != null && dist <= t.Horn.Range) return t.Horn;
    return null;
}
```

- Break: Wings (dive) > Horn; nunca Back (para aislar víctima)
- Otros: Back (barrida si grupo), Wings (dive), Horn (melee)

## Invariantes S101 + S100

- Un choque solo puede iniciarse desde Idle/Roaming (TryEngage lo valida implícitamente al consultar Percepts)
- Chain immunity previene dominós infinitos: si A golpea a B y B se va al aire, B no golpea a A de nuevo durante 0.8s
- **S101:** Gather/Decoy nunca inician choques (gating defensivo); solo Break/Guard pueden atacar
- Counter-attack solo es posible si Boldness >= ReengageBoldness y el atacante sigue en rango — evita persecución indefinida
- La transición Dazed → Decide solo ocurre si knockedByClash=true; de lo contrario Finish inmediato
- **S101:** Rivales Taunting son cebo para el grupo; Break busca ladrones específicamente

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]] (sección 5f: Gating y selección por Occupation)
- [[MoriMochiAgent]]
- [[AgentContext]]
- [[AgentPhysics]]
- [[ClashTuningSO]]
- [[ClashMoveSO]]
- [[MonchiGestureDriver]]
- [[ArenaCameraDirector]]
