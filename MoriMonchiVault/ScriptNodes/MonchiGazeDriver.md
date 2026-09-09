---
tags: [script, creatures, animation, realismo]
---

# MonchiGazeDriver.cs

**Ruta:** `World/Creatures/MonchiGazeDriver.cs`

**Responsabilidad:** Driver de realismo que rota el ModelRoot de la criatura en los ejes Y (yaw) y X (pitch) para mirar a objetivos dinámicos percibidos. **S109 COMPLETO:** Implementa mirada inteligente contextual: cazador con pitch bajo (huntPitch), escapando con vistazo sobre hombro (glance), custodio con escaneo de abanico (scanYaw), enfrentador con pitch directo (facePitch). Sin mirada si combate, suspensión, golpe o movimiento rápido.

**Campos Serializados (Inspeccionables):**

**Motor:**
- `agent` (MoriMochiAgent, Required) — referencia al agente
- `visualizer` (MonchiVisualizer, Required) — acceso a `visualizer.ModelRoot` para rotación
- `navAgent` (NavMeshAgent, optional) — si existe, valida stillness por velocity
- `combatDriver` (DragonAnimationDriver, optional) — si existe, descarta gaze durante combate

**Rotación y Suavizado:**
- `maxYaw` (float, default 70) — límite angular en grados (±70° = 140° FOV)
- `turnSpeed` (float, default 240) — velocidad interpolación yaw (°/s)
- `pitchSpeed` (float, default 90) — velocidad interpolación pitch (°/s)
- `stillSpeed` (float, default 0.15) — threshold NavMesh velocity para detectar quietud (m/s)

**Percepción de Rivales:**
- `rivalMaxDistance` (float, default 12) — máxima distancia para buscar rival activo (m)

**Modos de Mirada — Cazador:**
- `huntPitch` (float, default 12) — pitch hacia abajo cuando Hunting/Chasing (busca presa)

**Modos de Mirada — Escaper (fuga):**
- `glanceInterval` (float, default 4) — cada cuántos segundos vistazo sobre hombro cuando huye (s)
- `glanceHold` (float, default 0.8) — duración del vistazo (s)

**Modos de Mirada — Custodio (guardián)::**
- `scanYaw` (float, default 55) — amplitud barrido yaw sin rival (±55° = 110° total)
- `scanPeriod` (float, default 6) — período de barrido sinusoidal (s)

**Modos de Mirada — Enfrentador:**
- `facePitch` (float, default 10) — pitch cuando Orders.Contact == Fight (encarar directo)

**Campos Internos (Estado):**
- `currentYaw` (float) — yaw acumulado actual (interpolado)
- `currentPitch` (float) — pitch acumulado actual (interpolado)
- `scanPhase` (float) — offset de fase para barrido (randomizado en OnEnable)
- `nextGlanceAt` (float) — timestamp próximo vistazo (glance)
- `glanceUntil` (float) — timestamp fin vistazo actual

**Métodos Públicos:**

- `OnEnable()` — inicializa fases:
  - `scanPhase = Random.Range(0, scanPeriod)` (stagger entre custodios)
  - `nextGlanceAt = Time.time + Random.Range(0, glanceInterval)` (stagger vistazos)

- `LateUpdate()` — lógica principal:
  1. Determina `bodyFree`: ModelRoot != null, no combatiendo, no suspendido, no airborne, no recuperándose
  2. Determina `still`: navAgent == null O no enabled O no en NavMesh O velocity < stillSpeed
  3. Si bodyFree:
     - Busca `rival = FindRival()` (Monchi más cercano ≤ rivalMaxDistance)
     - Intent: Hunting/Chasing → desiredPitch = huntPitch
     - Contact == Flee + rival → vistazo cada glanceInterval por glanceHold
     - Still + Guarding → escanea con sin(time/scanPeriod)*scanYaw, o clava rival si existe
     - Still + Contact==Fight + rival → encarado con facePitch
     - Else → busca ExpeditionTarget > SocialPartner > primo percept
  4. Interpola yaw/pitch con MoveTowardsAngle/MoveTowards
  5. Aplica a ModelRoot.localRotation

