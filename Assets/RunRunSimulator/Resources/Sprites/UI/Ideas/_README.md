# Ideas — assets de UI a generar (workflow Juan ↔ IA)

Carpeta puente para piezas de arte de UI. Flujo:

1. La IA deja acá un `.md` por pieza: **prompt + nombre de archivo destino + specs técnicas**.
2. Las imágenes se generan con **Gemini (Nano Banana)** vía `Tools/gen-image.ps1` (lee el prompt, el `PNG destino` y el `Aspect` del propio `.md`).
3. El PNG queda en `Resources/Sprites/UI/` con el nombre destino, y la IA lo asigna en el panel (UXML/USS `background-image` o 9-slice).

## Generación automática (Gemini)

**La key NUNCA va en un archivo trackeado por git.** Dos formas seguras:

- **Opción A (entorno):** `setx GEMINI_API_KEY "tu-key"` (persiste; reiniciá la shell), o `$env:GEMINI_API_KEY = "tu-key"` para la sesión.
- **Opción B (archivo local):** pegá la key en `Tools/gemini.key` — ese archivo está en `.gitignore` (`Tools/*.key`), así que **no se pushea**. El script lo lee si no encuentra la variable de entorno.

```powershell
# Generar:
./Tools/gen-image.ps1 slot-frame    # una pieza
./Tools/gen-image.ps1 -All          # todas
./Tools/gen-image.ps1               # lista las ideas disponibles
```

Modelo por defecto: `gemini-3.1-flash-image-preview` (override con `-Model`). La key se lee del entorno, nunca se escribe en disco. Con `setx` hecho, el asistente puede correr el script y generar las piezas él mismo.

## Estética (alineada al juego)

**Retro 80s** — synthwave / outrun / arcade. NO cyberpunk moderno. La **distribución** es tipo ficha (personaje central + nodos alrededor + conectores), pero el **skin** es retro: tubos de neón chunky, CRT/scanlines, plástico ochentoso, grilla outrun, sol-horizonte.

**Paleta base** (para que pegue con el USS actual):
- Cian neón `#00DCFF` (rgb 0,220,255)
- Magenta neón `#FF46B4` (rgb 255,70,180)
- Púrpura/negro de fondo `#10121C` (rgb 16,18,28)
- Acentos ámbar opcional `#FFB347`

## Specs técnicas (importante)

- **PNG con alfa** (centro transparente) salvo el fondo de panel (puede ser opaco).
- **9-slice**: borde de grosor **uniforme** en los 4 lados y **centro vacío/transparente** → así escala sin deformar. Tras importar: Texture Type = `Sprite (2D and UI)`, y en el Sprite Editor seteo el `border` (Juan, dejámelo y yo paso los valores de `-unity-slice-*`).
- El **glow va horneado en el PNG** (UITK no tiene box-shadow/glow).

## Piezas

| Archivo md | PNG destino | Uso |
|---|---|---|
| `slot-frame.md` | `equip_slot_frame.png` | Marco de cada nodo de equipo (9-slice, se tinta por estado) |
| `portrait-frame.md` | `equip_portrait_frame.png` | Marco del MoriMochi central |
| `panel-bg.md` | `equip_panel_bg.png` | Fondo del panel Equipo (grilla + esquinas HUD) |
