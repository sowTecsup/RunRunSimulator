---
tags: [script, creatures, animation, realismo]
---

# MonchiGazeDriver.cs

**Ruta:** `World/Creatures/MonchiGazeDriver.cs`

**Responsabilidad:** Driver de realismo que rota el ModelRoot de la criatura en el eje Y para mirar a objetivos percibidos. Prioridad: ExpeditionTarget (si existe) → SocialPartner → primer percept cercano (Material/Monchi/Player). Descartado si está en combate (`combatDriver.IsBusy`), held, airborne, recovering, o moviéndose rápido.

## Propiedades

**Serializado (Inspector):**
- `agent` (MoriMochiAgent) — referencia requerida al agente
- `visualizer` (MonchiVisualizer) — referencia requerida; accede a `ModelRoot` para rotación Y
- `navAgent` (NavMeshAgent) — opcional; si existe, chequea `isOnNavMesh` y `velocity.magnitude` para determinar si está "quieto"
- `combatDriver` (DragonAnimationDriver) — opcional; si existe, descarta gaze si está en combate
- `maxYaw` (float, default 70) — ángulo máximo de giro en grados (±70° = 140° total de vista)
- `turnSpeed` (float, default 240) — velocidad de interpolación angular (°/s)
- `stillSpeed` (float, default 0.15) — threshold de velocidad NavMesh para considerarse "quieto" (m/s)
- `maxDistance` (float, default 8) — distancia máxima para mirar percepts (m)

**Internos:**
- `currentYaw` (float) — yaw acumulado actual (interpolado suavemente hacia desired)

## Lógica (LateUpdate)

```
1. Determinar si puede mirar (canGaze):
   - ModelRoot != null
   - (combatDriver == null || !IsBusy)
   - !IsHeld && !IsAirborne && !IsRecovering
   - (navAgent == null || !enabled || !isOnNavMesh || velocity.magnitude < stillSpeed)

2. Si canGaze, elegir target (en orden):
   a. agent.ExpeditionTarget (si existe)
   b. agent.SocialPartner (si existe)
   c. FindPerceptTarget() — busca Monchi/Player/Material más cercano <= maxDistance

3. Si target existe:
   - Calcular dirección planar (xz) hacia target
   - Calcular SignedAngle(transform.forward, to, Vector3.up) como desired yaw
   - Clampear a [-maxYaw, maxYaw]

4. Interpolar currentYaw hacia desired usando MoveTowardsAngle(turnSpeed * dt)

5. Aplicar currentYaw a visualizer.ModelRoot.localRotation = Euler(0, yaw, 0)
```

## FindPerceptTarget()

Itera `agent.Percepts` (acumula `AgentSenses`) e retorna el primero que cumpla:
- `Source != null`
- `SqrDistance <= maxDistance²`
- `Kind ∈ {Monchi, Player, Material}`

Devuelve null si no hay percept válido.

## OnDisable

Resetea `currentYaw = 0` y restaura `ModelRoot.localRotation = identity` para evitar desincronización visual.

## Invariantes S98

- **No interfiere con combate:** si `combatDriver.IsBusy`, descarta gaze (combate tiene su propio look).
- **Suave y reactivo:** interpolación angular evita snaps; `turnSpeed` modula fluidez.
- **No interfiere con movimiento:** solo rota head (ModelRoot), no afecta NavMesh o transform principal.
- **Prioritario:** ExpeditionTarget > SocialPartner > percepción. Refleja atención del agente.
- **Falla gracefully:** si `visualizer` o componentes no existen, `canGaze = false` y descarta.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]] (Parte 8: "Gestos y miradas")

## Conexiones

[[MoriMochiAgent]], [[MonchiVisualizer]], [[AgentSenses]], [[AgentExpedition]], [[AgentSocial]], [[DragonAnimationDriver]]
