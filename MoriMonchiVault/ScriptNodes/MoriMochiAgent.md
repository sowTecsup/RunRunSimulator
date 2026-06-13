---
tags: [memory-bank, script, player-world]
---

# MoriMochiAgent.cs

**Ruta:** `World/MoriMochiAgent.cs`

**Responsabilidad:** Cerebro IA de criatura viva. Máquina de estados (`Idle`, `Roaming`, `Reacting`, `Carried`, `Thrown`, `Recovering`, `SeekingNeed`, `UsingStation`, `Courting`). Personality-driven via `PersonalityProfileSO`. Decae necesidades cada frame, busca `NeedStation` cuando crítico. Implementa `IThrowable` (agarrar/lanzar/knock con física de peluche: bounce, bounceSpin, knockTransfer) e `IInteractable` (E para acariciar). Confinamiento en corral (`EnterConfinement`/`EnterCourtship`/`ExitCourtship`). Sobrevive a rebake de NavMesh. Gizmos de rangos.

**Vinculado a:** [[Index/06 - Player & World]], [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[NeedsState]], [[PersonalityProfileSO]], [[NeedStationRegistry]], [[MoriMochiContainer]], [[NameTag]], [[GameEvents]]
