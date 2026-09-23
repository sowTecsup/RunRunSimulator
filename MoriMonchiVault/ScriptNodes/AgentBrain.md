---
tags: [script, world, agent, internal, brain, fsm]
---

# AgentBrain.cs

**Ruta:** `World/AI/AgentBrain.cs`

**Responsabilidad:** Orquestación de máquina de estados NavMesh (comportamiento autónomo del MoriMochiAgent). Tick per-frame decay de necesidades (Health/Energy/Affect), búsqueda de estaciones, interacción con jugador, HandFeed, Intent expuesta. S131: Decay escalado por `GameClock.NeedsTimeScale` (0 si paused/no loaded, 1x normal play).

## Propiedades Consultables

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `IsBeingPetted` | bool | True mientras `pettingDisplayTimer > 0` |
| `IsInFriendlyReaction` | bool | En Reacting pero no fleeing |
| `CanBePetted` | bool | Reacting + friendly + player facing |
| `Intent` | `CreatureIntent` | Mapping de estado → intención |

## TickAlways (Decay S131)

**Cambio en decaimiento de necesidades:**

```csharp
private float healthDecayPerGameMinute = 0.07f;   // [FormerlySerializedAs]
private float energyDecayPerGameMinute = 0.1f;    // [FormerlySerializedAs]
private float affectDecayPerGameMinute  = 0.1f;   // [FormerlySerializedAs]

void TickAlways(float dt)
{
    if (!agent.IsSpawned) return;
    
    float gameMinutes = dt * GameClock.Instance?.NeedsTimeScale ?? 0f;
    
    dna.Needs.Health -= healthDecayPerGameMinute * gameMinutes;
    dna.Needs.Energy -= energyDecayPerGameMinute * gameMinutes;
    dna.Needs.Affect -= affectDecayPerGameMinute * gameMinutes;
    
    // Clamp to [0, 1]
}
```

**NeedsTimeScale behavior:**
- Normal: 1.0 × gameMinutesPerRealSecond
- Paused: 0 (no decay)
- Not loaded: 0 (no decay)

## Cambios S131

**Nuevo campo (refactorizado):**
- Campos `*DecayPerGameMinute` ahora con `[FormerlySerializedAs("oldName")]` (backward compat)
- Default: health=0.07, energy=0.1, affect=0.1

**Flujo:**
1. TickAlways recibe deltaTime (real seconds)
2. Multiplica por `GameClock.NeedsTimeScale` → game minutes
3. Decrementa necesidades proporcionalmente

**Escalado:**
```
actualDecay = decayPerGameMinute * (deltaTime * GameClock.NeedsTimeScale)
```

## Métodos Privados (FSM States)

| Método | Descripción |
|--------|-------------|
| `TickIdle()` | Espera, intenta HandFeed, intenta seeking, intenta reacción, roaming |
| `TickRoaming()` | Navega, intenta HandFeed, intenta seeking, reacción, continúa |
| `TickReacting()` | Si petting: TickPetting. Sino: continúa reacción, seeking/handFeed si crítica |
| `TickSeekingNeed()` | Avanza hacia estación |
| `TickUsingStation()` | Consume hasta llenar |
| `TickHandFeed()` | Acercarse, dudar, comer, consumir |
| `TickAlways(dt)` | Decay de necesidades (escalado por NeedsTimeScale) |

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `BeginPetSession()` | Entra petting dentro de Reacting |
| `EndPetSession()` | Cancela petting |
| `ReleaseStation()` | Libera reserva de necesidad |
| `EnterRoaming()` | Transición a roaming (S98: sin tocar Agent.speed) |

## Vinculado a

- [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]
- [[Index/09 - Active Context]]

## Conexiones

**Data:**
- [[CreatureDNA]] — necesidades mutable
- [[CreatureIntent]] — mapeo de estado

**Sistemas:**
- [[GameClock]] — proporciona NeedsTimeScale
- [[NeedStation]] — reserva/consumo
- [[MoriMochiAgent]] — componente padre
- [[AgentExpedition]] — Expedition state

## Notas (S131 HC-4)

- **NeedsTimeScale:** 0 cuando GameClock no loaded o Paused; 1x cuando playing normal.
- **Decay campos:** renombrados con FormerlySerializedAs para backward compat (valores en prefab: 0.07/0.1/0.1).
- **Game minutes:** `dt * NeedsTimeScale` convierte real seconds a game minutes (escalado por calendario).
- **Clamp:** necesidades siempre en [0, 1] (aplicado al final de TickAlways).
