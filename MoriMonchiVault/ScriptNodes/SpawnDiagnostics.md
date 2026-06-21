---
tags: [script, world]
---

# SpawnDiagnostics.cs

**Ruta:** `World/Spawning/SpawnDiagnostics.cs`

**Responsabilidad:** Componente dev standalone que se suscribe al bus `GameEvents` (solo lectura) y loguea eventos de spawning con frame + timestamp. Registra contadores globales: `registryChanged`, `registryReloaded`, `breedingCompleted`, `navRebakes`. Mantiene historial ordenado (rotativo, max 40 líneas) con readouts en inspector Odin. RegistryReloaded dispara warning ruidoso. Herramienta de diagnóstico para rastrear cadena de eventos durante rebakes de NavMesh y reloads de datos.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[GameEvents]]

**Métodos clave:**
- `OnChanged(registry)` → incrementa `registryChanged`, loguea sync quirúrgico
- `OnReloaded(registry)` → incrementa `registryReloaded`, warning ruidoso
- `OnBred(mother, father, child)` → incrementa `breedingCompleted`, loguea nombre de cría
- `OnWillRebake()` / `OnRebaked()` → instrumenta rebakes NavMesh
- `ClearHistory()` → reset contadores + historial (button)
