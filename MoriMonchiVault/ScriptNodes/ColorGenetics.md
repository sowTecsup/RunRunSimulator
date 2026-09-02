---
tags: [script, genetics, color]
---

# ColorGenetics.cs

**Ruta:** `Core/ColorGenetics.cs`

**Responsabilidad:** Lógica de color y determinismo visual. `RandomBase()` genera color base aleatorio pastel (S58). `RandomBase(System.Random rng)` **S95** sobrecarga determinista usando RNG explícito (para rival de combate). `DeriveSecondary()` desatura/clarifica desde base. `BuildFurPalette()` arma 4-tupla para Toon shader (Base, Shade1, Shade2, Rim). `Inherit()` mezcla dos padres con jitter. `RollShiny()` 0.5% por ShinyChance. `BuildHarmony()` determinista por hash RGB (4 esquemas: 40/30/20/10).

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `RandomBase()` | `Color` | **S58:** Pastel HSV (H 0-1, S 0.35-0.6, V 0.8-1) |
| `RandomBase(System.Random rng)` | `Color` | **S95** Pastel HSV determinista con RNG explícito; mismos rangos que RandomBase() |
| `DeriveSecondary(baseColor)` | `Color` | Hue+8%, sat*85%, value+15% |
| `BuildFurPalette(baseColor, secondary)` | `FurPalette` | Base, Shade1, Shade2, Rim |
| `Inherit(a, b)` | `Color` | Lerp 50% + jitter ±4°H ±5%S/V |
| `Inherit(mother, father)` | `FurType` | Random 50/50 |
| `RollShiny()` | `bool` | true con prob 0.005 (0.5%) |
| `BuildHarmony(baseColor, out wing, out accent)` | `void` | Determinista FNV-1a hash (40/30/20/10 split) |

## Cambios S58

**RandomBase() retorna colores pastel:**
- Antes: `Random.ColorHSV(0, 1, 0, 1, 0, 1)` — rango completo
- Ahora: `Random.ColorHSV(0, 1, 0.35f, 0.6f, 0.8f, 1f)` — pastel
  - Saturation: 35-60% (suave, no neon)
  - Value: 80-100% (brillante, no oscuro)

**Impacto:**
- Nuevas crías pastel más armoniosas
- Colores existentes NO afectados (DNA inmutable)
- Solo afecta GenerateRandom()

## Cambios S95

**RandomBase(System.Random rng) sobrecarga determinista:**
- Permite re-colorizar rivales con RNG explícito (seeded desde Timestamp ⊕ now)
- Rangos idénticos a RandomBase()

## Struct FurPalette

```csharp
public struct FurPalette
{
    public Color Base;    // Cuerpo
    public Color Shade1;  // Sombra oscura
    public Color Shade2;  // Sombra media
    public Color Rim;     // Borde/highlight
}
```

## Notas

- Herencia: 50% lerp + jitter (±4°H, ±5%S/V)
- Harmony determinista (mismo hash → mismos colores alas)
- ShinyChance 0.5% (1 en 200)
- BuildHarmony: 4 esquemas de armonía por rango hash

