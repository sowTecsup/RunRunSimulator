---
name: feedback-diseno-legible-en-simientos
description: Juan rechaza soluciones visuales/UI para problemas de sistema; una mecánica debe explicarse en una frase de texto plano
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 45ab2983-d898-4be8-b5e1-03e568fbc5ec
  modified: 2026-08-07T22:43:04.685Z
---

Cuando un sistema no se entiende, Juan NO quiere propuestas de presentación (más texto flotante, más indicadores, mejor cámara). Quiere replanteo de género y de mecánica. Su frase: *"los visuales solo sirven cuando una idea está bien implementada; Pokémon Rojo era solo texto e imágenes básicas y se entiende — una buena idea es legible en sus simientos"*.

**Why:** en S72 el orquestador propuso tres mejoras de legibilidad visual para el combate y Juan las descartó las tres por ser parches. El diagnóstico correcto resultó ser de diseño: exceso de estado simultáneo, causalidad no local y resultado dominado por el azar.

**How to apply:** antes de proponer cualquier cosa, pasarla por el **test del texto plano** — si la mecánica no se explica en una frase sin tabla, el problema es la mecánica. Referencia de lo que SÍ pasa el test: Darkest Dungeon ("este ataque se usa desde las posiciones 3-4 y golpea las 1-2"). Ver [[project_refundacion_combate]] y `MoriMonchiVault/Index/17 - Refundacion del Combate.md`.
