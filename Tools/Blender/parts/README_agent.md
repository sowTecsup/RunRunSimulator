# Parts pipeline (S134) - brief para agentes modeladores

- Cada parte es `Tools/Blender/parts/p_<Nombre>.py` con `SLOTS = ("Horn",)` (o "Back"/"Wing", o varios) y `build(arm) -> [objetos]`.
- Ejemplo completo y aprobado: `p_Unicornio.py`. Helpers en `part_common.py` (NO editarlo; si necesitas un helper, va en tu propio archivo):
  `head_top(x, y)` punto+normal sobre la cabeza (ray desde arriba; frente del dragon = -Y, arriba = +Z, alto ~1.3, cara en y~-0.78, cabeza arriba z~1.3 en y~-0.35);
  `surface_point(origin, dir)` ray contra `Dragon_body`; `tube(name, pts, radii, sides, side_hint)` (radii escalar o (ancho, grosor));
  `bezier(p0,p1,p2,p3,n)`; `mirror_x(obj, name)`; `join(objs, name)`; `skin_like(obj, arm, slot)` copia pesos del cuerno/espalda/alas horneados (Horn->HornA, Back->BackA, Wing->Wing_A) y agrega el modificador Armature — llamarlo en TODO objeto devuelto.
- Referencias del cuerpo actual: cuerno horneado HornA x +-0.49, y -0.61..-0.12, z 0.82..1.41; espalda BackA sigue la columna y -0.18..1.05, z hasta 1.42; alas Wing_A salen de los huesos WingL1/WingR1 en (+-0.5, -0.08, 1.09) y se extienden planas en X hasta +-1.19.
- Nombres de objeto (contrato con Unity): `Horn_<Nombre>...`, `Back_<Nombre>...`, `Wing_<Nombre>...` (se tiñen con el acento/alas del ADN) o `Deco_RRGGBB_<algo>` (color fijo hex, solo si la referencia es claramente multicolor o de color fijo).
- Correr: PowerShell `& "<repo>\Tools\Blender\parts\build_part.ps1" -Name <Nombre> -Out <dir>` (renders `<Nombre>_side.png`, `<Nombre>_front.png` y `MonchiPart_<Nombre>.fbx`). Vistas extra: `-Views side,front,top,back`.
- Comparar: `python "<repo>\Tools\Blender\parts\cmp.py" <foto_ref> "x0,y0,x1,y1" <dir>\cmp_<Nombre>.png <dir>\<Nombre>_side.png <dir>\<Nombre>_front.png` y mirar la imagen con Read.
- Los cuerpos de la referencia son distintos (ballena, bola); copiar la PARTE (forma, proporcion relativa a la cabeza, ubicacion, orientacion), no el cuerpo. Poligonos: < 2500 vertices por parte. Sin huecos visibles entre la parte y la piel (hundir la base unos cm).

## Vara de calidad (lecciones del lote 1)
- Ejemplos aprobados: `p_Unicornio.py`, `p_Astas.py` (puntas que se afinan con `tine()`), `p_Placas.py` (gemas facetadas), `p_Aletas.py` (membrana + varillas).
- Estilo chibi: las partes son GORDAS y GRANDES respecto de la cabeza. Si dudas, mas grande y mas grueso. Nada de palitos finos ni bloques chicos.
- Puntas afinadas hasta ~0.006 de radio (no cortes planos), transiciones suaves (arrancar la rama DENTRO del tronco).
- Medir el tamaño contra la foto: comparar el alto de la parte con el alto del cuerpo en el cmp antes de dar por terminado.

## Sets completos (feedback de Juan S134)
- Juan juzga cada parte DENTRO de su set (cuerno + espalda + alas como en la foto). Renderiza el set: `& "<repo>\Tools\Blender\parts\build_set.ps1" -Names "Rayo,PuasFinas" -Tag Rayo -Out <dir>` → `set_<Tag>_side.png` / `set_<Tag>_front.png`, y compara con `cmp.py` usando esos renders.
- Referencias ampliadas de las partes corregidas: `scratchpad\parts\refs_zoom.png` (3 celdas por parte).
