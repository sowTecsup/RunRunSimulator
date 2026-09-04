---
tags: [script, world, agent, internal, brain, fsm]
---

# AgentBrain.cs

**Ruta:** `World/AI/AgentBrain.cs`

**Responsabilidad:** Orquestación de la máquina de estados NavMesh (comportamiento autónomo del MoriMochiAgent). Maneja transiciones entre Idle/Roaming/Reacting/SeekingNeed/UsingStation/Courting/Socializing/HandFeed/Expedition. Tick per-frame decay de necesidades (Health/Energy/Affect), búsqueda de estaciones críticas, interacción con jugador (petting hold-E), HandFeed (comida de mano), Intent expuesta (para NameTag). **S69:** Petting hold-E: sesión interactiva — frena, mira jugador, Affect += petAffectPerSecond×(1+petTimer×petRampPerSecond) por segundo, emote Corazon periódico, termina por release/timeout/!IsPlayerFacingMe. **S69:** HandFeed state: gate (hotbar + Health < feedHungerThreshold + dist ≤ feedNoticeRadius), acercarse, (si tímido duda), comer, consumir. **S98 NUEVO:** `EnterRoaming()` ya NO fija `Agent.speed`; velocidad la aplica `AgentContext.ApplyGaitSpeed()` cada frame. **S97:** Expedition state integrado (leído pero no manejado acá, delegado a AgentExpedition).

## Propiedades Consultables

- `IsBeingPetted → bool` — S69 verdadero mientras `pettingDisplayTimer > 0`
- `IsInFriendlyReaction → bool` — en Reacting pero no fleeing
- `CanBePetted → bool` — S69 Reacting + friendly + player facing
- `Intent → CreatureIntent` — mapping de estado → intención textual (Idle, Wandering, Following, Eating, Socializing, SeekingFood, Eating, etc.)

## Métodos Privados (Internal)

- `IsPlayerFacingMe() → bool` — player a petRadius y dentro de petLookAngle cone (xz dot product)

## Tick Methods (Llamados por MoriMochiAgent.Update)

- `TickIdle()` — S69 espera timer, intenta HandFeed, intenta seeking, intenta reacción, luego roaming
- `TickRoaming()` — S69 navega, intenta HandFeed, intenta seeking, intenta reacción, continúa
- `TickReacting()` — S69 si petting activo, entra `TickPetting()`. Sino, continúa reacción, transición a seeking/handFeed si necesidad crítica
- `TickSeekingNeed()` — avanza hacia estación reservada
- `TickUsingStation()` — consume de la estación hasta llenar
- `TickHandFeed()` — S69 acercarse a jugador, dudar si tímido, comer, consumir
- `TickAlways(dt)` — decay de necesidades cada frame (solo si spawned)

## Métodos Públicos (Interacción)

- `BeginPetSession()` — S69 fachada pública: entra petting dentro de Reacting
- `EndPetSession()` — S69 fachada pública: cancela petting
- `ReleaseStation()` — libera reserva de necesidad (llamado al cambiar de estado)
- `EnterRoaming()` — transición forzada a roaming. **S98: ya NO toca Agent.speed** (delegado a `AgentContext.ApplyGaitSpeed()`)

## Estado Interno

- `activeReaction` (ProximityReaction) — tipo de reacción en curso
- `reservedStation` (NeedStation) — estación ocupada
- `idleTimer, idleDuration, reactingTimer, reactCooldownTimer, pettingDisplayTimer` — temporizadores de estado
- `stateBeforeReact` — estado previo para restaurar post-reacción
- `petting, petTimer, petEmoteTimer` — S69 para sesión interactiva
- `feedCooldownTimer, feedHesitateTimer, feedEatTimer, feedHesitated, feedEating` — S69 para HandFeed
- `repathTimer` — throttle de recalculación de ruta

## Métodos Privados (Lógica)

- `TryEnterNeedSeeking() → bool` — intenta buscar estación crítica
- `TryEnterHandFeed() → bool` — S69 gate: hotbar + health + distance
- `TryGetCriticalNeed(out NeedType) → bool` — prioridad Health > Energy > Affect
- `ReactIfPlayerNear() → bool` — arranca reacción si player a rango y personality lo permite
- `BeginReaction()` — transición a Reacting
- `NextRoamDestination() → Vector3` — destino aleatorio o con preferencia de área; **S64:** aplica AdjustRoamForAvoidance() para empujar punto lejos de Monchis a evitar
- `TryGetPreferredPoint(out Vector3)` — muestra punto en área preferida por rol
- `IsPlayerFacingMe() → bool` — dot product de player.forward · to-creature (xz), rango petRadius, cone petLookAngle
- `TickPetting()` — S69 suma Affect, emote, verifica condiciones
- `TickHandFeed()` — S69 acercarse, dudar, comer, consumir
- `TryConsumeActiveFood()` — S69 pedir a HotbarController para consumir ítem

## Cambios S98

**Delegación de velocidad:**
- **ANTES S98:** `EnterRoaming()` hacía `Agent.speed = ...` directamente
- **DESDE S98:** `EnterRoaming()` ya NO toca `Agent.speed`
- **Responsabilidad nueva:** `AgentContext.ApplyGaitSpeed()` es el único dueño, llamado cada frame desde `MoriMochiAgent.Update()`
- **Lógica:** Roaming → BaseSpeed × Profile.RoamSpeedFactor; resto → BaseSpeed; Courting → no cambia

