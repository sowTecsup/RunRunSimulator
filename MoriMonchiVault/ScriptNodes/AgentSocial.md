---
tags: [script, world, agent, internal, social]
---

# AgentSocial.cs

**Ruta:** `World/AI/AgentSocial.cs`

**Responsabilidad:** Colaborador interno de la composición del agente (espejo de AgentConfinement.courtship). Lee ctx.Percepts (escrito por AgentSenses) contra la lista polimórfica ReactionRuleBase del RoleWorldProfile para decidir acercarse, evitar, invitar a juego de persecución, dormir juntos o pelear. **S99 NUEVO:** Filtra percepto por `ExpeditionTeams.AreRivals()` — rivales NO pueden socializarse. Luego posee el estado Socializing end-to-end. El handshake de persecución/siesta/pelea refleja EnterCourtship: el iniciador pregunta TryJoinSocialPlay/TryJoinSocialSleep/TryJoinSocialFight (fachada de MoriMochiAgent → internos TryJoinSocialPlay/TryJoinSleep/TryJoinFight) al objetivo y solo procede si acepta. Una vez ambos dentro, NO hay más cross-calls — cada lado detecta pasivamente si el compañero salió del juego, consultando partner.IsSocializing cada tick. **S65:** Nuevos modos Sleeping (busca RestZone vía NeedStationRegistry, regen 4/s, +5 Affect) y Fighting (abalanzadas, −4 Affect ambos, knock final sin estrés). **S69:** El método `End()` ahora usa `t.ScaledSocialCooldown(ctx.Dna.Sociability)` en vez de `SocialCooldown` plano, permitiendo que Sociability escale el tiempo de espera entre interacciones. **S97:** Propiedad `Partner` expuesta como `internal` para lectura por `ArenaCueOverlay.DrawSocial()`. Tickeado por MoriMochiAgent.Update cuando el estado es Socializing.

## Campos internos

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

## Propiedades Públicas (fachada interna)

- **S97:** `Partner → MoriMochiAgent` — acceso read-only al compañero social actual (null si None). Expuesto internamente para que `ArenaCueOverlay.DrawSocial()` y `MoriMochiAgent.SocialPartner` lo lean.

## Filtro de Rivales S99

```csharp
// En TryEngage(), antes de evaluar reglas:
for (int i = 0; i < ctx.Percepts.Count; i++)
{
    var p = ctx.Percepts[i];
    if (ExpeditionTeams.AreRivals(owner.Team, p.Team)) continue;  // S99 NUEVO
    // Solo evaluar si NO son rivales
    for (int j = 0; j < rules.Count; j++) { ... }
}
```

**Significado:** Si `owner.Team == Player` y `p.Team == Rival`, se skippea el percepto. Rivales nunca inician ni aceptan interacciones sociales (Approach, PlayChase, SleepTogether, Fight).

## Métodos privados clave (S93)

- `CanPair(SocialTuningSO t, MoriMochiAgent initiator) → bool` — validación compartida por todos los handshakes (cooldown, estado libre, sin container, sin breeding, DNA válido)
- `Enter(SocialMode newMode, MoriMochiAgent newPartner, float newDuration, EmoteKind emote)` — setup de transición: limpia timers, entra Socializing, emite emote
- `TargetOf(in Percept p) → MoriMochiAgent` — extrae agente del percept (helper estático)

## Métodos públicos

- `TryEngage() → bool` — intenta iniciar interacción social: busca en Percepts la mejor regla coincidente, **S99:** filtra rivales, elige Approach/PlayChase/SleepTogether/Fight. Llamado por MoriMochiAgent.Update solo si el estado del cerebro quedó en Idle/Roaming este frame (las necesidades y reacciones nunca se interrumpen)
- `TryJoinSocialPlay(MoriMochiAgent initiator) → bool` — lado receptor del handshake de PlayChase: valida disponibilidad y energía independientemente. Ambos lados usan energía durante persecución, se intercambian roles a mitad de duración
- `TryJoinSleep(MoriMochiAgent initiator, NeedStation station, Vector3 fallbackSpot) → bool` — **S65 NUEVO** lado receptor de invitación de siesta: valida energía ≤ MaxEnergyToSleep y no-Sick; intenta reservar su propio slot en la MISMA estación del iniciador, si no puede duerme junto al fallbackSpot.
- `TryJoinFight(MoriMochiAgent initiator) → bool` — **S65 NUEVO** lado receptor de invitación de pelea: mismas validaciones que TryJoinSocialPlay (Healthy + Energy ≥ MinEnergyToPlay). Ambos se abalanzan mutuamente durante FightDuration.
- `TickSocializing() → void` — tick cuando el estado es Socializing: mueve hacia compañero (Approach), lo persigue (Chaser), huye (Runner), duerme juntos (Sleeping), se abalanza (Fighting). Termina por timeout o si el compañero se fue. Genera emoción EmitEmote y bonus de Affect al completar. Registra interacción en SocialGraphService.
- `End() → void` — **S69 MODIFICADO:** limpia partner, emite emote de despedida, resetea cooldown usando `t.ScaledSocialCooldown(ctx.Dna.Sociability)` (Sociability modula espera entre interacciones).

## Invariantes S99

- **Aislamiento de rivales:** Percepts donde `ExpeditionTeams.AreRivals(owner.Team, p.Team)` se saltan en TryEngage(). Rivales nunca se tocan socialmente.
- **Aliados solo:** solo percepto con `Team == ExpeditionTeam.None` (neutral) o `Team == owner.Team` (aliado) pueden iniciar social.
- **Handshake mutuo:** iniciador → receiver. Ambos deben cumplir CanPair() y requisitos específicos. Sin reciprocidad = falla silenciosa.
- **Registra en SocialGraph:** cada interacción completa registra delta en SocialGraphService (usado por S65 afinidad dinámica).

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]], [[Index/14 - Social V2]]

## Conexiones

- [[MoriMochiAgent]] (owner, tickeado por Update)
- [[AgentContext]] (lee ctx.Percepts, ctx.Profile.Reactions, mutea ctx.State)
- [[Perceivable]] (lee Kind, **S99:** lee Team)
- [[Percept]] (lee Source, Kind, **S99:** lee Team)
- **S99:** [[ExpeditionTeam]], [[ExpeditionTeams]] (filtro de rivales)
- [[SocialTuningSO]] (tuning: duraciones, energía, cooldown)
- [[RoleWorldProfileSO]] (lista de ReactionRuleBase por rol)
- [[ReactionRuleBase]] (evaluador de score)
- [[SocialGraphService]] (registra interacciones, **S65:** EffectiveAffinity)
- [[NeedStationRegistry]] (busca RestZone para dormir)
- [[ArenaCueOverlay]] (accede a Partner para dibujar enlaces sociales)
