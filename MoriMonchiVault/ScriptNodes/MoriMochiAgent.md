---
tags: [script, world, ai]
---

# MoriMochiAgent

**Ruta:** `World/AI/MoriMochiAgent.cs`

**Responsabilidad:** Cerebro IA de criatura viva. FSM (Idle, Roaming, Reacting, Carried, Thrown, Recovering, SeekingNeed, UsingStation, Courting). Personality-driven via `PersonalityProfileSO`. Decae necesidades cada frame, busca `NeedStation` cuando crítico. Implementa `IThrowable` (agarrar/lanzar/knock con física peluche: bounce, spin) e `IInteractable` (E acariciar). Confinement (pen/courtship). NavMesh confinado; sobrevive rebake. **Método `Rebind(dna, profileTable)`** re-vincula sin resetear NavMesh (reloads rápidos). **Pestaña Stats en .Tuning.cs (S32):** Muestra 6 stats Base → Final (CON/ATK/SPD/DEF/LCK/EVA) con delta de equipo, resuelto via `CombatStats.GetEffectiveStats()` + `EquipmentStats.Apply()`.

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

## Organización (partial class)

| Archivo | Responsabilidad |
|---------|-----------------|
| `MoriMochiAgent.cs` | Núcleo, lifecycle, dispatch, NavMesh helpers, gizmos |
| `MoriMochiAgent.Brain.cs` | Estados, needs, reacciones, intent |
| `MoriMochiAgent.Physics.cs` | Colisión, knock, throw, ragdoll, recovery |
| `MoriMochiAgent.Confinement.cs` | Pen, courtship, rebake, pooling |
| `MoriMochiAgent.Tuning.cs` | Campos Odin, readouts, dev buttons, **Stats tab (S32)** |

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

**Cambios S32:**
- `CombatService.GetEffectiveStats()` → `CombatStats.GetEffectiveStats()` (clase extraída)
- `CombatService.EffectiveStats` → `EffectiveStats` top-level

## Método Rebind

```csharp
public void Rebind(CreatureDNA newDna, CreatureLifeStageTableSO lifeStageTable)
{
    dna = newDna;
    personalityProfile = lifeStageTable.GetProfile(newDna.LifeStage);
    
    nameTag.Rebind(dna.CustomName);
    visualizer.Rebind(newDna, database);
    // NavMesh NO se resetea (fast reloads)
}
```

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/02 - Genetics & Breeding]]
- [[CreatureDNA]] — DNA viva
- [[PersonalityProfileSO]] — profile behavior
- [[NeedStationRegistry]] — busca estaciones
- [[CombatStats]] — calcula stats base (S32)
- [[EffectiveStats]] — struct stats (S32)
- [[EquipmentStats]] — aplica mods (S32)

## Conexiones

**Entrada:**
- `GameManager.MintRandomCreature()` → instancia via pooling
- `Rebind()` → reloads

**Salida:**
- NavMesh pathfinding
- `IThrowable`, `IInteractable` interfaces
- `GameEvents` (si hay acción importante)

## Notas (S32)

- **Stats display:** Usa `CombatStats.GetEffectiveStats()` + `EquipmentStats.Apply()` (clases extraídas).
- **Pestaña Stats:** Solo Play mode; muestra base (partes) → final (equipment).
