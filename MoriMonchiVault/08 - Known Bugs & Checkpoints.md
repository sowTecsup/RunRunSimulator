---
tags: [memory-bank, bugs, checkpoints, future]
---

# 08 — Known Bugs & Checkpoints

> Para fixes implementados ver el sistema afectado: [[03 - Combat]], [[02 - Genetics & Breeding]], [[05 - UI System]].

## Bugs activos / mitigados (causa raíz no resuelta)

### Fantasma de cola (causa raíz, mitigado)

- **Síntoma**: una criatura aparece `QueuedForCombat` en local aunque no está en el pool ni tiene resultado pendiente.
- **Causa raíz**: el `BusyState=Queued` se setea/pushea antes de confirmar el enqueue server-side, y los pushes fire-and-forget pueden llegar fuera de orden; el caso `default` de `EnqueueInternal` no hace rollback.
- **Estado**: auto-curado por `ReconcileGhostsAsync` en `PollResultsAsync` (ver [[03 - Combat]]).
- **Para eliminar la causa**: secuenciar los pushes y agregar rollback en `default`.

### DeathChance hardcoded en JS

- `DeathChance` está hardcoded a 15% en `process-matchmaking.js` y `run-combat.js`.
- Cambiar el `CombatManagerSO.DeathChance` solo afecta el combate local.
- Para sincronizar habría que pasar el valor como param o duplicarlo manualmente.

### BREED_DURATION_MS hardcoded en JS

- `BREED_DURATION_MS` está hardcoded a 30 min en `start-breeding.js`.
- `InheritanceOddsTableSO.BreedDurationMinutes` solo afecta el display local — misma limitación que `DeathChance`.

### Matchmaking pool sin race-condition handling

- Dos llamadas simultáneas pueden pisarse.
- Aceptable en testing; para producción con tráfico real, agregar `writeLock` del SetItemBody.

### Auto-repetición de input en grilla (menor)

- La grilla navega 1 paso por pulsación, sin **auto-repetición** al mantener la dirección.
- Cómodo de añadir con un timer si una grilla grande lo pide.

## Bugs ya resueltos (para referencia)

### Sistema de prioridad de UI (RESUELTO)

- **Antes**: no había stack ni router, el ESC cerraba todo y el tap E del mundo togglaba el panel de atrás.
- **Ahora**: `UIManager` mantiene un **stack ordenado** de paneles (tope = foco) y rutea el input solo al tope; ESC hace pop en orden LIFO. Action maps `Player`/`UI` mutuamente excluyentes, así que en menú el interact del mundo (E) no puede dispararse.
- Detalle completo en [[05 - UI System]] sección "STACK + Router".

### Gap async de battle log (RESUELTO)

- **Antes**: combates async no disparaban `OnCombatCompleted` → battle-log UI los perdía.
- **Ahora**: `AsyncCombatService.ApplyResult` dispara `OnCombatLogged(CombatLogEntry)` → cacheado por `CombatPanelUITK`.
- Ver [[07 - Persistence & Identity]] tabla de eventos.

## Checkpoints de diseño — Breeding Async (pendientes futuros)

### Busy-lock server-enforced

- **Hoy**: el flag `BusyState = Breeding` lo escribe el CLIENTE (espejo local en Player Data), sincronizado con eventos del server pero no impuesto por él.
- **Riesgo**: el timer SÍ es infalsificable (vive en Custom Data), pero un tramposo podría limpiar el `BusyState` local de un padre incubando y usarlo en combate/otro breed mientras el huevo sigue server-side.
- **Fix futuro**: que `start-breeding` y `enqueue-combat` cross-checkeen server-side contra el array de huevos / pool antes de aceptar la acción.
- Aplica igual al `BusyReason.QueuedForCombat`.

### Generación de la cría server-side

- **Hoy**: se mintea local + push.
- **Fix futuro**: mover a Cloud Code cuando se endurezca el anti-cheat (igual que `process-matchmaking` portó `CombatService`).

### Cross-device

- El countdown local (`BreedReadyAt`) no viaja entre dispositivos; el huevo autoritativo (Custom Data) sí.
- Resolver con un `get-breeding` peek si hace falta mostrar el timer en otro device.

### Crash entre hatch-ready y crear la cría

- Riesgo bajo de perder el breed (el huevo ya se borró server-side).
- Mitigar con borrado en dos fases más adelante.

## Pendientes del roadmap (resumen)

### Etapa 1.2 — Visualizador 3D

- Grilla de inspector ✅.
- Falta: visualizador 3D (leer DNA → ensamblar Prefab con anchor points 2-2-1).

### Etapa 1.3 — Breeding (refinamientos)

- Género por battle-index del padre (actualmente 50/50). 🔲
- Bonus de rareza en la 4ª cría (última posible). 🔲
- Herencia del nivel Tier de las partes. 🔲

### Etapa 2.5 — Vida en Escena (setup)

- Código ✅.
- Falta: NavMesh bake + 3 Areas (`ShopFrontDesk`, `ShopBackroom`, `Storage`), prefab del cubo, asset Personality Profile Table, wiring del spawner.

### Etapa 3 — Tienda y Mercado

- 3.1 Tienda Local (NPCs, inventario, vitrinas). 🔲
- 3.2 Mercado Online (P2P via Unity Services). 🔲

### Evolución por peso de Tier (futuro)

- Regla diseñada: **70% peso Tier1 → Tier2 / 20% peso Tier2 → Tier3 / 10% peso Tier3** (tier máximo, sin efecto).
- Si un pool está vacío se excluye y los pesos restantes se renormalizan.
- `CombatManagerSO.EvolutionChance` queda reservado para esto.
