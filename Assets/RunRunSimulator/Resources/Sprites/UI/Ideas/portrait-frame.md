# Marco del MoriMochi central (9-slice)

- **PNG destino:** `Resources/Sprites/UI/equip_portrait_frame.png`
- **Tamaño:** 768×768
- **Aspect:** 1:1
- **Key:** green
- **Alfa:** vía chroma key — fondo + centro en verde puro `#00FF00`, lo vuelve transparente `key-transparency.ps1` (ahí va el swatch/sprite del MoriMochi)
- **9-slice:** borde uniforme (centro vacío estirable)
- **Uso:** enmarca el portrait central del panel Equipo. Más vistoso que los slots.

## Prompt

> A larger square portrait frame, same family as the slot frames. Chunky beveled border with a clean neon edge accent and a small simple sun-and-grid mark centered along the bottom edge, hollow center, minimal corner marks. Front view, centered, symmetrical, no text, 768x768. Fill EVERYTHING that should be transparent — the outer background AND the hollow center — with flat uniform pure green #00FF00 (chroma key color); no checkerboard, no gradient, no shading inside the green areas. The frame itself uses the cyan/magenta palette only. Uniform border thickness for 9-slicing.

## Notas

- El centro queda transparente: por debajo pinto el `BaseColor` del MoriMochi (y a futuro su sprite).
