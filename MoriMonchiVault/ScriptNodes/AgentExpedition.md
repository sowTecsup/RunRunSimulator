---
tags: [script, world, ai, agent, internal, expedition]
---

# AgentExpedition.cs

**Ruta:** `World/AI/AgentExpedition.cs`

**Responsabilidad:** Colaborador interno que orquesta ocupaciones de tiempo en arena: Gather (material → notice → navigate → mine → return → deposit), Guard (vigilar mineral), Break (perseguir y golpear rival), Decoy (provocar y huir). Máquina de estados con fases (Noticing → Moving → Mining → Returning → Securing para Gather; Guarding para Guard; Hunting para Break; Decoying para Decoy). **S102 NUEVO:** ApproachPoint solo cuenta como ocupantes del borde a criaturas con intención Collecting/Taking (mineras); vigías, rompedores y señuelos plantados en el puesto ya no empujan a las mineras a girar. **S102 NUEVO:** BeginReturn sin HomeExit suelta la carga con emote Feliz, no intenta regresar.

## Métodos Internos

- `TryEngage() → bool` — entry point. Chequea ExpeditionRulesSO.Current. Según Occupation (None/Explore → Gather): llama TryGatherEngage, TryGuardEngage, TryBreakEngage o TryDecoyEngage. Devuelve true si engancha.

**Sub-métodos por ocupación:**
- `TryGatherEngage(ExpeditionRulesSO rules) → bool` — itera Percepts × Rules, elige mejor score. Entra Noticing → Moving → Mining → Returning → Securing.
- `TryGuardEngage(ExpeditionRulesSO rules) → bool` — busca GuardPost inyectado o MaterialPickup mejor. Entra Guarding (estático, vigila).
- `TryBreakEngage(ExpeditionRulesSO rules) → bool` — busca rival que recolecta/carga (Intent Collecting/Taking). Si lo encuentra, entra Hunting y persigue.
- `TryDecoyEngage(ExpeditionRulesSO rules) → bool` — busca rival guardián/rompedor, con cooldown. Entra Decoying (Approach → Taunt → Flee).

- `TickExpedition() → void` — orquesta máquina de estados por fase. Chequea target válido; si no, EnterLosing/Abort. Si tiempo > GiveUpSeconds, abandona.
- `ResetForReuse() → void` — pooling: limpia target, timers, fase.

## Propiedades Públicas (Fachada)

- `Collected → int` — contador acumulativo sesión local
- `Target → MaterialPickup` — recolectable actual (null si idle)
- `Carried → int` — material en mano (0–CarryCapacity)
- `MiningProgress → float` — 0–1 progreso de minería
- `TargetTransform → Transform` — transform del objetivo o null
- `Intent → CreatureIntent` — según fase

## Fases

**Gather:**
- Noticing → Moving → Mining → Returning → Securing
- Mining: `carried++` cada phaseTimer. Si carried >= CarryCapacity o mineral agotado → BeginReturn
- Returning: navega a HomeExit
- Securing: deposita material

**Guard:**
- Guarding: se planta cerca de MaterialPickup. Vigila sin interactuar

**Break:**
- Hunting: si prey exists → persigue; sino → espera en MaterialPickup

**Decoy:**
- Decoying: Approach → Taunt → Flee. Cooldown evita spam

## Métodos Privados Clave

**ApproachPoint S102 CAMBIO:**
```csharp
private Vector3 ApproachPoint(ExpeditionRulesSO rules)
{
    // Calcula punto de aproximación alrededor del mineral, evita:
    for (int i = 0; i < ctx.Percepts.Count; i++)
    {
        var p = ctx.Percepts[i];
        if (p.Source == null || p.Source.Monchi == null || p.Source.Monchi == owner) continue;
        if (p.Source.Monchi.ExpeditionTarget != target.transform) continue;
        
        // **S102 NUEVO:** solo cuenta ocupantes que recolectan/toman (Player/Rival teams)
        var otherIntent = p.Source.Monchi.Intent;
        if (otherIntent != CreatureIntent.Collecting && otherIntent != CreatureIntent.Taking) continue;
        
        // Calcula ángulo opuesto al ocupante
        Vector3 other = p.Source.Monchi.transform.position - center; other.y = 0f;
        if (other.sqrMagnitude >= selfSqrDist) continue;
        
        float b = Mathf.Atan2(other.z, other.x);
        float delta = Mathf.DeltaAngle(...) * Mathf.Deg2Rad;
        if (Mathf.Abs(delta) < sep)
            a = b + Mathf.Sign(...) * sep;
    }
    
    return center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rim;
}
```

**Significado S102:** antes contaba a TODO Monchi cuyo `ExpeditionTarget` fuera el mismo cristal (y desde S101 `ExpeditionTarget` también devuelve el puesto de vigías, rompedores y señuelos), así que un vigía plantado junto a la veta hacía girar a las mineras cada repath; ahora solo cuentan las criaturas con intención `Collecting` o `Taking` (las que de verdad van al borde).

**BeginReturn S102 CAMBIO:**
```csharp
private bool BeginReturn(ExpeditionRulesSO rules)
{
    exit = ctx.HomeExit;
    if (exit == null)
    {
        // **S102 NUEVO:** sin salida definida: suelta carga + emote, no retorna
        carried = 0;
        owner.EmitEmote(EmoteKind.Feliz);
        return false;
    }
    
    // Si existe exit: normalnavegación
    ctx.State = AgentState.Expedition;
    target = null;
    phase = Phase.Returning;
    ctx.SetStopped(false);
    ctx.SetDestinationSafe(exit.transform.position);
    return true;
}
```

**Significado S102:** antes intentaba regresar sin salida (posible deadlock); ahora libera carga con emote happy, aborta gracefully.

## Invariantes S102 + S101

- **ApproachPoint filtra por intención:** solo `Collecting`/`Taking` ocupan lugares del borde del cristal
- **BeginReturn graceful:** sin HomeExit no causa deadlock
- **Ocupaciones discretas:** Gather/Guard/Break/Decoy sin solapamiento
- **Gather es default:** None/Explore → Gather
- **Carried vs Secured:** carried = local al agente; exit.Secured = acumulador de equipo
- **Percepts read-only:** sin mutación desde AgentExpedition

## Conexiones

- [[MoriMochiAgent]] (owner, Intent, Occupation, EmitEmote)
- [[AgentContext]] (Percepts, SetDestinationSafe, Occupation, HomeExit, GuardPost)
- [[ExpeditionRulesSO]] (Current, tuning: NoticeSeconds, MiningSecondsPerUnit, etc.)
- [[MaterialPickup]] (target, Taken, Remaining)
- [[ExitZone]] (exit, Deposit)
- **S102:** [[CreatureIntent]] (Collecting, Taking para filtro ApproachPoint)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
