---
tags: [memory-bank, player, world, navmesh, personality]
---

# 06 — Player & World

> Relacionados: [[05 - UI System]] (action maps Player/UI excluyentes, IUINavigable), [[02 - Genetics & Breeding]] (Personality del DNA), [[03 - Combat]] (CombatHistory para Combat Visualizer futuro).

## Player FP (primera persona)

Tres scripts que **NO se referencian entre sí**; se comunican por **static events** en `PlayerInputs` (mismo patrón que `GameEvents`: el evento transporta la data, el listener cachea el payload). Suscribir en `OnEnable`, desuscribir en `OnDisable` (regla 9).

### PlayerInputs

- Única clase que toca `InputSystem_Actions`.
- Traduce callbacks crudos a static events: `MoveChanged(Vector2)`, `Jumped`, `InteractPressed` (E key-down), `InteractReleased` (E key-up), `ThrowPressed` (Attack).
- El `Look` NO está acá — lo maneja Cinemachine.
- Dueño del action map `Player`. Deshabilita el mapa Player mientras hay foco UI (escucha `OnUIFocusChanged`) → en menú no llega input de gameplay ni se puede cerrar con E.

### PlayerController

- Solo lógica. Move FP con `CharacterController` (relativo a la cámara), jump, grab/throw vía interfaces, y **state machine** (`PlayerStateType`).
- **NO referencia `PlayerInputs`** (se suscribe a sus static events).
- **NO maneja la cámara**: lee el `forward` para mover/agarrar/lanzar.
- Referencia una `CinemachineCamera`: de ahí saca el transform (forward) y el `CinemachineInputAxisController` (lo desactiva para congelar la cámara en `Menu`).
- Escucha `UIManager.OnUIFocusChanged` → `Menu` (cursor libre, sin control) / `Exploring`.
- Tap E = interactuar / Hold E = agarrar / press E cargando = soltar / Click = lanzar.

### PlayerAnimator

- Solo animación. Se suscribe a los static events.
- Inerte hasta asignar un `Animator` (todo guardado por null).
- Seam para cuando existan clips.

## Cámara

Cinemachine primera persona:
- Position Control = *Hard Lock to Target* sobre un `Head`.
- Rotation Control = *Pan Tilt*.
- + *Cinemachine Input Axis Controller* leyendo la acción `Look`.

`PlayerController` referencia una **`CinemachineCamera`** (no la Main Camera). De ella deriva en `Awake`:
- `cameraTransform` (su `forward` = mirada).
- `CinemachineInputAxisController` (vía `TryGetComponent`) que **desactiva para congelar la cámara** en estado `Menu`.

`using Unity.Cinemachine;` (sin asmdef, `Assembly-CSharp` ya referencia el package).

## Estado del Player (`PlayerStateType`: None / Exploring / Menu)

Conmuta de estado escuchando `UIManager.OnUIFocusChanged(bool)` (true cuando hay ≥1 panel abierto):

| Estado | Cursor | Cámara | Input |
|--------|--------|--------|-------|
| **Exploring** | Bloqueado/oculto | `CinemachineInputAxisController` activo | mapa `Player` habilitado |
| **Menu** | Libre/visible | Congelada | mapa `Player` **deshabilitado** (suspende todo el input de gameplay) |

`SetState` centraliza cursor + freeze de cámara. Estado inicial `Exploring` en `Start`.

- En `Menu` ya **NO se cierra con E**: el mapa `Player` está apagado → el interact del mundo no puede dispararse (esto además mata el bug del "interact togglea el panel de atrás"). Se cierra con **ESC** (router del UIManager).
- Los guards `if (state != Exploring) return;` quedan como red de seguridad.

## Grab / Interact / Throw (semántica de E + Click)

`E` significa distinto según contexto:

| Estado | Input | Acción |
|--------|-------|--------|
| Libre | **tap E** (press < `grabHoldDuration`) | Interactuar con un `IInteractable` |
| Libre | **hold E** (≥ `grabHoldDuration`) | Agarrar un `IThrowable` |
| Cargando | **press E** | Soltar en el sitio |
| Cargando | **Click (Attack)** | Lanzar |

