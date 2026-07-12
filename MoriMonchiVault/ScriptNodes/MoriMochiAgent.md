---
tags: [script, world, ai, partial]
---

# MoriMochiAgent

**Ruta:** `World/AI/MoriMochiAgent.cs` (partial class, deuda activa)

**Responsabilidad:** Cerebro IA de criatura viva. FSM (Idle, Roaming, Reacting, Carried, Thrown, Recovering, SeekingNeed, UsingStation, Courting). **Role-driven via `RoleWorldProfileSO`** (S39 cambio: antes `PersonalityProfileSO`). Decae necesidades cada frame, busca `NeedStation` cuando crítico. Implementa `IThrowable` (agarrar/lanzar/knock con física peluche: bounce, spin) e `IInteractable` (E acariciar). Confinement (pen/courtship). NavMesh confinado; sobrevive rebake. **Método `Initialize(dna, profileTable, player)`** resuelve perfil vía `dna.Role` (S39). **Método `Rebind(dna, profileTable)`** re-vincula sin resetear NavMesh (reloads rápidos).

## Máquina de Estados

| Estado | Descripción |
|--------|-------------|
| `Idle` | Esperando, sin actividad |
| `Roaming` | Navegación aleatoria |
| `Reacting` | Respuesta a evento (voice, hit) |
| `Carried` | Agarrado por jugador |
| `Thrown` | Lanzado en aire |
| `Recovering` | Post-lanzamiento, ragdoll → stand-up |
| `SeekingNeed` | Navegando a NeedStation |
| `UsingStation` | Usando estación (eat, sleep, play) |
| `Courting` | En cortejo (orbita/tienda hembra) |

## Organización (partial class — Deuda Activa S32)

| Archivo | Responsabilidad |
|---------|-----------------|
| `MoriMochiAgent.cs` | Núcleo, lifecycle, dispatch, NavMesh helpers, gizmos |
| `MoriMochiAgent.Brain.cs` | Estados, needs, reacciones, intent |
| `MoriMochiAgent.Physics.cs` | Colisión, knock, throw, ragdoll, recovery |
| `MoriMochiAgent.Confinement.cs` | Pen, courtship, rebake, pooling |
| `MoriMochiAgent.Tuning.cs` | Campos Odin, readouts, dev buttons, **Stats tab (S32)** |

## Método Initialize (S39 cambio)

```csharp
public void Initialize(CreatureDNA creature, RoleWorldProfileSO profileTable, Transform playerTransform)
{
    dna     = creature;
    profile = profileTable?.GetProfile(creature.Role)  // S39: creature.Role (no Personality)
              ?? RoleWorldProfile.Neutral();
    player  = playerTransform;

    RestoreNavMeshControl();
    if (nameTag != null) nameTag.Bind(creature, this);
    
    // NavMesh masks setup...
    EnterRoaming();
}
```

**Cambio S39:** 
- Antes: `profileTable?.GetProfile(creature.Personality)`
- Ahora: `profileTable?.GetProfile(creature.Role)` — RoleWorldProfileSO, no PersonalityProfileSO
- Retorna `RoleWorldProfile` que contiene modifiers de comportamiento (speed preferences, reaction biases, etc. para el mundo)

## Método Rebind (S39 cambio)

```csharp
public void Rebind(CreatureDNA newDna, RoleWorldProfileSO profileTable)
{
    dna = newDna;
    profile = profileTable?.GetProfile(newDna.Role)  // S39: newDna.Role
              ?? RoleWorldProfile.Neutral();
    
    if (nameTag != null) nameTag.Rebind(newDna);  // actualiza display
    if (visualizer != null) visualizer.Rebind(newDna, database);
    // NavMesh NO se resetea (fast reloads)
}
```

**Cambio S39:** 
- Antes: `GetProfile(newDna.Personality)`
- Ahora: `GetProfile(newDna.Role)`

## Campos Inyectados (Initialize/Rebind)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `dna` | `CreatureDNA` | DNA viva de la criatura |
| `profile` | `RoleWorldProfile` | Perfil de rol (modifiers de comportamiento del mundo, S39) |
| `player` | `Transform` | Ref al jugador (para orient/react/pet) |

## Pestaña Stats en .Tuning.cs (S32)

Muestra para cada stat una línea `Base → Final (delta)`:

```csharp
private EffectiveStats StatsBase() =>
    database != null ? CombatStats.GetEffectiveStats(dna, database)
                     : new EffectiveStats(...);

private EffectiveStats StatsFinal() =>
    database != null && equipDb != null
        ? EquipmentStats.Apply(StatsBase(), dna, equipDb)
        : StatsBase();
```

**Stats display:** Usa `CombatStats.GetEffectiveStats()` (clase extraída S32) + `EquipmentStats.Apply()` para mostrar deltas de equipo. Solo en Play mode.

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `DNA` | `CreatureDNA` | Acceso read-only a dna |
| `Intent` | `CreatureIntent` | Intent actual (para NameTag) |
| `IsAlive` | `bool` | `!dna.IsDead && state != Thrown + grace timeout` |
| `IsPenned` | `bool` | Confinado (location != "") |
| `IsForSale` | `bool` | Occupant de StoreContainer |
| `IsBeingPetted` | `bool` | Recibiendo input E en frame |
| `IsPlayerFacingMe()` | `bool` | Jugador forward · to-me (XZ plane, angle < petLookAngle) |
| `IsInFriendlyReaction` | `bool` | Reaccionando amistosamente (permite "[E] Acariciar") |
| `IsAirborne` | `bool` | Rigidbody dynamic + velocity > 0 (mid-throw/ragdoll) |

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/02 - Genetics & Breeding]]
- [[CreatureDNA]] — DNA viva
- [[RoleWorldProfileSO]] — profile mundo (S39, reemplaza PersonalityProfileSO)
- [[RoleWorldProfile]] — struct perfil comportamiento
- [[NeedStationRegistry]] — busca estaciones
- [[CombatStats]] — calcula stats base (S32)
- [[EffectiveStats]] — struct stats (S32)
- [[EquipmentStats]] — aplica mods (S32)
- [[MoriMonchiVisualizer]] — assembly visual
- [[NameTag]] — label world-space
- [[GameEvents]] — NavMesh rebake events

## Conexiones

**Entrada:**
- `Initialize(dna, profileTable, player)` — wiring inicial (MoriMochiSpawner)
- `Rebind(dna, profileTable)` — reload rápido (MoriMochiSpawner, OnRegistryReloaded)
- `GameEvents.OnNavMeshWillRebake/Rebaked` — reacciona a rebakes de furniture
- Estados actualizados por TakeTurn / Launch / Grab, etc.

**Salida:**
- NavMesh pathfinding + movement
- `IThrowable`, `IInteractable` implementations
- Cambios en `dna.NeedState` durante gameplay (presisten vía RegistryChanged)

## Notas (S32 + S39)

- **Partial class:** Deuda activa (Fase 6-9, refactor a componentes pequeños, Index/11 hoja de ruta).
- **S32 Stats display:** Usa `CombatStats.GetEffectiveStats()` + `EquipmentStats.Apply()` (clases extraídas).
- **S39 Role-based:** Comportamiento del mundo (speed, reaction biases) ahora data-driven vía RoleWorldProfile (separación de Role de combate vs Role de comportamiento).
- **ProfileTable fallback:** Si profileTable == null, usa `RoleWorldProfile.Neutral()` (valores defaults).
- **NavMesh survival:** Rebind preserva NavMeshAgent state; OnNavMeshRebaked hace detach + reattach via gameEvents.
