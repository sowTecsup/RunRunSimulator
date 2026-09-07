---
name: feedback-verificacion-visual-screenshots
description: El ritual de verificación DEBE incluir capturas del Game view (manage_camera screenshot) mirándolas de verdad — la verificación por estado no detecta problemas visuales
metadata: 
  node_type: memory
  type: feedback
  originSessionId: dc9b9793-00c1-456f-88a5-6dcc9494a6a0
  modified: 2026-08-27T21:57:41.494Z
---

Regla de pipeline acordada con Juan (2026-08-27, S85, tras la auditoría QoL): toda sesión que toque presentación, UI, escena o cámara cierra con verificación VISUAL, no solo por estado.

**Why:** 5 sesiones verificadas únicamente por estado (fases, eventos, posiciones, consola 0 errores) dejaron pasar 20 problemas de QoL — banner pisando la línea de selección, cards tapando tablero jugable, labels de canto tras la órbita, glifo ☠ renderizando tofu "□". Todos visibles en 5 segundos con una captura; ninguno detectable por asserts de estado. Juan: *"veo la ui overlapeada entre muchísimas otras cosas... ¿por qué no las has solucionado antes?"* La lista completa y el plan de fixes viven en `Index/20` §12.

**How to apply:** antes de declarar hecho cualquier trabajo con componente visible: entrar en Play, llevar el juego a los estados representativos (por código si hace falta), capturar con `manage_camera` action=screenshot include_image=true (también con la cámara ROTADA si hay órbita), y MIRAR la imagen con checklist: ¿algo se pisa? ¿algo jugable quedó detrás de la UI? ¿los textos se leen desde todos los ángulos? ¿los glifos renderizan? ¿el contraste alcanza? Relacionado: [[feedback-legibilidad-primero]] · [[feedback-vfx-solo-mmfeedbacks]] · [[feedback-diseno-legible-en-simientos]].
