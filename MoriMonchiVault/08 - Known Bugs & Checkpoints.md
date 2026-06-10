---
tags: [memory-bank, bugs, checkpoints, future]
---

# 08 — Known Bugs & Checkpoints

> Para fixes implementados ver el sistema afectado: [[03 - Combat]], [[02 - Genetics & Breeding]], [[05 - UI System]], [[10 - Furniture & Building]].

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

### MoriMochi atascado en Reacting cuando needs críticos (RESUELTO)

- **Síntoma**: una criatura que el jugador seguía de cerca (estado `Reacting`) quedaba atascada en "Se acerca" aunque todas sus needs cayeran a crítico.
- **Causa raíz**: `TickReacting` no tenía salida por need. Una vez en `Reacting`, las needs críticas no interrumpían.
- **Fix**: al inicio de `TickReacting` se llama `TryEnterNeedSeeking()`; si la need es crítica, vuelve a `Idle`/`Roaming` y la lógica de needs toma el control.

### E-key grab roto tras cambio de collider a trigger (RESUELTO)

- **Síntoma**: mantener E sobre un MoriMochi roameando no lo agarraba, aunque funcionaba antes.
- **Causa raíz**: `TryFindInView` usaba `QueryTriggerInteraction.Ignore`. Al volverse trigger el collider del MoriMochi en modo NavMesh, el raycast lo ignoraba por completo.
- **Fix**: `TryFindInView` reescrito con `RaycastAll` + `QueryTriggerInteraction.Collide`, hits ordenados por distancia; solid-non-T bloquea, trigger-non-T es transparente.

### NameTag no actualizaba tras reuso de pool (RESUELTO)

- **Síntoma**: un MoriMochi reactivado del pool mostraba el nombre de la vida anterior o ningún texto.
- **Causa raíz**: `ResolveElements()` verificaba `nameLabel != null`, pero `SetActive(false→true)` hace que `UIDocument` reconstruya su `rootVisualElement`. Las `Label` cacheadas apuntaban al árbol viejo (ya fuera de pantalla).
- **Fix**: `ResolveElements()` compara `docRoot == root` (identidad del árbol actual). Si cambia, re-query y re-cachea.

### Criatura aparecía y desaparecía en el primer spawn (RESUELTO)

- **Síntoma**: el primer MoriMochi spawneado aparecía en escena, luego desaparecía ~2s después, y volvía a aparecer en el siguiente intervalo del pump.
- **Causa raíz**: `CloudSyncService` dispara `OnRegistryReloaded` tras el pull inicial (~2s después del start). El handler de reload llamaba `ClearAll()`, lo que deactivaba la criatura recién spawneada y la devolvía al pool.
- **Fix**: `OnRegistryReloaded` ya no llama `ClearAll()`. Reconcilia: despawnea solo los muertos/removidos, rebindea en-place los vivos, encola solo los nuevos.

### Sistema de prioridad de UI (RESUELTO)

- **Antes**: no había stack ni router, el ESC cerraba todo y el tap E del mundo togglaba el panel de atrás.
- **Ahora**: `UIManager` mantiene un **stack ordenado** de paneles (tope = foco) y rutea el input solo al tope; ESC hace pop en orden LIFO. Action maps `Player`/`UI` mutuamente excluyentes, así que en menú el interact del mundo (E) no puede dispararse.
- Detalle completo en [[05 - UI System]] sección "STACK + Router".

### Gap async de battle log (RESUELTO)

- **Antes**: combates async no disparaban `OnCombatCompleted` → battle-log UI los perdía.
- **Ahora**: `AsyncCombatService.ApplyResult` dispara `OnCombatLogged(CombatLogEntry)` → cacheado por `CombatPanelUITK`.
- Ver [[07 - Persistence & Identity]] tabla de eventos.

### Muebles se elevan después del spawn al recargar (RESUELTO)

