---
tags: [script, world]
---

# StoreContainer.cs

**Ruta:** `World/Containers/StoreContainer.cs`

**Responsabilidad:** Vitrina de tienda que exhibe MoriMonchis para venta. Restaura las 3 necesidades a `restoreRate/s`. Hereda `MoriMochiContainer`. Gestiona puntos de uso (use points) para que NPCs clientes naveguen sin solapamiento (patrón idéntico a `NeedStation`).

**Propiedades y métodos públicos:**
- `List<Transform> usePoints` — puntos dónde se paran los NPCs para comprar (snappeo a NavMesh). Si vacío → slot implícito en `transform.position`.
- `HasFreeUsePoint` (bool) — True si hay un slot disponible.
- `TryReserveUsePoint(NpcAgent agent, Vector3 from, int areaMask, float sampleRadius, out Vector3 usePos)` — reserva el slot libre más cercano a `from`; retorna posición snappada. Re-llamar con el mismo agente retorna el slot ya reservado.
- `ReleaseUsePoint(NpcAgent agent)` — libera el slot del agente.
- **Gizmos:** esferas (amarillas libres, rojas ocupadas) + líneas a los `usePoints`.

**Ciclo de vida:**
- `OnEnable()` → auto-registra en `StoreDisplayRegistry`.
- `OnDisable()` → auto-desregistra.
- `Update()` → restaura necesidades a ocupantes cada frame.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiContainer]], [[NeedsState]], [[MoriMochiAgent]], [[NpcAgent]], [[StoreDisplayRegistry]]
