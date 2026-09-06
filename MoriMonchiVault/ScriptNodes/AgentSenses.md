---
tags: [script, world, agent, internal, perception]
---

# AgentSenses.cs

**Ruta:** `World/AI/AgentSenses.cs`

**Responsabilidad:** Colaborador interno de percepción de la composición del agente. Ejecuta escaneo throttled de Perceivables cercanas y escribe resultado en ctx.Percepts (lista ordenada, acotada). **S65:** afinidad social dinámica. **S99:** Team propagado de Perceivable. **S102 NUEVO:** filtro por cono de visión (si ExpeditionRulesSO.Current != null, solo los que pasen VisionProfile.CanSense). Nunca decide ni muta estado — AgentBrain lee ctx.Percepts y actúa.

## Campos Internos

- `nextScanAt` — tiempo del próximo escaneo (throttling estocástico)
- `primed` — primer escaneo completado
- `buffer` — List<Perceivable> temporal para QueryInRadius
- `selfPerceivable` — Perceivable del propietario (auto-exclusión)
- `selfPerceivableResolved` — lazy init flag

## Métodos

- `Tick() → void` — escaneo throttled:
  1. **Throttling:** si Time.time < nextScanAt, retorna
  2. **Primer tick:** si !primed, setea nextScanAt + retorna (stagger)
  3. **Validación:** si !NavMeshControlled || !DNA, limpia Percepts + retorna
  4. **Query:** PerceivableRegistry.QueryInRadius() en PerceptionRadius
  5. **Filtro S102 NUEVO:** si ExpeditionRulesSO.Current:
     - Resolve(DNA, rules) → radio, degrees, nearRadius
     - CanSense(forward, position, target, radio, degrees, nearRadius) por cada percepto
     - Solo agrega si CanSense() = true (pasa cono o audición)
  6. **Afinidad S65:** SocialGraphService.EffectiveAffinity (Monchi) o 0
  7. **Team S99:** copia Perceivable.Team a Percept
  8. **Ordenar:** por SqrDistance ascendiente
  9. **Capeo:** Max(MaxPercepts)

- `ResetForReuse() → void` — pooling cleanup

## Flujo de Percepción S102

```csharp
1. Throttling (nextScanAt)
2. Validación (NavMesh, DNA)
3. QueryInRadius(PerceptionRadius)
4. Para cada Perceivable:
   - Auto-exclusión
   - **S102 NUEVO:** si ExpeditionRulesSO.Current:
     - VisionProfile.Resolve(DNA, rules, out radius, out degrees, out nearRadius)
     - if !VisionProfile.CanSense(forward, position, target, radius, degrees, nearRadius)
       continue (skip this percept)
   - Afinidad (Monchi)
   - Team copy
   - Agregar Percept
5. Sort + Capeo
```

## Struct Percept S102

```csharp
new Percept
{
    Source      = p,                      // Perceivable
    Kind        = p.Kind,                 // PerceivableKind
    SqrDistance = (p.Position - ctx.Body.position).sqrMagnitude,
    Affinity    = SocialGraphService.EffectiveAffinity(...)  // S65
    Team        = p.Team,                 // S99, S102 filtrado por cono
}
```

## Invariantes S102 + S65 + S99

- **Cono de visión condicional:** solo si ExpeditionRulesSO.Current != null (null = tienda, no hay cono)
- **Audición aparte:** NearSenseRadius ignora conos (toque ciego)
- **Osadía skew:** VisionProfile aplica boldness a radio/ángulo
- **Afinidad dinámica:** recalculada cada scan (no cacheada)
- **Team propagado:** desde Perceivable (setup en ArenaSandbox)
- **Ordenada por distancia:** siempre, cono o no
- **Throttling:** evita N-squared load (todos los frames)

## Conexiones

- [[VisionProfile]] — Resolve + CanSense (S102 nuevo)
- [[ExpeditionRulesSO]] — Current != null chequeo (S102)
- [[PerceivableRegistry]] — QueryInRadius
- [[SocialGraphService]] — EffectiveAffinity
- [[AgentBrain]] — lector de ctx.Percepts
- [[SocialTuningSO]] — PerceptionRadius, ScanInterval

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
