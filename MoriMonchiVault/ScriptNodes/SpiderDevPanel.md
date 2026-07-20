---
tags: [script, prototype, tooling]
---

# SpiderDevPanel.cs

**Ruta:** `Prototype/Spider/SpiderDevPanel.cs`

**Responsabilidad:** Panel IMGUI de desarrollo (izquierda). Expone sliders para editar todos los knobs de `SpiderTuningSO` en runtime, organizados por secciones: Cuerpo, Patas, Cuerpo vivo (con Elasticidad y Salto), Ragdoll. Botones: toggle `AutoWalk`, "Saltar!" (llama `jump.Jump()`), switch ragdoll mode, "Lanzar!" (aplica impulso + ragdoll), "Reset" (vuelve a spawn). Guarda spawn point y pose al start. Lee/escribe directamente los fields del SO. Escala automáticamente con el tamaño de pantalla vía `GUI.matrix = Scale(max(1, Screen.height/1080))` para mantener legibilidad. NO toca GameEvents ni sistemas del juego real. **NUEVO (S52):** Sección "Acciones (driver)" con botones que disparan métodos del `SpiderAnimationDriver` (Atacar, Buff aliado, Recibir golpe, Victoria, Derrota, Idle, Ir al spawn). Usa refs serializadas `driver`, `palette` y `attackTarget` para targeteo. Botón "Colores random (60/30/10)" usa `SpiderPaletteApplier.Apply()` con ColorGenetics random. Status label muestra si driver está ocupado.

**Notas de prototipo:** Tooling puro de dev. Usa `EditorUtility.SetDirty()` para marcar cambios en el SO. Labels en español neutral. No es UI final. Regla nueva de Juan (S50): GUI.matrix scaling dinámico por altura de pantalla.

**Cambios S50:** Se agregó scaling automático vía GUI.matrix (escala por `Screen.height/1080`); se agregó ref serializada `SpiderJump jump`; se agregaron sliders "Elasticidad" y "Salto" en sección Cuerpo vivo; se agregó botón "Saltar!" que dispara `jump.Jump()`.

**Cambios S52:** Se agregaron refs serializadas `driver` (SpiderAnimationDriver), `palette` (SpiderPaletteApplier), `attackTarget` (Transform opcional para targeteo). Se agregó nueva sección "Acciones (driver)" con botones: Atacar (PlayAttack a attackTarget o frente), Buff aliado (MoveTo → PlayBuff), Recibir golpe, Victoria, Derrota, Idle, Ir al spawn. Se agregó botón "Colores random (60/30/10)" que invoca `palette.Apply()` con random base + derived secondary. Se agregó status label que muestra "driver: BUSY" o "driver: idle".

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `tuning` | `SpiderTuningSO` | SO parámetros de locomotion |
| `ragdollMode` | `SpiderRagdollMode` | Check state + SetRagdoll |
| `controller` | `SpiderBodyController` | AutoWalk toggle |
| `jump` | `SpiderJump` | Jump button (S50) |
| `rootBody` | `Rigidbody` | Physics impulso Launch |
| `spawnPoint` | `Transform` | Spawn position/rotation (opcional) |
| `driver` | `SpiderAnimationDriver` | **S52 NEW** Ref a driver animación |
| `attackTarget` | `Transform` | **S52 NEW** Target opcional para ataques |
| `palette` | `SpiderPaletteApplier` | **S52 NEW** Ref a aplicador colores |
| `panelWidth` | float | 290 | Ancho panel pixels |
| `show` | bool | true | Toggle visibility panel |

## Almacenamiento Interno

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `spawnPos` | Vector3 | Posición spawn (guardada Awake) |
| `spawnRot` | Quaternion | Rotación spawn (guardada Awake) |
| `scroll` | Vector2 | Scroll view offset |

## Secciones GUI

### Cuerpo
- Velocidad (moveSpeed) [0.1, 4]
- Giro (turnSpeed) [30, 270]
- Altura (rideHeight) [0.2, 1.1]

### Patas
- Largo de paso (stepDistance) [0.05, 0.6]
- Alto de paso (stepHeight) [0.02, 0.4]
- Duración (stepDuration) [0.04, 0.5]
- Apertura (footSplay) [0.3, 2]
- Anticipacion (stepOvershoot) [0, 0.3]
- Prediccion (anticipation) [0, 0.5]
- Torsion max (maxTwist) [5, 60]

### Cuerpo vivo
- Idle (idleAmount) [0, 1]
- Bob caminar (bobAmount) [0, 0.06]
- Inclinacion (leanAmount) [0, 1]
- Elasticidad (elasticAmount) [0, 1] — S50
- Salto (jumpImpulse) [1, 6] — S50

### Ragdoll
- Impulso (launchImpulse) [1, 12]

### Control (Botones)
- Status RAGDOLL/WALK (color-coded)
- Auto-caminar ON/OFF
- Saltar! (S50)
- Volver a caminar / Activar ragdoll
- Lanzar! (ragdoll + impulso forward)
- Reset (vuelve a spawn)

### Acciones (driver) — **S52 NEW**
- Atacar → PlayAttack(attackTarget.position o frente)
- Buff aliado → MoveTo(attackTarget) → PlayBuff
- Recibir golpe → PlayHit(1)
- Victoria → PlayVictory
- Derrota → PlayDefeat
- Idle → PlayIdle
- Ir al spawn → MoveTo(spawnPos)
- Colores random (60/30/10) → palette.Apply(randomBase, derivedSecondary)
- Status label: "driver: BUSY" o "driver: idle"

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `OnGUI()` | Renderiza panel IMGUI completo |
| `Row(label, value, min, max)` | Helper: retorna nuevo valor slider |
| `Launch()` | Aplica ragdoll + impulso forward |
| `ResetSpider()` | Vuelve a spawn, limpia velocidad |

## Notas

- **Deuda técnica:** Tooling puro de prototipo araña descartado. Eliminar cuando se implemente driver definitivo.
- **EditorUtility.SetDirty():** Marca SO modificado para guardado en editor; solo activo en #if UNITY_EDITOR.
- **Null-safety:** Chequea all refs antes de usarlas; si null, botón no hace nada.
- **GUI.matrix scaling:** Mantiene legibilidad en diferentes resoluciones de pantalla (ej: 1440p vs 1080p).
- **Colores (S52):** Botón "Colores random" genera base random vía ColorGenetics.RandomBase(), deriva secondary vía ColorGenetics.DeriveSecondary(), invoca palette.Apply() (regla 60/30/10).

## Vinculado a

- [[Index/06 - Player & World]] — prototipo
- Prototype/Spider

## Conexiones

**Usa:**
- [[SpiderTuningSO]] → todos los sliders
- [[SpiderRagdollMode]] → toggle ragdoll
- [[SpiderBodyController]] → AutoWalk property
- [[SpiderJump]] → Jump button (S50)
- [[SpiderAnimationDriver]] → Acciones driver (S52)
- [[SpiderPaletteApplier]] → Colores button (S52)
- [[ColorGenetics]] → RandomBase, DeriveSecondary (S52)

**Usado por:**
- Ninguno (tooling aislado)
