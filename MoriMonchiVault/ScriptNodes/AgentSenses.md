---
tags: [script, world, agent, internal, perception]
---

# AgentSenses.cs

**Ruta:** `World/AI/AgentSenses.cs`

**Responsabilidad:** Colaborador interno de percepción de la composición del agente (espejo de AgentBrain). Ejecuta un escaneo estrangulado/escalonado de Perceivables cercanas y escribe el resultado en el pizarrón compartido (ctx.Percepts) como lista ordenada y acotada. Nunca decide nada ni muta estado — AgentBrain o un future social brain lee ctx.Percepts y actúa. **S65:** Ahora calcula afinidad social dinámica vía `SocialGraphService.EffectiveAffinity()` que combina seed (SocialAffinity.Compute) + delta (historial de SocialGraph), reemplazando el cálculo estático de S64. Tickeado por MoriMochiAgent.Update.

## Campos internos

- `nextScanAt` — tiempo del próximo escaneo (throttling estocástico)
- `primed` — primer escaneo completado
- `buffer` — List<Perceivable> temporal para QueryInRadius (reutilizada, evita alloc)
- `selfPerceivable` — ref cacheada a la Perceivable del propietario (para auto-exclusión)
- `selfPerceivableResolved` — bandera de inicialización lazy

## Métodos

- `Tick() → void` — escaneo throttled: consulta PerceivableRegistry.QueryInRadius en el radio, computa afinidad **S65 NUEVA** con SocialGraphService.EffectiveAffinity() para Monchis, ordena por distancia, capea a MaxPercepts. Limpia ctx.Percepts si el agente no está NavMesh-controlado o sin DNA.
- `ResetForReuse() → void` — pooling: restaura estado inicial

## Flujo de Perception (Tick)

1. **Throttling:** Si `Time.time < nextScanAt`, retorna sin escanear (ahorra perf)
2. **Primer tick:** Si no primed, setea nextScanAt a random delay y retorna (stagger)
3. **Validación:** Si no NavMeshControlled o DNA null, limpia Percepts y retorna
4. **Query:** `PerceivableRegistry.QueryInRadius()` obtiene todos los Perceivables en PerceptionRadius
5. **Afinidad:** **S65 NUEVO** Para cada Percept de tipo Monchi, calcula `SocialGraphService.EffectiveAffinity(ctx.Dna, other.Dna, tuning)` que suma seed + delta de historia
6. **Llenar contexto:** Construye Percept con Kind, SqrDistance, Affinity
7. **Ordenar:** Sort por SqrDistance (cercano primero)
8. **Capeo:** Si Count > MaxPercepts, RemoveRange (preserva los más cercanos)

## Cambio S65: Afinidad Dinámica

**S64 (pre-S65):** Afinidad = `SocialAffinity.Compute(a, b, tuning)` — seed estática basada en Element, Kinship, Chemistry, RoleBias.

**S65:** Afinidad = `SocialGraphService.EffectiveAffinity(a, b, tuning)` que:
- Llama `SocialAffinity.Compute()` para seed
- Suma delta acumulado del SocialGraph si existe par en diccionario
- Clampea a [−1, 1]

```csharp
float affinity = SocialGraphService.EffectiveAffinity(ctx.Dna, p.Monchi.DNA, t);
```

**Impacto:** Historias de abalanzadas (+0.06), siestas (+0.08), peleas (−0.1) ahora modifican dinámicamente la afinidad percibida cada tick. Inversión anterior en PlayChase/SleepTogether/Fight paga dividendos en Score de las reglas de reacción.

## Vinculado a

- [[Index/06 - Player & World]]
- [[MoriMonchiVault/Index/14 - Social V2]]

## Conexiones

**Entrada:**
- `SocialTuningSO.Current` — para ScanIntervalMin/Max, PerceptionRadius, MaxPercepts
- `ctx.Dna` — DNA del agente (self)
- `PerceivableRegistry` — registry global de entidades perceptibles

**Salida:**
- `ctx.Percepts` — lista de Percept poblada/ordenada
- `SocialGraphService` — consulta (no mutador) via EffectiveAffinity
- `AgentSocial.TryEngage()` — lee ctx.Percepts para puntuar reglas

**Consumido por:**
- `AgentSocial.TryEngage()` — itera Percepts, puntúa reglas, elige mejor
- `AgentBrain.Tick()` — puede leer Percepts si futura lógica lo necesita
