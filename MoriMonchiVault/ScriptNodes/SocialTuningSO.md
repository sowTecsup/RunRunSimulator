---
tags: [script, data, social, tuning, so]
---

# SocialTuningSO.cs

**Ruta:** `Data/Social/SocialTuningSO.cs`

**Responsabilidad:** Asset Odin de ajustes globales para el sistema social V2/V3: parámetros de percepción (radio, throttling), afinidad (bonuses de elemento/parentesco/rol), economía de juego (energía consumida, afecto ganado, cooldowns), interacciones (persecución, acercamiento, evitación, siesta, pelea) e historial dinámico. Singleton (Current) resuelto en OnEnable. Un único asset se carga en escena; todos los agentes lo consultan en tiempo de ejecución. S67: agregados thresholds de afinidad para filtrado visual en tab Relaciones del detail. **S69:** Nuevos campos de diales genéticos (V3): `SociabilityAffinityShift`, `SociabilityCooldownScale`, `BoldnessFightShift`, `BoldnessAvoidShift` + métodos estáticos `DialShift()` y `ScaledSocialCooldown()`.

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

### Dormir juntos (S65)
- `MaxEnergyToSleep` — energía máxima de AMBOS para aceptar dormir juntos (default 45)
- `SleepDuration` — duración total de siesta compartida en segundos (default 12)
- `SleepEnergyPerSecond` — energía recuperada por segundo mientras duermen (default 4)
- `SleepAffectBoost` — afecto otorgado a ambos al completar siesta (default 5)
- `SleepStopDistance` — distancia de detención al acercarse a punto de dormir (default 1)

### Pelea de gremlins (S65)
- `FightDuration` — duración total de pelea de juego en segundos (default 6)
- `FightAffectLoss` — afecto perdido por ambos al terminar pelea (default 4)
- `FightLungeInterval` — cada cuántos segundos un peleador se abalanza (default 0.8)
- `FightStopDistance` — distancia a la que frena cada abalanzada (default 1)
- `FightKnockForce` — fuerza del empujón final al separarse (default 5)

### Historia/SocialGraph (S65)
- `PlayAffinityGain` — afinidad ganada por par al completar juego de persecución (default 0.06)
- `SleepAffinityGain` — afinidad ganada por par al dormir juntos (default 0.08)
- `FightAffinityLoss` — afinidad perdida por par al pelear (default 0.1, aplicado como negativo)
- `HistoryDeltaClamp` — tope absoluto del delta de historia acumulado por par (default 0.5, clamp ±0.5)

### Pestaña Relaciones (S67)
- `RelationsFriendThreshold` — afinidad mínima PARA QUE un MoriMochi aparezca en la lista "Le caen bien" del visualizador (default 0.25)
- `RelationsFoeThreshold` — afinidad máxima PARA QUE un MoriMochi aparezca en la lista "Le caen mal" (default 0.05)

### Diales genéticos (S69 - V3)

| Campo | Tipo | Propósito |
|-------|------|----------|
| `SociabilityAffinityShift` | float | **S69** Cuánto desplaza la Sociability los umbrales de afinidad de Approach/PlayChase/SleepTogether. Sociable alto (p.ej. 0.8) = umbral más bajo (interactúa más). Tímido bajo (p.ej. 0.2) = umbral más alto (interactúa menos). Default 0.15 (15% de shift). Se usa via `DialShift(sociability, SociabilityAffinityShift)`. |
| `SociabilityCooldownScale` | float | **S69** Cuánto escala la Sociability el cooldown social (SocialCooldown). Sociable alto = menos espera entre interacciones (cooldown más corto). Tímido = más espera. Default 0.4 (40% de escala). Se usa via `ScaledSocialCooldown(sociability)`. Rango [0, 1]. |
| `BoldnessFightShift` | float | **S69** Cuánto desplaza la Osadía el umbral de la pelea de gremlins. Osado alto (p.ej. 0.8) = pelea con menos motivo (umbral más bajo). Tímido (p.ej. 0.2) = rechaza pelear. Default 0.15. Se usa via `DialShift(boldness, BoldnessFightShift)`. |
| `BoldnessAvoidShift` | float | **S69** Cuánto desplaza la Osadía el umbral de evitación. Osado alto = evita menos (umbral más bajo, acepta acercarse a enemigas). Tímido = evita más agresivamente. Default 0.15. Se usa via `DialShift(boldness, BoldnessAvoidShift)`. |

**Métodos (S69):**
- `static DialShift(float dial, float shift) → float` — Transforma dial [0..1] a desviación simétrica centrada en 0.5. Fórmula: `(Mathf.Clamp01(dial) - 0.5f) * 2f * shift`. Ejemplo: dial=0.8, shift=0.15 → (0.8−0.5)×2×0.15 = +0.09. dial=0.2 → (0.2−0.5)×2×0.15 = −0.09.
- `ScaledSocialCooldown(float sociability) → float` — Retorna cooldown escalado por Sociability. Fórmula: `SocialCooldown * Mathf.Lerp(1f + SociabilityCooldownScale, 1f - SociabilityCooldownScale, Mathf.Clamp01(sociability))`. Ejemplo: Sociable 0.8 → cooldown más corto (menos espera). Tímido 0.2 → cooldown más largo.

