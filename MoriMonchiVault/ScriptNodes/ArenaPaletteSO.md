---
tags: [script, data, scriptableobject, expedition, palette]
---

# ArenaPaletteSO.cs

**Ruta:** `Data/Expedition/ArenaPaletteSO.cs`

**Responsabilidad:** Asset ScriptableObject que define una paleta de color para una escena de arena. Contiene rampas de colores (Dark/Mid/Light) por tipo de material (Ground, Grass, Foliage, Trunk, Rock, Wall) y tuning de iluminación/ambiente/fog/cielo. Serializado vía Odin para edición en Inspector.

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

**Rampas por slot de material (6 total):**
- `Ground` — suelo principal (verde terroso)
- `Grass` — pasto (verde claro)
- `Foliage` — follaje/arbustos (verde oscuro)
- `Trunk` — tronco de árbol (marrón)
- `Rock` — roca/piedra (gris)
- `Wall` — muro/pared (gris oscuro)

Cada ramp está pre-cargada con colores específicos por escena (pradera, desierto, etc.).

**Luz y aire:**
- `SunColor` — color del foco directional (default amarillo cálido)
- `SunIntensity` (float, Min 0, default 1.3)
- `AmbientColor` — luz ambiental plana (default azul grisáceo)
- `FogColor` — color del fog exponencial
- `FogDensity` (float, Range 0–0.05, default 0.006)
- `SkyColor` — color del cielo (solo si hay skyCamera)

## Métodos Públicos

- `RampFor(ArenaPaletteSlot slot) → Ramp` — devuelve la ramp según enum:
  - Ground → Ground
  - Grass → Grass
  - Foliage → Foliage
  - Trunk → Trunk
  - Rock → Rock
  - Wall → Wall
  - (default) → Ground

## Invariantes S102

- **6 slots de material:** correspondencia 1:1 con enum ArenaPaletteSlot
- **Colores precargados:** valores RGB editables en Inspector (no procedurales)
- **Luz global:** Sun, Ambient, Fog, Sky afectan RenderSettings (aplicado por ArenaPaletteApplier)
- **No instancia:** es un asset de data, no prefab

## Conexiones

- [[ArenaPaletteApplier]] (lee paleta, compila rampas a Texture2D 256x1, aplica RenderSettings)
- [[WorldEnums]] (ArenaPaletteSlot enum)
- [[ArenaSandbox]] (lista de palettes, selección por semilla o índice)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
