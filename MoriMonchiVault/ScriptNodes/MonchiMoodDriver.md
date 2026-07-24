---
tags: [script, animation, behavior, mood]
---

# MonchiMoodDriver.cs

**Ruta:** `World/Creatures/MonchiMoodDriver.cs`

**Responsabilidad:** Driver de emociones por estado interno del agente. Tick desincronizado cada 2.5-5s (per-creature stagger para evitar sincronización visual). Lee Condition (Sick → Enfermo, InNeed → Triste) e Intent (Resting → Dormido, Eating → Feliz, Playing → Emocionado, Held/Fleeing → Asustado, Tumbling → Mareado, S64: Socializing/Chasing → Feliz/Emocionado, **S65:** SleepingTogether → Dormido, Fighting → Enojado) del MoriMochiAgent, default mezcla 35% Feliz / 65% Neutral. No interfiere si DragonAnimationDriver.IsBusy (durante combate). Llama visualizer.SetMood() para cambiar la cara. Genera variedad visual orgánica sin afectar gameplay.

## Campos

- `agent` — MoriMochiAgent (requerido, para consultar Condition e Intent)
- `visualizer` — MonchiVisualizer (requerido, para SetMood)
- `combatDriver` — DragonAnimationDriver (opcional, si busy → no tick)
- `tickSeconds` — rango de tiempo entre ticks (default 2.5–5s)
- `nextTick` — próximo tiempo de tick (desincronizado en OnEnable)

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

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/10 - Visualization]]
- [[MoriMonchiVault/Index/14 - Social V2]] (S65 nuevos intents)

## Conexiones

**Entrada:**
- `MoriMochiAgent.Intent` — verbo actual (fuente de verdad)
- `MoriMochiAgent.Condition` — bienestar derivado (Healthy/InNeed/Sick)
- `DragonAnimationDriver.IsBusy` — flag: en combate ahora
- `MonchiVisualizer` — target del mood

**Salida:**
- `MonchiVisualizer.SetMood()` — aplica face material según mood
