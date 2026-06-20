---
tags: [memory-bank, script, player-world]
---

# MoriMochiAgent.cs

**Ruta:** `World/MoriMochiAgent.cs`

**Responsabilidad:** Cerebro IA de criatura viva. Máquina de estados (`Idle`, `Roaming`, `Reacting`, `Carried`, `Thrown`, `Recovering`, `SeekingNeed`, `UsingStation`, `Courting`). Personality-driven via `PersonalityProfileSO`. Decae necesidades cada frame, busca `NeedStation` cuando crítico. Implementa `IThrowable` (agarrar/lanzar/knock con física de peluche: bounce, bounceSpin, knockTransfer) e `IInteractable` (E para acariciar). Confinamiento en corral (`EnterConfinement`/`EnterCourtship`/`ExitCourtship`). Penned solo restringe acercarse al jugador (salta approach/follow, flee/retreat/roam siguen normales). `Knock` preserva `thrownTimer` si ya estaba en el aire (evita reinicio en cadenas de golpes). `BodyRadius` expone el radio planar del collider para que `BreedingContainer` calcule espaciado de cortejo. Sobrevive a rebake de NavMesh. Gizmos de rangos.

**Vinculado a:** [[Index/06 - Player & World]], [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[NeedsState]], [[PersonalityProfileSO]], [[NeedStationRegistry]], [[MoriMochiContainer]], [[NameTag]], [[GameEvents]]

**Organización (partial class):**
- `MoriMochiAgent.cs` — núcleo: campos, lifecycle, dispatch, helpers NavMesh, gizmos
- `MoriMochiAgent.Tuning.cs` — campos serializados Odin + readouts + dev buttons
- `MoriMochiAgent.Brain.cs` — estados + needs + reacciones + intent
- `MoriMochiAgent.Physics.cs` — colisión/knock/throw/ragdoll/recovery/handoff
- `MoriMochiAgent.Confinement.cs` — pen + courtship + rebake + pooling
