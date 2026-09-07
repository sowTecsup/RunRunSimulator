---
name: feedback-vfx-solo-mmfeedbacks
description: "Todo VFX/juice va por Feel/MMFeedbacks con estructura de prefab: hijo 'Feedbacks' con un GameObject por momento (OnHit...) cada uno con su MMF_Player, referenciado por slot serializado en el padre — NUNCA codear feedback a mano"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 10ebca94-e832-47c6-9937-408acdcc0f0b
  modified: 2026-08-27T19:51:37.560Z
---

Regla de Juan (2026-08-27, S84): todo lo relacionado a VFX/juice — temblores, shakers, cualquier feedback visual/audio/háptico — debe implementarse vía Feel/MMFeedbacks. NUNCA codear feedback a mano: nada de tweens manuales, corrutinas de shake, ni animación de transforms por código C#.

**Estructura canónica que Juan quiere (ampliada en S84, auditoría del prototipo):**
1. Dentro del prefab de la unidad/objeto: un GameObject hijo llamado `Feedbacks`.
2. Dentro de `Feedbacks`: un GameObject por momento de feedback, nombrado por evento (`OnHit`, `OnLand`, `OnDie`, ...), cada uno con su componente `MMF_Player`.
3. El script padre expone cada uno como slot serializado (`[SerializeField] MMF_Player onHit;`) y solo llama `onHit.PlayFeedbacks()` — el contenido (curvas, amplitudes, duraciones) vive en el Inspector, jamás en C#.
4. El wiring de eventos va "arriba" del script, estilo GameEvents del proyecto: los eventos se declaran/suscriben en la parte superior (OnEnable/OnDisable) y se enganchan a los feedbacks; la lógica no conoce el juice.
5. Los tiles/bloques del tablero siguen el mismo patrón (hoy `BoardImpactFeedback`+`MMWiggle` es la semilla de esto).

**Why:** Juan quiere tunear el juice desde el Inspector sin tocar código y mantener UN solo sistema de feedback (Feel ya está en el proyecto; pedido explícito en S81/S82, reafirmado en S84). ATENCIÓN: Juan señaló (S84) que esta es la regla que más ignoro en sesiones largas — la auditoría S84 encontró `CombatUnitView.FlashHit` como tween a mano y vistas de unidad creadas por código sin prefab donde colgar MMF_Players.

**How to apply:** antes de escribir CUALQUIER código visual/de juice (especialmente tarde en una sesión larga), releer esta regla. Si el objeto no tiene prefab con `Feedbacks/`, crearlo (con OK de Juan para escena/prefabs) en vez de tween-ear por código. Disparar solo vía API MM (`PlayFeedbacks()`, `WigglePosition()`). Relacionado: [[feedback-legibilidad-primero]] · [[feedback-no-overengineering]].