- Raycast genérico: `TryFindInView<T>` busca `T` (interface o clase) en el collider O en su `attachedRigidbody`. Lo usan grab (`IThrowable`) e interact (`IInteractable`).
- **Throw — converge a la mira**: el objeto flota en el `holdAnchor` (que está al costado), así que lanzar sobre `cameraTransform.forward` desde ahí saldría paralelo y nunca llegaría a la mira. En su lugar se lanza **desde el `holdAnchor` HACIA el punto que mira la cámara**: un raycast `throwAimDistance` (default 30 m) al centro de pantalla → ese hit (o el punto a 30 m si no pega nada) es el `aimPoint`; el objeto sostenido se ignora vía `IsChildOf`. `throwUpwardBias` (0–1, default 0.15) mezcla un leve arco. (Esta iteración **reintrodujo** `throwAimDistance`.)

## Mundo — MoriMonchis vivos

Convierte criaturas del registro (data) en cubos vivos en la escena. Tres scripts en `Scripts/World/`, comunicados por eventos (sin referencias cruzadas), fieles a la filosofía componente + bus.

### MoriMochiSpawner — bridge data→escena

- Escucha `GameEvents.OnRegistryChanged` (incremental: las mismas instancias de DNA se mutan in-place → los agents vivos siguen válidos) y `OnRegistryReloaded` (cloud pull/reset reemplaza los objetos DNA → rebuild completo). Suscribe/desuscribe en `OnEnable`/`OnDisable`.
- **Por ahora spawnea TODA criatura viva**; despawnea las muertas o removidas. (Futuro: zonas por estado — cola de combate, incubadora física.)
- **Spawn sesgado a "casa"**: `ResolveSpawnPosition` samplea un punto cerca del `spawnArea` con `areaMask = 1<<PreferredArea` → la criatura **arranca** en su área preferida. Fallback a `AllAreas` si esa área no es alcanzable. El sesgo es solo en el spawn; después se mueve libre (ver agent), no es una jaula.
- Resuelve prefab + assets de `GameManager.Instance` (registry, `PersonalityProfiles`). `Initialize(dna, table, player)` cablea cada agent.
- Botón **Respawn All** (DEV, solo Play).

### MoriMochiAgent (implementa `IThrowable`) — el cerebro

- `NavMeshAgent` + **state machine** `Idle / Roaming / Reacting / Carried / Thrown / Recovering`, **sesgada por la personalidad** (lee el `PersonalityProfile` resuelto — NUNCA hace `switch` por `Personality`). **`Carried`** = en la mano del player; **`Thrown`** = ragdoll en vuelo (antes ambos eran un solo `Held` desambiguado con bools — el refactor los separó: `IsAirborne => state==Thrown`, `IsHeld => state==Carried`).
- **Movimiento libre, preferencia ≠ confinamiento**: por defecto `agent.areaMask = AllAreas & ~(1<<BreedingRoom)` (los libres rodean los corrales; ver Corral abajo). En `EnterRoaming` el destino lo decide **`NextRoamDestination()`**: confinado → punto dentro de los bounds del corral; libre → con prob. `AreaPreference` apunta al `PreferredArea` (`TryGetPreferredPoint`), si no, random en `RoamRadius`. **`ConfineToArea` (la vieja jaula por personalidad) sigue ELIMINADO** — el único confinamiento por `areaMask` es el del corral.
- **Reacción por proximidad**: si el player entra en `ProximityRadius`, interrumpe y reacciona según personalidad (`Flee`/`Approach`/`Follow`/`Retreat`; `Ignore` no reacciona). Al alejarse (histéresis ×1.25) vuelve al estado anterior. El "follow" emerge de la personalidad, no es un comando.
- **Tint por personalidad**: `ApplyTint(profile.Tint)` en `Initialize` vía `MaterialPropertyBlock` (setea `_BaseColor` URP + `_Color` built-in) → **sin clonar material, sin fuga**. El mesh vive en el hijo `Model` (el root NO tiene mesh): `bodyRenderer` serializado, fallback a `transform.Find("Model")`.
- **Gizmos** (solo Play, ya inicializado el profile): `DrawWireSphere` de ProximityRadius/RoamRadius/FollowDistance + esfera con el Tint + línea al destino. Sin `Handles` → compila en build.
- **Inspector (Odin)**: tuning agrupado en tabs `Movement` (NavMesh) / `Physics` (throwable) / `Presentation` (visuals + Feedbacks), vía `[TabGroup]` + `[Title]`.

### Vuelo: bounce + knock + settle (100% por código)

> Decisión firme: **NADA de PhysicMaterials**. Todo el rebote/frenado se calcula en el script.

