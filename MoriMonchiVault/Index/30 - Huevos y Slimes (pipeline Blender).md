---
tags: [index, visual, pipeline, blender]
---

# 30 - Huevos y Slimes (pipeline Blender)

**Sesión:** S132 (2026-09-25) · **Estado:** assets de prueba validados por Juan; sin código de juego todavía (el componente que los arma va con la incubadora en HC-5; las animaciones, la sesión siguiente).

## 1. Idea (diseño de Juan, DRAFT)

Ciclo visual de tres etapas: **huevo → slime ("dango dragon") → dragón adulto**. La genética se revela de a poco:

| Etapa | Qué muestra | Regla de Juan |
|-------|-------------|---------------|
| Huevo | piel (patrón + paleta), lomo real y cachitos suaves, escamas muy ligeras | **insinúa, no copia** ("¡woah, qué me podrá tocar!"); sin alas, sin nariz |
| Slime | piel, **cuernos del adulto 1 a 1** y la cara con ánimo | solo cuernos de las partes; cara al 50 % |
| Dragón | todo (modelo Suriyun actual) | — |

Regla general: **toda parte de un MoriMochi tiene su versión bebé**; nunca se inventan piezas, se derivan de las mallas Suriyun reales. Pendiente de diseño: en qué `CreatureLifeStageTableSO` entra el slime.

## 2. Assets

| Asset | Ruta | Contenido |
|-------|------|-----------|
| `MonchiEgg.fbx` | `Resources/Models/MoriMochi/` | raíz = cáscara (32 cm, pivote en la base, frente +Z); hijos `Egg_Horn_A..D`, `Egg_Back_A`, `Egg_Back_B`, `Egg_Scales` |
| `MonchiSlime.fbx` | `Resources/Models/MoriMochi/` | raíz `Slime_Body` (30 × 21,4 cm); hijos `Horn_A..D`, `Face` |
| `EggScales.png` | `Resources/Textures/MoriMochi/Eggs/` | escamas de pez negras, alfa ≤ 0,5, repetible |
| `EggScales.mat` | `Resources/Materials/MoriMochi/Eggs/` | `Unlit/Transparent` con `EggScales.png` |

**Cómo se arma en Unity** (probado con `execute_code`, sin componente todavía):
- Cuerpo y partes usan `MonchiFur_XX` (el mismo material de la criatura, por `FurType`) y el mismo tinte MPB de [[MonchiVisualizer]] (`ColorGenetics.BuildFurPalette`; cuerpo = `BaseColor`, cuernos y lomo = `accent` de `BuildHarmony`).
- Pieza por cuerpo: `MonchiVisualBankSO.GetBody(BodyShapeID)` → `MonchiBody_X` → se prende `Horn_X`; lomo `Back_B` solo para el cuerpo D, el resto `Back_A`.
- `Face` del slime: material de ánimo de [[MonchiMoodDriver]]/`MonchiMood` (se llama `Face` a propósito para reusar `SetMood`).
- Shiny: material gema (`MonchiGem_*`) en todas las piezas.
- `Egg_Scales` lleva `EggScales.mat`; se puede apagar sin tocar la piel.

## 3. Pipeline (`Tools/Blender/`)

`build_monchi_eggs.ps1` corre todo en Blender 4.5.3 sin ventana (instalado por winget en S132) y copia los resultados a las rutas de arriba. Reproducible: mismos números en cada corrida.

| Script | Responsabilidad |
|--------|-----------------|
| `monchi_egg.py` | genera la cáscara (`egg` o `slime`) como superficie de revolución con UV |
| `transfer_skin.py` | transfiere la piel del `Dragon_body` real, pega las partes reales, exporta FBX |
| `gen_scales.py` | genera `EggScales.png` |
| `ref_profile.py` / `fit_profile.py` | miden la silueta de una imagen de referencia y ajustan el perfil por búsqueda en grilla |
| `compare.py` | compone referencia arriba / render abajo para comparar |

