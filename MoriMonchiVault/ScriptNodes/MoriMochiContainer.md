---
tags: [script, world]
---

# MoriMochiContainer.cs

**Ruta:** `World/Containers/MoriMochiContainer.cs`

**Responsabilidad:** Corral base con `BoxCollider` trigger. Admite criaturas lanzadas (`OnTriggerEnter`) o soltadas dentro (`OnTriggerStay`) hasta `capacity`. Rebotar si lleno (`BounceOut`). Expone `Occupants` (IReadOnlyList) y tabla `OccupantInfos` para inspector (nombre/género/personalidad). `Claim()` protegido compartido por admisión y `BreedingContainer`. `Release()` virtual para que subclases reaccionen. Confina al agente via `EnterConfinement` (cambia areaMask).

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[BreedingContainer]], [[MoriMochiAgent]], [[StoreContainer]]
