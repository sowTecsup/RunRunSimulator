---
tags: [script, world]
---

# SpawnBallistics.cs

**Ruta:** `World/Spawning/SpawnBallistics.cs`

**Responsabilidad:** Clase estática pura (sin estado). Dos métodos: `SolveLaunchVelocity(origin, target, angleRad)` resuelve la velocidad de lanzamiento que aterriza en target desde origin a ángulo de elevación angleRad (ecuación de rango balístico clásica; fallback a lob suave si target inalcanzable). `ResolvePlayer()` localiza el GameObject tagged "Player" o Camera.main.transform si no. Usada por MoriMochiSpawner para cada cañonazo.

**Vinculado a:** [[Index/06 - World Architecture]]

**Conexiones:** [[MoriMochiSpawner]]

**Fórmula:** y = d·tan(θ) − g·d² / (2·v²·cos²(θ)) ⇒ v = √(g·d² / (2·cos²(θ)·(d·tan(θ) − y)))
