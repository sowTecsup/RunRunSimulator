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

- `TryEngage() → bool` — intenta iniciar choque automático contra rival válido si:
  - Cooldown vencido (Time.time >= cooldownUntil)
  - Boldness >= tuning.MinBoldness
  - Rival dentro de EngageRange, no sostenido/volando/recuperándose, targetable
  - Elige movimiento según distancia/rivales: Back > Wings > Horn
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

## Conexiones

**Entrada:**
- **Creado por:** [[MoriMochiAgent.Awake()]] (línea 50)
- **TryEngage llamado por:** [[MoriMochiAgent.Update()]] en estados Idle/Roaming antes de social (línea 139-140)
- **TickClashing llamado por:** [[MoriMochiAgent.Update()]] en estado Clashing (línea 150)
- **TickAirborne llamado por:** [[MoriMochiAgent.Update()]] en estado Thrown (línea 142)
- **ReceiveHit llamado por:** [[MoriMochiAgent.ReceiveClashHit()]] (línea 230)
- **OnRecovered llamado por:** [[AgentPhysics.TickRecovering()]] (línea 191)

**Salida:**
- **ctx.State = Clashing** — durante combate y Dazed
- **owner.onClashTell** — al comenzar, para sincronía de gestos
- **owner.onClashHit** — al impactar, para VFX
- **owner.onKnocked** — al recibir golpe
- **victim.ReceiveClashHit()** — propaga golpe
- **owner.EmitEmote(Molesto)** — emote al comenzar combate
- **owner.RequestRoam()** — al finalizar combate normal
- **rival.ForceClash(move, target)** — para dev console ArenaClashDev

## Invariantes S100

- Un choque solo puede iniciarse desde Idle/Roaming (TryEngage lo valida implícitamente al consultar Percepts)
- Chain immunity previene dominós infinitos: si A golpea a B y B se va al aire, B no golpea a A de nuevo durante 0.8s
- Counter-attack solo es posible si Boldness >= ReengageBoldness y el atacante sigue en rango — evita persecución indefinida
- La transición Dazed → Decide solo ocurre si knockedByClash=true; de lo contrario Finish inmediato

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[MoriMochiAgent]]
- [[AgentContext]]
- [[AgentPhysics]]
- [[ClashTuningSO]]
- [[ClashMoveSO]]
- [[MonchiGestureDriver]]
- [[ArenaCameraDirector]]