### 3.1 Transferencia de piel
Cada polígono de la cáscara toma el triángulo más cercano del `Dragon_body` (coordenadas normalizadas a la caja de cada malla) y extrapola sus UV. Se excluyen patas, brazos y cola (huevo: toda la cola y el 25 % inferior del dragón; slime: cola 3-4). Todo polígono **malo** busca piel limpia desplazándose (arriba, abajo, costados, hasta 0,45):
- **sale de su isla UV**: el `Dragon_body` tiene 4 islas; se rasterizan a 512² y cada muestra debe caer en la isla de su triángulo fuente (esto eliminó las costuras);
- **oscuro en los patrones** (promedio de 01/09/16/25 < 0,45), revisando todos los píxeles del triángulo;
- **fosas nasales**: oscuro en `MonchiPattern_00` dentro de la zona del hocico.

### 3.2 Partes
- **Huevo:** `conform` pega cada vértice sobre la curva (punto más cercano del cuerpo → caja normalizada → rayo a la cáscara). Cuernos ×0,55, subidos y suavizados 25 pasadas; lomo ×1.
- **Slime:** `place_rigid` copia el cuerno **rígido** (escala uniforme 0,208, misma orientación) anclado en su raíz; los pares nacen en los hombros del domo (`lift` 0,6) y el único (B) arriba (0,25). Se hunde por bisección hasta tener **el mismo % de vértices adentro que en el adulto** (A 33 · B 28 · C 16 · D ~25).
- **Cara del slime:** `conform` de la malla `Face` al 50 % sobre la superficie (achicar en 3D la hundía).

### 3.3 Forma del slime (dango)
Superelipse de revolución ajustada a la silueta del slime blanco de la referencia de Juan: alto 0,714 del ancho, ancho máximo al 39 % de la altura, base p=3 con corte plano (t0 0,94), domo p=2,1. Error medio del perfil 0,008 (la versión a ojo daba 0,089).

## 4. Lecciones (quirks)

- **Pegar vértice a vértice deforma** las piezas grandes (la caja dragón→slime no escala igual en cada eje): para "1 a 1" usar copia rígida.
- La piel del dragón tiene zonas que en el dragón quedan tapadas u ocultas (fosas, huecos del atlas, panza de baja resolución): hay que detectarlas, no confiar en el mapeo.
- `HornB` va acostado sobre el hocico: medir "cuánto sale" contra el cuerpo no sirve para ese cuerno; por eso el hundido se calibra con el % de vértices adentro del adulto.
- En PowerShell `$f` y `$F` son la misma variable, y un argumento `""` se pierde al pasarlo a un ejecutable nativo.
- Verificación: render ortográfico en `GameScene` a y=-500, capa `MonchiFocus` (10), cámara con post-proceso URP, escena descartada al terminar (no se guarda).

## 5. Animaciones del slime (S133)

- `rig_slime.py` corre sobre `MonchiSlime_Skin.blend` y reescribe `MonchiSlime.fbx` con el esqueleto `Slime_Rig` (`Root` → `Body` → `Top`, cabeza de `Top` a 0,12 m) y 15 acciones. Pesos: `Top` = smoothstep(0,07 → 0,17 m de alto); cuernos y cara copian el peso del vértice del cuerpo más cercano. Cada clip es una función `pose(t)` muestreada cuadro a cuadro a 30 fps.
- Unity: Generic, clips `Slime_<Nombre>`, bucle solo en Idle/Move/Happy/Excited/Stun/Sick/Eating. `MonchiSlimeAnimator.controller` usa los nombres de estado del adulto (tabla en `09` S133). Banco de pruebas: `Resources/Scenes/SlimeAnimLab.unity`.
- Quirks: **si cambia el largo de un clip**, el importador conserva el rango viejo en `clipAnimations`; hay que copiar `firstFrame`/`lastFrame` desde `defaultClipAnimations`. Los signos de rotación de `Top` están invertidos respecto de la intuición (pitch negativo = hacia adelante): verificar midiendo `topY` (frente = −Y en Blender).
- Piel: `FLOOR` del slime = 0,3 y `plain_bottom` mapea la base plana a la esquina del atlas (fondo liso de cada patrón; en 6 de 33 patrones difiere del borde en más de 0,1).

## 6. Pendiente

1. Idles del huevo (tintineo, saltitos, bamboleo).
2. Componente de ensamblado (leer ADN → prender piezas → tintar), con la incubadora en HC-5.
3. Ubicar el slime en `CreatureLifeStageTableSO` (decisión de diseño).
4. Opcional: punta de cuerno en degradado; limpiar parámetros sin uso de `transfer_skin.py` (`marks`, `covers`, `strip_constant_marks`).
