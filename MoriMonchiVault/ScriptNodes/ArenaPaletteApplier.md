---
tags: [script, world, expedition, graphics, material, palette]
---

# ArenaPaletteApplier.cs

**Ruta:** `World/Expedition/ArenaPaletteApplier.cs`

**Responsabilidad:** Gestor de paletas de arena que compila rampas a texturas, remapea materiales de la escena vía substitución de paleta, y aplica globales de shader para niebla radial. Instancia materiales por original (cacheo), clasifica renderer materials por nombre, aplica sol/ambient/fog/cielo. S111: soporte para material de agua. S113: niebla radial (ArenaFog) empujada a shaders globales por paleta, centro configurable. S114: variantes de follaje (rampas alternativas por índice procedural para pasto y árboles). **S117:** conserva shader propio del material de pasto (_TrampleBend), fuerza variante 0 para ese material, clasifica "Blades" como Ground, marca _SnowReceiver en instancias de Ground, publica _ArenaSnowAmount.

## Campos Serializados

- `paletteMaterial` (Material, Required) — material plantilla "ArenaPalette.mat" con slots _Ramp, _BaseMap, etc.
- **S111:** `waterMaterial` (Material) — material especial para agua (no usa _BaseMap, solo _Ramp)
- `palettes` (List<ArenaPaletteSO>) — lista de assets de paleta (pradera, desierto, nevado, etc.)
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
  2. BuildRamps(palette) — compila rampas a Texture2D 256x1 (S114: con variantes de follaje)
  3. Itera roots → Renderers → Remap(renderer)
  4. ApplyEnvironment(palette) — RenderSettings + Sun + Sky + **S117:** _ArenaSnowAmount
  5. PushArenaFog(palette) — **S113:** push globales de niebla radial
  
- **S113:** `SetArenaCenter(Vector3 center) → void` — fija centro explícito para niebla, actualiza PushArenaFog

## Flujo Privado

**BuildRamps(palette) (S114 ACTUALIZADO):**
- Por cada ArenaPaletteSlot (7 totales: Ground, Grass, Foliage, Trunk, Rock, Wall, Water):
  - Crea o reutiliza Texture2D(256, 1, RGBA32, mipChain=false)
  - **S114:** si Foliage o Grass:
    - Itera palette.FoliageVariants y compila cada ramp a ramas alternativas
    - Usa `palette.RampFor(slot, variantIndex)` para obtener ramp con variante
    - Estructura: ramas[slot] = principal, variantRamps[slot] = lista de alternativas
  - Sino: llena 256 píxeles: `texture.SetPixel(x, ramp.Evaluate(x/255))`
  - Apply(false, false) → GPU

**Remap(renderer) (S113, S114, S117):**
- **Novedad S113:** IsBarrier(renderer.transform) → detecta si GO está en dimNames
- Por cada material en sharedMaterials:
  - Busca original (caché en originalByInstance)
  - TryClassify(material) → ArenaPaletteSlot
  - **S113:** GetDimInstance vs GetInstance según dim
  - **S114:** si Foliage/Grass, elige variante proceduralmente (ej. por posición o hash)
  - **S117:** si original.HasProperty(_TrampleBend), fuerza variant=0 (pasto trampled)
  - Reemplaza en sharedMaterials

**GetInstance(original, slot) (S111-S113-S114-S117):**
- **S117 NUEVO:** si original.HasProperty(TrampleBendID) → return new Material(original) con suffix (conserva shader propio)
- **Si Water y waterMaterial exists:** new Material(waterMaterial)
- **Sino:** new Material(paletteMaterial)
  - FindBaseMap(original) → busca _BaseMap, _Main_Texture, _Albedo_Map, _MainTex, _Texture
  - Copia baseMap + scale/offset + keywords + _AlphaClip, _Cutoff, _Cull
  - SetFloat(WindStrengthID) según foliageWind/grassWind
  - **S117:** SetFloat(_SnowReceiver, slot == Ground ? 1f : 0f) — marca instancias de Ground para recibir nieve
  - **S114:** si Foliage/Grass, calcula variantIndex y usa ramp con variante
- SetTexture(_Ramp, ramps[slot] o variantRamps[slot][variantIndex])
- Retorna instancia

**GetDimInstance(original, slot) (S113-S114-S117):**
- Similar a GetInstance pero:
  - **S117 NUEVO:** si original.HasProperty(TrampleBendID) → return new Material(original) (conserva shader)
  - Crea nuevo Material(paletteMaterial)
  - SetColor(_Tint, dimTint) — matiz gris oscuro
  - SetFloat(_SnowReceiver) — mismo que GetInstance
  - **S114:** si Foliage/Grass, aplica variante igual que GetInstance
  - Resto igual

**IsBarrier(transform) (S113):**
- Busca si el GO o algún padre tiene nombre en dimNames (e.g., "Surround", "Border")
- Retorna true si coincide → usa dim material

**TryClassify(material) → (bool, ArenaPaletteSlot) (S117 ACTUALIZADO):**
- Busca substrings en material.name:
  - **S117:** "Blades" → Ground (clasificación de pasto ArenaGrassField)
  - "Trunk" → Trunk
  - "Leaves"/"Tree"/"Plants" → Foliage
  - "Moss"/"Rock"/"Pebble"/"PolygonNature_0" → Rock
  - "Generic_0"/"Grass"/"Flower" → Grass
  - "ArenaWater"/"ArenaSea" → Water
  - "ArenaGround"/"ArenaOutskirts" → Ground
  - "ArenaWall" → Wall
  - (else) → Ground, retorna false

**ApplyEnvironment(palette) (S117 ACTUALIZADO):**
- **S117 NUEVO:** `Shader.SetGlobalFloat(ArenaSnowAmountID, palette.Snow ? 1f : 0f)` — publica flag de nieve
- ApplyLighting(palette)

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

## Invariantes S102+S111+S113+S114+S117

- **Cacheo por original:** cada material original → una instancia per slot
- **Rampas 256x1:** evaluadas en Evaluate() para suavidad
- **Clasificación por nombre:** fallback a Ground si no coincide; "Blades" → Ground (S117)
- **Wind per slot:** solo foliage y grass tienen viento
- **Water slot (S111):** usa waterMaterial si existe, sino paletteMaterial (fallback)
- **Dim materials (S113):** detecta por nombre de GO parent, aplica tint gris
- **ArenaFog globals (S113):** push en Apply(), limpieza en OnDisable
- **Center explicit (S113):** SetArenaCenter() fija centro para niebla independiente de arenaCenter
- **Variantes de follaje (S114):** Foliage y Grass leen variante desde palette.FoliageVariants por índice procedural
- **Pares siempre (S114):** variante pares en lista → índice procedural % VariantCount siempre retorna válido
- **Pasto trampled (S117):** material con _TrampleBend conserva shader, fuerza variant=0, se marca _SnowReceiver
- **Nieve global (S117):** _ArenaSnowAmount = 1.0 si paleta.Snow=true, 0.0 sino

## Conexiones

- [[ArenaPaletteSO]] (lee paletas, rampas, ArenaFog*, Snow, FoliageVariants)
- [[ArenaSandbox]] (llama ApplyIndex en BuildRoom)
- [[WorldEnums]] (ArenaPaletteSlot enum)
- [[ArenaPalette.mat]], [[ArenaPaletteWater.mat]] (template materials)
- [[ArenaTrampleMap]] (complemento: publica _ArenaTrampleTex, S117)
- [[ArenaGrassField]] (genera pasto con "Blades", S117)

## Vinculado a

[[Index/20 - MVP Combate]], [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], S114, S117
