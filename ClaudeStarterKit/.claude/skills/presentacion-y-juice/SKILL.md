---
name: presentacion-y-juice
description: Como se implementa todo lo visual no-funcional — juice y feedback SOLO via Feel/MMFeedbacks con estructura de prefab canonica (nunca tweens ni corrutinas a mano), y guias visuales en mundo a la altura del asset Shapes (punteados, puntas redondas, curvas, transiciones). Cargar ANTES de escribir cualquier codigo de VFX, shake, flash, tween, indicador, anillo, linea de guia o transicion de UI.
---

# Presentacion y juice

> **Estas son las dos reglas que mas se ignoran en sesiones largas.** Releerlas antes de
> codear cualquier cosa visual, especialmente si ya llevas horas trabajando.

---

## Regla 1 — Todo el juice va por Feel / MMFeedbacks

**Nunca codear feedback a mano.** Nada de tweens manuales, corrutinas de shake, ni
animacion de transforms por codigo C#. Temblores, flashes, escalas, pops, sonidos,
haptics: todo por MMFeedbacks.

### Estructura canonica del prefab

```
UnidadPrefab
└── Feedbacks/                 <- GameObject hijo, siempre con este nombre
    ├── OnHit      (MMF_Player)
    ├── OnLand     (MMF_Player)
    ├── OnDie      (MMF_Player)
    └── OnSpawn    (MMF_Player)
```

1. Dentro del prefab de la unidad/objeto: un hijo llamado **`Feedbacks`**.
2. Dentro de `Feedbacks`: **un GameObject por momento**, nombrado por evento
   (`OnHit`, `OnLand`, `OnDie`...), cada uno con su `MMF_Player`.
3. El script padre expone cada uno como **slot serializado**
   (`[SerializeField] MMF_Player onHit;`) y **solo** llama `onHit.PlayFeedbacks()`.
   El contenido — curvas, amplitudes, duraciones — vive en el Inspector, **jamas en C#**.
4. El wiring de eventos va **arriba** del script, al estilo del bus del proyecto: los
   eventos se suscriben y desuscriben en `OnEnable`/`OnDisable` y se enganchan a los
   feedbacks. **La logica no conoce el juice.**
5. Los tiles, bloques y elementos del tablero siguen el mismo patron.

### Por que

Juan tunea el juice desde el Inspector sin tocar codigo, y el proyecto mantiene **un solo**
sistema de feedback.

### Si el objeto no tiene prefab donde colgar los feedbacks

**Crearlo** (pidiendo OK para tocar prefabs) en vez de tween-ear por codigo. Una vista
creada por codigo sin prefab es exactamente el caso que rompe esta regla, y ya paso.

**Disparar solo por la API de MM** (`PlayFeedbacks()`, `WigglePosition()`), nunca
manipulando el transform desde la logica.

### Quirks de Feel cazados

- **Un `MMF_Player` agregado por script tiene su lista de feedbacks en `null`.** Hay que
  inicializarla antes de agregar el primer feedback, o tira.
- **Los feedbacks de particulas en modo Pool no devuelven al pool sistemas que nunca se
  apagan.** Con prefabs de demo que vienen en `loop = true`, el pool crece sin tope (visto:
  45 -> 68 sistemas en un minuto, la escena tapada de humo). Hacer **variantes propias** con
  `loop = false`, `playOnAwake = false`, stop action en Disable y escala razonable. Con eso
  el pool se estabiliza. Ojo tambien con la opcion de no anidar las particulas: las
  instancias no se destruyen con su dueno y se acumulan al respawnear.
- **El feedback de escala en modo "hacia un destino" aplica un remapeo** sobre la escala
  resultante, y la curva por defecto es un punch: sale un objeto gigante en vez de un
  squash. Para sumar directamente el valor de la curva, usar el modo **aditivo** con el
  remapeo en 0..1.
- Los paquetes de efectos comprados suelen venir para el pipeline built-in: hay que
  convertir los materiales al pipeline del proyecto (la conversion es reversible por git).

### Regla de color

Para cualquier composicion de color en pantalla o en mundo, **60/30/10**: un dominante, un
secundario, un acento. Sobrevivio a varios sistemas descartados porque es de las pocas cosas
que siempre aplicaron.

---

## Regla 2 — Las guias visuales salen a nivel Shapes de entrada

> *"Asi como mejore tus clues visuales iniciales, no quiero tener que decirtelo; por eso te
> pase de referencia ese asset, que tiene MUY BUENAS clues visuales, que me gustaria que
> pudieras generar igual para que se vea mas appealing."*

La vara de calidad de cualquier guia visual en mundo es el asset **Shapes** (Freya Holmer).
La primera version tipica — anillo liso, linea recta, sin transiciones — **cumple el
minimo y no alcanza**. Juan no quiere tener que pedir cada uno de estos elementos.

### Checklist obligatorio antes de entregar una guia visual

- [ ] Trazos con **grosor consistente** y anti-aliasing (nada de lineas de 1 px aliaseadas).
- [ ] **Puntas redondas** en los extremos de todo trazo.
- [ ] **Punteados con offset animado** en la direccion del movimiento.
- [ ] **Arcos y anillos parciales**, no solo circulos completos.
- [ ] **Degradados de alfa** a lo largo del trazo.
- [ ] **Curvas en vez de rectas** cuando la guia representa un camino real.
- [ ] Todo **aparece y desaparece con fundido y escala** — nada "popea".
- [ ] **Movimiento lento y constante** donde algo esta vivo o activo.

### Como se implementa

Un shader SDF propio para las formas vectoriales + un drawer que las compone. Si una forma
del checklist no esta soportada, **se agrega al shader antes de entregar**, no despues de
que Juan la pida.

### Por que

La legibilidad y el appeal de las guias son **parte del prototipo**, no decoracion. Juan
las lee como senal de la calidad del sistema entero. Es la version concreta, para el mundo
3D, de "nunca cumplir el minimo" (ver `qa-visual`).

---

## Cierre

Todo trabajo bajo esta skill cierra con el ritual de capturas de `qa-visual`. Un juice que
no se vio en una captura mirada no esta verificado.
