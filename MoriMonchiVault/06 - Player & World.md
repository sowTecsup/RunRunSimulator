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
- **Throw**: la fuerza va **directo sobre `cameraTransform.forward`** (respeta el pitch siempre — mirar arriba lanza arriba). Un `throwUpwardBias` (0–1, default 0.15) agrega un leve arco para que un tiro horizontal no salga completamente plano. `throwAimDistance` fue eliminado.

## Mundo — MoriMonchis vivos

Convierte criaturas del registro (data) en cubos vivos en la escena. Tres scripts en `Scripts/World/`, comunicados por eventos (sin referencias cruzadas), fieles a la filosofía componente + bus.

### MoriMochiSpawner — bridge data→escena

- Escucha `GameEvents.OnRegistryChanged` (incremental: las mismas instancias de DNA se mutan in-place → los agents vivos siguen válidos) y `OnRegistryReloaded` (cloud pull/reset reemplaza los objetos DNA → rebuild completo).
- **Por ahora spawnea TODA criatura viva**; despawnea las muertas o removidas. (Futuro: zonas por estado — cola de combate, incubadora física.)
- Coloca cada cubo en el NavMesh cerca de un `spawnArea`; si la personalidad confina a un área, samplea dentro de esa `areaMask`.
- Resuelve prefab + assets de `GameManager.Instance` (registry, `PersonalityProfiles`). `Initialize(dna, table, player)` cablea cada agent.
- Botón **Respawn All** (DEV).

### MoriMochiAgent (implementa `IThrowable`) — el cerebro

- `NavMeshAgent` + **state machine** `Idle / Roaming / Reacting / Held / Recovering`, **sesgada por la personalidad** (lee el `PersonalityProfile` resuelto — NUNCA hace `switch` por `Personality`).
- **Roaming**: samplea un punto aleatorio en `RoamRadius`, camina, a veces idlea (`IdleChance`). **Confinamiento real** por `NavMeshAgent.areaMask` (la criatura solo pisa polígonos de su `WorldArea` si `ConfineToArea`).
- **Reacción por proximidad**: si el player entra en `ProximityRadius`, interrumpe su comportamiento y reacciona según personalidad (`Flee`/`Approach`/`Follow`/`Retreat`; `Ignore` no reacciona). Al alejarse (con histéresis ×1.25) vuelve a su estado anterior. El "follow" emerge de la personalidad, no es un comando.

### Handoff NavMesh⇄Throwable (la tensión técnica real)

Normalmente `NavMeshAgent` activo + `Rigidbody` kinematic.

- Al agarrar (`OnGrab`): agent off, rb dinámico, sigue el anchor por velocidad (igual feel que `ThrowableObject`).
- Al lanzar (`OnThrow`): impulso físico.
- Al asentarse (velocidad < `settleSpeed` por `settleDelay`): entra en **`Recovering`** (no reanuda inmediatamente).
  - `BeginGetUp()`: hace `NavMesh.SamplePosition` → `agent.Warp`, apaga `agent.updateRotation`, calcula la rotación objetivo (yaw conservado, pitch a 0) y entra en `Recovering`.
  - `TickRecovering()`: espera `downedDelay` (aturdido inmóvil, default 0.6 s), luego `Slerp` suave a vertical durante `getUpDuration` (default 0.5 s). Al terminar: `EnterRoaming()` restaura `agent.updateRotation = true`.

⚠️ **El cubo de la criatura usa `MoriMochiAgent`, NO `ThrowableObject`** (el agent ya implementa `IThrowable`; el player lo agarra/lanza por el mismo contrato).

### PersonalityProfileSO — tuning data-driven

SO singleton (`Current`), `Dictionary<Personality, PersonalityProfile>` `[OdinSerialize]`. Botón **Populate Defaults** llena las 6.

**Campos de `PersonalityProfile`**: `MoveSpeed`, `IdleChance`, `IdleMin/Max`, `RoamRadius`, `ProximityRadius`, `Reaction`, `FollowDistance`, `PreferredArea` (`WorldArea`), `ConfineToArea`.

**Mapeo por defecto:**

| Personality | Área | Reaction |
|-------------|------|----------|
| Social / Curious / Aggressive | ShopFrontDesk | Follow / Approach / Approach |
| Lazy | ShopBackroom | Ignore |
| Skittish / Grumpy | Storage | Flee / Retreat |

Es el **endpoint reservado** para que la personalidad importe a futuro (combate, breeding) sin tocar el enum ni esparcir switches.

### NameTag

- Label 3D (TMP) flotante: nombre + línea de estado (En cola / Incubando / Muerto, leídos de `BusyState`/`IsDead` cada frame).
- **Billboard** a la cámara, **visible solo por proximidad** (`showDistance`).
- `Bind(dna)`. Vista pura.

### NavMesh — setup de escena (3 Areas)

- Crear en **Navigation → Areas** tres áreas con nombre EXACTO: `ShopFrontDesk`, `ShopBackroom`, `Storage` (sin espacios — `WorldArea.ToString()` debe matchear; `NavMesh.GetAreaFromName`).
- Requiere el package **AI Navigation** (`NavMeshSurface` para hornear). El runtime solo usa `UnityEngine.AI` (core).

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
