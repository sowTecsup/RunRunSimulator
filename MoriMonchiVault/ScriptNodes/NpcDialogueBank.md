---
tags: [script, world, npc, data]
---

# NpcDialogueBank.cs

**Ruta:** `World/Npc/NpcDialogueBank.cs`

**Responsabilidad:** Clase estática pura que proporciona frases de diálogo en español para NPCs, espejo de [[NpcNameBank]]. Almacena arrays de 5-6 frases por situación (estado + motivo de salida). Sin estado mutable.

**Datos públicos (arrays privados, acceso vía método `Pick`):**
- `wandering[]` (6 frases): vagando por la tienda sin objetivo. Ej: "¿Qué tendrán hoy?", "A ver qué me encuentro…".
- `inspecting[]` (6 frases): examinando una criatura en estante. Ej: "Mmm, déjame ver…", "Qué ojitos tiene…".
- `approaching[]` (5 frases): decidió comprar, va hacia caja. Usa `{0}` como placeholder del nombre MM. Ej: "¡Me llevo a {0}!", "¡{0} es perfecto!".
- `queueing[]` (5 frases): espera en fila. Ej: "Esperaré mi turno…", "Uf, qué fila.".
- `waiting[]` (5 frases): frente de fila, esperando respuesta del jugador. Ej: "¿Hay alguien en la caja?", "Tum, tum, tum…".
- `negotiating[]` (5 frases): durante negociación con jugador. Usa `{0}`. Ej: "¿Cuánto por {0}?", "¿Me hace precio por {0}?".
- `purchased[]` (5 frases): compró exitoso. Usa `{0}`. Ej: "¡{0} se viene conmigo!", "¡Qué felicidad, {0}!".
- `outbid[]` (5 frases): otro cliente compró su objetivo. Usa `{0}`. Ej: "¡Me ganaron a {0}!", "¡No! ¡Quería a {0}…!".
- `queueFull[]` (5 frases): no pudo entrar a la fila. Ej: "¡Está muy lleno!", "Mejor vuelvo luego.".
- `noDeal[]` (5 frases): se va sin comprar (rechazó contraoferta, timeout, etc.). Ej: "Será en otra ocasión…", "Bah, me voy.".

**Métodos públicos:**
- `Pick(NpcAgent.NpcState state, NpcAgent.LeaveReason reason, string targetName) → string`: devuelve una frase random (formateada con `targetName`) según el estado actual del cliente.
  - Para estados `Wandering`, `InspectingDisplay`, `Queueing`, `WaitingAtRegister`: ignora `reason`, consulta el array correspondie al estado.
  - Para estado `ApproachingRegister`, `Negotiating`: ignora `reason`, consulta el array del estado.
  - Para estado `Leaving`: ramifica según `reason`:
    - `LeaveReason.Purchased` → elige de `purchased[]`.
    - `LeaveReason.Outbid` → elige de `outbid[]`.
    - `LeaveReason.QueueFull` → elige de `queueFull[]`.
    - `LeaveReason.None` (o default) → elige de `noDeal[]`.
  - Reemplaza `{0}` en la frase con `targetName` usando `string.Format()`.

**Privados:**
- `One(string[] bank, string targetName) → string`: auxiliar que elige un elemento aleatorio del array y lo formatea con `targetName`.

**Datos por situación (resumen de contenido):**

| Situación | Estado | Clave | Contenido |
|-----------|--------|-------|----------|
| Explorando | Wandering | vagancia | Curiosidad, exploración, expectativa. |
| Inspeccionando | InspectingDisplay | análisis | Deliberación, indecisión, observación. |
| Yendo a caja | ApproachingRegister | decisión | Entusiasmo por la criatura elegida (con nombre). |
| Fila de espera | Queueing | paciencia | Tolerancia, aburrimiento, impaciencia. |
| Frente de caja | WaitingAtRegister | impotencia | Llamadas de atención, golpeteo, fastidio. |
| Negociando | Negotiating | regateador | Preguntas sobre precio (con nombre MM). |
| Compró feliz | Leaving + Purchased | victoria | Alegría, satisfacción, promesa de cuidado (con nombre). |
| Perdió subasta | Leaving + Outbid | derrota | Decepción, arrepentimiento, reclamo (con nombre). |
| Fila llena | Leaving + QueueFull | impotencia | Queja por aglomeración, sin nombre. |
| Se va vacío | Leaving + None/Other | resignación | Conformismo, próxima oportunidad, sin nombre. |

**Notas de implementación:**
- Cada array tiene 5-6 frases para evitar repetición (Random.Range sorteea índice).
- Los placeholders `{0}` en frases siempre se reemplazan, incluso si `targetName = "ese"` (fallback de [[NpcThoughtTag]]).
- No hay estado privado ni inicialización: es purely functional.
- Consumidor único: [[NpcThoughtTag]] llama `Pick()` en `UpdateThought()` cuando detecta cambio de situación.
- Llamadas cada Frame (aunque cacheadas en [[NpcThoughtTag]] por única vez por situación).

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[NpcThoughtTag]] (único consumidor), [[NpcAgent]] (lee Estado y LeaveReason), [[NpcNameBank]] (patrón paralelo).
