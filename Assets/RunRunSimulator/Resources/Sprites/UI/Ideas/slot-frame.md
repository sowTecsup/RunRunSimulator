# Marco de slot de equipo (9-slice)

- **PNG destino:** `Resources/Sprites/UI/equip_slot_frame.png`
- **Tamaño:** 512×512
- **Aspect:** 1:1
- **Alfa:** sí — centro totalmente transparente
- **9-slice:** borde uniforme en los 4 lados (para el `border` del Sprite Editor)
- **Uso:** marco de cada nodo (Arma/Armadura/Amuleto). Se tinta por código según estado/rareza, así que generalo en **gris-neón claro / casi blanco** para que el tinte mande (o magenta neutro). El ícono del ítem va adentro.

## Prompt

> Single square retro-1980s arcade UI inventory slot frame, synthwave/outrun style. Chunky beveled neon-tube border with a soft baked outer glow, hollow fully-transparent center, small corner rivets and bracket accents, subtle CRT scanline texture on the border only. Clean, symmetrical, front view, transparent background PNG with alpha, no text, high contrast, 512x512. Uniform border thickness on all four sides so it can be 9-sliced. Neon color near-white/light so it can be recolored, soft magenta-cyan glow.

## Notas

- Si lo querés en dos estados, generá también `equip_slot_frame_active.png` (mismo prompt, borde más brillante + glow más fuerte). Opcional: hoy el "encendido" lo hago tinteando por código.
