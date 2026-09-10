---
tags: [script, data, scriptableobject, expedition, palette]
---

# ArenaPaletteSO.cs

**Ruta:** `Data/Expedition/ArenaPaletteSO.cs`

**Responsabilidad:** Asset ScriptableObject que define una paleta de color para una escena de arena. Contiene rampas de colores (Dark/Mid/Light) por tipo de material, tuning de iluminación/ambiente/fog/cielo, y parámetros de niebla radial. Serializado vía Odin para edición en Inspector. **S113:** niebla radial (ArenaFog*) configurable por paleta.

## Struct Ramp

```csharp
public struct Ramp
{
    public Color Dark;      // Sombra
    public Color Mid;       // Medio tono
    public Color Light;     // Luz
    
    public Color Evaluate(float t)  // [0,1] → color interpolado
}
```

Ramp es una rampa tricolor suavizada: `t < 0.5 ? Lerp(Dark, Mid, t*2) : Lerp(Mid, Light, (t-0.5)*2)`

## Campos Públicos

**Identidad:**
- `DisplayName` (string, default "Pradera") — nombre legible en UI

**Rampas por slot de material (7 total en S111+S113):**
- `Ground` — suelo principal (verde terroso)
- `Grass` — pasto (verde claro)
- `Foliage` — follaje/arbustos (verde oscuro)
- `Trunk` — tronco de árbol (marrón)
- `Rock` — roca/piedra (gris)
- `Wall` — muro/pared (gris oscuro)
- `Water` — agua (azul océano) con Dark/Mid/Light

Cada ramp está pre-cargada con colores específicos por escena (pradera, desierto, etc.).

**Luz y aire:**
- `SunColor` — color del foco directional (default amarillo cálido)
- `SunIntensity` (float, Min 0, default 1.3)
- `AmbientColor` — luz ambiental plana (default azul grisáceo)
- `FogColor` — color del fog exponencial
- `FogDensity` (float, Range 0–0.05, default 0.006)
- `SkyColor` — color del cielo (solo si hay skyCamera)

**Niebla de arena (S113):**
- `ArenaFogInner` (float, Min 0, default 26) — radio interior (sin efecto)
- `ArenaFogOuter` (float, Min 0, default 60) — radio exterior (máximo efecto)
- `ArenaFogTint` (Color, default 0.06, 0.09, 0.1 oscuro) — color de la niebla
- `ArenaFogStrength` (float, [0-1], default 0.95) — intensidad del efecto
- `ArenaFogDim` (float, [0-1], default 0.75) — dimming de periféricos (Border/Surround)

## Métodos Públicos

- `RampFor(ArenaPaletteSlot slot) → Ramp` — devuelve la ramp según enum:
  - Ground → Ground
  - Grass → Grass
  - Foliage → Foliage
  - Trunk → Trunk
  - Rock → Rock
  - Wall → Wall
  - Water → Water
  - (default) → Ground

## Invariantes S102+S111+S113

- **7 slots de material:** correspondencia 1:1 con enum ArenaPaletteSlot
- **Colores precargados:** valores RGB editables en Inspector (no procedurales)
- **Luz global:** Sun, Ambient, Fog, Sky afectan RenderSettings (aplicado por ArenaPaletteApplier)
- **ArenaFog:** parámetros globales de niebla radial (aplicados por ArenaPaletteApplier.PushArenaFog)
- **No instancia:** es un asset de data, no prefab

## Conexiones

- [[ArenaPaletteApplier]] (lee paleta, compila rampas a Texture2D 256x1, push ArenaFog globales)
- [[WorldEnums]] (ArenaPaletteSlot enum)
- [[ArenaSandbox]] (lista de palettes, selección por semilla o índice)

## Vinculado a

[[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]
