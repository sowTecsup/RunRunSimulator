---
tags: [script, world]
---

# MoriMochiAgent.cs

**Ruta:** `World/AI/MoriMochiAgent.cs`

## Responsabilidad

Cerebro IA de criatura viva. Máquina de estados (`Idle`, `Roaming`, `Reacting`, `Carried`, `Thrown`, `Recovering`, `SeekingNeed`, `UsingStation`, `Courting`). Personality-driven via `PersonalityProfileSO`. Decae necesidades cada frame, busca `NeedStation` cuando crítico. Implementa `IThrowable` (agarrar/lanzar/knock con física de peluche: bounce, bounceSpin, knockTransfer) e `IInteractable` (E para acariciar). Confinamiento en corral (`EnterConfinement`/`EnterCourtship`/`ExitCourtship`). Penned solo restringe acercarse al jugador (salta approach/follow, flee/retreat/roam siguen normales). `Knock` preserva `thrownTimer` si ya estaba en el aire (evita reinicio en cadenas de golpes). `BodyRadius` expone el radio planar del collider para que `BreedingContainer` calcule espaciado de cortejo. Sobrevive a rebake de NavMesh. **Método público `Rebind(dna, profileTable)`**: re-vincula DNA + profile SIN resetear navegación (para reloads rápidos). `RestoreNavMeshControl` ahora hace Warp anti-snap tras habilitar agente. **Pestaña Dev en .Tuning.cs**: readouts CurrentState/NavStatus + `forceRagdoll` (se queda ragdoll, nunca rejoina) + `logStateTransitions` (loguea transiciones). `TickThrown` respeta `forceRagdoll`. Gizmos de rangos. **Pestaña Stats en .Tuning.cs** (Sesión 26): muestra para cada stat (CON/ATK/SPD/DEF/LCK/EVA) un readout de dos líneas `Base → Final` con delta de bonificación de equipo, resuelto vía `CombatService.GetEffectiveStats()` (base con partes) + `EquipmentStats.Apply()` (aplicar equipo). Solo en Play mode.

## Cambios en S21 (Confinement)

- `RestoreNavMeshControl()` (pool reuse): ahora llama `currentContainer.DetachOccupant(this)` (silencioso) en vez de cualquier lógica de Release.
- `PrepareForPool()` (despawn): ahora llama `currentContainer.DetachOccupant(this)` (silencioso).
- `Release()` queda SOLO para `OnGrab` (el jugador levanta la criatura) — vía `currentContainer.Release()` que persiste.

**Vinculado a:** [[Index/06 - Player & World]], [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[NeedsState]], [[PersonalityProfileSO]], [[NeedStationRegistry]], [[MoriMochiContainer]], [[NameTag]], [[GameEvents]]

**Organización (partial class):**
- `MoriMochiAgent.cs` — núcleo: campos, lifecycle, dispatch, helpers NavMesh, gizmos
- `MoriMochiAgent.Tuning.cs` — campos serializados Odin + readouts + dev buttons
- `MoriMochiAgent.Brain.cs` — estados + needs + reacciones + intent
- `MoriMochiAgent.Physics.cs` — colisión/knock/throw/ragdoll/recovery/handoff
- `MoriMochiAgent.Confinement.cs` — pen + courtship + rebake + pooling
