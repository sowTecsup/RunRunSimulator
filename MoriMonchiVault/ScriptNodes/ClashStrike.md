---
tags: [script, world, ai, expedition, internal]
---

# ClashStrike.cs

**Ruta:** `World/AI/ClashStrike.cs`

**Responsabilidad:** Ejecutor físico de golpe/movimiento. Encapsula maquinaria de embestida (Horn: Agent.Move + sincronización Rb.position), despegue+caída (Wings: velocidad balística fija), barrida (Back: área zonal). Maneja impacto sobre rivales, recolecciones múltiples por golpe, timing de conexión, knockback, cadenas. **S116 NUEVO:** reemplaza lógica inline de AgentClash.Tick(), exponiendo HitAt/HitPoint para telegrafía visual.

**Métodos Internos:**

- `ClashStrike(MoriMochiAgent owner, AgentContext ctx)` — constructor; inicializa campos privados

- `void Begin(ClashMoveSO clashMove, MoriMochiAgent clashTarget, Vector3 forward, Vector3 point, float strikeSeconds)` — inicia golpe:
  - Horn: bloquea avoidance, detiene agent, fija forward, dashStart = posición actual, stuckFrames = 0
  - Wings: calcula velocidad balística `v = (d - 0.5*g*T²) / T` para caer en DiveSeconds
  - Back: detiene movimento (ocupación defensiva)

- `bool Tick(float dt)` — avanza golpe cada frame (retorna true si terminó):
  - Horn: Agent.Move + sincronización Rb.position, QueryInRadius para impactos, cuenta stuckFrames, retorna true si traveled >= Range O phaseTimer <= 0 O stuck >= 2
  - Back: QueryInRadius zonal si phaseTimer <= 0, retorna Sweep()
  - Chequea target.IsHeld (aborta si se carga)

- `bool TickAirborne()` — detecta ápice + impacto en vuelo (Wings):
  - `landed`: Rb.velocity.y <= 0 y altura <= impactPoint.y + 0.4
  - `arrived`: distancia planar <= 0.5 y altura <= impactPoint.y + 0.8
  - Retorna true si landed O arrived, llama Land()

- `void Cancel()` — aborta sin cambiar cooldown; restaura damping

- `void ResetForReuse()` — pooling; limpia todo (move=null, diving=false, hitAt=-1, struckThisStrike.Clear)

**Fachadas Públicas (Readonly):**
- `float HitAt { get; }` — timestamp del último impacto exitoso (-1 si nunca)
- `Vector3 HitPoint { get; }` — posición rival donde se conectó el golpe
- `int HitsLanded { get; }` — conteo de impactos exitosos en este golpe
- `bool Diving { get; }` — si en dive animation (Wings)
- `Vector3 ImpactPoint { get; }` — punto de impacto objetivo

**Propiedades Internas:**
- `move` — movimiento actual (ClashMoveSO)
- `target` — rival objetivo
- `lockedForward` — dirección bloqueada al inicio
- `impactPoint` — punto de impacto calculado
- `phaseTimer` — timer de ejecución
- `diving` — si en vuelo (Wings)
- `dashStart` — posición inicial del dash (Horn)
- `stuckFrames` — contador de frames sin avance
- `hitsLanded` — contador de impactos exitosos
- `hitAt`, `hitPoint` — timestamp y posición del último impacto
- `avoidanceOverridden`, `savedAvoidance` — estado de NavMesh (Horn)
- `struckThisStrike` — HashSet de rivales ya golpeados (evita múltiples impactos)

**Métodos Privados:**

- `void Land()` — impacto de picada (Wings):
  - QueryInRadius en impactPoint con HitRadius
  - Impacta cada rival rival en rango (zonal)
  - Restaura damping, aplica velocidad descendente

- `bool Sweep()` — barrida defensiva (Back):
  - QueryInRadius con SweepRadius desde posición actual
  - Retorna true si algún impacto exitoso

- `void Impact(MoriMochiAgent victim)` — procesa impacto sobre una víctima:
  - Horn: dirección = mix(locked, hacia-víctima, 60/40)
  - Wings/Back: dirección = hacia-víctima normalizada
  - Impulso = `(dir + up*UpBias).normalized * Impulse`
  - Llama `victim.ReceiveClashHit()`, `owner.onClashHit?.Invoke()`
  - Horn con SelfRecoil: llama `owner.RequestPlayfulKnock()`
  - Actualiza HitAt, HitPoint, NotifyCharge(Hit)

- `void OverrideNav()` — bloquea obstacle avoidance para embestida directa
- `void RestoreNav()` — restaura avoidance
- `float PlanarDistance(Vector3 a, Vector3 b)` — distancia 2D ignorando Y

**Integración:**

- Instanciado en AgentClash.Awake()
- Begin() llamado desde AgentClash.StartStrike()
- Tick() llamado desde AgentClash.Tick() en fase Striking
- TickAirborne() llamado desde AgentClash.TickAirborne() en fase Wings
- Cancel() / ResetForReuse() llamados desde AgentClash sí

- HitAt/HitPoint leídos por ArenaCueOverlay para activar ImpactRing visual

**Invariantes S116:**

- Solo una víctima por frame puede ser golpeada (QueryInRadius retorna lista)
- struckThisStrike previene múltiples impactos a mismo rival en mismo golpe
- Damping = 0 en vuelo Wings, restaurado en Land
- impactPoint es fijo para Back (posición atacante), dinámico para Horn (pies + dir*range), calculado para Wings

**Vinculado a:** [[Index/22 - Bajada Nocturna y Linaje]], [[Index/23 - Arena Sandbox & Expedicion]], S116

**Conexiones:** [[AgentClash]], [[MoriMochiAgent]], [[AgentContext]], [[ClashMoveSO]], [[PerceivableRegistry]]
