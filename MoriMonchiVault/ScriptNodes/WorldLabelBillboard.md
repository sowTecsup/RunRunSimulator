---
tags: [script, combat-prototype, presentation]
---

# WorldLabelBillboard.cs

**Ruta:** `CombatPrototype/WorldLabelBillboard.cs`

**Responsabilidad:** Componente de billboard simple que rota el GameObject para que siempre mire a la cámara principal. En LateUpdate, copia la rotación de `Camera.main`. Usado en objetos mundo con etiquetas TMP (labels de unidades, etc.) para asegurar legibilidad desde cualquier ángulo de cámara sin distorsión de perspectiva.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatUnitView]], UI mundo
