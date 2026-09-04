---
tags: [script, animation, behavior, mood]
---

# MonchiMoodDriver.cs

**Ruta:** `World/Creatures/MonchiMoodDriver.cs`

**Responsabilidad:** Driver de emociones por estado interno del agente. Tick desincronizado cada 2.5-5s (per-creature stagger para evitar sincronización visual). Lee Condition (Sick → Enfermo, InNeed → Triste) e Intent (Resting → Dormido, Eating → Feliz, Playing → Emocionado, Held/Fleeing → Asustado, Tumbling → Mareado, S64: Socializing/Chasing → Feliz/Emocionado, S65: SleepingTogether → Dormido/Fighting → Enojado, S97: Collecting → Emocionado, **S98-S99:** Taking → Feliz/Losing → Triste) del MoriMochiAgent, default mezcla 35% Feliz / 65% Neutral. No interfiere si DragonAnimationDriver.IsBusy (durante combate). Llama visualizer.SetMood() para cambiar la cara. Genera variedad visual orgánica sin afectar gameplay.

## Mapeo de Mood (ResolveMood) S98

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
  Taking → Feliz                (S98 NUEVO) — criatura está recolectando mineral
  Losing → Triste               (S99 NUEVO) — rival acaba de tomar mineral

Default (Idle, Wandering, etc.):
  35% Feliz, 65% Neutral
```

## Cambios S98

**Nuevos casos en ResolveMood:**
- `CreatureIntent.Taking` → `MonchiMood.Feliz` — criatura muestra alegría al agarrar/consumir mineral (celebración de éxito)
- `CreatureIntent.Losing` → `MonchiMood.Triste` — criatura muestra decepción al perder mineral a rival (beat de reacción)

**Notas:**
- Taking y Losing son intents discretos derivados de las 4 fases de AgentExpedition (S98-S99): Noticing → Moving → **Taking** → Losing
- Taking es breve (~1.2s), muestra éxito; Losing es un beat reactivo (~1s) que acompaña la deslusión
- Combinados con MonchiGestureDriver, ofrecen retroalimentación visual completa del beat de recolección

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

## Métodos Públicos

- `Update()` — tick principal: consulta tiempo desincronizado, si es hora resuelve mood y aplica
- `ResolveMood() → MonchiMood` — mapeo puro que devuelve mood según Condition + Intent actuales

## Campos

- `agent` (MoriMochiAgent, required) — para consultar Condition e Intent
- `visualizer` (MonchiVisualizer, required) — para SetMood
- `combatDriver` (DragonAnimationDriver, optional) — si busy → no tick
- `tickSeconds` (Vector2, default 2.5–5s) — rango de tiempo entre ticks
- `nextTick` (float) — próximo tiempo de tick (desincronizado en OnEnable)

## Invariantes S98

- **Lógica sin estado:** ResolveMood es función pura de (condition, intent) → mood; cambios de S98 son solo extensión del mapping.
- **Prioridad Condition > Intent:** Sick/InNeed siempre ganan; dentro de Intent, orden de evaluación es top-to-bottom.
- **Default aleatorio:** si no hay match (intent desconocido), mezcla 35% Feliz / 65% Neutral usando Random.value; evita puro Neutral monótono.
- **Beat timing:** Taking/Losing duran solo ~1-1.2s cada uno; al volver a Collecting o Wandering, mood cambia; no persiste tristeza artificial.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

**Entrada:**
- `MoriMochiAgent.Intent` — verbo actual (fuente de verdad). **S98-S99:** incluye Taking, Losing
- `MoriMochiAgent.Condition` — bienestar derivado (Healthy/InNeed/Sick)
- `DragonAnimationDriver.IsBusy` — flag: en combate ahora
- `MonchiVisualizer` — target del mood

**Salida:**
- `MonchiVisualizer.SetMood()` — aplica face material según mood

**S98 Integration:**
- [[AgentExpedition]] (popula Taking/Losing intents)
- [[ExpeditionRulesSO]] (beat timers que disparan transiciones)
- [[MonchiGestureDriver]] (sincronizado en paralelo con Taking/Losing)
- [[ArenaCueOverlay]] (colorea rutas según intent incluyendo Taking/Losing)
