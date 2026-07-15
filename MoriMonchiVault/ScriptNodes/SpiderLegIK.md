---
tags: [script, prototype, core]
---

# SpiderLegIK.cs

**Ruta:** `Prototype/Spider/SpiderLegIK.cs`

**Responsabilidad:** Resuelve IK de 2 huesos por ley de cosenos analítica. Datos de entrada: `upperLength` (antebrazo), `lowerLength` (mano), target (pie). Computa rodilla via triángulo; aplica pole vector para orientación (fallback offset en local si no hay polo). Calcula ejes de rotación via cross products y aplica SOLO rotaciones (los huesos son Transforms sin escala; las mallas son hijas). Expone `KneePosition` y `PolePosition` para debug. `SolveTo(target)` es llamado cada frame por `SpiderBodyController` después de que `SpiderLegStepper` actualiza footPosition.

**Notas de prototipo:** Resuelve sin usar `Quaternion.LookAt` directo; construye rotaciones via axis/angle para evitar singularidades. Gizmos de debug muestran los huesos y pole.

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderBodyController]]
