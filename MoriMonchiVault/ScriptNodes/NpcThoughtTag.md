---
tags: [script, world, npc, ui]
---

# NpcThoughtTag.cs

**Ruta:** `World/Npc/NpcThoughtTag.cs`

**Responsabilidad:** Burbuja de pensamiento world-space (UITK) sobre NPCs. Billboard hacia cámara, distance-gated. Muestra diálogo dinámico en español obtenido de [[NpcDialogueBank]] según `NpcAgent.State` y `LeaveReason`. Cachea frase y aplica delay de reacción (`agent.ReactionDelay`) antes de mostrar frase nueva (evita parpadeo).

**Propiedades serializadas:**
- `showDistance` (float, 12): distancia máxima a cámara para mostrar burbuja.
- `uprightOnly` (bool, true): si true, ignora camera pitch (mantiene etiqueta vertical).

**Ciclo:**
- `Awake()` → obtiene UIDocument, busca NpcAgent parent via `GetComponentInParent`, caché Camera.main.
- `LateUpdate()` → distance gate, billboard (LookRotation hacia cámara), `Refresh()`.
- `SetShown(bool)` → toggle display del root (Flex/None).
- `ResolveElements()` → queries UITK `npc-name-label` y `thought-label` del root.
- `Refresh()` → actualiza nameLabel, llama `UpdateThought()`.

**UpdateThought() - Lógica clave (Sesión 20):**
- Detecta cambio de situación comparando tupla `(State, Reason)` contra `(lastState, lastReason)`.
- Al detectar cambio:
  - Extrae nombre target de `agent.TargetMM.CustomName` (fallback "ese").
  - Consulta `NpcDialogueBank.Pick(state, reason, name)` para obtener UNA frase random.
  - Cachea la frase en `pendingLine`.
  - Resetea `responseTimer = agent.ReactionDelay` (comienza cuenta atrás).
- Mientras `responseTimer > 0`: decrementa cada frame, mantiene `shownLine` con frase anterior (sin parpadeo).
- Cuando `responseTimer ≤ 0`: actualiza `shownLine = pendingLine` (muestra la frase nueva).
- Renderiza `thoughtLabel.text = shownLine`.

**nameLabel:**
- Muestra `agent.DisplayName` si no es vacío.
- Fallback: `agent.Archetype.DisplayName`.
- Fallback final: "Cliente".

**Requerimientos:**
- `[DisallowMultipleComponent]` — un nodo hijo del NPC por burbuja.
- `[RequireComponent(typeof(UIDocument))]` — necesita UIDocument en el mismo GameObject.

**Cambios principales (Sesión 20):**
- Eliminó método `ThoughtText()` (frases fijas).
- Ahora `UpdateThought()` consulta `NpcDialogueBank.Pick(state, reason, targetName)` → frase dinámica random.
- Implementó delay de reacción: cachea frase en `pendingLine`, espera `agent.ReactionDelay` segundos, luego muestra en `shownLine`.
- Distingue comprador (Purchased) vs perdedor (Outbid) vía `agent.Reason`.
- Sin parpadeo: mantiene frase anterior mientras cuenta atrás el delay.

**Privados:**
- `lastState`, `lastReason` — cache de la última situación procesada.
- `situationInit` (bool) — flag para detectar primera vez.
- `pendingLine` (string) — frase nueva a mostrar (cached).
- `shownLine` (string) — frase visible actualmente (con delay aplicado).
- `responseTimer` (float) — cuenta atrás del delay de reacción.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[NpcAgent]] (lee `State`, `Reason`, `ReactionDelay`, `TargetMM`, `DisplayName`), [[NpcDialogueBank]] (consulta frases por situación), [[CreatureDNA]] (lee `CustomName`), [[CustomerArchetypeSO]] (fallback displayName).
