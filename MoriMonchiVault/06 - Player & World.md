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

- `NavMeshAgent` + **state machine** `Idle / Roaming / Reacting / Held / Recovering`, **sesgada por la personalidad** (lee el `PersonalityProfile` resuelto — NUNCA hace `switch` por `Personality`).
- **Movimiento libre, preferencia ≠ confinamiento**: `agent.areaMask = NavMesh.AllAreas` siempre. En `EnterRoaming`, con probabilidad `AreaPreference` el punto de roam apunta al `PreferredArea` (`TryGetPreferredPoint`), si no, a un punto random en `RoamRadius`. **`ConfineToArea` fue ELIMINADO** — ya no hay jaula por `areaMask`.
- **Reacción por proximidad**: si el player entra en `ProximityRadius`, interrumpe y reacciona según personalidad (`Flee`/`Approach`/`Follow`/`Retreat`; `Ignore` no reacciona). Al alejarse (histéresis ×1.25) vuelve al estado anterior. El "follow" emerge de la personalidad, no es un comando.
- **Tint por personalidad**: `ApplyTint(profile.Tint)` en `Initialize` vía `MaterialPropertyBlock` (setea `_BaseColor` URP + `_Color` built-in) → **sin clonar material, sin fuga**. El mesh vive en el hijo `Model` (el root NO tiene mesh): `bodyRenderer` serializado, fallback a `transform.Find("Model")`.
- **Gizmos** (solo Play, ya inicializado el profile): `DrawWireSphere` de ProximityRadius/RoamRadius/FollowDistance + esfera con el Tint + línea al destino. Sin `Handles` → compila en build.

### Vuelo: bounce + knock + settle (100% por código)

> Decisión firme: **NADA de PhysicMaterials**. Todo el rebote/frenado se calcula en el script.

- **Rebote tipo peluche** (`OnCollisionEnter`, solo en vuelo): `lastVelocity` se captura cada `FixedUpdate` mientras vuela (la `rb.velocity` post-impacto ya viene alterada por la respuesta de contacto). En el choque refleja `Vector3.Reflect(lastVelocity, normal) * bounciness`, hasta `maxBounces` veces, + torque random (`bounceSpin`) para que lea como peluche. Impactos < `minBounceSpeed` no cuentan (evita micro-rebotes infinitos). El frenado lo dan `thrownLinear/AngularDamping` del Rigidbody.
- **Knock / ragdoll en cadena**: `IThrowable.Knock(Vector3)` se agregó al contrato. Un MoriMochi en vuelo que choca a OTRO `IThrowable` lo manda a volar (handoff NavMesh→física + impulso `knockTransfer`, con `knockUpBias` de pop vertical) → reacción en cadena. Un objeto en mano ignora el Knock.
- **Settle robusto** (`TickHeld`): solo asienta cuando está lento **Y** `IsGrounded()` (raycast hacia abajo ignorando su propio collider) — velocidad baja en pleno rebote o resbalando por un borde no cuenta. Red de seguridad: `maxThrownTime` (default 6 s) lo recupera sí o sí aunque siga deslizando.

### Handoff NavMesh⇄Throwable + levantarse

Normalmente `NavMeshAgent` activo + `Rigidbody` kinematic.

- **Grab** (`OnGrab`): agent off, rb dinámico (sin gravedad), persigue el `holdAnchor` por velocidad (`followSpeed`) en `FixedUpdate`.
- **Throw/Release** (`OnThrow`/`OnRelease`): rb con gravedad + damping, impulso, resetea `bounceCount`/timers. Queda en `Held` hasta asentar.
- **Knock**: igual que un throw pero disparado por otro throwable (ver arriba).
- **Levantarse natural** (`BeginGetUp` → `Recovering`): al asentar, `NavMesh.SamplePosition` → `agent.Warp` (si no puede reengancharse, sigue caído y reintenta), apaga `agent.updateRotation`. `downedDelay`/`getUpDuration` se **escalan por `RecoverySpeed`** (lazy = groggy, skittish = salta) **+ `getUpJitter`** random (mismos arquetipos no se levantan en sync). `TickRecovering` espera el daze y luego `Slerp` a vertical (yaw conservado, pitch a 0). Al terminar → `EnterRoaming` (restaura `updateRotation`).

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

### Corral de confinamiento (DISEÑO — sin implementar)

> Base para el **corral de breeding** futuro. Por ahora **solo confinamiento**: un mueble donde tiro hasta N MoriMonchis y quedan caminando confinados; salen solo si yo los sujeto. La lógica de breeding (juntar 2 → cría) llega después y recién ahí se enganchará a `GameEvents`.

**Es furniture, reúso total.** El corral es un `FurnitureDefinitionSO` con `Footprint = 2x2` y su `Price`, comprado/colocado por el sistema de furniture existente. El **prefab** lleva el componente `MoriMochiContainer` + un `BoxCollider` **trigger**. **`FurnitureService` / `FurnitureSpawner` / grid / `FurnitureRegistry` NO se tocan** — un corral es solo un prefab de furniture con componentes extra.

