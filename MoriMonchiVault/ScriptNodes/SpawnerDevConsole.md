---
tags: [script, world, spawning, dev]
---

# SpawnerDevConsole.cs

**Ruta:** `World/Spawning/SpawnerDevConsole.cs`

**Responsabilidad:** Panel de herramientas de desarrollo para [[MoriMochiSpawner]]. Botones Odin para testing del spawn pipeline: RespawnAll (limpia + resincroniza), FireDebugShot (lanza ragdoll manual desde el cañón con ballística resuelta), ClearDebugShots (limpia disparos de debug), DumpSpawnState (logging de contadores: spawned/queued/prewarmed/pooled). Antiguamente vivía como métodos en MoriMochiSpawner.Debug.cs; ahora es componente separado con refs serializadas (S55 Fase 9 composición).

**Métodos públicos (botones Odin):**
- `RespawnAll()` — `spawner.ClearAll()` + `spawner.Sync(GameManager.Instance.Registry)`. Test: re-población completa en vivo
- `FireDebugShot()` — instancia ragdoll manual en muzzle, resuelve balística a RandomLandingPoint, aplica impulso. Mantiene lista de debugShots
- `ClearDebugShots()` — destruye y limpia lista de debugShots
- `DumpSpawnState()` — Debug.Log de contadores vivos + iterable de spawned entries

**Campos serializados:**
- `spawner` (MoriMochiSpawner) — ref required al spawner a debuguear

**State internals:**
- `debugShots` (List<GameObject>) — GameObjects lanzados manualmente para testing

**Métodos privados:**
- Ninguno (todo public/button Odin)

## Uso en escena

Arrastrar SpawnerDevConsole como componente en un GO cualquiera (típicamente el mismo que MoriMochiSpawner), asignar la ref `spawner`. En Play mode, los botones disparan herramientas de debug en Odin Inspector.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiSpawner]], [[SpawnBallistics]], [[GameManager]]
