---
tags: [script, world, npc, core]
---

# NpcAgent.cs

**Ruta:** `World/Npc/NpcAgent.cs`

**Responsabilidad:** Componente MonoBehaviour que encarna un cliente NPC en la tienda: navega entre estantes inspeccionando criaturas, se posiciona en la fila de caja, negocia precio, compra o se va. Dueño de su máquina de estados (`NpcState`, 8 estados) y motivo de salida (`LeaveReason`, 4 valores). Genera "personalidad" per-instancia sorteando variación en velocidad, prioridad de colisión y delay de reacción. Se suscribe a `GameEvents.OnCustomerSold` para detectar si otro cliente le arrebató su objetivo (Outbid).

**Datos públicos:**
- `Archetype` (CustomerArchetypeSO): perfil del cliente (min/max inspecciones, duración inspección, timeout espera).
- `DisplayName` (string): nombre generado al instanciar vía `NpcNameBank.GetRandomName()`, ej. "Carmen Pérez".
- `ReactionDelay` (float): delay random (s) antes de que [[NpcThoughtTag]] muestre una frase nueva. Sorteado en `ApplyInstanceVariation()`.
- `Reason` (LeaveReason): enum anidado `{ None, Purchased, Outbid, QueueFull }` que explica por qué se va: compró exitoso, otro le ganó el objetivo, o no entró a la fila.
- `TargetMM` (CreatureDNA): criatura elegida al inspeccionar. Null si está vagando o sin decisión.
- `InitialOffer` / `CurrentOffer` (int): precio estimado inicial y oferta actual (puede cambiar tras contraoferta).
- `HasCounteredOnce` (bool): flag para permitir solo UNA contraoferta por compra.
- `State` (NpcState): máquina de 8 estados (Spawned, Wandering, InspectingDisplay, ApproachingRegister, Queueing, WaitingAtRegister, Negotiating, Leaving).
- `CurrentDisplay` (StoreContainer): estante donde está inspeccionando.
- `AreaMask` (int, read-only): máscara de áreas de NavMesh por las que el agente puede caminar (`navAgent.areaMask`, o `NavMesh.AllAreas` si aún no inicializado). La consume [[CashRegister]] para muestrear la cola en las mismas áreas (single source of truth del cerco).

**Enums públicos:**
- `NpcState`: {Spawned, Wandering, InspectingDisplay, ApproachingRegister, Queueing, WaitingAtRegister, Negotiating, Leaving}.
- `LeaveReason`: {None, Purchased, Outbid, QueueFull}.

**Métodos públicos:**
- `Initialize(CustomerArchetypeSO archetype, IReadOnlyList<StoreContainer> shopDisplays, CashRegister cashRegister, NpcController owner)` — inicializa, genera nombre, obtiene NavMeshAgent, llama a `ApplyWalkableAreas()` + `ApplyInstanceVariation()`, transiciona a Wandering.
- `AcceptCurrentOffer()` — marca MM como Sold + estampa `SaleDate`, setea `Reason = Purchased`, emite `GameEvents.CustomerSold`, suma dabloons, emite `RegistryChanged`/`InventoryChanged`, transiciona a Leaving.
- `TryCounterOffer()` — si `!HasCounteredOnce`, estima y evalúa contraoferta. Si acepta, actualiza `CurrentOffer` y devuelve true. Si rechaza, transiciona a Leaving y devuelve false.
- `RejectByPlayer()` — transiciona a Leaving.
- `EnterNegotiating()` → Negotiating.
- `ExitNegotiating()` → WaitingAtRegister si estaba en Negotiating.

