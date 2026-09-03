---
tags: [script, animation, behavior, mood]
---

# MonchiMoodDriver.cs

**Ruta:** `World/Creatures/MonchiMoodDriver.cs`

**Responsabilidad:** Driver de emociones por estado interno del agente. Tick desincronizado cada 2.5-5s (per-creature stagger para evitar sincronización visual). Lee Condition (Sick → Enfermo, InNeed → Triste) e Intent (Resting → Dormido, Eating → Feliz, Playing → Emocionado, Held/Fleeing → Asustado, Tumbling → Mareado, S64: Socializing/Chasing → Feliz/Emocionado, **S65:** SleepingTogether → Dormido, Fighting → Enojado, **S97:** Collecting → Emocionado) del MoriMochiAgent, default mezcla 35% Feliz / 65% Neutral. No interfiere si DragonAnimationDriver.IsBusy (durante combate). Llama visualizer.SetMood() para cambiar la cara. Genera variedad visual orgánica sin afectar gameplay.

## Campos

- `agent` (MoriMochiAgent, required) — para consultar Condition e Intent
- `visualizer` (MonchiVisualizer, required) — para SetMood
- `combatDriver` (DragonAnimationDriver, optional) — si busy → no tick
- `tickSeconds` (float range, default 2.5–5s) — rango de tiempo entre ticks
- `nextTick` (float) — próximo tiempo de tick (desincronizado en OnEnable)

## Mapeo de Mood (ResolveMood)

```csharp
Condition (prioridad alta):
  Sick → Enfermo
  InNeed → Triste

Intent (prioridad media):
  Resting → Dormido
  Eating → Feliz
  Playing → Emocionado
  Held → Asustado
  Tumbling → Mareado
  Fleeing → Asustado
  Chasing → Emocionado
  Socializing → Feliz        (S64 NUEVO)
  SleepingTogether → Dormido (S65 NUEVO)
  Fighting → Enojado         (S65 NUEVO)
  Collecting → Emocionado    (S97 NUEVO)

Default (Idle, Wandering, etc.):
  35% Feliz, 65% Neutral
```

## Cambios S65

**Nuevos casos en ResolveMood:**
- `CreatureIntent.SleepingTogether` → `MonchiMood.Dormido` — misma cara que Resting (ojos cerrados)
- `CreatureIntent.Fighting` → `MonchiMood.Enojado` — cara de enojo (ceño fruncido)

**Notas:**
- SleepingTogether y Fighting son comportamientos nuevos de AgentSocial (S65), así que ResolveMood debe tenerlos para que el mood no se resetee a Neutral
- No interfieren con combatDriver; si el agente está en combate, DragonAnimationDriver es autoridad (combatDriver.IsBusy retorna true)

## Cambios S97

**Nuevo caso en ResolveMood:**
- `CreatureIntent.Collecting` → `MonchiMood.Emocionado` — cara de emoción (ojos brillantes, sonrisa)

**Notas:**
- Collecting es nuevo estado de AgentExpedition (S97), activo cuando persigue material recolectable
- Emocionado es mood compartido con Playing (S64) y Chasing (S64), creando cohesión visual en actividades "emocionantes"
- No interfiere con combate; si combatDriver.IsBusy, se mantiene autoridad de DragonAnimationDriver

## Métodos Públicos

- `Update()` — tick principal: consulta tiempo desincronizado, si es hora resuelve mood y aplica
- `ResolveMood(CreatureCondition condition, CreatureIntent intent) → MonchiMood` — mapeo puro (static-equivalent) que devuelve mood según entrada

## Invariantes S97

- **Lógica sin estado:** ResolveMood es función pura de (condition, intent) → mood; cambios de S97 son solo extensión del mapping.
- **Prioridad Condition > Intent:** Sick/InNeed siempre ganan; dentro de Intent, orden de evaluación es top-to-bottom.
- **Default aleatorio:** si no hay match (intent desconocido), mezcla 35% Feliz / 65% Neutral usando Random.value; evita puro Neutral monótono.

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/10 - Visualization]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S97: Collecting mood)
- [[MoriMonchiVault/Index/14 - Social V2]] (S65 nuevos intents)

## Conexiones

**Entrada:**
- `MoriMochiAgent.Intent` — verbo actual (fuente de verdad). **S97:** incluye `Collecting`
- `MoriMochiAgent.Condition` — bienestar derivado (Healthy/InNeed/Sick)
- `DragonAnimationDriver.IsBusy` — flag: en combate ahora
- `MonchiVisualizer` — target del mood

**Salida:**
- `MonchiVisualizer.SetMood()` — aplica face material según mood
