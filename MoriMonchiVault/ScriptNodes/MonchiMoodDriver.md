---
tags: [script, world, animation, mood, creatures]
---

# MonchiMoodDriver.cs

**Ruta:** `World/Creatures/MonchiMoodDriver.cs`

**Responsabilidad:** Driver de emociones por estado interno. Tick desincronizado cada 2.5-5s. Resuelve mood de Condition (Sick, InNeed) e Intent (S97+ ocupaciones, S100 combate, **S103 exploración**). Mapeo puro: no hay estado. Llama visualizer.SetMood() para cambiar expresión facial.

**Mapeo ResolveMood() (S103 ACTUALIZADO):**

**Condition (prioridad alta):**
- Sick → Enfermo
- InNeed → Triste

**Intent (ocupaciones + expedición S103):**
- Resting, SleepingTogether → Dormido
- Eating, Taking, Socializing, Securing → Feliz
- Playing, Collecting, Carrying → Emocionado
- **Exploring → Neutral** — scout investiga tranquilo (S103 NUEVO)
- **Reporting → Emocionado** — scout reporta descubrimiento (S103 NUEVO)
- Held, Fleeing → Asustado
- Tumbling, Dazed → Mareado
- Chasing, Clashing → Emocionado
- Losing → Triste
- Guarding → Neutral
- Hunting, Taunting, Fighting → Enojado

**Default (Idle, Wandering):**
- 35% Feliz, 65% Neutral

**S103 Cambios:**
- Agrega casos Exploring y Reporting en ResolveMood()
- Exploring → Neutral (exploración tranquila, investigación)
- Reporting → Emocionado (reporte exitoso de veta, celebración)

**Métodos Públicos:**
- `Update()` — tick throttled, resuelve + aplica SetMood()
- `ResolveMood() → MonchiMood` — mapeo puro (condition, intent) → mood

**Campos:**
- `agent` [Required] — para Condition, Intent
- `visualizer` [Required] — para SetMood()
- `combatDriver` (optional) — si busy, no tick
- `tickSeconds` (Vector2) = (2.5, 5) — rango de throttle

**Invariantes:**
- Función pura ResolveMood
- Prioridad Condition > Intent
- Default aleatorio
- S103: Exploring neutral (calma), Reporting emocionado (logro)

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMochiAgent]], [[MonchiVisualizer]], [[AgentExpedition]], [[AgentScout]], [[CreatureIntent]], [[CreatureCondition]]
