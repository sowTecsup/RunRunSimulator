---
tags: [script, world, agent, internal, social]
---

# AgentSocial.cs

**Ruta:** `World/AI/AgentSocial.cs`

**Responsabilidad:** Colaborador interno de la composición del agente (espejo de AgentConfinement.courtship). Lee ctx.Percepts (escrito por AgentSenses) contra la lista polimórfica ReactionRuleBase del RoleWorldProfile para decidir acercarse, evitar, invitar a juego de persecución, dormir juntos o pelear. Luego posee el estado Socializing end-to-end. El handshake de persecución/siesta/pelea refleja EnterCourtship: el iniciador pregunta TryJoinSocialPlay/TryJoinSocialSleep/TryJoinSocialFight (fachada de MoriMochiAgent → internos TryJoinSocialPlay/TryJoinSleep/TryJoinFight) al objetivo y solo procede si acepta. Una vez ambos dentro, NO hay más cross-calls — cada lado detecta pasivamente si el compañero salió del juego, consultando partner.IsSocializing cada tick. **S65:** Nuevos modos Sleeping (busca RestZone vía NeedStationRegistry, regen 4/s, +5 Affect) y Fighting (abalanzadas, −4 Affect ambos, knock final sin estrés). **S69:** El método `End()` ahora usa `t.ScaledSocialCooldown(ctx.Dna.Sociability)` en vez de `SocialCooldown` plano, permitiendo que Sociability escale el tiempo de espera entre interacciones. Tickeado por MoriMochiAgent.Update cuando el estado es Socializing.

**Campos internos:**
- `mode` — SocialMode enum (None/Approach/Chaser/Runner/Sleeping/Fighting, estado de la interacción)
- `partner` — MoriMochiAgent del compañero social (null si inactivo)
- `timer/duration` — temporizador y duración de la interacción actual
- `repathTimer` — throttling de repath durante persecución
- `cooldownUntil` — tiempo hasta poder reiniciar nueva interacción social
- `swapped` — bandera: si ya intercambiaron roles en la persecución
- `sleepStation` — NeedStation (RestZone) reservada para la siesta, null si duermen en el sitio
- `sleepSpot` — punto de dormir (slot de la estación o fallback punto medio/lateral)
- `lungeTimer` — temporizador de la próxima abalanzada en modo Fighting
- `emoteTimer` — temporizador de emotes periódicos (Zzz cada 3s / Molesto en pelea)

**Métodos privados clave (S93):**
- `CanPair(SocialTuningSO t, MoriMochiAgent initiator) → bool` — validación compartida por todos los handshakes (cooldown, estado libre, sin container, sin breeding, DNA válido)
- `Enter(SocialMode newMode, MoriMochiAgent newPartner, float newDuration, EmoteKind emote)` — setup de transición: limpia timers, entra Socializing, emite emote
- `TargetOf(in Percept p) → MoriMochiAgent` — extrae agente del percept (helper estático)

**Métodos públicos:**
- `TryEngage() → bool` — intenta iniciar interacción social: busca en Percepts la mejor regla coincidente, elige Approach/PlayChase/SleepTogether/Fight. Llamado por MoriMochiAgent.Update solo si el estado del cerebro quedó en Idle/Roaming este frame (las necesidades y reacciones nunca se interrumpen)
- `TryJoinSocialPlay(MoriMochiAgent initiator) → bool` — lado receptor del handshake de PlayChase: valida disponibilidad y energía independientemente. Ambos lados usan energía durante persecución, se intercambian roles a mitad de duración
- `TryJoinSleep(MoriMochiAgent initiator, NeedStation station, Vector3 fallbackSpot) → bool` — **S65 NUEVO** lado receptor de invitación de siesta: valida energía ≤ MaxEnergyToSleep y no-Sick; intenta reservar su propio slot en la MISMA estación del iniciador, si no puede duerme junto al fallbackSpot.
- `TryJoinFight(MoriMochiAgent initiator) → bool` — **S65 NUEVO** lado receptor de invitación de pelea: mismas validaciones que TryJoinSocialPlay (Healthy + Energy ≥ MinEnergyToPlay). Ambos se abalanzan mutuamente durante FightDuration.
- `TickSocializing() → void` — tick cuando el estado es Socializing: mueve hacia compañero (Approach), lo persigue (Chaser), huye (Runner), duerme juntos (Sleeping), se abalanza (Fighting). Termina por timeout o si el compañero se fue. Genera emoción EmitEmote y bonus de Affect al completar. Registra interacción en SocialGraphService.
- `End(bool completed, bool notifyPartner = true) → void` — **S69** Completa la interacción: registra delta de afinidad en SocialGraphService, asigna cooldown vía `t.ScaledSocialCooldown(ctx.Dna.Sociability)` (Sociable → cooldown corto, Tímido → cooldown largo).
- `CompleteFromPartner() → void` — **S65 ACTUALIZADO** notificación one-way: el compañero terminó el juego, sincroniza ambos lados para cobrar el reward juntos. SOLO el lado que notifica registra en SocialGraphService (evita doble delta).
- `AdjustRoamForAvoidance(Vector3) → Vector3` — filtro repulsivo barato: empuja un punto de roam alejado de Perceivables que coinciden reglas Avoid. Usado por AgentBrain.NextRoamDestination
- `ResetForReuse() → void` — pooling: restaura estado inicial
- `Intent → CreatureIntent` — intent actual (Chasing, Socializing, Wandering, SleepingTogether, Fighting) para NameTag
- `Describe() → string` — debug: "Chaser ↔ Monichitriste (3.5/8.0s)" o "Sleeping ↔ Monchifeliz (5.2/12.0s)" o "Fighting ↔ Monchi-unknown (2.1/6.0s)"

