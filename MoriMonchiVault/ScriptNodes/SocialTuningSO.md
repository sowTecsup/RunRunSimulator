---
tags: [script, data, social, tuning, so]
---

# SocialTuningSO.cs

**Ruta:** `Data/Social/SocialTuningSO.cs`

**Responsabilidad:** Asset Odin de ajustes globales para el sistema social V2: parámetros de percepción (radio, throttling), afinidad (bonuses de elemento/parentesco/rol), economía de juego (energía consumida, afecto ganado, cooldowns), interacciones (persecución, acercamiento, evitación, siesta, pelea) e historial dinámico. Singleton (Current) resuelto en OnEnable. Un único asset se carga en escena; todos los agentes lo consultan en tiempo de ejecución.

**Secciones y campos:**

### Percepción
- `ScanIntervalMin/Max` — segundos entre escaneos de percepción (range throttled, default 2–4s)
- `PerceptionRadius` — radio en el que un agente ve otros MoriMonchis (default 6m)
- `MaxPercepts` — máxima cantidad de percepts procesadas por escaneo (default 8, cheapshot para perf)

### Afinidad (semilla estática)
- `SameElementBonus` — bonus si comparten Element (default 0.35)
- `KinshipBonus` — bonus si hay parentesco directo (padre/madre/hermanos, default 0.4)
- `PairChemistrySpread` — amplitud de "química de par" determinista por hash (default 0.25, seed del SocialGraph V2)
- `RoleSocialBias` [Dict Role→float] — sesgo por Role del percibidor (default: Protector=0, Agresivo=-0.15, Empatico=+0.25)

### Economía social
- `SocialAffectBoost` — Affect otorgado al completar interacción positiva (default 8)
- `ChaseEnergyPerSecond` — energía consumida/s durante persecución (default 0.6)
- `MinEnergyToPlay` — energía mínima para iniciar juego social (default 30)

### Persecución de juego
- `ChaseDuration` — duración total de una persecución (default 8s)
- `ChaseSwapFraction` — fracción de duración antes de intercambiar roles (default 0.5, a los 4s)
- `SocialCooldown` — cooldown post-juego con mismo par (default 25s)
- `ChaseFleeStep` — distancia del paso de huida (runner fugaz, default 4m)
- `ChaseRepath` — cada cuántos segundos recalcular camino durante persecución (default 0.25s)

### Acercamiento
- `ApproachDuration` — duración del acercamiento social (default 4s)
- `ApproachStopDistance` — distancia de detención al acercarse (default 1.2m)

### Evitación
- `AvoidClearance` — distancia mínima para mantener al evitar (default 2.5m)

### Dormir juntos (S65 NUEVO)
- `MaxEnergyToSleep` — energía máxima de AMBOS para aceptar dormir juntos (default 45)
- `SleepDuration` — duración total de siesta compartida en segundos (default 12)
- `SleepEnergyPerSecond` — energía recuperada por segundo mientras duermen (default 4)
- `SleepAffectBoost` — afecto otorgado a ambos al completar siesta (default 5)
- `SleepStopDistance` — distancia de detención al acercarse a punto de dormir (default 1)

### Pelea de gremlins (S65 NUEVO)
- `FightDuration` — duración total de pelea de juego en segundos (default 6)
- `FightAffectLoss` — afecto perdido por ambos al terminar pelea (default 4)
- `FightLungeInterval` — cada cuántos segundos un peleador se abalanza (default 0.8)
- `FightStopDistance` — distancia a la que frena cada abalanzada (default 1)
- `FightKnockForce` — fuerza del empujón final al separarse (default 5)

### Historia/SocialGraph (S65 NUEVO)
- `PlayAffinityGain` — afinidad ganada por par al completar juego de persecución (default 0.06)
- `SleepAffinityGain` — afinidad ganada por par al dormir juntos (default 0.08)
- `FightAffinityLoss` — afinidad perdida por par al pelear (default 0.1, aplicado como negativo)
- `HistoryDeltaClamp` — tope absoluto del delta de historia acumulado por par (default 0.5, clamp ±0.5)

**Métodos:**
- `GetRoleBias(Role) → float` — lee RoleSocialBias con fallback a 0 si falta key

**Notas**
- Asset único por escena; cualquier cambio afecta TODOS los agentes globalmente
- Los parámetros de afinidad de semilla (element/kinship/role/chemistry) NO se mutaran; SocialGraphService solo suma deltas
- S65: SocialGraph persiste SOLO local (social_graph_<playerId>.json); sync a cloud es futuro
- MaxEnergyToSleep es gate: si ALGUNO tiene energía > este valor, rechazan la invitación de dormir
- PlayAffinityGain y SleepAffinityGain son positivos; FightAffinityLoss se guarda como −t.FightAffinityLoss
- HistoryDeltaClamp limita acumulación por par (±0.5 default); afinidad final también se clampea a [−1, 1]

## Vinculado a

- [[Index/06 - Player & World]]
- [[MoriMonchiVault/Index/14 - Social V2]]

## Conexiones

**Entrada:**
- `GameManager` — referencia única al asset en escena
- `SocialTuningSO.Current` — todos los agentes consultan via static en `Start()/Tick()`

**Salida:**
- `AgentSenses.Tick()` — consulta ScanInterval, PerceptionRadius, MaxPercepts, GetRoleBias
- `AgentSocial.TryEngage()`, `TickSocializing()` — consulta duraciones, energía, cooldowns de cada modo
- `SocialGraphService.RecordInteraction()` — consulta PlayAffinityGain, SleepAffinityGain, FightAffinityLoss, HistoryDeltaClamp
- `ReactionRuleBase` subclases — consultan GetRoleBias para scoring
