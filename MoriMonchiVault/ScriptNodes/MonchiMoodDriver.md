---
tags: [script, world, animation, mood, creatures]
---

# MonchiMoodDriver.cs

**Ruta:** `World/Creatures/MonchiMoodDriver.cs`

**Responsabilidad:** Driver de emociones por estado interno. Tick desincronizado cada 2.5-5s. Resuelve mood de Condition (Sick, InNeed) e Intent (S97+ ocupaciones, S100 combate, S103 exploración). Mapeo puro: no hay estado. Llama visualizer.SetMood() para cambiar expresión facial. **S115:** Campo `knocked` detecta transición a/desde intent Dazed/Tumbling; al cambiar, fuerza `nextTick = Time.time` para resolver la cara Mareado al instante sin esperar bloqueo de `combatDriver.IsBusy` mientras está noqueado.

**Mapeo ResolveMood() (S103 ACTUALIZADO):**

**Condition (prioridad alta):**
- Sick → Enfermo
- InNeed → Triste

**Intent (ocupaciones + expedición S103):**
- Resting, SleepingTogether → Dormido
- Eating, Taking, Socializing, Securing → Feliz
- Playing, Collecting, Carrying → Emocionado
- Exploring → Neutral — scout investiga tranquilo (S103 NUEVO)
- Reporting → Emocionado — scout reporta descubrimiento (S103 NUEVO)
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

**S115 Cambios:**
- Campo `knocked` (bool) — detección de estado noqueado actual
- En Update(), calcula `knockedNow = agent.Intent == Dazed || agent.Intent == Tumbling`
- Si `knockedNow != knocked`: cambio de estado, fuerza `nextTick = Time.time` (tick inmediato)
- Lógica: cuando entra a Dazed/Tumbling o sale de ellos, la cara Mareado se resuelve al instante sin esperar el throttle de 2.5-5s

**Métodos Públicos:**
- `Update()` — tick throttled, detección knocked, resuelve + aplica SetMood()
- `ResolveMood() → MonchiMood` — mapeo puro (condition, intent) → mood

**Campos:**
- `agent` [Required] — para Condition, Intent
- `visualizer` [Required] — para SetMood()
- `combatDriver` (optional) — si busy, no tick
- `tickSeconds` (Vector2) = (2.5, 5) — rango de throttle
- `knocked` (bool) — **S115 NUEVO** estado actual noqueado (Dazed/Tumbling)

**Invariantes:**
- Función pura ResolveMood
- Prioridad Condition > Intent
- Default aleatorio
- S103: Exploring neutral (calma), Reporting emocionado (logro)
- **S115:** Cambio Dazed/Tumbling fuerza tick inmediato, sin esperar throttle

## Cambios S115

**Campos línea 14:**
```csharp
private bool knocked;
```
- Nuevo: bandera de estado noqueado

**Update() línea 21-28:**
```csharp
bool knockedNow = agent != null && (agent.Intent == CreatureIntent.Dazed || agent.Intent == CreatureIntent.Tumbling);
if (knockedNow != knocked)
{
    knocked = knockedNow;
    nextTick = Time.time;  // Fuerza tick inmediato
}
```
- Calcula si criatura está noqueada (Dazed O Tumbling)
- Si cambio: actualiza `knocked` y resetea `nextTick` para tick inmediato
- Contexto: cuando entra a Dazed, resuelve cara Mareado sin demora; cuando sale, resuelve cara siguiente sin demora

**Impacto S115:**
- Transiciones visuales de mood instantáneas en noqueado (no hay lag)
- El resto de mood transitions siguen el throttle de 2.5-5s
- Evita que `combatDriver.IsBusy` bloquee la resolución mientras está en Dazed

## Vinculado a

- [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

## Conexiones

- [[MoriMochiAgent]] — Intent, Condition
- [[MonchiVisualizer]] — SetMood()
- [[DragonAnimationDriver]] — combatDriver (optional check IsBusy)
- [[CreatureIntent]], [[CreatureCondition]]

