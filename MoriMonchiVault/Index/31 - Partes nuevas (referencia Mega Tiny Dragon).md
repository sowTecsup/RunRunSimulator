---
tags: [index, visual, genetics, referencia]
---

# 31 - Partes nuevas (referencia Mega Tiny Dragon)

**Sesión:** S133 (2026-09-25) · **Estado:** referencia guardada; diseño de partes pendiente (sesión propia).

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
3. ¿Cuántas partes por ranura entran en la primera tanda?
