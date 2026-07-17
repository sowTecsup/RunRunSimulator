---
tags: [script, prototype, core]
---

# SpiderLegStepper.cs

**Ruta:** `Prototype/Spider/SpiderLegStepper.cs`

**Responsabilidad:** Decide cuándo y dónde pisa una pata individual. Computa home predictivo: offset relativo a cadera + anticipación por velocidad de cadera + raycast a ground. Disparadores de paso: distancia actual vs. umbral (depende si se mueve o reposa) Y torsión angular (ángulo entre posición de pie actual y home, **gateado: solo dispara cuando gira o en reposo**). Pasos con urgencia adaptativa basada en drag/twist. NO tiene Update; lo tickea `SpiderBodyController` vía `Tick(mayStep, turning)`. Internamente maneja: estado stepping/resting, interpolación de paso (smooth curve), duración adaptada según urgencia, raycast para grounding. Expone: `IsStepping`, `FootPosition`, `WantsStep` (boolean calculado), `Drag` (distancia a home), `Twist` (ángulo).

**Notas de prototipo:** Los raycast son locales por pata. El sistema de anticipación predice dónde pisar basado en velocidad de la cadera. El campo `turningNow` guarda el flag de giro actual para gatealizar el término de Twist en `WantsStep`.

**Cambios S49:** El contrato de `Tick` pasó de `Tick(bool mayStep)` a `Tick(bool mayStep, bool turning)`. Se agregó campo `private bool turningNow` (línea 30). La condición de Twist en `WantsStep` se gateó: ahora solo aplica cuando `turningNow || !moving` (girando o en reposo), evitando que la métrica degenere en marcha recta donde la pata trasera leía permanentemente 180°.

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderBodyController]], [[SpiderTuningSO]]