- **Rebote tipo peluche** (`OnCollisionEnter`, solo en vuelo): `lastVelocity` se captura cada `FixedUpdate` mientras vuela (la `rb.velocity` post-impacto ya viene alterada por la respuesta de contacto). En el choque refleja `Vector3.Reflect(lastVelocity, normal) * bounciness`, hasta `maxBounces` veces, + torque random (`bounceSpin`) para que lea como peluche. Impactos < `minBounceSpeed` no cuentan (evita micro-rebotes infinitos). El frenado lo dan `thrownLinear/AngularDamping` del Rigidbody.
- **Knock / ragdoll en cadena**: `IThrowable.Knock(Vector3)` se agregó al contrato. Un MoriMochi en vuelo que choca a OTRO `IThrowable` lo manda a volar (handoff NavMesh→física + impulso `knockTransfer`, con `knockUpBias` de pop vertical) → reacción en cadena. Un objeto en mano ignora el Knock.
- **Settle robusto** (`TickThrown`): solo asienta cuando está lento **Y** `IsGrounded()` (raycast hacia abajo ignorando su propio collider) — velocidad baja en pleno rebote o resbalando por un borde no cuenta. Red de seguridad: `maxThrownTime` (default 6 s) lo recupera sí o sí aunque siga deslizando.

### Handoff NavMesh⇄Throwable + levantarse

Normalmente `NavMeshAgent` activo + `Rigidbody` kinematic. El toggle se centraliza en 3 helpers (refactor): **`DetachToPhysics()`** (agent off + rb dinámico), **`ApplyThrownPhysics()`** (gravedad + damping + reset de `bounceCount`/timers) y **`RejoinNavMesh(desired, mask)`** (kinematic + agent on + `SamplePosition`+`Warp`+`ResetPath`, devuelve bool de éxito).

- **Grab** (`OnGrab` → `Carried`): `DetachToPhysics()` + sin gravedad, persigue el `holdAnchor` por velocidad (`followSpeed`) en `FixedUpdate`.
- **Throw/Release** (`OnThrow`/`OnRelease` → `Thrown`): `ApplyThrownPhysics()` + impulso. Queda en `Thrown` hasta asentar.
- **Knock** (`Thrown`): `DetachToPhysics()` + `ApplyThrownPhysics()` + impulso, disparado por otro throwable. Ignorado si está `Carried` o confinado en un corral.
- **Levantarse natural** (`BeginGetUp` → `Recovering`): usa `RejoinNavMesh(posición actual, areaMask)`; si no puede reengancharse vuelve a `Thrown` y reintenta. Apaga `agent.updateRotation`. `downedDelay`/`getUpDuration` se **escalan por `RecoverySpeed`** (lazy = groggy, skittish = salta) **+ `getUpJitter`** random. `TickRecovering` espera el daze y `Slerp` a vertical (yaw conservado) → al terminar `EnterRoaming` (restaura `updateRotation`).

⚠️ **El cubo de la criatura usa `MoriMochiAgent`, NO `ThrowableObject`** (el agent ya implementa `IThrowable`; el player lo agarra/lanza/knockea por el mismo contrato).

### Feel-ready (juice sin acoplar código)

- 5 `UnityEvent` en el agent: `onGrab` / `onThrow` / `onBounce` / `onLand` / `onGetUp`. Disparan ya (compila sin Feel instalado).
- Feel **ya está instalado** (`Assets/Feel`). Plantilla: poner un `MMF_Player` en el prefab y cablear su `PlayFeedbacks()` al `UnityEvent` correspondiente **en el inspector** — cero acoplamiento de código. Es el patrón que todo script con "juice visual" futuro debe seguir.
- **Estructura del prefab**: root `MoriMochi Agent` (sin mesh; NavMeshAgent+Rigidbody+Collider+MoriMochiAgent+NameTag) → hijos `Model` (mesh, lo tiñe `bodyRenderer`) y `Feedbacks` (los `MMF_Player`).

### PersonalityProfileSO — tuning data-driven

SO singleton (`Current`), `Dictionary<Personality, PersonalityProfile>` `[OdinSerialize]`. Botón **Populate Defaults** llena las 6.

