---
name: qa-visual
description: Como verificar y juzgar trabajo con componente visible — capturas del Game view miradas de verdad, checklist de legibilidad, QA proactivo con la vara de juegos referentes, y el test del texto plano para separar problemas de sistema de problemas de presentacion. Cargar antes de declarar hecho cualquier trabajo de UI, HUD, camara, escena, guias visuales o feedback, y cuando Juan reporta que algo "no se entiende".
---

# QA visual y legibilidad

Tres reglas de Juan que se refuerzan entre si, y una trampa que hay que evitar antes que
todas ellas.

---

## 0. Primero: ¿es un problema de sistema o de presentacion?

**Trampa que ya costo una sesion:** cuando algo "no se entiende", el reflejo es proponer
mejoras de presentacion (mas texto flotante, mas indicadores, mejor camara). Juan rechaza
eso de plano si el problema es de diseno.

> *"Los visuales solo sirven cuando una idea esta bien implementada; Pokemon Rojo era solo
> texto e imagenes basicas y se entiende — una buena idea es legible en sus simientos."*

**Test del texto plano:** si la mecanica no se explica en **una frase, sin tabla**, el
problema es la mecanica, no su presentacion.
Referencia de lo que SI pasa el test: Darkest Dungeon — *"este ataque se usa desde las
posiciones 3-4 y golpea las 1-2"*.

Sintomas de que el problema es de sistema, no de UI: exceso de estado simultaneo,
causalidad no local (la consecuencia aparece lejos de la causa), resultado dominado por
el azar.

Si el test falla -> **replantear la mecanica**, no agregar indicadores.
Si el test pasa -> recien ahi aplican las reglas 1, 2 y 3 de abajo.

---

## 1. Legibilidad: nunca cumplir el minimo

> *"Lo que mas me importa es que sea legible; asegurate que sea legible, no cumplas con lo
> minimo; siempre preguntate: ¿esto que estoy planteando es legible para el usuario o
> deberia anadir algo mas para que lo sea?"*

Al implementar cualquier feature de presentacion, no entregar el minimo funcional.
Preguntarse activamente **que mas necesita el usuario para leer el estado**:

- Indicadores en mundo **y** en HUD, redundantes a proposito.
- Hints de controles visibles, no asumidos.
- Estados bloqueados / gastados / no disponibles, marcados explicitamente.
- Impactos y transiciones con juice perceptible (ver `presentacion-y-juice`).
- Que se lea desde todos los angulos de camara, no solo desde el que usaste para probar.

La prioridad declarada del prototipo era: **gameplay > legibilidad > todo lo demas.**

---

## 2. QA proactivo con la vara de los referentes

> *"No puede ser que yo te tenga que decir que un personaje no gira hacia su direccion de
> ataque — inspirate de juegos similares para agarrar el timing o la estetica, el flujo de
> acciones, manteniendo nuestras reglas."*

Antes de entregar, pasar la vara de los referentes del genero (Into the Breach, Bad North,
Mewgenics, o los que correspondan al proyecto) sobre **cada momento visible**:

- ¿La unidad **mira** hacia donde actua?
- ¿El timing **respira** o es brusco?
- ¿El flujo de acciones se entiende **sin leer texto**?
- ¿Hay anticipacion antes del impacto y reposo despues?
- ¿Algo aparece o desaparece de golpe donde deberia haber una transicion?

Detectar y proponer **antes** de que Juan lo reporte. La verificacion por estado y consola
no ve nada de esto; las capturas tampoco, si no se miran con ojos de jugador.

---

## 3. Verificacion visual: capturas MIRADAS

Regla de pipeline: **toda sesion que toque presentacion, UI, escena o camara cierra con
verificacion visual, no solo por estado.**

**Por que:** cinco sesiones verificadas unicamente por estado (fases, eventos, posiciones,
consola en 0 errores) dejaron pasar veinte problemas de calidad — un banner pisando la
linea de seleccion, cards tapando el tablero jugable, labels de canto tras la orbita de
camara, un glifo renderizando como tofu. Todos visibles en cinco segundos con una captura;
**ninguno** detectable por asserts de estado.

### Ritual

1. Entrar en Play.
2. Llevar el juego a los **estados representativos** (por codigo si hace falta, no a mano).
3. Capturar el Game view. Para que el overlay de UI Toolkit aparezca, la unica via
   confiable es la captura del backbuffer compuesto (source=screen) **estando en Play** —
   ver `unity-editor`.
4. Capturar tambien **con la camara rotada** si hay orbita.
5. **MIRAR la imagen** con este checklist:

| Chequeo | Pregunta |
|---------|----------|
| Overlap | ¿Algo se pisa con algo? |
| Oclusion | ¿Algo jugable quedo detras de la UI? |
| Angulos | ¿Los textos se leen desde todas las rotaciones de camara? |
| Glifos | ¿Todos los caracteres renderizan, o hay tofu? |
| Contraste | ¿Alcanza el contraste sobre el fondo real, no sobre el mockup? |
| Jerarquia | ¿Lo importante es lo primero que se ve? |
| Estados | ¿Se distingue lo activo de lo bloqueado de lo gastado? |

6. Si algo falla, arreglarlo **antes** de reportar, y volver a capturar.

**Una captura no mirada no cuenta como verificacion.** Si el trabajo tiene componente
visible y no hay captura mirada, no esta hecho.

### Trampas de la captura misma

- **La primera captura despues de instanciar prefabs nunca usados, o de cambiar la paleta
  o el fog, sale gris o cyan.** No es un bug del material: son variantes de shader
  compilando de forma asincrona. **Recapturar a los 4-5 segundos** antes de diagnosticar
  nada.
- **Resoluciones distintas segun el metodo**: la captura por API dentro de Play da
  resolucion completa con el overlay de UI Toolkit; la captura del game view por CLI da
  una resolucion menor. Para juzgar legibilidad de texto chico, usar la primera.
- **Capturar cerca del final de una secuencia larga puede salir negro.** Si sale negra,
  es la captura, no la escena: repetir en otro momento del ciclo.
- Para revisar **movimiento** (timing, giros, flujo) una captura no alcanza: capturar una
  secuencia de frames desde un delegado en Play, escribiendola **fuera de la carpeta de
  assets** para no importar cientos de imagenes, y armar el video con ffmpeg calculando el
  framerate con el tiempo real transcurrido (la captura baja los fps y si no se corrige,
  el video queda en camara lenta).