**Modos Internos (SocialMode enum):**
- `None` — inactivo
- `Approach` — acercándose al compañero, sin energía consumption
- `Chaser` — persiguiendo al compañero, consume energía
- `Runner` — siendo perseguido, consume energía
- `Sleeping` — **S65 NUEVO** durmiendo juntos en RestZone, regenera energía
- `Fighting` — **S65 NUEVO** peleando, consume energía como el chase, abalanzadas cada FightLungeInterval; −FightAffectLoss y knock final sin estrés al terminar

**Invariantes S93 (rescatados de comentarios):**
- El handshake de chase/sleep/fight es simétrico al cortejo: el iniciador pide `TryJoin*` al target y solo procede si acepta; después no hay más llamadas cruzadas — cada lado detecta que el otro se fue por `partner.IsSocializing` cada tick.
- `End(completed, notifyPartner)`: el cierre natural completa también el lado del partner en el mismo frame (los dos cobran el Affect); la historia en `SocialGraphService` se registra SOLO en el lado que notifica (primario) para evitar el doble registro por carrera de frames. El knock del final de una pelea lo aplica cada lado sobre sí mismo.
- `BeginSleep`: reserva la estación ANTES del handshake y la libera si el partner rechaza.

**Cambios S69:**

**Método `End()` con ScaledSocialCooldown:**
```csharp
cooldownUntil = Time.time + (t != null ? 
    t.ScaledSocialCooldown(ctx.Dna != null ? ctx.Dna.Sociability : 0.5f) 
    : 20f);
```

**Interpretación:**
- **Sociable alto (dial 0.8):** cooldown corto → quiere jugar nuevamente pronto
- **Sociable bajo (dial 0.2):** cooldown largo → prefiere esperar antes de siguiente interacción
- Si `ctx.Dna` es null, fallback a Sociability neutral 0.5

**Impacto:**
- Permite selección artificial: criar Sociable alto → agentes que interactúan más frecuentemente
- Interacciones más dinámicas: sociables generan chain reactions, tímidos son solitarios
- No cambia duración de la interacción en sí (ChaseDuration, SleepDuration, etc.), solo espera entre ellas

**Notas:**
- SocialMode es enum interno (None/Approach/Chaser/Runner/Sleeping/Fighting), no visible afuera
- La energía se gasta solo en Chaser/Runner (persecución activa), no en Approach
- En Sleeping, energía se GANA (+4/s default); Affect se gana al completar (+5 default)
- En Fighting, Affect se PIERDE (−4/s default); empujón final via AgentPhysics.Knock(force, stress=false) para evitar estrés doble
- El swap de roles en persecución es silencioso entre los dos — cada uno maneja su transición internamente sin cross-call
- Cuando completa una interacción vía timeout, notifica al compañero UNA SOLA VEZ para evitar doble-reward por race condition de ticks
- SocialGraphService.RecordInteraction() se llama en CompleteFromPartner SOLO por el lado notificador (semaforista)

## Interacciones S65

**PlayChase (S64→S65 sin cambios):**
- Duración: ChaseDuration (8s default)
- Energía: ChaseEnergyPerSecond (0.6/s) consumida por Chaser y Runner
- Swap: a ChaseSwapFraction (50% = 4s) intercambian roles
- Reward: +0.06 afinidad por par (SocialGraphService)
- Emote: Jugando (PlayChaseRule)

**SleepTogether (S65 NUEVO):**
- Duración: SleepDuration (12s default)
- Energía: −SleepEnergyPerSecond (4/s) por segundo, ambos regen
- Gate: Ambos MaxEnergyToSleep (45 default) — si uno supera, rechaza
- Destino: RestZone via NeedStationRegistry.TryReserve(), fallback duerme en sitio
- Reward: +0.08 afinidad por par + SleepAffectBoost (+5) a ambos
- Emote: Zzz

**GremlinFight (S65 NUEVO):**
- Duración: FightDuration (6s default)
- Energía: —
- Affect: −FightAffectLoss (4 default) a ambos al terminar
- Mecánica: Cada FightLungeInterval (0.8s) abalanzada; empujón final con AgentPhysics.Knock(FightKnockForce, stress=false)
- Reward: −0.1 afinidad por par (FightAffinityLoss aplicado como negativo)
- Emote: Molesto

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/02 - Genetics & Breeding]]
- [[MoriMonchiVault/Index/14 - Social V2]]

## Conexiones

**Entrada:**
- `AgentSenses.Percepts` — lista de entes perceptibles cada tick
- `RoleWorldProfileSO.Rules` — reglas de reacción por rol
- `SocialTuningSO` — parámetros de duración, energía, cooldowns, ScaledSocialCooldown (S69)
- `NeedStationRegistry` — para buscar RestZone en modo Sleeping

**Salida:**
- `AgentContext.State` — Socializing cuando activo
- `MoriMochiAgent.Intent` — CreatureIntent (Socializing, Chasing, SleepingTogether, Fighting)
- `AgentPhysics.Knock()` — empujón final en Fighting (stress=false)
- `SocialGraphService.RecordInteraction()` — registra al completar (solo notificador)
- `MoriMochiAgent.OnEmote` — emociones visuales
- `MoriMonchiMoodDriver` — Feliz/Zzz/Molesto según modo
