---
tags: [enum, creature, core, expedition]
---

# CreatureEnums.cs

**Ruta:** `Core/Enums/CreatureEnums.cs`

**Responsabilidad:** Enumeraciones centrales de comportamiento y estado. CreatureGender, LifeStage, MonchiMood, Tier, BusyReason, NeedType, CreatureCondition, CreatureIntent (S103: 30 valores), ProximityReaction, EmoteKind, SocialInteractionKind. **S103:** Agrega `CreatureIntent.Exploring = 28` (scout navega) y `CreatureIntent.Reporting = 29` (scout reporta veta).

**Enumeraciones:**

| Enum | Valores |
|------|---------|
| CreatureGender | Unknown, Male, Female |
| LifeStage | Newborn, Child, Teen, Adult, Elder |
| MonchiMood | Neutral, Feliz, Triste, Dolor, Enojado, Dormido, Enfermo, Mareado, Asustado, Amoroso, Emocionado, KO |
| Tier | Tier1, Tier2, Tier3 |
| BusyReason | None, Breeding, Sold |
| NeedType | Health, Energy, Affect |
| CreatureCondition | Healthy, InNeed, Sick |
| **CreatureIntent** | **S103: 30 valores** (Idle 0 - Reporting 29) |
| ProximityReaction | Ignore, Flee, Approach, Follow, Retreat |
| EmoteKind | Curioso, Feliz, Jugando, Molesto, Corazon, Zzz |
| SocialInteractionKind | PlayChase, SleepTogether, GremlinFight |

**CreatureIntent S103 (30 valores):**
```
Idle = 0
Wandering = 1
Following = 2
Approaching = 3
Fleeing = 4
Retreating = 5
SeekingFood = 6
SeekingRest = 7
SeekingPlay = 8
Eating = 9
Resting = 10
Playing = 11
Held = 12
Tumbling = 13
Socializing = 14
Chasing = 15
SleepingTogether = 16
Fighting = 17
Collecting = 18        (S97: busca material)
Taking = 19            (S98: minando)
Losing = 20            (S99: rival toma su mineral)
Clashing = 21          (S100: combate físico)
Dazed = 22             (S100: post-golpe)
Carrying = 23          (S101: cargando material)
Securing = 24          (S101: depositando)
Guarding = 25          (S101: vigilando)
Hunting = 26           (S101: persiguiendo rival)
Taunting = 27          (S101: provocando rival)
Exploring = 28         (S103: scout navega a veta NUEVO)
Reporting = 29         (S103: scout reporta veta NUEVO)
```

**S103 Cambios:**

**Exploring = 28** — scout viajando a veta descubierta
- Generado por: AgentScout.Step=Traveling
- Gesto: locomotion normal (no mapeado)
- Mood: Neutral (exploración tranquila)
- Color Cue: verde azulado (0.55, 0.9, 0.6)
- Duración: hasta arribo

**Reporting = 29** — scout reportando veta al pizarrón
- Generado por: AgentScout.Step=Reporting
- Gesto: "Yes" (celebración reporte, S103)
- Mood: Emocionado (descubrimiento exitoso)
- Color Cue: amarillo-verde (0.75, 1, 0.45)
- Duración: ReportSeconds

**Ocupación Explore → Fases → Intent:**
- TryEngage → Exploring (via scout.TryEngage)
- Traveling → Exploring
- Reporting → Reporting + EmitEmote(Curioso/Feliz) + gesto "Yes"

**Mapeo Ocupación Explore (S103):**
```
Explore:
  - Exploring (navegando a sitio reportado por pizarrón)
  - Reporting (stand + reporte al pizarrón + emote)
```

**Integración con Drivers S103:**

**MonchiMoodDriver:**
- Exploring → Neutral (calma exploratoria)
- Reporting → Emocionado (logro de reporte)

**MonchiGestureSetSO:**
- Reporting → "Yes" (celebración)
- Exploring → unmapped (locomotion)

**CueStyleSO:**
- Exploring color: (0.55, 0.9, 0.6)
- Reporting color: (0.75, 1, 0.45)

**Invariantes S103:**
- Ocupación Explore ≠ Intent Exploring: Ocupación asignada al spawn, Intent es acción viva
- Scout usa Exploring/Reporting, no Collect/Take/Carry/Secure
- Exploring delegado a AgentScout (colaborador de AgentExpedition)
- Reporting incluye emisión de reporte a pizarrón + counter de reportes

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[AgentScout]], [[AgentExpedition]], [[TeamBlackboard]], [[MonchiMoodDriver]], [[MonchiGestureSetSO]], [[CueStyleSO]], [[CreatureDNA]], [[MoriMochiAgent]]
