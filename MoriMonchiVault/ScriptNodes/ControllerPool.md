---
tags: [script, world]
---

# ControllerPool.cs

**Ruta:** `World/Spawning/ControllerPool.cs`

**Responsabilidad:** Clase plana (sin estado mutable público) dueña de una Queue<MoriMonchiController>. Reutiliza instancias para evitar Destroy/Instantiate en runtime. Métodos: `Get(pos)` devuelve controller reusado (queue) o crea fresh (Instantiate), posiciona y activa. `Return(controller)` desactiva, enqueues. Usada por MoriMochiSpawner para lifecycle de criaturas (spawn → despawn → reuso o garbage). Propiedad pública `Count` para debug.

**Vinculado a:** [[Index/06 - World Architecture]]

**Conexiones:** [[MoriMochiSpawner]], [[MoriMonchiController]]

**Patrón:** Object Pool genérico. MoriMochiSpawner.Awake() crea `new ControllerPool(prefab)`. SpawnOne() → Get(pos) → Launch(). Despawn() → Return().
