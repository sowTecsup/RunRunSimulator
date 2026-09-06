---
tags: [script, world, expedition, graphics, material]
---

# ArenaPaletteApplier.cs

**Ruta:** `World/Expedition/ArenaPaletteApplier.cs`

**Responsabilidad:** Gestor de paletas de arena que compila rampas a texturas y remapea materiales de la escena vía substitución de paleta. Instancia materiales por original (cacheo), clasifica renderer materials por nombre, aplica sol/ambient/fog/cielo. Llamado desde ArenaSandbox.ApplyPalette().

## Campos Serializados

- `paletteMaterial` (Material, Required) — material plantilla "ArenaPalette.mat" con slots _Ramp, _BaseMap, etc.
- `palettes` (List<ArenaPaletteSO>) — lista de assets de paleta (pradera, desierto, etc.)
- `roots` (List<GameObject>) — gameobjects con Renderers a remapear
- `sun` (Light) — foco directional (recibe SunColor/Intensity de paleta)
- `skyCamera` (Camera) — cámara de cielo (recibe backgroundColor)
- `foliageWind`, `grassWind` (float, Min 0) — intensidad de viento por slot

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

## Flujo Privado

**BuildRamps(palette):**
- Por cada ArenaPaletteSlot (6 totales):
  - Crea o reutiliza Texture2D(256, 1, RGBA32, mipChain=false)
  - Llena 256 píxeles: `texture.SetPixel(x, ramp.Evaluate(x/255))`
  - Apply(false, false) → GPU

**Remap(renderer):**
- Por cada material en sharedMaterials:
  - Busca original (caché en originalByInstance)
  - TryClassify(material) → ArenaPaletteSlot (por nombre)
  - GetInstance(original, slot) → crea/cachea material instancia
  - Reemplaza en sharedMaterials

**GetInstance(original, slot):**
- Si no está cached:
  1. new Material(paletteMaterial) + nombre + slot
  2. FindBaseMap(original) → busca _BaseMap, _Main_Texture, _Albedo_Map, _MainTex, _Texture
  3. Copia baseMap + scale/offset a la instancia
  4. Copia _AlphaClip, _Cutoff, _Cull, habilita keywords si aplica
  5. SetFloat(WindStrengthID) según foliageWind/grassWind
- SetTexture(_Ramp, ramps[slot])
- Retorna instancia

**TryClassify(material) → (bool, ArenaPaletteSlot):**
- Busca substrings en material.name:
  - "Trunk" → Trunk
  - "Leaves"/"Tree"/"Plants" → Foliage
  - "Moss"/"Rock"/"Pebble"/"PolygonNature_0" → Rock
  - "Generic_0"/"Grass"/"Flower" → Grass
  - "ArenaGround"/"ArenaOutskirts" → Ground
  - "ArenaWall" → Wall
  - (else) → Ground, retorna false (no clasificado)

**ApplyEnvironment(palette):**
- Sun: color + intensity
- RenderSettings: AmbientMode.Flat, ambientLight, fog=exponentialSquared, fogColor, fogDensity
- SkyCamera (si existe): clearFlags=SolidColor, backgroundColor

## Invariantes S102

- **Cacheo por original:** cada material original → una instancia per slot
- **Rampas 256x1:** evaluadas en Evaluate() para suavidad
- **Clasificación por nombre:** fallback a Ground si no coincide
- **Wind per slot:** solo foliage y grass tienen viento
- **Clonación lazy:** materiales instancia se crean bajo demanda
- **Cleanup en Destroy():** limpia rampas y materials instancia

## Conexiones

- [[ArenaPaletteSO]] (lee paletas y rampas)
- [[ArenaSandbox]] (llama ApplyIndex/ApplySeed en BuildRoom)
- [[WorldEnums]] (ArenaPaletteSlot enum)
- [[ArenaPalette.mat]] (template material Synty)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
