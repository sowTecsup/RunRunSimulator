---
tags: [script, prototype, core]
---

# SpiderRagdollMode.cs

**Ruta:** `Prototype/Spider/SpiderRagdollMode.cs`

**Responsabilidad:** Interruptor walk ↔ ragdoll. Entrada: `SetRagdoll(bool)` o field serializado. Al cambiar: desactiva `SpiderBodyController` y `SpiderLegIK[]` en walk, activa `Rigidbody` dinámicos en ragdoll. Sincronización: antes de soltar Rigidbody, sincroniza `rb.position` y `rb.rotation` de transform para evitar saltos. En ragdoll activa interpolación; en walk desactiva. Expone `IsRagdoll` para consultas.

**Notas de prototipo:** Los Rigidbody del spider están en kinematic en walk (no física). El cambio es instantáneo. Gizmos muestran estado (rojo=ragdoll, verde=walk).

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderBodyController]], [[SpiderLegIK]], [[SpiderBodyMotion]], [[SpiderDevPanel]]
