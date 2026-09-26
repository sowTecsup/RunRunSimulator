---
tags: [index, visual, genetics, referencia]
---

# 31 - Partes nuevas (referencia Mega Tiny Dragon)

**Sesión:** S133-S134 (2026-09-25) · **Estado:** 29 partes modeladas e injertables (S134); falta el alta en el ADN.

Juan quiere sumar variedad de **cuernos, espalda y alas** tomando como referencia el **MEGA Tiny Dragon Pack** (Suriyun, el mismo autor de `Dragons_SD`): <https://assetstore.unity.com/packages/3d/characters/creatures/mega-tiny-dragon-pack-263748>. USD 144 · 329 MB · v3.2 (2025-12-15) · Built-in/URP/HDRP · EULA estándar. La página no detalla partes ni si son mallas separadas.

Con esto, según Juan, se completa la variedad que le falta a los MoriMonchis, con **3 formas de adulto**, y se cierra la sección de personaje.

## Capturas (`References/MegaTinyDragon/`)

| Archivo | Silueta | Lo que aporta |
|---------|---------|---------------|
| `MTD_1_ballena_unicornio_astas_carnero.png` | cuerpo de ballena con colita | unicornio espiral + melena, astas de ciervo, carnero enroscado, placas de estegosaurio, aletas |
| `MTD_2_bola_antenas_lana_rayos.png` | bola con patitas | antenas largas dobles, nube de lana, rayos en zigzag, cresta de cometa |
| `MTD_3_bola_hoces_orejeras_cristales.png` | bola con alitas | hoces, orejeras enroscadas, racimo de cristales, bloques de malvavisco, moño, cintas |
| `MTD_4_rino_triceratops_luna_abanico.png` | robusto | cuerno de rinoceronte y triceratops, luna creciente, abanico o vela de cresta |
| `DragonsSD_actual.png` | el cuerpo actual | lo que ya tenemos: cuernitos de toro, cuerno único, hilera de púas |

## Catálogo preliminar por ranura (lectura a ojo, confirmar con el pack)

- **Cuerno:** unicornio espiral · astas · carnero · antenas · rayo · hoz · orejeras · rinoceronte/triceratops · luna.
- **Espalda:** placas · lana · cometa · cristales · malvaviscos · abanico · púas · moño.
- **Alas:** aletas · alitas de murciélago · cintas · plumitas.

## Reglas que aplican

- Los IDs de parte no pueden llevar `-` (separador del ADN). El mapa parte → habilidad vive en `AbilitySO.PartIds`.
- **Toda parte tiene su versión bebé** en el huevo y el slime ([[Index/30 - Huevos y Slimes (pipeline Blender)]]), derivada de la malla real: el pipeline de Blender las genera solas si las mallas existen.

## Decisiones abiertas (Juan)

1. ~~¿Comprar el pack o modelar?~~ **Resuelto al cerrar S133: se modela** (sesión S134).
2. ¿Cuáles son las 3 formas de adulto y cómo entran en `BODYSHAPE` del ADN?
3. ~~¿Cuántas partes por ranura entran en la primera tanda?~~ **Resuelto S134: todas las de las fotos.**

## Implementación (S134)

**29 partes** modeladas en Blender sobre el cuerpo actual (`Tools/Blender/parts/p_<Nombre>.py`), FBX en `Resources/Models/MoriMochi/Parts/`, prefabs con `MonchiFur_00` en `Resources/Prefabs/MoriMochi/Parts/`.

| Set (como en la foto) | Cuerno | Espalda | Alas |
|---|---|---|---|
| Unicornio | Unicornio (arcoíris + melena, `Deco_`) | Borla (penacho arcoíris) | horneada |
| Astas | Astas | horneada | horneada |
| Carnero | Carnero | horneada | horneada |
| Ballena de aletas | AletasCara | Placas | Aletas |
| Antenas | Antenas | horneada | horneada |
| Lana | Lana | LomoLana | horneada |
| Rayo | Rayo | PuasFinas | horneada |
| Cometa | Cometa | horneada | horneada |
| Copete | Mechon | horneada | horneada |
| Hoz | Hoz | horneada | Murcielago |
| Pez abisal | Senuelo | horneada | Cintas |
| Cristal | Cristal | Cristales | Plumitas |
| Malvavisco | Tapones | Malvaviscos | horneada |
| Triceratops | Triceratops | horneada | horneada |
| Rinoceronte | Rinoceronte | PuasGruesas | horneada |
| Cuernitos | Cuernitos | PuasDobles | horneada |
| Abanico | horneado | Abanico | horneada |

- **Pipeline:** `part_common.py` (helpers, pesos copiados de la parte horneada del mismo slot), `build_part.ps1 -Name X` (FBX + renders), `build_set.ps1 -Names "A,B" -Tag T` (set completo), `cmp.py` (foto al lado del render), `README_agent.md` (brief y vara de calidad para agentes).
- **Contrato Blender → Unity:** nombres `Horn_*`/`Back_*`/`Wing_*` (tinte del ADN) o `Deco_RRGGBB_*` (color fijo). Injerto en [[MonchiPartGrafter]].
- **Criterio de Juan:** cada parte se juzga dentro de su set; colores multicolor solo donde la foto lo es.
- **Pendiente:** veredicto de Juan sobre la ronda de correcciones; alta en las bases de partes (ID sin `-`, stats, rareza, habilidad) y `partMeshes` del banco; versión bebé en huevo/slime.
