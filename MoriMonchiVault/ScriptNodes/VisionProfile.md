---
tags: [script, world, agent, perception, static-utility]
---

# VisionProfile.cs

**Ruta:** `World/AI/VisionProfile.cs`

**Responsabilidad:** Utilidad estática para resolver los parámetros de visión de un agente basados en su DNA y las reglas de expedición. Centraliza la lógica de cálculo de cono de visión (radio/ángulo/audición) con skew por osadía, y prueba si un objetivo está dentro del cono.

## Métodos Estáticos

- `Resolve(CreatureDNA dna, ExpeditionRulesSO rules, out float radius, out float degrees, out float nearRadius) → void` — calcula los tres parámetros de percepción:
  - **boldness:** clamp01(dna.Boldness) o 0.5 si dna nulo
  - **skew:** rules.BoldnessVisionSkew * (boldness - 0.5) * 2 (rango [-0.5, 0.5])
  - **radius:** rules.VisionRadius * (1 + skew) — osados ven más lejos
  - **degrees:** clamp(rules.VisionDegrees * (1 - skew), 30°, 360°) — osados ven más estrecho (focus)
  - **nearRadius:** rules.NearSenseRadius (audición, ignorar conos)

- `CanSense(Vector3 forward, Vector3 from, Vector3 target, float radius, float degrees, float nearRadius) → bool` — comprueba si `target` está dentro del cono o audición:
  1. Calcula `dir = target - from` (solo XZ)
  2. Si sqrDist ≤ nearRadius² → true (audición ciega)
  3. Si sqrDist > radius² → false (fuera de rango)
  4. Si degrees ≥ 360° → true (visión omnidireccional)
  5. Si forward casi nulo → true (fallback seguro)
  6. Retorna `Vector3.Angle(forward, dir) ≤ degrees * 0.5f` (ángulo mitad en cada lado)

- `FacingAngle(Vector3 forward) → float` — calcula atan2(forward.z, forward.x) para ángulo visual (ignorar Y). Retorna 0 si forward casi nulo.

## Invariantes S102

- **Skew suavizado:** la osadía escala radio y ángulo opuestamente (osados = visión de túnel, tímidos = visión amplia pero cercana)
- **Audición zona muerta:** nearRadius ignora cualquier cono, permite percepción táctil
- **Ignore Y:** todas las pruebas en plano XZ (terreno 2D)
- **Ángulo simétrico:** degrees/2 a cada lado del forward
- **Fallback nulo:** si forward casi nulo, asumir que puede sentir (evitar deadlock)

## Conexiones

- [[ExpeditionRulesSO]] (VisionRadius, VisionDegrees, NearSenseRadius, BoldnessVisionSkew)
- [[AgentSenses]] (llama Resolve + CanSense en throttled Tick)
- [[MoriMochiAgent]] (fachada HasVisionCone, VisionRadius, VisionDegrees, NearSenseRadius)
- [[ArenaCueOverlay]] (DrawVisionCone visualiza el cono)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]] (sección visión)