**Impacto en EnterRoaming():**
```csharp
internal void EnterRoaming()
{
    ReleaseStation();
    ctx.State = AgentState.Roaming;
    ctx.Agent.updateRotation = true;
    ctx.SetStopped(false);
    ctx.SetDestinationSafe(NextRoamDestination());
    // YA NO: ctx.Agent.speed = ...
    // ApplyGaitSpeed() lo hará en el siguiente frame (desde MoriMochiAgent.Update)
}
```

## Cambios S69

### Petting Hold-E (Sesión Interactiva)

**Campos internos nuevos:**
```csharp
private bool  petting;
private float petTimer;
private float petEmoteTimer;
```

**Métodos nuevos:**
```csharp
internal void BeginPetSession()  // fachada pública: entra petting dentro de Reacting
internal void EndPetSession()    // fachada pública: cancela petting
private void TickPetting()       // core: suma Affect, emote, verifica condiciones
```

**Lógica en TickReacting():**
```csharp
if (petting) { TickPetting(); return; }
// ... resto de reacción
```

**TickPetting() core:**
```csharp
// Suma Affect por segundo (con rampa opcional)
float affectGain = owner.petAffectPerSecond * (1f + petTimer * owner.petRampPerSecond);
ctx.Dna?.Needs.AddAffect(affectGain * Time.deltaTime);

petTimer += Time.deltaTime;
petEmoteTimer += Time.deltaTime;

// Emote periódico (Corazon cada petEmoteInterval)
if (petEmoteTimer >= owner.petEmoteInterval)
{
    owner.EmitEmote(EmoteKind.Corazon);
    petEmoteTimer = 0f;
}

// Termina por timeout
if (petTimer >= owner.petMaxDuration)
    EndPetSession();

// Termina si jugador se va (no facing, o lejos)
if (!IsPlayerFacingMe())
    EndPetSession();
```

**Knobs:**
- `petAffectPerSecond` float (default 2)
- `petRampPerSecond` float (default 0.1)
- `petMaxDuration` float (default 30)
- `petEmoteInterval` float (default 2)

### HandFeed State (Comida de la Mano)

**Campos internos nuevos:**
```csharp
private float feedCooldownTimer;
private float feedHesitateTimer;
private float feedEatTimer;
private bool  feedHesitated;
private bool  feedEating;
```

**Métodos nuevos:**
```csharp
private bool TryEnterHandFeed()  // gate: hotbar + health + distance
private void TickHandFeed()      // acercarse, dudar si tímido, comer, consumir
```

**TryEnterHandFeed() logic:**
- Prioridad: tras `TryEnterNeedSeeking()`, antes de `ReactIfPlayerNear()`
- Gate 1: `HotbarController.IsOfferingFood`
- Gate 2: `ctx.Dna.Needs.Health < owner.feedHungerThreshold`
- Gate 3: `ctx.PlanarDistanceToPlayer() <= owner.feedNoticeRadius`
- Si todos: `ctx.State = AgentState.HandFeed`, reset timers, return true

**TickHandFeed() state machine:**
1. **Approach:** Navegar a feedDistance del jugador
2. **Hesitate (si tímido):** Si `DNA.Sociability < owner.feedShyBelow`, esperar en `feedShyDistance` durante `feedHesitateSeconds`
3. **Eat:** Una vez a feedDistance, comer durante `feedEatSeconds`
4. **Consume:** Llamar `HotbarController.TryConsumeActiveFood()`, aplica boosts
5. **Cooldown:** antes de HandFeed nuevamente

**Knobs:**
- `feedNoticeRadius` float (default 3)
- `feedDistance` float (default 1.5)
- `feedShyBelow` float (default 0.4)
- `feedShyDistance` float (default 3)
- `feedHesitateSeconds` float (default 5)
- `feedEatSeconds` float (default 3)
- `feedHungerThreshold` float (default 40)
- `feedHealthBoost` float (default 30)
- `feedAffectBoost` float (default 5)
- `feedCooldown` float (default 10)

## Notas S64

- NextRoamDestination() llama a `owner.AdjustRoamForAvoidance(candidate)` (via MoriMochiAgent/AgentSocial) para filtrar puntos rechazados por reglas Avoid sociales
- El filtro es barato (one-pass) y sin recursión
- La lógica de Avoid NO afecta el pathfinding del NavMesh

## Invariantes S98 + S69

- **S98 Velocidad centralizada:** `ApplyGaitSpeed()` es la única vía para cambiar `Agent.speed`. `EnterRoaming()` no la toca.
- **S69 Petting:** sucede DENTRO del estado Reacting (amistosa), no cambia agredir/evitar lógica.
- **S69 HandFeed:** prioridad MÁS BAJA que necesidades críticas; cede a miedo/cortejo.
- **S69 Sociability:** modula HandFeed (tímido duda, aventurero come rápido).
- **S64 Avoid:** filtro barato de destino, sin recursión.

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/02 - Genetics & Breeding]]
- [[Index/14 - Social V1]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S97: Expedition; S98: velocidades)

## Conexiones

- [[MoriMochiAgent]] — propietario, llama todos los Tick*() métodos
- [[AgentContext]] — S98: ya NO toca speed (delegó a ApplyGaitSpeed)
- [[AgentPhysics]]
- [[AgentSocial]]
- [[RoleWorldProfileSO]] — Profile.RoamSpeedFactor (S98)
- [[NeedStationRegistry]], [[NeedStation]]
- [[HotbarController]]
- [[PlayerController]]
- [[CreatureDNA]]
- [[SocialTuningSO]]
- [[AgentExpedition]] — S97 state leído, delegado
