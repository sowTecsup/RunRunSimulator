---
tags: [script, world, expedition, procedural, generation, surround]
---

# ArenaShapeSurround.cs

**Ruta:** `World/Expedition/ArenaShapeSurround.cs`

**Responsabilidad:** Coloca bosque oscuro en anillos concéntricos fuera del contorno de la sala (el "surround"). Decora la periferia con árboles, arbustos y rocas según opciones probabilísticas. S113: nuevo revestimiento decorativo del perímetro.

**Métodos Públicos:** ninguno (solo Rebuild via event)

**Campos Serializados:**
- **Semilla:**
  - `seed` (int, default 7) — base RNG

- **Bosque (anillos):**
  - `treePrefabs` (List<GameObject>) — árboles
  - `bushPrefabs` (List<GameObject>) — arbustos
  - `rockPrefabs` (List<GameObject>) — rocas
  - `rings` (int, min 1) — cantidad de anillos concéntricos (5)
  - `ringStart` (float) — distancia del primer anillo al contorno (3)
  - `ringStep` (float) — incremento de distancia por anillo (3.5)
  - `stepAlong` (float, min 0.5) — espaciado a lo largo del contorno (3)
  - `jitter` (float, [0-1]) — variación aleatoria de posición (0.5)
  - `rockChance, bushChance` (float, [0-1]) — probabilidades de tipo (0.25, 0.35)
  - `treeScale, bushScale, rockScale` (Vector2) — rangos de escala
  - `drop` (float) — hundimiento Y para bordes exteriores (0.05)

**Ciclo de Regeneración:**
1. OnEnable(): suscribe a shape.Rebuilt += Dress
2. Dress():
   - DestroySurround() (limpia "Surround" GO previo)
   - Crea GameObject("Surround") con hideFlags=DontSave
   - RNG(seed)
   - Para cada ring i=[0, rings):
     - distance = ringStart + i * ringStep
     - step = stepAlong * (1 + i * 0.25) — pasos más largos en anillos lejanos
     - AlongEdge(outline, rng, step, -distance, jitter) → puntos externos
     - Para cada punto: randRoll:
       - 25% → rock (si rockPrefabs existe)
       - 35% → bush (si no rock, si bushPrefabs existe)
       - Sino → tree (fallback a arbustos o rocas si falta)
     - Spawn con escala random + yaw + y -= drop

**Invariantes:**
- Puntos se generan a distancia NEGATIVA (fuera del contorno)
- Todos los objetos tienen colisores destruidos (puramente decorativos)
- hideFlags=DontSave → no persisten

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaShape]], [[ArenaShapeScatter]]
