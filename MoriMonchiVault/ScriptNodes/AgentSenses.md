---
tags: [script, world, agent, internal, perception]
---

# AgentSenses.cs

**Ruta:** `World/AI/AgentSenses.cs`

**Responsabilidad:** Colaborador interno de percepción de la composición del agente (espejo de AgentBrain). Ejecuta un escaneo estrangulado/escalonado de Perceivables cercanas y escribe el resultado en el pizarrón compartido (ctx.Percepts) como lista ordenada y acotada. Nunca decide nada ni muta estado — AgentBrain o un future social brain lee ctx.Percepts y actúa. **S65:** Ahora calcula afinidad social dinámica vía `SocialGraphService.EffectiveAffinity()` que combina seed (SocialAffinity.Compute) + delta (historial de SocialGraph), reemplazando el cálculo estático de S64. **S99:** Popula campo `Team` en Percept desde `Perceivable.Team`. Tickeado por MoriMochiAgent.Update.

## Campos internos

- `nextScanAt` — tiempo del próximo escaneo (throttling estocástico)
- `primed` — primer escaneo completado
- `buffer` — List<Perceivable> temporal para QueryInRadius (reutilizada, evita alloc)
- `selfPerceivable` — ref cacheada a la Perceivable del propietario (para auto-exclusión)
- `selfPerceivableResolved` — bandera de inicialización lazy

## Métodos

- `Tick() → void` — escaneo throttled: consulta PerceivableRegistry.QueryInRadius en el radio, computa afinidad **S65 NUEVA** con SocialGraphService.EffectiveAffinity() para Monchis, **S99:** copia Team desde Perceivable.Team a Percept, ordena por distancia, capea a MaxPercepts. Limpia ctx.Percepts si el agente no está NavMesh-controlado o sin DNA.
- `ResetForReuse() → void` — pooling: restaura estado inicial

## Flujo de Perception (Tick) S99

```csharp
1. **Throttling:** Si Time.time < nextScanAt, retorna sin escanear (ahorra perf)
2. **Primer tick:** Si no primed, setea nextScanAt a random delay y retorna (stagger)
3. **Validación:** Si no NavMeshControlled o DNA null, limpia Percepts y retorna
4. **Query:** PerceivableRegistry.QueryInRadius() obtiene todos los Perceivables en PerceptionRadius
5. **Para cada Perceivable:**
   - Auto-exclusión: si es el propietario mismo, skip
   - Afinidad: Si Kind=Monchi, calcula SocialGraphService.EffectiveAffinity(ctx.Dna, other.Dna, tuning)
     Sino (Player/Customer/Prop/Material), affinity = 0
   - **Team S99:** Copia p.Team a Percept.Team (propagado desde Perceivable)
   - Crear Percept con Source, Kind, SqrDistance, Affinity, **Team**
   - Agregar a ctx.Percepts
6. **Ordenar:** Sort(ctx.Percepts) por SqrDistance ascendiente (más cercano primero)
7. **Capeo:** Si Count > t.MaxPercepts, truncar (mantener los más cercanos)
```

## Struct Percept poblado (S99)

```csharp
new Percept
{
    Source      = p,                                      // Perceivable del objeto
    Kind        = p.Kind,                                  // PerceivableKind
    SqrDistance = (p.Position - ctx.Body.position).sqrMagnitude,
    Affinity    = (Monchi) ? SocialGraphService.EffectiveAffinity(...) : 0f,
    Team        = p.Team,  // S99 NUEVO: ExpeditionTeam (None/Player/Rival)
}
```

## Invariantes S99

- **Team propagación:** cada Perceivable tiene un Team; AgentSenses lo copia al Percept. Usado en `AgentExpedition.ApproachPoint()` para evitar competencia directa con rivales.
- **No cachear:** Percepts se recalculan en cada scan (no reutilizar entre frames); es un snapshot.
- **Throttling estocástico:** evita que todos los agentes scaneen al mismo tiempo; `ScanIntervalMin/Max` de `SocialTuningSO`.
- **Afinidad dinámica S65:** cada scan recalcula afinidad desde el grafo social vigente, no cachea.
- **Ordena por distancia:** siempre, para que las decisiones de comportamiento tengan preferencia al más cercano.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

- [[MoriMochiAgent]] (owner, tickeado por Update)
- [[AgentContext]] (escribe en `ctx.Percepts`)
- [[PerceivableRegistry]] (QueryInRadius: obtiene perceivables cercanas)
- [[Perceivable]] (lee Kind, Team **S99**)
- [[SocialGraphService]] (calcula afinidad dinámica **S65**)
- [[SocialTuningSO]] (PerceptionRadius, ScanInterval, MaxPercepts)
- [[AgentBrain]], [[AgentSocial]], [[AgentExpedition]] (lectores de ctx.Percepts)
