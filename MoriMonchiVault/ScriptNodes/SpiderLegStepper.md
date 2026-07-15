---
tags: [script, prototype, core]
---

# SpiderLegStepper.cs

**Ruta:** `Prototype/Spider/SpiderLegStepper.cs`

**Responsabilidad:** Decide cuándo y dónde pisa una pata individual. Computa home predictivo: offset relativo a cadera + anticipación por velocidad de cadera + raycast a ground. Disparadores de paso: distancia actual vs. umbral (depende si se mueve o reposa) Y torsión angular (ángulo entre posición de pie actual y home). Pasos con urgencia adaptativa basada en drag/twist. NO tiene Update; lo tickea `SpiderBodyController` vía `Tick(mayStep)`. Internamente maneja: estado stepping/resting, interpolación de paso (smooth curve), duración adaptada según urgencia, raycast para grounding. Expone: `IsStepping`, `FootPosition`, `WantsStep` (boolean calculado), `Drag` (distancia a home), `Twist` (ángulo).

**Notas de prototipo:** Los raycast son locales por pata. El sistema de anticipación predice dónde pisar basado en velocidad de la cadera.

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderBodyController]], [[SpiderTuningSO]]