**Campos de `PersonalityProfile`**: `MoveSpeed`, `IdleChance`, `IdleMin/Max`, `RoamRadius`, `ProximityRadius`, `Reaction`, `FollowDistance`, `PreferredArea` (`WorldArea`), **`AreaPreference`** (0–1: prob. de que el roam apunte a la preferida; reemplaza a `ConfineToArea`), **`RecoverySpeed`** (ritmo de levantarse: >1 más rápido), **`Tint`** (color del cuerpo por personalidad).

**Mapeo por defecto:**

| Personality | Área preferida | Reaction | AreaPref |
|-------------|----------------|----------|----------|
| Skittish | Storage | Flee | 0.80 |
| Aggressive | ShopFrontDesk | Approach | 0.60 |
| Lazy | ShopBackroom | Ignore | 0.70 |
| Curious | ShopBackroom | Approach | 0.20 |
| Social | ShopFrontDesk | Follow | 0.50 |
| Grumpy | Storage | Retreat | 0.75 |

Es el **endpoint reservado** para que la personalidad importe a futuro (combate, breeding) sin tocar el enum ni esparcir switches.

> ⚠️ El `.asset` de `PersonalityProfileTable` que ya exista en disco tiene campos `ConfineToArea` viejos → **re-pulsar Populate Defaults** para migrarlo a `AreaPreference`/`RecoverySpeed`/`Tint`.

### NameTag

- Label 3D (TMP) flotante: nombre + línea de estado (En cola / Incubando / Muerto, leídos de `BusyState`/`IsDead` cada frame).
- **Billboard** a la cámara, **visible solo por proximidad** (`showDistance`).
- `Bind(dna)`. Vista pura.

### NavMesh — setup de escena (3 Areas)

- Crear en **Navigation → Areas** tres áreas con nombre EXACTO: `ShopFrontDesk`, `ShopBackroom`, `Storage` (sin espacios — `WorldArea.ToString()` debe matchear; `NavMesh.GetAreaFromName`).
- Requiere el package **AI Navigation** (`NavMeshSurface` para hornear). El runtime solo usa `UnityEngine.AI` (core).
- **`BreedingRoom`**: Area type adicional para los corrales (ver Corral abajo). Los agentes libres lo **excluyen** de su `areaMask`; los confinados quedan **restringidos** a él. No va en el enum `WorldArea` (se resuelve por el campo `breedingAreaName` del agente).

### Corral de confinamiento (breeding pen) — IMPLEMENTADO ✅

> Base para el **breeding pen** futuro. Hoy: un mueble donde tiro hasta `capacity` MoriMonchis y quedan caminando confinados; salen **solo si yo los sujeto**. La lógica de breeding (juntar 2 → cría) llega después y recién ahí se enganchará a `GameEvents`. Archivos: `MoriMochiContainer.cs` (World/) + cambios en `MoriMochiAgent`.

**Es furniture, reúso total.** El corral es un `FurnitureDefinitionSO` (`Footprint = 2x2`) cuyo **prefab** lleva `MoriMochiContainer` + un `BoxCollider` **trigger** + un `NavMeshModifier` que pinta su piso con el Area type `BreedingRoom`. `FurnitureService`/`FurnitureSpawner`/grid/`FurnitureRegistry` **no cambian** por el corral (es solo un prefab con componentes extra).

#### Mecanismo: área pintada + `areaMask` (una sola superficie)

Se descartaron las propuestas A/B y el volume/carve. **Una superficie continua**: el `NavMeshModifier` del corral pinta su footprint como `BreedingRoom`; al colocar el mueble se **rebakea** (puntual; botón + auto-rebake en `FurnitureService`, ver [[10 - Furniture & Building]]). La exclusión y el confinamiento son por **`areaMask` por agente** (NO por costo: `SamplePosition` ignora el costo → no fencea):
- **Libres**: `agent.areaMask = AllAreas & ~(1<<BreedingRoom)` (en `Initialize`) → el piso del corral es intransitable, ni se samplea ahí → rodean **todos** los corrales.
- **Confinados**: `areaMask = 1<<BreedingRoom` → no salen caminando. **Múltiples corrales** coexisten con **un solo** Area type: el mask los mantiene fuera del piso normal y el roam-por-bounds los ata a SU corral (aunque dos pisos de breeding se toquen).

`breedingAreaName` es un **campo serializado** en `MoriMochiAgent` (dropdown Odin `[ValueDropdown]` con los nombres reales de Navigation → Areas). Se resuelve con `NavMesh.GetAreaFromName`; si da -1 (área sin crear) degrada a `AllAreas`. **No** hace falta sumarlo al enum `WorldArea`.

