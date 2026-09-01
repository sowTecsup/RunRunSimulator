---
tags: [index, temporal, scriptnodes]
---

# 09c - Invariantes rescatados de comentarios (S93) — NOTA TEMPORAL

> En S93 (2026-09-01) se aplicó la regla 3 de CLAUDE.md a todo el proyecto: **1.858 líneas de comentario → 0**. Los coders que purgaron los archivos más cargados devolvieron las advertencias que valía la pena conservar. **Destino: los ScriptNodes correspondientes** (input del `vault-documenter` en `/cerrar-sesion`); una vez volcadas, borrar esta nota. Todo lo borrado, palabra por palabra, sigue en git (`git show 3cc5eb5:<ruta>`).

## PlayerController
- Nunca referencia `PlayerInputs` directamente: solo se suscribe a sus eventos static (mismo patrón que `GameEvents`). Look/aim lo posee Cinemachine; el controller solo lee su forward. Grab/throw pasa por `IThrowable`, así el controller nunca conoce el tipo concreto sujetado.
- `SetState`: Exploring y Building corren en primera persona (cursor lock + cámara viva); solo Menu libera el cursor y congela la cámara. Abrir un menú o entrar a build mode a mitad de un petting termina la sesión de petting (safety net).
- `OnBuildModeChanged`: build mode es el tercer estado del player — movimiento y cámara siguen vivos (se apunta el ghost mirando), grab/throw/jump quedan suspendidos; `BuildModeController` es dueño del ghost y el placement.
- `Move`: el player camina en Exploring y Building; suspendido en Menu.
- Contrato de la tecla E (`OnInteractPressed`/`OnInteractReleased`/`UpdateGrabHold`): libre + PRESS sobre criatura petteable → petting mientras se mantiene E · libre + TAP → interact (`IInteractable`) · libre + HOLD ≥ `grabHoldDuration` → grab (`IThrowable`) · cargando + PRESS → drop en el lugar · cargando + Click (Attack) → throw. Los agentes MoriMonchi mantienen el grab físico; para todo lo demás, mantener E lanza el ítem activo del hotbar (los props viven en el hotbar, no en un grab físico).
- En un menú el action map Player está deshabilitado (`PlayerInputs` lo gatea por UI focus): ningún grab/interact llega; los paneles se cierran con ESC (`UIInputs` → `UIManager` pop del stack), no con E.
- `ComputeThrowImpulse`: el objeto flota en el hold anchor descentrado; tirar por `camera.forward` desde ahí vuela paralelo y nunca llega al punto apuntado — se apunta desde el anchor HACIA donde mira la cámara para converger en el centro de pantalla.
- `TryBeginPetting`: usa `OverlapSphere` (no raycast) para que los paneles world-space de `NameTag` no bloqueen la detección.
- `TryFindInView<T>`: `QueryTriggerInteraction.Collide` porque los MoriMonchis llevan collider TRIGGER mientras están NavMesh-driven (se vuelven sólidos en vuelo tras el handoff a throwable); recorre los hits por distancia — el primero que resuelve a T gana, un collider SÓLIDO que no es T bloquea (no se agarra a través de paredes), un trigger que no es T se ve a través (paneles, zonas de estación).

## UIManager
- Los eventos UI-domain viven acá y no en `GameEvents`, a propósito: `GameEvents` es gameplay-only; cada dominio tiene su bus.
- **NUNCA `SetActive(false)` a un GameObject con `UIDocument`**: un documento inactivo no tiene `rootVisualElement` y deja de actualizarse; se togglea `display` del root (patrón de `CreatureGridUITK`, que vive en este objeto siempre activo).
- `UIManager` es el ÚNICO suscriptor de `UIInputs`: Navigate/Submit se despachan al panel top del stack; Cancel (ESC) popea el top → los paneles cierran en orden inverso. `RouteCancel`: el panel top maneja el cancel internamente primero (cerrar sub-vista); solo si no lo consume se popea.
- `Start`: `OnEnable` corre antes que `Start`, por eso los `rootVisualElement` ya existen para ocultar los paneles ahí.
- `UpdateFocus`: dispara `OnUIFocusChanged` solo en el flanco 0↔1.

## BuildModeController
- Sub-máquina: **Browsing** (nada seleccionado; 1-4 empieza a colocar pieza de hotbar — path de test/legado, el browser real usa `SelectPieceFromBrowser`; E sobre pieza colocada → editar; clic derecho → target para borrar) · **Placing** (pieza NUEVA sigue el aim; R rota; clic izquierdo/F pinea en celda libre → Editing; verde/rojo por `grid.CanPlace`) · **Editing** (pieza fija en su celda; R rota; F guarda si verde o revierte el giro colisionante si rojo → Browsing) · **Deleting** (pieza levantada en rojo; F confirma, Esc restaura). Esc cancela la selección (restaurando la pieza levantada) → Browsing; en Browsing sale del build mode.
- `OnDisable`: si `active`, siempre `ExitBuildMode()` — nunca dejar una pieza levantada o un ghost huérfano.
- `Update`: solo Placing sigue el piso bajo la mira; Editing/Deleting quedan fijos. El ray de cámara elige la celda (XZ); la Y y la pendiente vienen de una sonda vertical en esa celda, para que el preview se asiente donde el spawner lo va a re-asentar.
- `TryPickFurnitureCell`: raycast a capas FURNITURE; devuelve la celda ancla leída de `PlacedFurnitureMarker` (que estampa el spawner).
- `OnConfirm` (Editing): si `PlacementValid()` falla, revierte a `lastValidRotation` en vez de bloquear el input.
- `PlacementValid`: única fuente de verdad de "puede sentarse en `currentCell`" (celda libre + piso plano + sin overlap físico) — la usan el tint, `OnPin` y `OnConfirm`.
- `OverlapsObstacle`: box orientado sobre el footprint (XZ de la grilla, altura del mesh del ghost) contra `obstacleMask`; los colliders del ghost están deshabilitados y una pieza levantada ya está despawneada, así que nada se auto-triggerea; un inset chico evita atrapar vecinos a ras. `BuildGhost` deshabilita los colliders del ghost (un preview no colisiona ni bloquea el aim ray) y toma la media altura del mesh para el box.

