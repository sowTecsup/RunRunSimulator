---
tags: [script, world, expedition, rendering, static, deprecated]
---

# CueRibbonDrawer.cs

**Ruta:** `World/Expedition/CueRibbonDrawer.cs` (ELIMINADO EN S116)

**Estado:** DEPRECADO/ELIMINADO en S116

**Responsabilidad (Histórica):** Dibujante estático en modo inmediato que creaba cintas (ribbons) 3D parabólicas, orientadas a la cámara. Se utilizaba para visualizar el arco de la picada (Wings) durante la fase de anticipación (S110-S114). Contrato: `Configure(material, additiveMaterial)` en `OnEnable`, luego cada frame `Arc()` para dibujar.

## Por qué fue eliminado (S116)

**Cambio de diseño visual — Plantilla única = hitbox:**
- La cinta parabólica (arco visual S110) se reemplaza por un disco simple en el punto de impacto
- Las Wings ahora usan la misma plantilla que Horn/Back (disco con relleno lineal)
- Se elimina toda la complejidad de muestreo parabólico, extrusión y orientación a cámara
- Resultado: feel más limpio, menos clutter visual, predictibilidad uniforme

**Archivos relacionados eliminados:**
- `Shaders/MonchiRibbon.shader` — shader de renderizado de cintas (ELIMINADO)
- Materiales asociados: `MonchiRibbon.mat`, `MonchiRibbon_Additive.mat` (ELIMINADOS)

## Contexto S110-S114 (Histórico)

CueRibbonDrawer fue introducido en S110 para visualizar la trayectoria esperada de la picada (Wings). Parámetros de `CueStyleSO` (DiveArc*) configuraban:
- Ancho de la cinta
- Escalas de cola y cabeza
- Muestras del arco
- Parámetros de dash (longitud, separación, flujo)
- Alturas y colores

## Impacto S116

- **ArenaCueOverlay.OnEnable():** se elimina línea `CueRibbonDrawer.Configure(ribbonMaterial, ribbonAdditiveMaterial)`
- **ArenaCueOverlay:** se eliminan campos serializados `ribbonMaterial`, `ribbonAdditiveMaterial`
- **CreatureCueDrawer.Telegraph():** se elimina rama `CueRibbonDrawer.Arc()` para Wings
- **CueStyleSO:** se elimina sección entera "Flecha de la picada" (11 campos)
- **ClashMoveSO:** se elimina `LaunchAngle`, se agregan `RiseHeight` y `DiveSeconds` (geometría simplificada)

## Alternativa (S116)

Wings ahora sigue el patrón de Horn/Back: disco único en impactPoint con relleno lineal por Tell01. Despegue vertical puro sin ángulo variable. Permite una lectura visual unitaria de todas las formas de golpe.

## Conexiones (Histórico)

- ~~ArenaCueOverlay~~ — llamador
- ~~CreatureCueDrawer~~ — usa en Telegraph()
- ~~CueStyleSO~~ — parámetros DiveArc* (eliminados)
- ~~MonchiRibbon.shader~~ — contrato de propiedades (eliminado)

## Notas de Archivos

- Código del script: eliminado del repositorio
- Referencias en otros scripts: removidas/refactorizadas
- Documentación: este nodo mantiene el historial para auditoría

