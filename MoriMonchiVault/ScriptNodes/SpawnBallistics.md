---
tags: [script, world]
---

# SpawnBallistics.cs

**Ruta:** `World/Spawning/SpawnBallistics.cs`

**Responsabilidad:** Clase estática pura (sin estado). Métodos para balística de lanzamiento y visualización de trayectorias. `SolveLaunchVelocity(origin, target, angleRad)` resuelve la velocidad de lanzamiento que aterriza en target desde origin a ángulo de elevación angleRad (ecuación de rango balístico clásica; fallback a lob suave si target inalcanzable). `ResolvePlayer()` localiza el GameObject tagged "Player" o Camera.main.transform si no. **S93 NUEVOS:** métodos estáticos `DrawSimulatedArc()` y `DrawRing()` para gizmos de visualización de trayectorias en el editor.

**Métodos públicos:**
- `SolveLaunchVelocity(Vector3 origin, Vector3 target, float angleRad) → Vector3` — resuelve velocidad balística
- `ResolvePlayer() → Transform` — busca tag "Player" o Camera.main
- `DrawSimulatedArc(Vector3 origin, Vector3 vel, float groundY, float g) → void` — **S93 NUEVO** dibuja arco simulado (48 pasos) usando Gizmos
- `DrawRing(Vector3 center, float radius, int segments) → void` — **S93 NUEVO** dibuja anillo en XZ usando Gizmos

**Cambios S93:**
- `DrawSimulatedArc()` y `DrawRing()` son nuevos métodos estáticos usados por `MoriMochiSpawner.OnDrawGizmosSelected()` para visualizar trayectorias del cañón (8 arcos de min/max elevación, anillo de radio spawn).

**Vinculado a:** [[Index/06 - World Architecture]]

**Conexiones:** [[MoriMochiSpawner]]

**Fórmula:** y = d·tan(θ) − g·d² / (2·v²·cos²(θ)) ⇒ v = √(g·d² / (2·cos²(θ)·(d·tan(θ) − y)))
