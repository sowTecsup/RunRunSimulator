---
tags: [script, genetics]
---

# ColorGenetics.cs

**Ruta:** `Core/ColorGenetics.cs`

**Responsabilidad:** Lógica de color y fur type para MoriMonchis. `RandomBase()` genera color aleatorio en rango saturado (0.6..1 S, 0.6..1 V). `DeriveSecondary(Color base)` → hue-shift 0.08 + S×0.85 + V+0.15 determinista desde BaseColor (acento visual). `BuildFurPalette(Color base, Color secondary)` → **nuevo struct `FurPalette`** con 4 colores para shader Toon: `Base` (el base), `Shade1` (base 60% value + 8% sat), `Shade2` (Lerp base→secondary 35% con 40% value + 12% sat), `Rim` (el secondary). `Inherit(Color, Color)` mezcla dos padres con variación aleatoria en H/S/V. `Inherit(FurType, FurType)` selecciona 50/50 de un padre.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[CreatureGenerator]], [[BreedingService]], [[MoriMonchiVisualizer]], [[FurType]]
