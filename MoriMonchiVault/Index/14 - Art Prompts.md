---
tags: [index, art, ui, pixel-art]
---

# 14 - Art Prompts (referencias "El Diario del Pet Shop")

> Prompts para generar REFERENCIAS de las piezas del kit de pixel art (nano banana pro o similar). Extraídos de la biblia de producción de S66. **Las salidas de IA son referencia para calcar/rehacer en Aseprite, NO asset final** — los modelos de imagen casi nunca dan pixeles perfectos en grilla.

## Cómo usar

1. Pegar el **prompt base** al inicio de CADA prompt de pieza (bloquea estilo y paleta).
2. Generar, elegir la mejor salida, y usarla de referencia en Aseprite con la paleta `Art/Palettes/morimonchi.gpl` cargada.
3. Redibujar a mano al tamaño exacto de la pieza (specs abajo). Outline SIEMPRE tinta `#3E2A1D`, nunca negro puro.
4. Export PNG 1×, fondo transparente, un archivo por pieza en `Assets/…/UI/Pixel/`.

## Prompt base (pegar al inicio de todos)

```
16-bit pixel art game UI asset, cozy warm pet-shop journal aesthetic, thick dark ink outline (#3E2A1D), flat shading with 3-tone ramps, no anti-aliasing, transparent background, single centered sprite, front view, palette limited to: coral #EF6440, teal #1F9E8A, gold #E9A11F, cream paper #FBF0DC, ink #3E2A1D.
```

## Prompts por pieza

### 1 · Hero (mockup completo — para tono general, no es una pieza)

```
…a full cozy pet-shop UI mockup styled like a handmade breeder's journal page: paper frame with hand-drawn double border, creature photos held by washi tape, ink-stamp role badges, hanging handwritten price tags, little doodles (egg, fern, dino footprint) in the margins. Warm and lived-in.
```

### 2 · Marco de página (9-slice) ★ empezar por acá

Specs: canvas **48×48 px** · 9-slice **16/16/16/16** · borde 3px + hairline interno · esquinas con pixelito de "clavo/costura" · centro liso (se estira).

```
…a rectangular paper panel frame, hand-inked double border with tiny stitch marks in the corners, empty cream center, designed for 9-slice scaling, 48x48.
```

### 3 · Cinta washi + sellos + etiqueta de precio

Specs: cinta **32×12 px** (sprite simple, alpha ~85%) · sello **40×20 px** (9-slice 8/8/6/6, se tinta por código: dibujar blanco) · etiqueta **40×24 px** (9-slice 14/8/8/8).

```
…a sheet of small UI props: a strip of washi tape (teal and peach), an ink-stamp badge with worn edges, a hanging price tag with a punched hole and string. Each item separated on transparent background.
```

### 4 · Set de garabatos (marginalia)

Specs: **16×16 px** c/u · 1 spritesheet de 6–8 · monocromo (tinta) · se salpican en esquinas al ~50% alpha.

```
…a set of tiny monochrome ink doodles on transparent background: an egg with a crack, a fern sprig, a three-toed dinosaur footprint, a sparkle, a bone, a heart. Hand-drawn marginalia style, 16x16 each, arranged in a grid.
```

### 5 · Moneda "Dabloon"

Specs: **16×16 px** · sprite (opcional 4 frames de giro).

```
…a single round gold coin icon called a "Dabloon", shiny highlight top-left, thick ink outline, cozy 16-bit, 16x16.
```

## Piezas SIN prompt (dibujar directo, son geometría simple)

- **Marco de carta/foto**: 32×32 px · 9-slice 10/10/10/10 · 2 variantes (normal + seleccionada coral) · variante "polaroid" con borde blanco-hueso grueso abajo.
- **Barra de stat**: 24×12 px · 3-slice 6/6 (izq/der) · riel + relleno recoloreable por código.

## Orden de producción recomendado

1. **Marco de página** — sola ya transforma toda la UI; con ella se arma el panel piloto (Vivero).
2. Cinta + marco de foto — las cartas de criatura quedan con alma.
3. Sellos + etiqueta + moneda — cierra tienda y ficha.
4. Garabatos — el toque final.

## Import a Unity (recordatorio)

Texture Type **Sprite (2D and UI)** · Filter **Point (no filter)** · Compression **None** · PPU **16** · Sprite Editor → Border = mismos números del 9-slice. En USS: `-unity-slice-*` + `-unity-slice-scale: 1px`.

## Vinculado a

- [[Index/05 - UI System]] — recetario UI
- Biblia completa (artifact): https://claude.ai/code/artifact/2e263d0d-8b00-41a4-b6e8-590b031f8c05
- Mockup de dirección (artifact): https://claude.ai/code/artifact/d7b9cc0d-c905-4990-ad0a-833308ace3d7
- Paleta Aseprite: `Art/Palettes/morimonchi.gpl`
