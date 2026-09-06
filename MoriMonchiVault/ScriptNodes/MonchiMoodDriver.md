---
tags: [script, animation, behavior, mood]
---

# MonchiMoodDriver.cs

**Ruta:** `World/Creatures/MonchiMoodDriver.cs`

**Responsabilidad:** Driver de emociones por estado interno del agente. Tick desincronizado cada 2.5-5s. Lee Condition (Sick → Enfermo, InNeed → Triste) e Intent (Resting → Dormido, Eating → Feliz, Playing → Emocionado, Held/Fleeing → Asustado, Tumbling → Mareado, S98-S99: Taking → Feliz/Losing → Triste, **S100:** Clashing → Enojado/Dazed → Mareado, **S101:** Carrying/Securing → Emocionado/Guarding → Neutral/Hunting → Enojado/Taunting → Enojado), default mezcla 35% Feliz / 65% Neutral. Llama visualizer.SetMood() para cambiar cara.

## Mapeo de Mood (ResolveMood) S101

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
  Socializing → Feliz           (S64)
  SleepingTogether → Dormido    (S65)
  Fighting → Enojado            (S65)
  Collecting → Emocionado       (S97)
  Taking → Feliz                (S98)
  Losing → Triste               (S99)
  Carrying → Emocionado         (S101 NUEVO) — cargando material, progreso
  Securing → Feliz              (S101 NUEVO) — depositando, conclusión exitosa
  Clashing → Enojado            (S100)
  Dazed → Mareado               (S100)
  Guarding → Neutral            (S101 NUEVO) — vigilancia tranquila
  Hunting → Enojado             (S101 NUEVO) — persiguiendo rival, agresión
  Taunting → Enojado            (S101 NUEVO) — provocando rival, intensidad

Default (Idle, Wandering, etc.):
  35% Feliz, 65% Neutral
```

## Cambios S101: Ocupaciones

**Nuevos casos en ResolveMood:**
- `CreatureIntent.Carrying` → `MonchiMood.Emocionado` — cargando material (entre Gathering y Securing), progreso emocional
- `CreatureIntent.Securing` → `MonchiMood.Feliz` — depositando material en salida, conclusión exitosa (similar a Taking)
- `CreatureIntent.Guarding` → `MonchiMood.Neutral` — vigilancia estática, compostura profesional
- `CreatureIntent.Hunting` → `MonchiMood.Enojado` — persiguiendo rival, agresión (similar a Fighting)
- `CreatureIntent.Taunting` → `MonchiMood.Enojado` — provocando rival, intensidad (similar a Hunting/Fighting)

**Notas:**
- Carrying/Securing forman progresión: Collecting (emoción inicial) → Carrying (progreso) → Securing (conclusión) → todos positivos o neutros
- Guarding es defensivo/tranquilo (Neutral); Hunting/Taunting agresivos (Enojado)
- Integración con MonchiGestureDriver (si gesto "Stand" para Guarding, cara Neutral hace coherencia visual)

**Líneas de código (ResolveMood, S101 tentativo):**
```csharp
case CreatureIntent.Carrying: return MonchiMood.Emocionado;
case CreatureIntent.Securing: return MonchiMood.Feliz;
case CreatureIntent.Guarding: return MonchiMood.Neutral;
case CreatureIntent.Hunting: return MonchiMood.Enojado;
case CreatureIntent.Taunting: return MonchiMood.Enojado;
```

## Cambios S100: Combate Físico

**Nuevos casos en ResolveMood:**
- `CreatureIntent.Clashing` → `MonchiMood.Enojado` — combatiendo
- `CreatureIntent.Dazed` → `MonchiMood.Mareado` — post-golpe

## Cambios S98-S99

**Nuevos casos en ResolveMood:**
- `CreatureIntent.Taking` → `MonchiMood.Feliz`
- `CreatureIntent.Losing` → `MonchiMood.Triste`

## Métodos Públicos

- `Update()` — tick principal: consulta tiempo desincronizado, resuelve mood y aplica
- `ResolveMood() → MonchiMood` — mapeo puro

## Campos

- `agent` (MoriMochiAgent, required) — para consultar Condition e Intent
- `visualizer` (MonchiVisualizer, required) — para SetMood
- `combatDriver` (DragonAnimationDriver, optional) — si busy → no tick
- `tickSeconds` (Vector2, default 2.5–5s) — rango de tiempo entre ticks
- `nextTick` (float) — próximo tiempo de tick

## Invariantes S101 + S100 + S98

- **Lógica sin estado:** ResolveMood es función pura de (condition, intent) → mood
- **Prioridad Condition > Intent:** Sick/InNeed siempre ganan
- **Default aleatorio:** si no hay match, 35% Feliz / 65% Neutral
- **Ocupaciones unificadas:** Guarding neutral (defensa tranquila), Hunting/Taunting enojados (agresión coordinada)

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

**Entrada:**
- `MoriMochiAgent.Intent` (S101 nuevos: Carrying, Securing, Guarding, Hunting, Taunting)
- `MoriMochiAgent.Condition`
- `DragonAnimationDriver.IsBusy`

**Salida:**
- `MonchiVisualizer.SetMood()`

**S101 Integration:**
- [[AgentExpedition]] (popula nuevos intents)
- [[ExpeditionRulesSO]] (beat timers)
- [[Occupation]] (Guard, Break, Decoy)
- [[MonchiGestureDriver]] (sincronizado en paralelo)
- [[ArenaCueOverlay]] (colorea rutas)
- [[ArenaCameraDirector]] (enfoca si interesante)
