---
tags: [script, world]
---

# NeedStation.cs

**Ruta:** `World/Needs/NeedStation.cs`

**Responsabilidad:** Estación abstracta de restauración de necesidades. Slot-based capacity, fill rate. Se auto-registra en `NeedStationRegistry`. Abstracta: subclases implementan `Need` enum (Energy para RestZone, Health para Feeder, etc.).

**Métodos principales:**
- `TryReserve(MoriMochiAgent agent, Vector3 from, int areaMask, float sampleRadius, out Vector3 usePos) → bool` — reserva slot más cercano (re-entrante: si ya tiene slot, reusa)
- `Release(MoriMochiAgent agent)` — libera slot

**Invariantes S93 (rescatados de comentarios):**
- Capacidad = cantidad de use points (slots); cada agente reserva el slot libre y ALCANZABLE más cercano y lo retiene hasta terminar o ser interrumpido; sin puntos → un slot implícito en el transform.
- `TryReserve` es re-entrante (si ya tiene slot, lo reusa).

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[NeedStationRegistry]], [[Feeder]], [[PlayZone]], [[RestZone]], [[MoriMochiAgent]], [[NeedsState]]
