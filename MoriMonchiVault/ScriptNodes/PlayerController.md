---
tags: [script, world, player]
---

# PlayerController.cs

**Ruta:** `Player/PlayerController.cs`

**Responsabilidad:** Movimiento first-person del jugador. Lee input de `PlayerInputs` (move, look, interact), aplica fuerzas al Rigidbody. **S69:** Petting hold-E: `TryBeginPetting()` en press-E (OverlapSphere, busca MoriMochiAgent cercano, llama `agent.BeginPetting()`); release-E → `EndPetting()` (llama `petTarget?.EndPetting()` si existe). Tap-interact (raycast) tiene prioridad sobre grab timer. Cambio de estado fuera de Exploring cierra sesión de petting. Expone propiedades `IsMoving`, `Velocity`, `IsGrounded` para animación.

## Campos y propiedades

**Input:**
- `moveDirection` — input de movimiento (WASD)
- `lookDelta` — delta de rotación por frame (mouse/gamepad)
- `interactPressed` — E tecla presionada (tap)

**S69 NUEVOS (Petting):**
- `petTarget` (MoriMochiAgent) — criatura siendo acariciada durante hold-E
- `petPressE` — E prensionada (hold, no tap)
- `petReleaseE` — E soltada (final de hold)

**Estado:**
- `CurrentState` (PlayerState enum) — Exploring, Carrying, Frozen
- `IsMoving → bool` — velocidad > 0
- `Velocity → Vector3` — velocidad del Rigidbody
- `IsGrounded → bool` — contacto con suelo (raycast)

## Métodos clave

**Movimiento:**
- `ApplyMovement()` — lee input, aplica fuerzas al Rigidbody (aceleración + fricción)
- `ApplyGravity()` — gestiona caída (no Rigidbody gravity)

**Interacción:**
- `CheckForInteraction()` — raycast a click-interact (IInteractable)
- `UpdateCarrying()` — mientras sujeta criatura, aplicacuerpo a destino de mano (holdAnchor)

**S69 NUEVOS (Petting):**
- `TryBeginPetting()` — press-E: `OverlapSphere(transform.position, interactRadius)` busca MoriMochiAgent, si `agent.CanBePetted`, llama `agent.BeginPetting()`
- `EndPetting()` — release-E: llama `petTarget?.EndPetting()`, limpia `petTarget = null`

**State:**
- `SetState(PlayerState newState)` — transición, **S69:** si sale de Exploring, llama `EndPetting()` (cierra sesión petting)
- `EnterExploring()`, `EnterCarrying()`, `EnterFrozen()` — transiciones

## Cambios S69

**Press-E (petting hold):**
```csharp
if (petPressE)
{
    if (petTarget == null)
        TryBeginPetting();  // intenta iniciar
    // else: ya en petting, continúa
}
```

**Release-E (petting stop):**
```csharp
if (petReleaseE)
{
    EndPetting();  // cancela si estaba en sesión
}
```

**TryBeginPetting() core:**
```csharp
private void TryBeginPetting()
{
    var hits = Physics.OverlapSphere(transform.position, interactRadius, interactLayerMask);
    foreach (var collider in hits)
    {
        var agent = collider.GetComponentInParent<MoriMochiAgent>();
        if (agent != null && agent.CanBePetted)
        {
            petTarget = agent;
            agent.BeginPetting();
            return;
        }
    }
}
```

**EndPetting() core:**
```csharp
private void EndPetting()
{
    petTarget?.EndPetting();
    petTarget = null;
}
```

**SetState() cambio:**
```csharp
public void SetState(PlayerState newState)
{
    if (newState != PlayerState.Exploring)
    {
        EndPetting();  // cierra sesión si sales de Exploring
    }
    // ... resto de transición
}
```

**Prioridades de input:**
1. Tap-interact (raycast): IInteractable click
2. Grab (hold LMB): tomar criatura
3. Petting (press/release E): sesión interactiva
4. Movimiento/cámara: siempre activo

## Invariantes S93 (rescatados de comentarios)

