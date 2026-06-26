# Marco de slot de equipo (9-slice)

- **PNG destino:** `Resources/Sprites/UI/equip_slot_frame.png`
- **Tamaño:** 512×512
- **Aspect:** 1:1
- **Key:** green
- **Alfa:** vía chroma key — se genera con fondo + centro en verde puro `#00FF00` y `key-transparency.ps1` lo vuelve transparente (Nano Banana no entrega alfa real)
- **9-slice:** borde uniforme en los 4 lados (para el `border` del Sprite Editor)
- **Uso:** marco de cada nodo (Arma/Armadura/Amuleto). Se tinta por código según estado/rareza, así que generalo en **gris-neón claro / casi blanco** para que el tinte mande (o magenta neutro). El ícono del ítem va adentro.

## Prompt

> A single square equipment slot frame. Simple chunky beveled border with one clean neon edge accent, hollow center, small minimal corner marks. Symmetrical, front view, no text, 512x512. Fill EVERYTHING that should be transparent — the outer background AND the hollow center — with flat uniform pure green #00FF00 (chroma key color); no checkerboard, no gradient, no shading inside the green areas. The frame itself uses the cyan/magenta palette only. Uniform border thickness on all four sides so it can be 9-sliced.

## Notas

- Si lo querés en dos estados, generá también `equip_slot_frame_active.png` (mismo prompt, borde más brillante + glow más fuerte). Opcional: hoy el "encendido" lo hago tinteando por código.