**Privados clave:**
- `ApplyWalkableAreas()` — convierte `walkableAreaNames` a una máscara de bits vía `NavMesh.GetAreaFromName` (`1 << idx` por cada área válida) y la asigna a `navAgent.areaMask`. Fallback a `NavMesh.AllAreas` si la lista queda vacía o ningún nombre existe. Cerca el pathfinding: el NPC nunca rutea por el breeding room.
- `EditorNavMeshAreaNames()` — helper estático editor-only (envuelto en `#if UNITY_EDITOR`, devuelve `NavMesh.GetAreaNames()`; array vacío en build). Alimenta el `[ValueDropdown]` de `walkableAreaNames`. Espejo del de [[MoriMochiAgent]].
- `ApplyInstanceVariation()` — sorteea por cliente: `navAgent.speed/angularSpeed/acceleration` (±`moveVariation`), `avoidancePriority` (rango `avoidancePriorityRange`), `ReactionDelay` (rango `reactionDelayRange`). Cada cliente sale con "personalidad" de movimiento y reacción distintos.
- `TransitionTo(NpcState next)` — centraliza lógica de cambio (limpia timers, libera slots, posiciona NavMesh).
- `TickWandering()` — busca estantes con criaturas, intenta `TryReserveUsePoint()`, transiciona a InspectingDisplay o Leaving.
- `TickInspecting()` — espera duración, elige mejor MM del estante (máximo precio estimado), emite `GameEvents.CustomerDecided`.
- `TickApproachingRegister()` — solicita slot en fila. Si null, setea `Reason = QueueFull` y transiciona a Leaving.
- `TickQueueing()` — mantiene posición (repolla `CurrentSlotOf()` para detectar cambios), detecta cuando es siguiente (IsFrontSlot).
- `TickWaiting()` — espera respuesta del jugador (timeout → Leaving).
- `TickLeaving()` — navega a ExitPoint, emite `GameEvents.CustomerLeft`, se despawatea.
- `OnEnable()` — suscribe a `GameEvents.OnCustomerSold`.
- `OnDisable()` — desuscribe, libera slots.
- `OnSomeoneSold(buyer, mm, price)` — si otro cliente (≠ this) compró su `TargetMM`, setea `Reason = Outbid` y transiciona a Leaving.

**Serialized (Odin Inspector):**
- `[Title("Movement")]` `arriveDistance` (float, 0.5): tolerancia de distancia para "llegué".
- `[Title("Walkable areas")]` `walkableAreaNames` (List<string>, default {"ShopFrontDesk","Outside"}): áreas de NavMesh por las que el NPC PUEDE caminar — su único cerco. `[ValueDropdown(nameof(EditorNavMeshAreaNames))]` alimenta el dropdown con los nombres reales de Navigation. Vacío o nombres inexistentes → sin restricción (AllAreas). La cola de [[CashRegister]] hereda esta máscara.
- `[Title("Per-instance variation")]` `moveVariation` (float, 0.15): ±15% en velocidad/giro/aceleración.
- `avoidancePriorityRange` (Vector2Int, 30-70): rango de prioridad de colisiones.
- `reactionDelayRange` (Vector2, 0.2-1.2s): rango de delay antes de mostrar frase nueva.

**Cambios principales (Sesión 20):**
- Reemplazó `QueueWasFull` (bool) por enum `LeaveReason` (4 estados del "porqué me voy").
- Propiedades nuevas: `ReactionDelay`, `Reason` y `AreaMask`.
- `Initialize()` ahora llama a `ApplyWalkableAreas()` + `ApplyInstanceVariation()`.
- `AcceptCurrentOffer()` setea `Reason = Purchased` antes de disparar `CustomerSold`.
- `TickApproachingRegister()` setea `Reason = QueueFull` si no hay slot.
- Se suscribe a `GameEvents.OnCustomerSold` en `OnEnable()` / desuscribe en `OnDisable()` (handler `OnSomeoneSold`).
- **Cerco de áreas caminables:** campo `walkableAreaNames` + `ApplyWalkableAreas()` construyen `navAgent.areaMask` desde nombres de Navigation. Nuevo `AreaMask` público lo expone para que [[CashRegister]] muestree la cola en las mismas áreas (single source of truth). El NPC nunca rutea por el breeding room.

**Eventos:**
- `GameEvents.CustomerDecided(this, pick)` — tras decidir MM.
- `GameEvents.CustomerArrivedAtRegister(this)` — llega a frente de fila.
- `GameEvents.CustomerSold(this, mm, offer)` — aceptó compra.
- `GameEvents.CustomerLeft(this, wasSold)` — salió de escena.
- `GameEvents.OnCustomerSold` — escucha (suscrito en OnEnable): otro cliente compró su objetivo.

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[NpcController]], [[NpcNameBank]], [[NpcDialogueBank]], [[NpcThoughtTag]], [[StoreContainer]], [[CashRegister]] (lee `AreaMask` para la cola), [[CustomerArchetypeSO]], [[CustomerService]], [[CreatureDNA]], [[GameEvents]], [[GameManager]], [[MoriMochiAgent]] (mismo patrón de `walkableAreaNames` + dropdown editor)