- Nunca referencia `PlayerInputs` directamente: solo se suscribe a sus eventos static (mismo patrón que `GameEvents`). Look/aim lo posee Cinemachine; el controller solo lee su forward. Grab/throw pasa por `IThrowable`, así el controller nunca conoce el tipo concreto sujetado.
- `SetState`: Exploring y Building corren en primera persona (cursor lock + cámara viva); solo Menu libera el cursor y congela la cámara. Abrir un menú o entrar a build mode a mitad de un petting termina la sesión de petting (safety net).
- `OnBuildModeChanged`: build mode es el tercer estado del player — movimiento y cámara siguen vivos (se apunta el ghost mirando), grab/throw/jump quedan suspendidos; `BuildModeController` es dueño del ghost y el placement.
- `Move`: el player camina en Exploring y Building; suspendido en Menu.
- **Contrato de la tecla E:** libre + PRESS sobre criatura petteable → petting mientras se mantiene E · libre + TAP → interact (`IInteractable`) · libre + HOLD ≥ `grabHoldDuration` → grab (`IThrowable`) · cargando + PRESS → drop en el lugar · cargando + Click (Attack) → throw. Los agentes MoriMonchi mantienen el grab físico; para todo lo demás, mantener E lanza el ítem activo del hotbar (los props viven en el hotbar, no en un grab físico).
- En un menú el action map Player está deshabilitado (`PlayerInputs` lo gatea por UI focus): ningún grab/interact llega; los paneles se cierran con ESC (`UIInputs` → `UIManager` pop del stack), no con E.
- `ComputeThrowImpulse`: el objeto flota en el hold anchor descentrado; tirar por `camera.forward` desde ahí vuela paralelo y nunca llega al punto apuntado — se apunta desde el anchor HACIA donde mira la cámara para converger en el centro de pantalla.
- `TryBeginPetting`: usa `OverlapSphere` (no raycast) para que los paneles world-space de `NameTag` no bloqueen la detección.
- `TryFindInView<T>`: `QueryTriggerInteraction.Collide` porque los MoriMonchis llevan collider TRIGGER mientras están NavMesh-driven (se vuelven sólidos en vuelo tras el handoff a throwable); recorre los hits por distancia — el primero que resuelve a T gana, un collider SÓLIDO que no es T bloquea (no se agarra a través de paredes), un trigger que no es T se ve a través (paneles, zonas de estación).

## Tap-interact (raycast)

Raycast desde cámara; si hit IInteractable, llama Interact() (caso especial: excluye MoriMochiAgent para evitar petting accidental). En S69, tap-interact y grab son independent; release-LMB no cancela petting.

## Campos Tuning

**Movement:**
- `moveAcceleration` — aplicación de fuerzas al Rigidbody
- `moveSpeed` — velocidad máxima
- `moveFriction` — fricción
- `jumpForce` — impulso de salto
- `groundCheckDistance` — rango raycast para IsGrounded
- `groundLayer` — mask de suelo

**Interaction:**
- `interactRadius` — rango de OverlapSphere para TryBeginPetting (default 2–3m)
- `interactLayerMask` — layers que pueden ser peteados

## Ciclo de Actualización

```csharp
Update():
  ReadInput()  // move, look, interact, S69: press/release E
  CheckForInteraction()
  ApplyMovement()
  ApplyGravity()
  
  if (pressing LMB near creature) → StartCarrying()
  if (released LMB) → StopCarrying()
  
  // S69: petting hold
  if (petPressE) TryBeginPetting()
  if (petReleaseE) EndPetting()
  
  // state dispatch
  switch (CurrentState):
    Exploring  → UpdateExploring()
    Carrying   → UpdateCarrying()
    Frozen     → (nothing)

FixedUpdate():
  UpdateCarrying()  // follow hold anchor
```

## Eventos Suscritos

Ninguno (input directo).

## Notas

- S69: Petting es hold-E, NOT tap. Diferencia clave de tap-interact
- S69: Petting funciona SOLO si `agent.CanBePetted` (Reacting + amistosa + facing)
- S69: SetState limpia petting al salir de Exploring (cargar escena, llevar criatura, etc.)
- Prioridad: tap-interact (click) > grab (hold LMB) > petting (hold E)
- E es clave de interacción limpia (no compete con movimiento)

## Vinculado a

[[Index/06 - Player & World]]

## Conexiones

[[PlayerAnimator]], [[ThrowableObject]], [[MoriMochiAgent]], [[HotbarController]]