## MoriMochiAgent
- `RestoreNavMeshControl`: un agente reusado del pool conserva el estado de su vida anterior si no se resetea; es el reset idempotente llamado al inicio de `Initialize`.
- `PrepareForPool` / `AgentConfinement.DetachForReuse`: detach de reciclaje silencioso, NO es una salida del jugador — no persiste ni cancela estado de dominio (el huevo). `Release` es exclusivo de `OnGrab`.
- `Initialize` (`breedingAreaName`/`areaMask`): los agentes libres EXCLUYEN el área de cría (rodean los corrales); un agente encerrado está RESTRINGIDO a ella; sin área configurada (-1) cae a `AllAreas`.
- El estado `Carried` no tiene tick propio: el seguimiento de carga corre en `FixedUpdate`.

## AgentPhysics
- `Knock`: un knock en pleno vuelo NO resetea el timeout de seguridad (`thrownTimer`); un cluster de criaturas golpeándose lo resetearía indefinidamente y quedarían colgadas en el aire.
- `RecoverIfStuckOffMesh`: red de seguridad de cold-start — un handoff fallido (primera carga antes de bakear, pull tardío, rebake) puede dejar una criatura kinematic FUERA de la malla; un criador encerrado se re-ancla sin tocar el censo del corral ni cancelar su huevo (`Release` cancelaría la cría); uno libre cae a física.
- `TickThrown`: el settle solo cuenta si está lento Y apoyado en el piso — velocidad baja en medio de un rebote o cayendo de un borde no cuenta.

## BreedingContainer
- `TryRollPair`: `StartBreedingAsync` solo marca a los padres como `Breeding` si el SERVIDOR aceptó el huevo; si devolvió `already_breeding`, no se miente en el reporte ni se quema cooldown. `StartBreedingAsync` persiste ANTES de que se seteen `LocationKey`/`LocationSlot`: hay que volver a persistir (`GameEvents.RegistryChanged`) después, o se pierden al recargar (reclaim y cortejo dependen de ellos).
- `Release`/`CancelBreeding`: sacar una criatura del corral cancela el emparejamiento en curso; ambos padres vuelven a no-breeding para que sus tags pierdan corazón/timer.

## AgentSocial (de la lectura del orquestador)
- El handshake de chase/sleep/fight es simétrico al cortejo: el iniciador pide `TryJoin*` al target y solo procede si acepta; después no hay más llamadas cruzadas — cada lado detecta que el otro se fue por `partner.IsSocializing` cada tick.
- `End(completed, notifyPartner)`: el cierre natural completa también el lado del partner en el mismo frame (los dos cobran el Affect); la historia en `SocialGraphService` se registra SOLO en el lado que notifica (primario) para evitar el doble registro por carrera de frames. El knock del final de una pelea lo aplica cada lado sobre sí mismo.
- `BeginSleep`: reserva la estación ANTES del handshake y la libera si el partner rechaza.

## MoriMochiSpawner (de la lectura del orquestador)
- Startup: prewarm de 1 criatura por frame mientras corre `startDelay`, luego espera `WorldReady` (primer `OnNavMeshRebaked` tras cargar muebles, con debounce) y `DataReady` (reload autoritativo o `dataReadyTimeout`); el cañón no dispara antes.
- Toda criatura libre sale como RAGDOLL (velocidad balística resuelta para caer dentro de `spawnRadius`); el agente solo toma control al asentarse. Las ancladas (`LocationKey`) se colocan DIRECTO en su lugar; si el lugar no está registrado se difieren hasta `anchorPlaceTimeout` y recién ahí van por cañón limpiando el anchor huérfano (con `RegistryChanged`).
- Recién nacidos: `RegisterBirthLaunch` (lo llama el corral) fija muzzle y aterrizaje para que la cría salga del corral.
- Activar siempre sobre un punto de NavMesh válido (`ResolveActivationPoint`) antes de `Launch`, nunca al revés (`NavMeshAgent.OnEnable` fuera de malla da error).

## NeedStation (del reporte del script)
- Capacidad = cantidad de use points (slots); cada agente reserva el slot libre y ALCANZABLE más cercano y lo retiene hasta terminar o ser interrumpido; sin puntos → un slot implícito en el transform. `TryReserve` es re-entrante (si ya tiene slot, lo reusa).
