---
tags: [script, world, expedition, graphics, material, palette]
---

# ArenaPaletteApplier.cs

**Ruta:** `World/Expedition/ArenaPaletteApplier.cs`

**Responsabilidad:** Gestor de paletas de arena que compila rampas a texturas, remapea materiales de la escena vía substitución de paleta, y aplica globales de shader para niebla radial. Instancia materiales por original (cacheo), clasifica renderer materials por nombre, aplica sol/ambient/fog/cielo. **S111:** soporte para material de agua. **S113:** niebla radial (ArenaFog) empujada a shaders globales por paleta, centro configurable.

## Campos Serializados

- `paletteMaterial` (Material, Required) — material plantilla "ArenaPalette.mat" con slots _Ramp, _BaseMap, etc.
- **S111:** `waterMaterial` (Material) — material especial para agua (no usa _BaseMap, solo _Ramp)
- `palettes` (List<ArenaPaletteSO>) — lista de assets de paleta (pradera, desierto, etc.)
- `roots` (List<GameObject>) — gameobjects con Renderers a remapear
- `sun` (Light) — foco directional (recibe SunColor/Intensity de paleta)
- `skyCamera` (Camera) — cámara de cielo (recibe backgroundColor)
- `arenaCenter` (Transform) — referencia de centro (fallback si no hay explícito)
- `foliageWind`, `grassWind` (float, Min 0) — intensidad de viento por slot
- **S113:** `dimNames` (List<string>) — nombres de GO que usan material dim (ej. "Surround", "Border")
- **S113:** `dimTint` (Color) — tinte aplicado a materiales dim (0.42, 0.48, 0.5, 1 grayish)

## Propiedades Públicas

- `Palettes → IReadOnlyList<ArenaPaletteSO>` — lista (read-only)
- `Current → ArenaPaletteSO` — paleta activa (o null)
- `CurrentIndex → int` — índice de Current (init -1)

## Métodos Públicos

- `IndexForSeed(int seed) → int` — retorna `Abs(seed) % palettes.Count` (-1 si vacío)
- `ApplyIndex(int index) → void` — normaliza index al rango, aplica paleta[index]
- `Apply(ArenaPaletteSO palette) → void` — aplica paleta:
  1. Guarda Current = palette
  2. BuildRamps(palette) — compila rampas a Texture2D 256x1
  3. Itera roots → Renderers → Remap(renderer)
  4. ApplyEnvironment(palette) — RenderSettings + Sun + Sky
  5. PushArenaFog(palette) — **S113:** push globales de niebla radial
  
- **S113:** `SetArenaCenter(Vector3 center) → void` — fija centro explícito para niebla, actualiza PushArenaFog

## Flujo Privado

**BuildRamps(palette):**
- Por cada ArenaPaletteSlot (7 totales: Ground, Grass, Foliage, Trunk, Rock, Wall, Water):
  - Crea o reutiliza Texture2D(256, 1, RGBA32, mipChain=false)
  - Llena 256 píxeles: `texture.SetPixel(x, ramp.Evaluate(x/255))`
  - Apply(false, false) → GPU

**Remap(renderer) (S113):**
- **Novedad S113:** IsBarrier(renderer.transform) → detecta si GO está en dimNames
- Por cada material en sharedMaterials:
  - Busca original (caché en originalByInstance)
  - TryClassify(material) → ArenaPaletteSlot
  - **S113:** GetDimInstance vs GetInstance según dim
  - Reemplaza en sharedMaterials

**GetInstance(original, slot) (S111-S113):**
- **Si Water y waterMaterial exists:** new Material(waterMaterial)
- **Sino:** new Material(paletteMaterial)
  - FindBaseMap(original) → busca _BaseMap, _Main_Texture, _Albedo_Map, _MainTex, _Texture
  - Copia baseMap + scale/offset + keywords + _AlphaClip, _Cutoff, _Cull
  - SetFloat(WindStrengthID) según foliageWind/grassWind
- SetTexture(_Ramp, ramps[slot])
- Retorna instancia

**GetDimInstance(original, slot) (S113):**
- Similar a GetInstance pero:
  - Crea nuevo Material(paletteMaterial)
  - SetColor(_Tint, dimTint) — matiz gris oscuro
  - Resto igual

**IsBarrier(transform) (S113):**
- Busca si el GO o algún padre tiene nombre en dimNames (e.g., "Surround", "Border")
- Retorna true si coincide → usa dim material

**TryClassify(material) → (bool, ArenaPaletteSlot):**
- Busca substrings en material.name:
  - "Trunk" → Trunk
  - "Leaves"/"Tree"/"Plants" → Foliage
  - "Moss"/"Rock"/"Pebble"/"PolygonNature_0" → Rock
  - "Generic_0"/"Grass"/"Flower" → Grass
  - "ArenaWater"/"ArenaSea" → Water
  - "ArenaGround"/"ArenaOutskirts" → Ground
  - "ArenaWall" → Wall
  - (else) → Ground, retorna false

**ApplyEnvironment(palette):**
- Sun: color + intensity
- RenderSettings: AmbientMode.Flat, ambientLight, fog=exponentialSquared, fogColor, fogDensity
- SkyCamera (si existe): clearFlags=SolidColor, backgroundColor

**PushArenaFog(palette) (S113):**
- Resuelve center: explicitArenaCenter (si fue SetArenaCenter) → arenaCenter.position → transform.position
- Shader.SetGlobal*:
  - ArenaFogCenter → center
  - ArenaFogColor → palette.ArenaFogTint
  - ArenaFogInner → palette.ArenaFogInner (radio mínimo de niebla, ej. 26)
  - ArenaFogOuter → palette.ArenaFogOuter (radio máximo, ej. 60)
  - ArenaFogStrength → palette.ArenaFogStrength ([0-1], ej. 0.95)
  - ArenaFogDim → palette.ArenaFogDim ([0-1], dimming de perif, ej. 0.75)

**OnDisable (S113):**
- Setea ArenaFogStrength, Dim, Outer → 0f para desactivar niebla

## Invariantes S102+S111+S113

- **Cacheo por original:** cada material original → una instancia per slot
- **Rampas 256x1:** evaluadas en Evaluate() para suavidad
- **Clasificación por nombre:** fallback a Ground si no coincide
- **Wind per slot:** solo foliage y grass tienen viento
- **Water slot (S111):** usa waterMaterial si existe, sino paletteMaterial (fallback)
- **Dim materials (S113):** detecta por nombre de GO parent, aplica tint gris
- **ArenaFog globals (S113):** push en Apply(), limpieza en OnDisable
- **Center explicit (S113):** SetArenaCenter() fija centro para niebla independiente de arenaCenter

## Conexiones

- [[ArenaPaletteSO]] (lee paletas y rampas ArenaFog*)
- [[ArenaSandbox]] (llama ApplyIndex en BuildRoom)
- [[WorldEnums]] (ArenaPaletteSlot enum)
- [[ArenaPalette.mat]], [[ArenaPaletteWater.mat]] (template materials)

## Vinculado a

[[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]
