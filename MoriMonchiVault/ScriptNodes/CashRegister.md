---
tags: [script, world, npc]
---

# CashRegister.cs

**Ruta:** `World/Containers/CashRegister.cs`

**Responsabilidad:** Singleton que gestiona la cola de clientes como cadena lineal ortogonal que tiende hacia una salida (exit). Es propietario del orden (List<Link>{Agent,Pos}). Posee ref `queueRoot` (transform raíz de la fila, frente = índice 0). Parámetros tuneables: `queueTowards` (Transform hacia salida; fallback `NpcController.ExitPoint`; fallback alejar de la caja), `slotSpacing` (distancia entre clientes), `sampleRadius` (búsqueda NavMesh), `maxSnap` (tolerancia al snapear al mesh — si queda lejos, obstáculo descarta candidato), `minSeparation` (separación mínima para no solapar), `maxQueueDepth` (largo máximo de la fila).

Usa dos handlers puros internos: `QueueDirectionHandler` (genera 3 candidatos ortogonales estrictamente: Atrás → Izquierda → Derecha, ángulos 90° relativos a un eje fijo) y `QueueAvailabilityHandler` (valida si cada candidato es válido: en NavMesh, NO se desvió más de maxSnap al snapear, camino libre desde anterior, sin solapamiento). Estructura interna `Link{Agent,Pos}` almacena cliente actual y posición mundial.

**API pública:**
- `TryReserveSlot(NpcAgent)` → Vector3 o null. Agrega cliente si hay espacio, devuelve posición del slot.
- `ReleaseSlot(NpcAgent)` → quita cliente, dispara `OnCurrentCustomerChanged` si era frente, llama Recompute() para rearmar.
- `IsFrontSlot(NpcAgent)` → bool. ¿Es el cliente en index 0?
- `CurrentSlotOf(NpcAgent)` → Vector3?. Posición actual del cliente en la fila (repolleado cada frame por NpcAgent.TickQueueing).
- Propiedades: `CurrentCustomer` (chain[0].Agent), `QueueRootPos`, `Instance` (singleton).
- Evento: `OnCurrentCustomerChanged(NpcAgent)` disparado cuando cambia el cliente en frente (transición desde/a null o nuevo cliente).

**Métodos privados internos:**
- `Recompute()` → rearma todas las posiciones de chain (índice 0 a fin) cuando el frente se va; los ya-en-fila conservan lugar si un TryComputeLink falla.
- `TryComputeLink(int index, NpcAgent, out Link)` → calcula posición de un cliente. Para index==0, snappea a queueRoot sobre NavMesh. Para index>0, prueba 3 candidatos ortogonales vía handlers; devuelve true si alguno válido, false si todos fallan (sin fallback).
- `BackDirection()` → SnappToOrthogonal(RawBackDirection()).
- `RawBackDirection()` → apunta hacia `queueTowards`, fallback `NpcController.ExitPoint`, fallback alejar de la caja hacia y=0.
- `SnapToOrthogonal(dir)` → snappea vector arbitrario a ±forward o ±right de la caja (4 direcciones cardinales).
- Gizmos: flecha azul (dirección hacia salida) + cadena (verde frente, amarillo resto).

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[QueueDirectionHandler]], [[QueueAvailabilityHandler]], [[NpcAgent]], [[NpcController]]
