---
tags: [memory-bank, script, genetics]
---

# ColorGenetics.cs

**Ruta:** `Core/ColorGenetics.cs`

**Responsabilidad:** Lógica de color y fur type para MoriMonchis. `RandomBase()` genera color aleatorio en rango saturado. `DeriveShadow()`/`DeriveOutline()` derivan colores secundarios desde el base con variación HSV. `Inherit(Color, Color)` mezcla dos padres con variación aleatoria en H/S/V. `Inherit(FurType, FurType)` selecciona 50/50 de un padre.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[CreatureGenerator]], [[BreedingService]], [[MoriMonchiVisualizer]], [[FurType]]
