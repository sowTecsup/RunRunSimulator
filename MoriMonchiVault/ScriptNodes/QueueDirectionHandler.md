---
tags: [script, world, npc]
---

# QueueDirectionHandler.cs

**Ruta:** `World/Containers/QueueDirectionHandler.cs`

**Responsabilidad:** Clase pura [Serializable] sin estado mutable. Genera 3 candidatos ortogonales (90° exactos) para el siguiente cliente de la fila. Recibe posición de anclaje (anterior cliente), dirección de retroceso (eje fijo de la fila), espaciado. Devuelve candidatos en orden ortogonal ESTRICTAMENTE: Atrás → Izquierda → Derecha (rotaciones ±90° sobre eje Y, sin ángulos 45°).

**Struct público:**
- `Candidate{Pos: Vector3}` — posición mundial candidata.

**Método público:**
- `Candidates(anchorPos: Vector3, backAxis: Vector3, spacing: float, outBuf: List<Candidate>)` → vacía outBuf y agrega 3 candidatos. Normaliza backAxis (o Vector3.forward si es cero). Calcula left/right rotando backAxis ±90° sobre Y. Devuelve:
  1. anchorPos + back * spacing
  2. anchorPos + left * spacing
  3. anchorPos + right * spacing

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[CashRegister]]
