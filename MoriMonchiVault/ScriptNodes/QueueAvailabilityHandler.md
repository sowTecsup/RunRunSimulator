---
tags: [script, world, npc]
---

# QueueAvailabilityHandler.cs

**Ruta:** `World/Containers/QueueAvailabilityHandler.cs`

**Responsabilidad:** Clase pura [Serializable] sin estado mutable. Valida si una posición candidata es válida para un cliente. Verifica: candidato cae en NavMesh + NO se desvió más de maxSnap al snapear (si queda lejos = obstáculo descarta), camino libre desde el cliente anterior (Raycast), no se solapa con otros (minSeparation).

**Método público:**
- `IsAvailable(from: Vector3, candidate: Vector3, areaMask: int, sampleRadius: float, maxSnap: float, occupied: IReadOnlyList<Vector3>, minSeparation: float, out snapped: Vector3)` → bool.
  - `from` — posición del cliente anterior.
  - `candidate` — posición candidata a validar.
  - `areaMask` — máscara de áreas NavMesh (suele ser AllAreas).
  - `sampleRadius` — radio de búsqueda para SamplePosition.
  - `maxSnap` — tolerancia máxima de desviación al snapear. Si |snapped - candidate| > maxSnap, return false (obstáculo).
  - `occupied` — lista de posiciones de otros clientes en la fila (todos menos `from`).
  - `minSeparation` — distancia mínima entre clientes.
  - `snapped` — salida: posición ajustada al NavMesh.
  - **Validaciones en orden:**
    1. SamplePosition(candidate, sampleRadius, ...) → si falla, return false.
    2. (snapped - candidate).sqrMagnitude > maxSnap² → desvío excesivo, return false (obstáculo).
    3. Raycast(from, snapped, ...) → si hay obstáculo, return false.
    4. Ninguno en occupied dentro de minSeparation² → si hay solapamiento, return false.
  - Si pasa todas, return true + snapped.

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[CashRegister]]