#### `MoriMochiContainer` (World/)

- **Aforo**: `[Min(1)] capacity` (default 2). **Censo**: `occupants`; expone `Occupants`, `IsFull`, `Center`, `InteriorBounds`, y `OccupantDNAs` (`[ShowInInspector, ReadOnly]` → se ven en runtime; para breeding/UI futuro).
- **Entrada por trigger**: `OnTriggerEnter` admite solo si `agent.IsAirborne` (lanzado, ragdoll). Con cupo → `Admit`; lleno → `BounceOut` (`agent.Knock` arriba+afuera). `OnTriggerStay` atrapa el caso de **soltar adentro** (ya estaba dentro → no hay Enter); en Stay nunca rebota.
- `Admit` registra al ocupante **solo si `agent.EnterConfinement(this)` devuelve true** (evita ocupante fantasma si el piso no está bakeado).
- **Salida**: solo el jugador → el `OnGrab` del agente llama `pen.Release(this)`.

#### Lado `MoriMochiAgent`

- `IsAirborne => state == Thrown` (distingue "lo tiraron" de "se metió caminando" — un libre ni puede entrar por el mask).
- `EnterConfinement(pen)` → `bool`: `RejoinNavMesh(pen.Center, confinedAreaMask)` (teleport al centro + corta ragdoll). Si el piso no está pintado+bakeado, **no confina** (warning, vuelve a físicas) → no se registra ocupante (ni se llama `ResetPath` off-mesh).
- Roam confinado: `NextRoamDestination()` samplea dentro de `pen.InteriorBounds`.
- `OnGrab`: si confinado, `Release` + `areaMask = freeAreaMask` (al tirarlo a otro lado `BeginGetUp` lo re-engancha normal).
- **Inmune a tackle**: `Knock` hace early-out si `currentContainer != null` → un confinado (kinematic) no es empujado por otros lanzados; actúa como obstáculo sólido. Solo el player lo saca.

**Desacople**: agent ↔ corral = mismo dominio World → refs directas (como `IThrowable`), sin `GameEvents`. **Sin persistencia de ocupantes** (runtime; la colocación del corral ya la persiste `FurnitureRegistry`).

**Setup de escena**: crear Area `BreedingRoom` (Navigation → Areas); prefab del corral con `BoxCollider` (isTrigger ✓) + `MoriMochiContainer` + `NavMeshModifier` (set area = BreedingRoom); rebakear tras colocar. En el prefab del MoriMochi, elegir el área en el dropdown `breedingAreaName`.

**Pendiente conocido**: levantar/mover un corral **ocupado** en build mode → bloquear `TryLift` si tiene ocupantes (no implementado).

### Sistema de Necesidades (Needs) — IMPLEMENTADO ✅

> 3 stats mutables que el agente desgasta mientras está spawneado; el MoriMochi busca **estaciones** (muebles) para recargarlas, y degrada su comportamiento si no hay. Persisten dentro del save SIN saturar la nube.

**Las 3 stats — `NeedsState` (Data/), anidado en `CreatureDNA.Needs`:**
- `Health` [0,100] — hambre/bienestar, decae pasivo.
- `Energy` [0,100] — decae **al moverse**; gasto puntual en breeding/combate.
- `Affect` [-100,100] — +100 feliz ↔ -100 estresado; **deriva hacia negativo** con el tiempo, baja con throw/colisión brusca, sube en `PlayZone`.
- Mutadores clampeados (`AddHealth/Energy/Affect`), `SpendEnergy` (endpoint), `Restore`/`Get` por `NeedType`.
- **Vive en `CreatureDNA`** (no en un wrapper) porque DNA ya ES el record persistido (como `CombatHistory`/`BusyState`) → cero plomería en SaveSystem/Cloud. No es parte del genetic string. Detalle de persistencia en [[07 - Persistence & Identity]].

**Estaciones — `NeedStation` (abstracta, World/) + `Feeder`/`RestZone`/`PlayZone`:**
- Cada una satisface un `NeedType` (Health/Energy/Affect). Van en un **prefab de furniture**.
- `usePoint` (dónde se para el agente), `fillPerSecond` (recarga continua hasta 100), lock de un usuario (`TryReserve`/`Release`), `Refill(needs, dt)` (true al llegar a 100).
- Auto-registro en `OnEnable`/`OnDisable` → no tocan `FurnitureService`.
- *(Futuro: estaciones con recursos consumibles que el jugador repone; hoy recargan a 100.)*

