---
tags: [script, world, ai, expedition, internal]
---

# ClashStrike.cs

**Ruta:** `World/AI/ClashStrike.cs`

**Responsabilidad:** Ejecutor físico de golpe/movimiento. Encapsula maquinaria de embestida (Horn), despegue+caída (Wings), barrida (Back). Maneja impacto sobre rivales, timing, knockback, cadenas. **S116 NUEVO:** Reemplaza lógica inline de AgentClash.Tick(). **S122:** Dispara `owner.onDiveLaunch` en Begin (Wings), `owner.onDiveSlam` en Land (Wings).

**Métodos Internos:**
- `void Begin(ClashMoveSO clashMove, MoriMochiAgent clashTarget, Vector3 forward, Vector3 point, float strikeSeconds)` — inicia golpe
  - **S122:** Si Wings: dispara `owner.onDiveLaunch` (para VFX despegue)
  
- `bool Tick(float dt)` — avanza golpe cada frame (retorna true si terminó)
  - Horn: Agent.Move + Rb.position sync, impactos, stuckFrames
  - Back: zonal si timer=0
  
- `bool TickAirborne()` — detecta ápice + impacto (Wings)
  
- `void Land()` — impacto zonal en suelo
  - **S122:** Si Wings: dispara `owner.onDiveSlam` (para VFX impacto)
  - QueryInRadius zonal, calcula knockback, dispara onClashHit

**S116:** Delegación de AgentClash.
**S122:** onDiveLaunch en Begin (Wings) + onDiveSlam en Land (Wings).

**Conexiones:** [[AgentClash]], [[MoriMochiAgent]], [[MMFeedbacks]]