- `OnDisable()` — reset:
  - `currentYaw = 0`, `currentPitch = 0`
  - `ModelRoot.localRotation = identity` (restaura neutro)

**Métodos Privados:**

- `float YawTo(Vector3 worldPos)` — calcula yaw deseado:
  - Diferencia planar (xz) hacia posición
  - Ángulo signed desde forward del agente
  - Clampeado a [-maxYaw, maxYaw]
  - Retorna 0 si muy cerca (< 0.1m)

- `Transform FindRival()` — primer percepto Monchi rival ≤ rivalMaxDistance:
  - Itera `agent.Percepts`
  - Si Kind == Monchi Y SqrDistance ≤ (rivalMaxDistance)² Y son rivales (ExpeditionTeams.AreRivals)
  - Retorna Source.transform o null

- `Transform FindPerceptTarget()` — primer percepto cercano ≤ maxDistance:
  - Itera `agent.Percepts`
  - Si Kind ∈ {Monchi, Player, Material} Y SqrDistance ≤ maxDistance²
  - Retorna Source.transform

**Flujo de Mirada (LateUpdate) S109:**

```
1. ¿Puede mirar? (bodyFree & no combate & no suspensión)
   NO → no desiredYaw/desiredPitch, solo interp actual hacia 0

2. Busca rival cercano (≤ rivalMaxDistance)

3. Por intención/orden:
   a. HUNTING → desiredPitch = huntPitch
   b. FLEEING + rival cercano:
      - Timer vistazos: cada glanceInterval, mira 0.8s al rival
      - Sino: forward normal (huye recto)
   c. QUIET + GUARDING:
      - Si rival: mira al rival
      - Sino: barre ±55° sinusoidalmente cada 6s
   d. QUIET + CONTACT==FIGHT + rival → encarado (facePitch)
   e. QUIET + else → persigue ExpeditionTarget > SocialPartner > percept

4. Interpola suavemente actual → desired (turnSpeed, pitchSpeed)

5. Aplica ModelRoot.localRotation = Euler(pitch, yaw, 0)
```

**Invariantes S109:**

- **Contextual:** mirada cambia radicalmente por intención (Hunting ≠ Fleeing ≠ Guarding)
- **Suave:** sin snaps de rotación; interpolación angular evita twitching
- **Quieto = activo:** solo se gira si está quieto (navMesh velocity < 0.15), excepto hunting (siempre pitch)
- **Rival prioridad alta:** si hay rival activo, casi toda lógica lo considera (Fleeing glance, Guarding lock, Fight face)
- **Falla gracefully:** si visualizer == null O combatDriver == null, descarta gaze
- **Fase randomizada:** scanPhase y glanceInterval staggereados para evitar sincronización visual grupal
- **No interfiere combate:** si combatDriver.IsBusy, desactiva gaze por completo
- **Reset limpios:** OnDisable restaura ModelRoot a identidad

**Integración:**

- Agregado como componente en prefab del agente
- Refs serializadas + configurables por SO (TuningSO) o por criatura
- Funciona en arena sandbox, expedición, tienda (lee Intent, Orders, Percepts)
- Indépendente de animación (MonchiAnimationDriver) — solo rota modelo

**S99 Cambios Previos:**
- Se agregó gaze básico (mira ExpeditionTarget > SocialPartner > primero cercano)

**S108 Cambios Previos:**
- Se integró con DragonAnimationDriver combat check

**S109 Cambios (COMPLETA REESCRITURA):**

- Campos nuevos: rivalMaxDistance, scanYaw, scanPeriod, glanceInterval, glanceHold, huntPitch, facePitch, pitchSpeed
- FindRival() nuevo: busca primer Monchi rival ≤ rivalMaxDistance
- YawTo() nuevo: calcula ángulo signed clampeado
- Lógica contextual: Hunting → pitch bajo; Fleeing + rival → glances periódicos; Guarding → escaneo abanico; Fight → encarado; else → sigue target
- OnEnable/OnDisable: inicializa/resetea fases
- Realismo: mirada refleja emociones y estrategia según intención

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMochiAgent]], [[MonchiVisualizer]], [[AgentSenses]], [[AgentExpedition]], [[AgentSocial]], [[DragonAnimationDriver]], [[CreatureIntent]]
