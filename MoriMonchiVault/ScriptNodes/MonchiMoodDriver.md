---
tags: [script, animation, behavior, mood]
---

# MonchiMoodDriver.cs

**Ruta:** `World/Creatures/MonchiMoodDriver.cs`

**Responsabilidad:** Driver de emociones por estado interno del agente. Tick desincronizado cada 2.5-5s (per-creature stagger para evitar sincronización visual). Lee Condition (Sick → Enfermo, InNeed → Triste) e Intent (Resting → Dormido, Eating → Feliz, Playing → Emocionado, Held/Fleeing → Asustado, Tumbling → Mareado, S64: Socializing/Chasing → Feliz/Emocionado, S65: SleepingTogether → Dormido/Fighting → Enojado, S97: Collecting → Emocionado, S98-S99: Taking → Feliz/Losing → Triste, **S100:** Clashing → Enojado/Dazed → Mareado) del MoriMochiAgent, default mezcla 35% Feliz / 65% Neutral. No interfiere si DragonAnimationDriver.IsBusy (durante combate). Llama visualizer.SetMood() para cambiar la cara. Genera variedad visual orgánica sin afectar gameplay.

## Mapeo de Mood (ResolveMood) S100

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
  Taking → Feliz                (S98) — criatura está recolectando mineral
  Losing → Triste               (S99) — rival acaba de tomar mineral
  Clashing → Enojado            (S100 NUEVO) — combatiendo
  Dazed → Mareado               (S100 NUEVO) — post-golpe mareado

Default (Idle, Wandering, etc.):
  35% Feliz, 65% Neutral
```

## Cambios S100: Combate Físico

**Nuevos casos en ResolveMood:**
- `CreatureIntent.Clashing` → `MonchiMood.Enojado` — criatura muestra enojo durante choque (cara de combate)
- `CreatureIntent.Dazed` → `MonchiMood.Mareado` — criatura muestra aturdimiento post-golpe (espirales, desorientación)

**Notas:**
- Clashing mapea a mismo mood que Fighting (S65), unificando el lenguaje visual del combate
- Dazed mapea a mismo mood que Tumbling (ragdoll), transmitiendo descontrol físico
- Ambos son breves: Clashing dura Anticipating+Striking+Resolving (~2-3s), Dazed dura tuning.DazedSeconds (~0.7s)
- Al transicionar a counter/retrete/roaming, mood cambia a nuevo intent

**Líneas de código (ResolveMood, S100):**
```csharp
case CreatureIntent.Clashing: return MonchiMood.Enojado;
case CreatureIntent.Dazed: return MonchiMood.Mareado;
```

## Cambios S98-S99

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

- `agent` (MoriMochiAgent, required) — para consultar Condition e Intent (**S100:** incluye Clashing/Dazed)
- `visualizer` (MonchiVisualizer, required) — para SetMood
- `combatDriver` (DragonAnimationDriver, optional) — si busy → no tick
- `tickSeconds` (Vector2, default 2.5–5s) — rango de tiempo entre ticks
- `nextTick` (float) — próximo tiempo de tick (desincronizado en OnEnable)

## Invariantes S100 + S98 + S65 + S97

- **Lógica sin estado:** ResolveMood es función pura de (condition, intent) → mood; cambios son solo extensión del mapping.
- **Prioridad Condition > Intent:** Sick/InNeed siempre ganan; dentro de Intent, orden de evaluación es top-to-bottom.
- **Default aleatorio:** si no hay match (intent desconocido), mezcla 35% Feliz / 65% Neutral usando Random.value; evita puro Neutral monótono.
- **Beat timing:** Taking/Losing/Clashing/Dazed duran solo ~0.7-1.2s cada uno; al volver a Collecting/Wandering/Roaming, mood cambia; no persiste tristeza/enojo artificial.
- **Unificación visual:** Clashing usa Enojado (igual que Fighting), Dazed usa Mareado (igual que Tumbling); mantiene lenguaje visual coherente.

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

**Entrada:**
- `MoriMochiAgent.Intent` — verbo actual (fuente de verdad). **S100:** incluye Clashing, Dazed
- `MoriMochiAgent.Condition` — bienestar derivado (Healthy/InNeed/Sick)
- `DragonAnimationDriver.IsBusy` — flag: en combate ahora
- `MonchiVisualizer` — target del mood

**Salida:**
- `MonchiVisualizer.SetMood()` — aplica face material según mood

**S100 Integration:**
- [[AgentClash]] (popula Clashing/Dazed intents vía clash.Intent)
- [[ClashTuningSO]] (beat timers: DazedSeconds, ResolveSeconds)
- [[MoriMochiAgent]] (delegación de Intent a clash)
- [[MonchiGestureDriver]] (sincronizado en paralelo con Clashing/Dazed)
- [[ArenaCueOverlay]] (colorea rutas según intent incluyendo Clashing/Dazed)
- [[ArenaCameraDirector]] (enfoca si Clashing/Dazed)

**S98-S99 Integration:**
- [[AgentExpedition]] (popula Taking/Losing intents)
- [[ExpeditionRulesSO]] (beat timers que disparan transiciones)
- [[MonchiGestureDriver]] (sincronizado en paralelo con Taking/Losing)
