---
tags: [script, world, npc, ui]
---

# NpcThoughtTag.cs

**Ruta:** `World/Npc/NpcThoughtTag.cs`

**Responsabilidad:** Burbuja de pensamiento world-space (UITK, patrón idéntico a NameTag) sobre NPCs. Billboard hacia cámara, distance-gated (`showDistance`). Muestra diálogo en español según `NpcAgent.State` y `TargetMM`.

**Propiedades:**
- `showDistance` (float) — distancia máxima a cámara para mostrar.
- `uprightOnly` (bool) — si true, ignora camera pitch (mantiene etiqueta derecha).

**Ciclo:**
- `Awake()` → obtiene UIDocument, busca NpcAgent parent via `GetComponentInParent`, caché Camera.main.
- `LateUpdate()` → distance gate, billboard, `Refresh()`.
- `SetShown(bool)` → toggle display del root.
- `ResolveElements()` → queries `npc-name-label` y `thought-label` del root UITK.
- `Refresh()` → actualiza nameLabel con `agent.DisplayName` (fallback archetype.DisplayName, fallback "Cliente"), llama `ThoughtText(agent.State, agent.TargetMM, agent.QueueWasFull)`, actualiza thoughtLabel.

**nameLabel:**
- Muestra `agent.DisplayName` si no es vacío.
- Fallback: `agent.Archetype.DisplayName`.
- Fallback final: "Cliente".

**ThoughtText (mapeo NpcState → diálogo ES):**
- `Wandering` → "¿Qué tendrán hoy?"
- `InspectingDisplay` → "Mmm, déjame ver…"
- `ApproachingRegister` → $"¡Me llevo a {targetName}!"
- `Queueing` → "Esperaré mi turno…"
- `WaitingAtRegister` → "¿Hay alguien en la caja?"
- `Negotiating` → $"¿Cuánto por {targetName}?"
- `Leaving` → condicional:
  - si `target.IsSold` → $"¡{targetName} se viene conmigo!"
  - si `queueWasFull` → "¡Está muy lleno!"
  - sino → "Será en otra ocasión…"

**Requerimientos:**
- `[RequireComponent(typeof(UIDocument))]` — el nodo hijo del NPC que representa la burbuja.
- `[DisallowMultipleComponent]`.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[NpcAgent]], [[NpcNameBank]], [[CreatureDNA]], [[CustomerArchetypeSO]]
