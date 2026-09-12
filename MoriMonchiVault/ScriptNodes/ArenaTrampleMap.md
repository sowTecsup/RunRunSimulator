---
tags: [script, world, expedition, graphics, rtx]
---

# ArenaTrampleMap.cs

**Ruta:** `World/Expedition/ArenaTrampleMap.cs`

**Responsabilidad:** Único dueño del mapa de pisado de la arena. Mantiene dos RenderTexture ARGBHalf (current/scratch) con decaimiento exponencial por half-life, recolecta sellos de criaturas y minerales percibibles en radio, procesa golpes (clash hits) y dibuja sellos GL-inmediato en el shader `MoriMonchi/ArenaTrample`. Publica globales `_ArenaTrampleTex` (RenderTexture activa) y `_ArenaTrampleArea` (bounds normalizado). **Canales:** RG=empuje (push), B=aplastado fresco, A=rastro acumulado (para nieve).

**Métodos públicos:**
- `Instance` (static property) — singleton que se inicializa en OnEnable, se limpia en OnDisable
- `Stamp(Vector3 world, float radius, float strength) → void` (static) — API de sello directo (golpes de combate, etc); encolados y dibujados en DrawStamps()

**Campos serializados:**
- `trampleShader` (Shader) — shader `ArenaTrample` con pass de decaimiento y sellado
- `resolution` (int, Min 64, default 512) — resolución RTX cuadrada
- `padding` (float, default 4) — padding alrededor de bounds de ArenaShape
- `freshHalfLife` (float, default 0.35) — half-life del aplastado fresco (canal B)
- `trailHalfLife` (float, default 6) — half-life del rastro acumulado (canal A, nieve)
- `creatureRadius` (float, default 0.9) — radio de sello por criatura percibida
- `mineralRadius` (float, default 0.5) — radio de sello por mineral
- `impactRadius` (float, default 2.5) — radio de sello por impacto de choque
- `trailGain` (float, default 0.6) — ganancia en canal A (rastro) cuando criatura presiona
- `stampRate` (float, default 8) — stamps/seg de criaturas (suavizado por dt)

**Flujo privado:**

**OnEnable / OnDisable:**
- OnEnable: obtiene ArenaShape, crea RTX (current/scratch), crea Material(trampleShader) para decaimiento y sellado, publica defaults (Texture2D.blackTexture), suscribe a shape.Rebuilt
- OnDisable: desuscribe, limpia perceiveBuffer y lastClashHit, libera RTX, destruye materiales, limpia Instance

**LateUpdate:**
- Si current == null o no playing, publica Texture2D.blackTexture
- Si no, corre ApplyDecay → CollectPerceivedStamps → DrawStamps
- Publica current como global _ArenaTrampleTex

**ApplyDecay(float dt):**
- Calcula factor de decaimiento exponencial por half-life: `Exp(-dt * Ln(2) / halfLife)`
- Setea decayMaterial con _FreshDecay y _TrailDecay
- Graphics.Blit(current → scratch con decayMaterial)
- Swapea current ↔ scratch

**CollectPerceivedStamps():**
- PerceivableRegistry.QueryInRadius(shape.Center, areaSize) → perceiveBuffer
- Para cada Perceivable:
  - Si tiene Monchi: EnqueueStamp(position, creatureRadius, stampRate*dt, trailGain*pressure)
  - CollectClashStamp(agent) — detecta cambio en ClashHitAt, encolada impacto
  - Sino (mineral): EnqueueStamp(position, mineralRadius, pressure, 0)
- stampRate suaviza presión: `Min(1, stampRate * dt)`

**CollectClashStamp(MoriMochiAgent agent):**
- Detecta cambio en agent.ClashHitAt vs lastClashHit[agent]
- Si cambió: EnqueueStamp(agent.ClashHitPoint, impactRadius, 1.0, 1.0)
- Cachea en lastClashHit

**DrawStamps():**
- Si queuedStamps vacío, return
- Graphics.SetRenderTarget(current)
- stampMaterial.SetPass(1)
- GL.Begin(GL.QUADS) ortho loop:
  - Por cada stamp: calcula UV (world → texel space), dibuja quad con GL.Color (push, trail, 0, 0)
  - Vértices: (u-ru, v-ru) → (u+ru, v+ru) — cuadrado 4-point
- Graphics.SetRenderTarget(null)
- Limpia queuedStamps

**RecalculateArea():**
- Resuelve bounds de ArenaShape con padding
- Normaliza a cuadrado: areaSize = Max(sizeX, sizeZ)
- Computa origin y invSize para world → UV
- Publica _ArenaTrampleArea: (originX, originZ, invSize, invSize)

**Internals:**
- `current`, `scratch` — RenderTexture ARGBHalf, swap en decaimiento
- `perceiveBuffer`, `lastClashHit` — caché para recolección incremental
- `queuedStamps` — buffer de sellados en frame (clear por DrawStamps)
- `areaOriginX/Z`, `areaSize`, `areaInvSize` — parámetros de transformación world-to-uv

**Invariantes:**
- **Singleton:** se publica en OnEnable, se limpia en OnDisable
- **RTX ARGBHalf:** precisión media para decaimiento suave
- **Decaimiento exponencial:** half-life controlado, suavizado por dt
- **Recolección lazy:** Perceivable y ClashHit se cachean para evitar reconversión de datos
- **Publish defaults:** antes de crear Instance, los globales son negros (fallback)
- **Swap eficiente:** current ↔ scratch alternancia simple sin copia

**Conexiones:** [[PerceivableRegistry]], [[MoriMochiAgent]], [[ArenaShape]], [[ArenaTrample.shader]]

**Vinculado a:** [[Index/22 - MVP Combate]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]
