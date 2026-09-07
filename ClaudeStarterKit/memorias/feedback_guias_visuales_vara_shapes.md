---
name: feedback-guias-visuales-vara-shapes
description: "La vara de calidad de toda guía visual en el mundo es el asset Shapes (Freya Holmér); Juan no quiere tener que pedir trazos punteados, puntas redondas, movimiento, curvas ni transiciones — deben salir así de entrada"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: d44a63c7-b003-446d-9cec-998ec5b5a681
  modified: 2026-09-03T22:06:51.308Z
---

Juan (S97, 2026-09-03): *"Así como mejoré tus clues visuales iniciales, no quiero tener que decírtelo; por eso te pasé de referencia ese asset, que tiene MUY BUENAS clues visuales, que me gustaría que pudieras generar igual para que se vea más appealing."* La primera versión de las guías (anillo liso, línea recta, sin transiciones) cumplía el mínimo y él tuvo que pedir: punteado con puntas redondas, rotación constante, ruta curva según el camino, transiciones suaves al cambiar.

**Why:** la legibilidad y el "appeal" de las guías son parte del prototipo, no decoración; Juan las lee como señal de calidad del sistema. Es la versión concreta, para el mundo 3D, de [[feedback-legibilidad-primero]] ("nunca cumplir el mínimo") y de [[feedback-qa-proactivo-referentes]].

**How to apply:** antes de entregar cualquier guía visual, pasarla por la lista de Shapes: trazos con grosor consistente y anti-aliasing; puntas redondas; punteados con offset animado en la dirección del movimiento; arcos y anillos parciales; degradados de alfa a lo largo del trazo; curvas en vez de rectas; todo aparece y desaparece con fundido y escala, nada "pope"; movimiento lento y constante donde algo está vivo o activo. Vocabulario detallado en `Index/23` (sección "Lenguaje visual de guías"). Nuestra implementación es el shader SDF `MonchiCue` + `CueDrawer`; si una forma no está, se agrega al shader antes de entregar, no después de que Juan la pida.
