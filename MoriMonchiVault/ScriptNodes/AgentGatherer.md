---
tags: [script, world, ai, expedition, task]
---

# AgentGatherer.cs

**Ruta:** `World/AI/AgentGatherer.cs`

**Responsabilidad:** Colaborador de `AgentExpedition` (composición) que implementa `IExpeditionTask`. Maneja recolección de material: navega a sitio planeado o descubierto, detecta arribo, mina, carga, huye si hay rival sin custodio, vuelve a salida, deposita. Soporta orden Loot (Big/Small lode bias), Flee (huida con cadena a aliados/salida), Protect (custodio de aliados cercanos). S105: PlannedSite prefiere cristales caídos en Break/Decoy; MiningSeconds varía por tipo de drop. S109: lee `ctx.Stats.CarryCapacity` (dinámico por habilidades); OnKnocked respeta `ctx.Stats.KeepCarryOnKnock`. **S118:** notifica carga vía `owner.NotifyCharge()` al minar y asegurar, alimentando Super abilities del agente. Emite emotes (Curioso, Molesto, Feliz).

**Máquina de estados:**
- `Noticing` — espera antes de moverse a sitio
- `Moving` — navega hacia site con repath, detecta arribo por distancia o bloqueo
- `Mining` — extrae unidad por MiningSeconds (varía por tipo de material), repite hasta lleno o site agotado
- `Losing` — se detiene y orienta a último sitio tras perderlo
- `Returning` — navega a salida
- `Securing` — deposita carga en exit zone
- `Fleeing` — huye de rival durante FleeSeconds + FleeCooldown, luego retorna si tiene carga

**Propiedades internas:**
- `Carried` — unidades en poder (0 a CarryCapacity)
- `Collected` — total minado en sesión
- `Secured` — total depositado en salida
- `Fled` — conteo de huidas
- `Guardian` — aliado custodio detectado (null si sin custodio)
- `FleeCooldown01` — normalized [0,1] de cooldown post-huida
- `MiningProgress` — [0,1] avance de minado en fase Mining
- `CarryCapacity` — desde ctx.Stats (S109)

**Métodos público:**
- `bool TryEngage(ExpeditionRulesSO rules) → bool` — intenta comenzar recolección; retorna false si carga llena, sin site usable, ni reglas
- `bool Tick(ExpeditionRulesSO rules) → bool` — procesa frame; retorna false al terminar
- `void Cancel()` — aborta sin resetear elapsed
- `void ResetForReuse()` — limpia para pool recycle
- `void OnKnocked(ExpeditionRulesSO rules)` (S109 ACTUALIZADO) — suelta carga solo si `!ctx.Stats.KeepCarryOnKnock` (antes: siempre soltaba), aborta
- `Transform TargetTransform { get; }` — actual objetivo (target o exit según fase)
- `CreatureIntent Intent { get; }` — intención actual (Taking, Losing, Carrying, Securing, Fleeing, Collecting)

**PlannedSite (S105):**
- Si Occupation es Break O Decoy: primero busca `NearestDrop(ctx, DropPickupRadius)` (cristales caídos por recolectores en contacto)
  - Cristales se recogen rápido (DropPickupSecondsPerUnit 0.5f)
- Fallback: post inyectado → veta conocida → veta cercana

**MiningSeconds (S105):**
- Si target `IsDrop`: retorna `DropPickupSecondsPerUnit` (0.5f, más rápido)
- Si target `IsLode`: retorna `LodeMiningSecondsPerUnit` (2f)
- Else: retorna `MiningSecondsPerUnit` (4f, vetas)

**S109 Cambios:**

- `TryEngage()` (S109): ahora compara `carried >= ctx.Stats.CarryCapacity` (antes: carried >= Capacity(rules))
- Método auxiliar `Capacity(rules)` eliminado; delega a ctx.Stats
- `OnKnocked()` (S109): suelta carga solo si `!ctx.Stats.KeepCarryOnKnock`:
  - Habilidades pasivas pueden otorgar resistencia a golpes
  - Si KeepCarryOnKnock true, sigue cargando tras knock (ventaja defensiva)

**S118 Cambios (Notificación de Carga):**

- **Línea 237 (Fase Mining):** tras minar exitoso, `owner.NotifyCharge(AbilityChargeSource.Mined)` notifica a abilities (Super abilities acumulan carga)
- **Línea 392 (Método Secure):** tras depositar en salida, `owner.NotifyCharge(AbilityChargeSource.Secured)` notifica
- Integra loop: minado → carga Super → disparo automático tras delay

**Integración:**

- Llamado desde `AgentExpedition.TryEngage()` por defecto o si otra ocupación falla
- Tickeado en `AgentExpedition.TickExpedition()` si es activo
- Puede ser abortado por `PostureEngages` si hay rival y ocupación no es Gather
- S105: PostureEngages llama `hunter.TryHunt()` directamente, Gatherer cancela
- S109: Stats resueltos por AgentAbilities.Bind(), no cambian durante sesión (salvo SetOrders)
- S118: NotifyCharge() llama a `abilities.AddCharge(source)` vía MoriMochiAgent.NotifyCharge()

**Flujo Tick:**
1. Valida target usable; si no → Losing → false
2. Si Noticing/Moving y timeout → false
3. Chequea rival sin custodio → BeginFlee
4. Switch fase:
   - Noticing: countdown, luego → Moving
   - Moving: repath, detecta arribo, → Mining
   - Mining: extrae unidad cada MiningSeconds, **S118:** notifica Mined, repite o → Returning
   - Losing: countdown, luego → false
   - Returning: navega exit, detecta arribo, → Securing
   - Securing: countdown, luego **S118:** Secure() (notifica Secured) → false
   - Fleeing: navega FleePoint, countdown, luego Returning o → false

**Invariantes:**

- Capacity varía dinámicamente por ctx.Stats (Gather/Explore = 3 base; Break/Decoy = 2 base; habilidades pueden modificar)
- Drop no es lode (IsLode = false), se recoge rápido
- Huida activa CD (FleeCooldown) post-FleeSeconds
- MiningSeconds dinámico por tipo material (no precálculado)
- **KeepCarryOnKnock:** habilidad pasiva puede prevenir drop post-golpe (valor defensivo)
- **PlannedSite:** Break/Decoy priorizan cristales caídos (speed run)
- **S118:** Cada minado/asegurado notifica carga, alimentando Super abilities

**Vinculado a:** [[Index/23 - Arena Sandbox y Expedicion]], [[Index/22 - Bajada Nocturna y Linaje]], S118

**Conexiones:** [[IExpeditionTask]], [[AgentExpedition]], [[MoriMochiAgent]], [[AgentContext]], [[AgentAbilities]], [[ExpeditionNav]], [[TeamBlackboard]], [[ExpeditionRulesSO]], [[MaterialPickup]], [[ExpeditionStats]]