- **Síntoma**: al recargar la escena los muebles aparecen en la Y correcta y luego suben levemente. Durante la sesión de placement funcionan perfectamente.
- **Causa raíz**: `OnFurnitureReloaded` → `FurnitureSpawner.OnReloaded` → `ClearAll()` + `Sync()` en el **mismo frame**. `Destroy()` en Unity es diferido: los colliders viejos siguen vivos en physics ese frame. `PlacementGrid.floorMask` tiene default `~0` (todos los layers), así que `TrySampleFloor` golpeaba el **techo del mueble anterior** → `pos.y` = cima del mueble viejo → spawn elevado.
- **Fix**: `OnReloaded` ahora usa un coroutine: `ClearAll()` → `yield return null` → `Sync()`. El frame de espera garantiza que los `Destroy()` diferidos terminen antes del raycast. También forzar `isKinematic = true` en cualquier `Rigidbody` de los prefabs de furniture en `SpawnOne`.
- **Nota de setup**: en el `PlacementGrid` del inspector, `floorMask` debe estar configurado **solo en el layer Floor**. El default `~0` (todos los layers) es la causa de que el raycast golpeara meshes de otros objetos. Ver [[10 - Furniture & Building]].

### Throw no funcionaba en WorldPropInstance sin ThrowableObject (RESUELTO)

- **Síntoma**: los objetos del hotbar de play-mode podían agarrarse y soltarse pero al lanzarlos (hold E) caían sin impulso.
- **Causa raíz**: `HotbarController.ThrowActive` buscaba `IThrowable` en el objeto. Los prefabs de `WorldPropInstance` no tienen `ThrowableObject` (que implementa `IThrowable`), así que el check fallaba y no se aplicaba ninguna fuerza.
- **Fix**: `ThrowActive` ahora tiene un fallback: si no encuentra `IThrowable`, busca `Rigidbody` directamente y aplica `linearVelocity = force / mass` — la misma técnica que `ThrowableObject.OnThrow` (inmune al bug kinematic→dynamic del mismo frame).

### StorageContainer — re-captura inmediata al ejectar (RESUELTO)

- **Síntoma**: al sacar un objeto del almacén con "Sacar (Q)" el objeto desaparecía inmediatamente y volvía al inventario. Parecía que "no dejaba sacarlo".
- **Causa raíz**: `Eject()` instanciaba el prop en `ejectPoint` (o en el transform del container si no estaba asignado). Si esa posición quedaba dentro de la trigger zone del container, el primer frame de physics detectaba el contacto → `OnTriggerEnter` re-almacenaba y destruía el prop.
- **Fix**: `StorageContainer` guarda `justEjectedId` (instanceID del prop recién eyectado). `OnTriggerEnter` salta ese ID por 2 `FixedUpdate`. **Adicionalmente**: asignar `ejectPoint` en el inspector a un Transform fuera de la trigger zone (idealmente 1-2m enfrente del container).

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

## Pendientes de código — combate y UI

### Countdown en Resultados: "Instantánea" vs timer

- **Síntoma/Deseo**: en la tab Resultados, las criaturas encoladas vía modo Instant muestran el mismo countdown al :00 UTC que las de timer, lo cual no tiene sentido (su combate no depende del cron).
- **Fix pendiente**: al agregar la fila en `AddQueueRow`, detectar si la criatura está en `instant_pool` (puede ser un flag en `QueuedAt` o un campo separado `IsInstantQueue`). Si es instant, mostrar label `"Instantánea"` en lugar del countdown.

### Ordenar cola de Resultados de más antiguo a más nuevo

- **Síntoma/Deseo**: las criaturas en cola deberían listarse por hora de encolado ascendente (la más antigua arriba).
- **Fix pendiente**: `RebuildResults` ordena las criaturas `QueuedForCombat` por `QueuedAt` ascendente antes de `AddQueueRow`.

### Árbol de ascendencia/descendencia — renderizado mejorado

- **Síntoma/Deseo**: el árbol actual puede quedar grande para generaciones profundas o muchos hijos; el scroll horizontal/vertical no es cómodo para árboles amplios.
- **Fix pendiente**: evaluar un layout más compacto (chips más pequeños, nodos colapsables o paginados por generación) o un canvas con zoom/pan.

### Prewarm de pool de MoriMonchis (`MoriMochiSpawner`)

- **Síntoma/Deseo**: el primer spawn hace `Instantiate` en caliente. Para scenes con muchas criaturas, el primer tick del pump puede causar frame drop.
- **Fix pendiente**: agregar `prewarmCount` (inspector) en `MoriMochiSpawner`. En `Awake`/`Start`, instanciar X agentes vacíos con `SetActive(false)` y encolarlos en el pool. `GetAgent` los saca del pool antes de hacer `Instantiate`.

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