**Flujo confirmado (independiente de A/B de abajo):**

- **Entrada = solo lanzado.** El `BoxCollider` trigger del corral → `OnTriggerEnter` dispara para cualquier MoriMochi (el `Rigidbody` kinemático del agent igual genera trigger events). Ramas:
  - es ocupante mío → ignorar (es el de adentro, no un intruso).
  - `agent.IsAirborne` (lanzado, en ragdoll) → intento de entrada: con cupo → **admitir** (guardo ref + lo paso a modo confinado); lleno → `agent.Knock(arriba+afuera)` → **rebote del aforo** (reusa el `Knock`/bounce que ya existe).
  - caminando (kinemático, no ocupante) → **intruso** → `agent.AvoidArea(bounds)` ("el evento adentro del morimonchi" que lo hace re-rutear lejos).
- **Aforo configurable**: `[MinValue(1)] capacity` en el inspector (default 2).
- **Censo**: lista `occupants` (cada agent expone `DNA` → el corral sabe *quiénes*, no solo cuántos).
- **Salida = solo el jugador al sujetarlo**: `OnGrab` → si confinado, `pen.Release(this)` (sale del censo, vuelve a libre). Al tirarlo a otro lado, `BeginGetUp` lo re-engancha al NavMesh global normal.

**Requiere exponer en `MoriMochiAgent`**: `IsAirborne` (= `state==Held && !heldByPlayer && !rb.isKinematic`) — única forma limpia de distinguir "lo tiraron" de "se metió caminando".

#### Disyuntiva abierta — cómo se mueve adentro + cómo lo evitan los de afuera

La tensión clave: **carve y NavMesh-adentro NO coexisten en el mismo 2x2**. Si carveás el footprint para que los de afuera lo esquiven, borrás el NavMesh interior y el confinado ya no puede recorrer con NavMesh.

> Aclaración importante (corrige un miedo): lo que reposiciona raro a todos los agents es un **rebake completo** (`NavMeshSurface.BuildNavMesh()`). Un **`NavMeshObstacle` con Carve** NO hace eso — recorta un hueco **local** y los demás simplemente rodean, sin re-hornear todo ni reposicionarse.

- **Propuesta A (todo NavMesh, confinamiento blando)** — *la que el usuario eligió inicialmente*:
  - Adentro: el agent **sigue siendo NavMeshAgent**; el destino se samplea **dentro de `boxCollider.bounds`** + `NavMesh.SamplePosition` para validar. No se escapa porque solo le damos destinos de adentro. Sin isla, sin bake.
  - Afuera: evitación **reactiva** — en el `OnTriggerEnter` del intruso, `AvoidArea` le da un destino alejándose del centro.
  - **Caveat**: la evitación es reactiva → los de afuera caminan HASTA el corral, "chocan", y recién ahí rodean (se ve el bumpeo). Confinamiento "blando" (depende de que solo sampleemos adentro).

- **Propuesta B (carve + steering interno sin NavMesh)** — *recomendada por Claude para que se vea pulido*:
  - El corral lleva un `NavMeshObstacle` (carve) → los de afuera lo **esquivan de lejos**, limpio y proactivo.
  - Adentro: como el footprint quedó carveado (sin NavMesh), el confinado se mueve con **steering simple acotado a bounds** (no NavMesh) → agrega un modo de movimiento no-NavMesh al agent. Confinamiento "duro" (clamp a bounds).

**Código nuevo (acotado, ambas propuestas):**

| Pieza | Qué |
|------|-----|
| `MoriMochiContainer.cs` (NEW, World/) | `BoxCollider` trigger, `[MinValue(1)] capacity`, censo `occupants`, `OnTriggerEnter` (admite/rebota/repele), `Release` |
| `MoriMochiAgent.cs` | + `IsAirborne` (público), `EnterConfinement(pen)`, `AvoidArea(...)`, hook en `OnGrab`; + sampling confinado en `EnterRoaming` (A) **o** estado de steering no-NavMesh (B) |

Opcional: `ICreatureReceiver` (drop-a-script estilo `IThrowable`) para que el agent no dependa del concreto.

**Desacople**: agent ↔ corral son **mismo dominio World** → refs directas / interface (como `IThrowable`), no es lookup a singleton de otro sistema. **Sin `GameEvents` ni persistencia de ocupantes todavía** (breeding y compra = futuro; la colocación del corral ya la persiste `FurnitureRegistry`). Edge a resolver al implementar: **levantar/mover un corral ocupado en build mode** → lo más limpio es **bloquear `TryLift` si tiene ocupantes**.

**PARA RETOMAR**: decidir **A vs B**. Con eso confirmado se implementa directo.

### Estado del roadmap

**Etapa 2.5 — Vida en Escena** 🔶 Código ✅ (World/: MoriMochiSpawner, MoriMochiAgent, NameTag · Personality enum + PersonalityProfileSO · CombatRecord/CombatTurn en DNA, JS sincronizado).

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
