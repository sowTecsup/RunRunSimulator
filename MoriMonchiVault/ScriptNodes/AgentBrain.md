---
tags: [script, world, agent, internal, brain, fsm]
---

# AgentBrain.cs

**Ruta:** `World/AI/AgentBrain.cs`

**Responsabilidad:** Orquestación de la máquina de estados NavMesh (comportamiento autónomo del MoriMochiAgent). Maneja transiciones entre Idle/Roaming/Reacting/SeekingNeed/UsingStation/Courting/Socializing/**HandFeed**. Tick per-frame decay de necesidades (Health/Energy/Affect), búsqueda de estaciones críticas, interacción con el jugador (petting hold-E), HandFeed (comida de mano), e Intención expuesta (para NameTag). **S69:** Petting hold-E: sesión interactiva `BeginPetSession()/EndPetSession()/TickPetting()` dentro de Reacting — frena, mira al jugador, Affect += petAffectPerSecond×(1+petTimer×petRampPerSecond) por segundo, emote Corazon periódico, termina por release/petMaxDuration/!IsPlayerFacingMe. **S69:** HandFeed state: `TryEnterHandFeed()` (gate: hotbar IsOfferingFood + Health<feedHungerThreshold + dist≤feedNoticeRadius, prioridad tras TryEnterNeedSeeking y antes de ReactIfPlayerNear) + `TickHandFeed()` (acercarse → tímido Sociability<feedShyBelow duda a feedShyDistance por feedHesitateSeconds → bocado feedEatSeconds → TryConsumeActiveFood + feedHealthBoost/feedAffectBoost + cooldown). Intent: HandFeed → SeekingFood/Eating. NUEVO en S64: integración con AgentSocial para filtrado de puntos de roam por evitación social (AdjustRoamForAvoidance).

**Propiedades consultables:**
- `IsBeingPetted → bool` — **S69 NUEVO** verdadero mientras `pettingDisplayTimer > 0`
- `IsInFriendlyReaction → bool` — en Reacting pero no fleeing
- `CanBePetted → bool` — **S69 NUEVO** Reacting + friendly + player facing
- `Intent → CreatureIntent` — mapping de estado → intención textual (Idle, Wandering, Following, Eating, Socializing, Chasing, SleepingTogether, Fighting, SeekingFood, HandFeed, etc.)
- `IsPlayerFacingMe() → bool` — player a petRadius y dentro de petLookAngle

**Tick methods (llamados por MoriMochiAgent.Update):**
- `TickIdle()` — **S69 ACTUALIZADO** espera timer, intenta HandFeed, intenta seeking, intenta reacción, luego roaming
- `TickRoaming()` — **S69 ACTUALIZADO** navega, intenta HandFeed, intenta seeking, intenta reacción, continúa
- `TickReacting()` → **S69 ACTUALIZADO** si petting activo, entra `TickPetting()`. Si no, continúa reacción normal, transición a seeking/handFeed si necesidad crítica
- `TickSeekingNeed()` — avanza hacia estación reservada
- `TickUsingStation()` — consume de la estación hasta llenar
- `TickHandFeed()` — **S69 NUEVO** acercarse a jugador, dudar si tímido, comer, consumir
- `TickAlways(dt)` — decay de necesidades cada frame (solo si spawned)

**Interacción:**
- `BeginPetSession()` — **S69 NUEVO** fachada pública: entra petting dentro de Reacting
- `EndPetSession()` — **S69 NUEVO** fachada pública: cancela petting
- `Interact()` — **S69 ELIMINADO** (reemplazado por BeginPetSession)
- `ReleaseStation()` — libera reserva de necesidad (llamado al cambiar de estado)
- `EnterRoaming()` — transición forzada a roaming (switchboard público)

**State internals:**
- `activeReaction` (ProximityReaction) — tipo de reacción en curso
- `reservedStation` (NeedStation) — estación ocupada
- `idleTimer, idleDuration, reactingTimer, reactCooldownTimer, pettingDisplayTimer` — temporizadores
- `stateBeforeReact` — estado previo para restaurar post-reacción
- `petting, petTimer, petEmoteTimer` — **S69 NUEVO** para sesión interactiva
- `feedCooldownTimer, feedHesitateTimer, feedEatTimer, feedHesitated, feedEating` — **S69 NUEVO** para HandFeed

**Métodos privados de lógica:**
- `TryEnterNeedSeeking() → bool` — intenta buscar estación crítica
- `TryEnterHandFeed() → bool` — **S69 NUEVO** gate: hotbar + health + distance
- `TryGetCriticalNeed(out NeedType) → bool` — prioridad Health > Energy > Affect
- `ReactIfPlayerNear() → bool` — arranca reacción si player a rango y personality lo permite
- `BeginReaction()` — transición a Reacting
- `NextRoamDestination() → Vector3` — destino aleatorio o con preferencia de área; **S64: aplica AdjustRoamForAvoidance() para empujar el punto lejos de Monchis que se deben evitar**
- `TryGetPreferredPoint(out Vector3)` — muestra punto en área preferida por rol
- `IsPlayerFacingMe() → bool` — **S69** dot product de player.forward · to-creature (xz), rango `petRadius`, cone `petLookAngle`
- `BeginPetSession()` — **S69 NUEVO** core: entra petting, resetea timers
- `EndPetSession()` — **S69 NUEVO** core: cancela petting, resetea timers
- `TickPetting()` — **S69 NUEVO** core: suma Affect, emote, verifica condiciones
- `TickHandFeed()` — **S69 NUEVO** core: acercarse, dudar, comer, consumir
- `TryConsumeActiveFood()` — **S69 NUEVO** pedir a HotbarController para consumir ítem

## Cambios S69

### Petting hold-E (sesión interactiva)

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
internal bool IsBeingPetted => pettingDisplayTimer > 0f;
internal bool CanBePetted => ctx.State == AgentState.Reacting && activeReaction != ProximityReaction.Flee && IsPlayerFacingMe();
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

**Knobs en MoriMochiAgent.Tuning.Needs:**
- `petAffectPerSecond` float (default 2) — Affect/s base mientras petting
- `petRampPerSecond` float (default 0.1) — amplificación lineal (t*this) que crece conforme el petting dura
- `petMaxDuration` float (default 30) — segundos máximos de sesión antes de auto-terminar
- `petEmoteInterval` float (default 2) — segundos entre emotes de Corazon

---

### HandFeed state (comida de la mano)

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
- Gate 1: `HotbarController.IsOfferingFood` (ítem activo, categoría Food)
- Gate 2: `ctx.Dna.Needs.Health < owner.feedHungerThreshold` (hambriento)
- Gate 3: `ctx.PlanarDistanceToPlayer() <= owner.feedNoticeRadius` (cerca)
- Si todos: `ctx.State = AgentState.HandFeed`, reset timers, return true

**TickHandFeed() state machine:**
1. **Approach:** Navegar a feedDistance del jugador (default 1.5m)
2. **Hesitate (si tímido):** Si `DNA.Sociability < owner.feedShyBelow` (default 0.4), esperar en `feedShyDistance` (default 3m) durante `feedHesitateSeconds` (default 5s). Si se acerca más, abortar
3. **Eat:** Una vez a feedDistance, comenzar a comer durante `feedEatSeconds` (default 3s)
4. **Consume:** Llamar `HotbarController.TryConsumeActiveFood()` → quita ítem de hotbar, aplica `feedHealthBoost` (default 30) + `feedAffectBoost` (default 5), resetear estado HandFeed
5. **Cooldown:** `feedCooldown` (default 10s) antes de permitir HandFeed nuevamente

**Intent en TickHandFeed():**
- Pre-eat: `CreatureIntent.SeekingFood`
- During eat: `CreatureIntent.Eating`

**Knobs en MoriMochiAgent.Tuning.Needs:**
- `feedNoticeRadius` float (default 3) — detecta comida a esta distancia
- `feedDistance` float (default 1.5) — distancia final de comida (parado frente jugador)
- `feedShyBelow` float (default 0.4) — Sociability threshold: si < esto, criatura tímida duda
- `feedShyDistance` float (default 3) — a esta distancia, criatura tímida se frena con dudas
- `feedHesitateSeconds` float (default 5) — cuántos segundos duda antes de acercarse (si tímida)
- `feedEatSeconds` float (default 3) — cuántos segundos tarda en comer
- `feedHungerThreshold` float (default 40) — Health máxima para aceptar HandFeed (si < esto, hambriento)
- `feedHealthBoost` float (default 30) — Health que recupera al comer
- `feedAffectBoost` float (default 5) — Affect bonus al comer
- `feedCooldown` float (default 10) — segundos antes de poder pedir HandFeed de nuevo

---

## Notas S64

- NextRoamDestination() llama a `owner.AdjustRoamForAvoidance(candidate)` (via MoriMochiAgent.AdjustRoamForAvoidance → AgentSocial.AdjustRoamForAvoidance) para filtrar puntos rechazados por reglas Avoid sociales
- El filtro es barato (one-pass) y sin recursión: si el punto ajustado sigue siendo malo, el próximo tick roam lo empuja de nuevo
- La lógica de Avoid NO afecta el pathfinding del NavMesh (que está blind a reglas sociales), solo el punto de destino elegido

## Notas S69

- Petting sucede DENTRO del estado Reacting (amistosa), no cambia agredir/evitar lógica
- Petting termina por release E, timeout, o si jugador se va (not facing)
- HandFeed es prioridad MÁS BAJA que necesidades críticas, cede a miedo/cortejo
- Petting + HandFeed son ortogonales: puede estar en reacting (petting) y cambiar a HandFeed si `TryEnterHandFeed()` gate abre
- Intent mapea HandFeed a SeekingFood (pre-eat) o Eating, usado por NameTag
- Sociability modula HandFeed: tímido (Sociability<0.4) duda antes de comer; aventurero come rápido

## Impacto Gameplay (S69)

**Petting:**
- Nueva mecánica: jugador presiona/suelta E mientras está facing a criatura amable para sesión interactiva
- Affect gain por petting, con bonificación por duración (incentiva sesiones largas)
- Emotes visuales (Corazon)

**HandFeed:**
- Nueva mecánica: criatura hambriento se acerca a comida en mano del jugador
- Sociability modula confianza (tímido duda, aventurero come rápido)
- Afecto + salud sin costo de equipo/inversión
- Forma de entrenar criaturas tímidas

## Vinculado a

[[Index/06 - Player & World]], [[Index/02 - Genetics & Breeding]], [[MoriMonchiVault/Index/14 - Social V2]]

## Conexiones

[[MoriMochiAgent]], [[AgentContext]], [[AgentPhysics]], [[AgentSocial]], [[RoleWorldProfileSO]], [[NeedStationRegistry]], [[NeedStation]], [[HotbarController]], [[PlayerController]], [[CreatureDNA]], [[SocialTuningSO]]
