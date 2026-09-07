---
name: feedback-no-overengineering
description: "No poner responsabilidades en el componente equivocado por \"completitud\"; el approach más simple suele ser el correcto"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 7589ca40-e0ed-4cc1-afdd-a0779a89c2ea
---

El enfoque más simple para asignar responsabilidades en Unity casi siempre es el correcto. Antes de diseñar un sistema, preguntarse: ¿quién ya tiene la información que necesito?

**Why:** En el Visual Assembler, el primer diseño puso el mapa de sockets (armSockets, eyeSockets, mouthSocket) dentro del prefab del body (BodyShapeJoints). Esto era incorrecto: el body prefab no sabe qué otras partes existen, y forzarlo a saber genera acoplamiento y confusión de responsabilidades. La solución correcta fue obvia en retrospectiva: el Visualizer ya tenía todas las posiciones de los anchors, así que EL VISUALIZER es el mapa de sockets. Los prefabs de partes solo necesitan saber su propio punto de inserción.

**How to apply:** Antes de agregar un campo o componente nuevo, preguntar:
1. ¿Este objeto ya tiene la información, o estoy duplicando algo que existe en otro lado?
2. ¿Este componente necesita realmente saber sobre los otros, o puede ser completamente ignorante de ellos?
3. Si la respuesta a 2 es "no necesita saber" → no darle referencias cruzadas.

Tres líneas similares son mejor que una abstracción prematura. Un componente que hace UNA cosa bien es mejor que uno que hace TODO "para completitud".