**Manager — `NeedStationRegistry` (estático, World/):** auto-registro, `GetClosest(pos, type, onlyAvailable)`. Dedicado (no en FurnitureService) por separación de responsabilidades; mismo dominio World que el agente (como el corral) → query directa OK.

**FSM del agente (estados nuevos `SeekingNeed`/`UsingStation`):**
- Tab Odin **Needs**: rates de decay, **umbrales críticos configurables** (`criticalHealth`/`criticalEnergy`/`criticalAffect`), penalizaciones de afecto (`affectOnThrow`/`affectOnHardCollision`/`hardImpactThreshold`), `degradedSpeedMultiplier`.
- `TickNeeds(dt)` (cada Update, antes del switch): decae en memoria **sin disparar eventos**; Energy solo si `IsMoving`; aplica velocidad degradada.
- En Idle/Roaming → `TryEnterNeedSeeking()`: si hay need crítico (prioridad Health > Energy > Affect) pide `GetClosest`, reserva y va a `SeekingNeed` → al llegar `UsingStation` (`isStopped=true`, recarga hasta 100) → `EnterRoaming` (libera).
- **Sin estación libre → degradado**: lento sin energía (`degradedSpeedMultiplier`), sigue perdiendo salud sin comida, **huye del player si está estresado** (`ReactIfPlayerNear` fuerza `Flee` vía `activeReaction`).
- **Interrupción por grab**: `OnGrab` llama `ReleaseStation()` → corta seeking/using limpio → `Carried`. Confinados (corral) no buscan estaciones.

**Endpoints de energía (breeding/combate)** — el monto lo configura cada manager:
- `CombatManagerSO.EnergyCostToQueue` → `AsyncCombatService` lo gasta al encolar.
- `AsyncBreedingService.energyCostPerParent` → lo gasta a ambos padres al iniciar.

**Setup en Unity (pendiente del usuario):** poner `Feeder`/`RestZone`/`PlayZone` en prefabs de furniture (con hijo `usePoint` si el punto de parado difiere), sobre/junto al NavMesh; tunear la tab Needs del agente, `EnergyCostToQueue` y `energyCostPerParent`.

**Próximos pasos / pendientes:**
- Cablear el **flush en logout** (`GameManager.FlushToCloud()` desde `CloudSyncService` al cerrar sesión) — quedó público pero sin enganchar.
- *(Opcional)* petting directo del jugador (E sobre la criatura) además de la `PlayZone`.
- *(Futuro)* recursos consumibles en estaciones; muerte por inanición (hoy Health solo decae, no mata); decay offline (catch-up por timestamp al cargar — hoy solo decae spawneado).

### Estado del roadmap

**Etapa 2.5 — Vida en Escena** 🔶 Código ✅ (World/: MoriMochiSpawner, MoriMochiAgent, NameTag · Personality enum + PersonalityProfileSO · CombatRecord/CombatTurn en DNA, JS sincronizado · **Needs system: NeedsState + NeedStation/Registry + FSM**).

Falta setup de escena en Unity (NavMesh bake + 3 Areas, prefab del cubo, asset Personality Profile Table, wiring del spawner).

## Archivos clave

```
Assets/RunRunSimulator/Scripts/Player/
├── PlayerInputs.cs                   # Dueño del action map "Player". Static events
├── PlayerController.cs               # Solo lógica: move + grab/throw + state machine
├── PlayerAnimator.cs                 # Solo animación. Inerte hasta asignar Animator
├── FirstPersonController.cs          # (referencia de proyecto viejo, sin usar)
└── ThirdPersonController.cs          # (referencia de proyecto viejo, sin usar)

Assets/RunRunSimulator/Scripts/World/
├── MoriMochiSpawner.cs               # Bridge data→escena. Escucha OnRegistryChanged/Reloaded
├── MoriMochiAgent.cs                 # NavMeshAgent + state machine + IThrowable
└── NameTag.cs                        # Label 3D flotante (TMP)

Assets/RunRunSimulator/Scripts/Data/
└── PersonalityProfileSO.cs           # SO singleton (Current): Dictionary<Personality, PersonalityProfile>

Assets/RunRunSimulator/Scripts/Core/
└── Enums.cs                          # ... + PlayerStateType, Personality, ProximityReaction, WorldArea
```