**Métodos antiguos:**
- `GetRoleBias(Role) → float` — lee RoleSocialBias con fallback a 0 si falta key

## Cambios S69

**Nuevos campos de diales genéticos (V3):**

```csharp
[Title("Diales genéticos (V3)")]
[Tooltip("Cuánto desplaza la Sociabilidad los umbrales de afinidad...")]
public float SociabilityAffinityShift = 0.15f;

[Tooltip("Cuánto escala la Sociabilidad el cooldown social...")]
[Range(0f, 1f)]
public float SociabilityCooldownScale = 0.4f;

[Tooltip("Cuánto desplaza la Osadía el umbral de la pelea de gremlins...")]
public float BoldnessFightShift = 0.15f;

[Tooltip("Cuánto desplaza la Osadía el umbral de evitación...")]
public float BoldnessAvoidShift = 0.15f;
```

**Nuevos métodos estáticos:**

```csharp
public static float DialShift(float dial, float shift) 
    => (Mathf.Clamp01(dial) - 0.5f) * 2f * shift;

public float ScaledSocialCooldown(float sociability) =>
    SocialCooldown * Mathf.Lerp(
        1f + SociabilityCooldownScale, 
        1f - SociabilityCooldownScale, 
        Mathf.Clamp01(sociability));
```

**Impacto en ReactionRuleBase (S69):**
- `ApproachFriendRule.Matches()` → `minAff = MinAffinity - DialShift(self.DNA.Sociability, tuning.SociabilityAffinityShift)`
- `PlayChaseRule.Matches()` → `minAff = MinAffinity - DialShift(self.DNA.Sociability, tuning.SociabilityAffinityShift)`
- `SleepTogetherRule.Matches()` → `minAff = MinAffinity - DialShift(self.DNA.Sociability, tuning.SociabilityAffinityShift)`
- `AvoidDislikedRule.Matches()` → `maxAff = MaxAffinity - DialShift(self.DNA.Boldness, tuning.BoldnessAvoidShift)` (osado evita MENOS)
- `GremlinFightRule.Matches()` → `maxAff = MaxAffinity + DialShift(self.DNA.Boldness, tuning.BoldnessFightShift)` (osado pelea MÁS)

**Impacto en AgentSocial (S69):**
- `AgentSocial.End()` → `t.ScaledSocialCooldown(ctx.Dna.Sociability)` en vez de `SocialCooldown` plano

**Interpretación:**
- Sociability alta (0.8) = umbrales de afinidad BAJAN (interactúa con monchis menos afines) + cooldowns CORTOS (quiere jugar frecuentemente)
- Sociability baja (0.2) = umbrales de afinidad SUBEN (solo con buenos amigos) + cooldowns LARGOS (prefiere soledad)
- Boldness alta (0.8) = pelea incluso con baja afinidad + evita menos (acepta acercarse a enemigos)
- Boldness baja (0.2) = rechaza pelea + evita más agresivamente (huye de enemigas)

## Notas

- Asset único por escena; cualquier cambio afecta TODOS los agentes globalmente
- Los parámetros de afinidad de semilla (element/kinship/role/chemistry) NO se mutaran; SocialGraphService solo suma deltas
- S69: DialShift() transforma dial 0..1 a cambio simétrico alrededor de 0 (neutral en 0.5)
- S69: ScaledSocialCooldown() usa Lerp entre 1+Scale e 1-Scale, invirtiendo el parámetro de Sociability (Clamp01 primero para sanidad)
- S65: SocialGraph persiste SOLO local (social_graph_<playerId>.json); sync a cloud es futuro
- MaxEnergyToSleep es gate: si ALGUNO tiene energía > este valor, rechazan la invitación de dormir
- PlayAffinityGain y SleepAffinityGain son positivos; FightAffinityLoss se guarda como −t.FightAffinityLoss
- HistoryDeltaClamp limita acumulación por par (±0.5 default); afinidad final también se clampea a [−1, 1]
- S67: RelationsFriendThreshold y RelationsFoeThreshold son thresholds de **visualización** únicamente (no afectan gameplay)

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/02 - Genetics & Breeding]]
- [[MoriMonchiVault/Index/14 - Social V2]]
- [[DetailRelationsPresenter]]

## Conexiones

**Entrada:**
- `GameManager` — referencia única al asset en escena
- `SocialTuningSO.Current` — todos los agentes consultan via static en `Start()/Tick()`

**Salida:**
- `AgentSenses.Tick()` — consulta ScanInterval, PerceptionRadius, MaxPercepts, GetRoleBias
- `AgentSocial.TryEngage()`, `TickSocializing()` — consulta duraciones, energía, cooldowns de cada modo
- `AgentSocial.End()` — consulta `ScaledSocialCooldown(sociability)` (S69)
- `ReactionRuleBase` subclases — consultan `DialShift()` para umbrales efectivos (S69)
- `SocialGraphService.RecordInteraction()` — consulta PlayAffinityGain, SleepAffinityGain, FightAffinityLoss, HistoryDeltaClamp
- `SocialGraphService.EffectiveAffinity()` — usado para calcular afinidad vista por gameplay
- `DetailRelationsPresenter.Rebuild()` — consulta RelationsFriendThreshold y RelationsFoeThreshold para filtrar monchis en tab Relaciones (S67)
