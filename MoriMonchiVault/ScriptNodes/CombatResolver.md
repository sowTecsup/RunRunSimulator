---
tags: [combat, resolver, context, equipment, synergy]
---

# CombatResolver

**Ruta:** `Systems/Combat/CombatResolver.cs`

**Responsabilidad:** Implementa `ICombatContext`, el contrato que los `CombatProcEffect` usan para aplicar acciones (daño, curación, status, stun). Centraliza salvaguardas anti-permastun (no re-stun si ya stunned, inmunidad post-despertar), stacking independiente de estados, y **detección/disparo de sinergias (S32):** cuando se agrega un status, CheckSynergies verifica recetas, quema stacks FIFO y aplica efectos de portadores. **S35:** Captura automática de `TargetStatusAfter` en cada proc.

## Métodos Públicos (ICombatContext)

| Método | Descripción |
|--------|-------------|
| `DamageOpponent(amount, source)` | Reduce HP del oponente, graba `ReturnDamage` proc |
| `HealSelf(amount, source)` | Incrementa HP propio (capped a MaxHp), graba `Heal` proc |
| `ApplyStatusToOpponent(kind, turns, magnitude, source)` | Aplica status al oponente → `AddStatus()` |
| `ApplyStatusToSelf(kind, turns, magnitude, source)` | Aplica status propio → `AddStatus()` |
| `StunOpponent(int turns)` | **Anti-permastun:** rechaza si ya stunned o en inmunidad; si acepta, aplica y graba `Stun` |
| `StunBearer(Combatant bearer, int turns)` | **(S32)** Stun sobre portador genérico (usado por SynergyStunEffect) — delega a `StunTarget()` |
| `DamageBearer(Combatant bearer, float amount, string source)` | **(S32)** Daño sobre portador, graba `Synergy` proc |
| `HealBearer(Combatant bearer, float amount, string source)` | **(S32)** Curación sobre portador, graba `Synergy` proc |
| `AddStatusTo(Combatant bearer, ModifierEffectKind kind, int turns, int magnitude, string source)` | **(S32)** Status sobre portador, delega a `AddStatus()` |
| `Record(ModifierEffectKind, Combatant, float amount)` | Crea `CombatProcEvent` con snapshot de `TargetStatusAfter` y lo append a `TurnProcs` **(S35)** |
| `Record(ModifierEffectKind, Combatant)` | Sobrecarga: graba sin amount (0f) |

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Result` | `CombatResult` | Referencia a result para logging/turns |
| `Self` | `Combatant` | El combatiente que dispara este resolver (atacante/defensor según contexto) |
| `Opponent` | `Combatant` | El oponente |
| `TurnProcs` | `List<CombatProcEvent>` | Buffer del turno actual; se crea fresh cada turno |
| `BeforeStrike` | `bool` | true si estamos antes del golpe, false después |
| `Synergies` | `SynergyTableSO` | **(S32)** Ref a tabla de recetas (null = sin sinergias) |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `StunTarget(Combatant t, int turns)` | **Refactorizado S32:** Guard compartido por `StunOpponent()` y `StunBearer()`. Rechaza si stunned o inmune, aplica si válido |
| `AddStatus(Combatant t, ModifierEffectKind kind, int turns, int magnitude, string source)` | Crea `ActiveEffect`, incrementa stacks localmente, graba proc, **NUEVO S32:** llama `CheckSynergies(t)` |
| `CheckSynergies(Combatant bearer)` | **(S32)** Detección y disparo de sinergias sobre portador |
| `FirstSatisfiedRule(Combatant bearer)` | **(S32)** Busca primera regla cuya receta esté satisfecha contra stacks activos |
| `ConsumeStacks(Combatant bearer, SynergyRule rule)` | **(S32)** Quema stacks FIFO según requisitos de la regla |
| `StatusMarks(Combatant c)` | **(S35)** Static helper: retorna `List<CombatStatusMark>` con snapshot de stacks activos + stun |

## Anti-Permastun (S32 refactor)

Guard compartido `StunTarget()` — ambos `StunOpponent()` y `StunBearer()` llaman aquí:

```csharp
private void StunTarget(Combatant t, int turns)
{
    if (t.StunTurns > 0)
    {
        Result.Log.Add($"    [stun] {t.Name} is already stunned — no effect");
        return;
    }
    if (t.StunImmunityTurns > 0)
    {
        Result.Log.Add($"    [stun] {t.Name} resists (immune, {t.StunImmunityTurns}t left)");
        return;
    }
    t.StunTurns = turns;
    Result.Log.Add($"    [stun] {t.Name} stunned for {t.StunTurns} turn(s)");
    Record(ModifierEffectKind.Stun, t, turns);
}
```

Cuando el stun expira (`StunTurns == 0`), `TakeTurn()` en `CombatService` seteea `StunImmunityTurns = CombatManagerSO.StunImmunityTurns` (default 1).

## Stacking Independiente

`AddStatus()` permite múltiples instancias del mismo `Kind`:

```csharp
private void AddStatus(Combatant t, ModifierEffectKind kind, int turns, int magnitude, string source)
{
    t.Active.Add(new ActiveEffect { Kind = kind, RemainingTurns = turns, Magnitude = magnitude });
    int stacks = 0;
    foreach (var a in t.Active) if (a.Kind == kind) stacks++;
    Result.Log.Add($"    [{source}] {t.Name} gains {kind} ({magnitude}/turn, {turns}t){(stacks > 1 ? $" — x{stacks} stacks" : "")}");
    Record(kind, t, magnitude);
    CheckSynergies(t);  // NEW S32
}
```

Cada instancia es independiente, con su propio contador de turnos. Se procesan simultáneamente en `TickStatuses()`. Los Magnitude de stacks del mismo Kind se suman en propiedades dinámicas de Combatant (EffDefense, EffSpeed, etc.) — S35.

## Motor de Sinergias (S32)

### CheckSynergies(Combatant bearer)

Se llama desde `AddStatus()` cada vez que se agrega un efecto. Guard anti-reentrada + cap de 8 iteraciones (previene loops infinitos):

```csharp
private bool resolvingSynergies;

private void CheckSynergies(Combatant bearer)
{
    if (resolvingSynergies || Synergies == null || Synergies.Rules == null) return;
    resolvingSynergies = true;
    for (int guard = 0; guard < 8; guard++)
    {
        var rule = FirstSatisfiedRule(bearer);
        if (rule == null) break;
        Result.Log.Add($"    [synergy] ¡{rule.Name}! detonates on {bearer.Name}");
        ConsumeStacks(bearer, rule);           // Quema FIFO
        Record(ModifierEffectKind.Synergy, bearer);  // Graba DESPUÉS de quemar (S35)
        foreach (var e in rule.Effects)
            if (e != null) e.Apply(this, bearer);
    }
    resolvingSynergies = false;
}
```

**Flujo:**
1. Si `resolvingSynergies == true` o tabla null, retorna (guard reentrancia)
2. Loop máx 8 veces (cap)
3. Busca primera regla satisfecha
4. Si nada, break
5. Log, quema stacks FIFO
6. Graba proc Synergy — **IMPORTANTE (S35):** ocurre DESPUÉS de ConsumeStacks, para capturar estado post-quema en `TargetStatusAfter`
7. Aplica todos los efectos polimórficamente

### FirstSatisfiedRule(Combatant bearer)

```csharp
private SynergyRule FirstSatisfiedRule(Combatant bearer)
{
    foreach (var rule in Synergies.Rules)
    {
        if (rule == null || rule.Requirements == null) continue;
        bool satisfied = true;
        foreach (var req in rule.Requirements)
        {
            int count = 0;
            foreach (var a in bearer.Active) if (a.Kind == req.Kind) count++;
            if (count < req.Stacks) { satisfied = false; break; }
        }
        if (satisfied) return rule;
    }
    return null;
}
```

Valida que TODOS los requisitos se cumplan (AND lógico).

### ConsumeStacks(Combatant bearer, SynergyRule rule)

```csharp
private void ConsumeStacks(Combatant bearer, SynergyRule rule)
{
    foreach (var req in rule.Requirements)
    {
        int toRemove = req.Stacks;
        for (int i = 0; i < bearer.Active.Count && toRemove > 0; )
        {
            if (bearer.Active[i].Kind == req.Kind)
            { bearer.Active.RemoveAt(i); toRemove--; }
            else i++;
        }
        Result.Log.Add($"    [synergy] consumed {req.Stacks}x {req.Kind} stacks");
    }
}
```

FIFO: por cada tipo, remueve exactamente la cantidad requerida de instancias `ActiveEffect`.

### StatusMarks Helper (S35)

```csharp
public static List<CombatStatusMark> StatusMarks(Combatant c)
{
    var counts = new Dictionary<ModifierEffectKind, int>();
    foreach (var a in c.Active)
        counts[a.Kind] = counts.TryGetValue(a.Kind, out var n) ? n + 1 : 1;

    var marks = new List<CombatStatusMark>();
    foreach (ModifierEffectKind kind in System.Enum.GetValues(typeof(ModifierEffectKind)))
        if (counts.TryGetValue(kind, out var stacks))
            marks.Add(new CombatStatusMark { Kind = kind, Stacks = stacks });

    if (c.StunTurns > 0)
        marks.Add(new CombatStatusMark { Kind = ModifierEffectKind.Stun, Stacks = c.StunTurns });

    return marks;
}
```

Calcula counts de cada Kind en `c.Active`, luego itera todos los enums de ModifierEffectKind para obtener el orden consistente. Agrega Stun como mark si activo. Se usa en `Record()` automáticamente.

## Vinculado a

- [[Index/03 - Combat]]
- [[ICombatContext]] — interfaz que implementa
- [[CombatService]] — instancia un resolver por simulación, pasa `config.Synergies`
- [[CombatProcEffect]] — recibe `this` (ICombatContext) en `Apply()`
- [[CombatManagerSO]] — campo `Synergies: SynergyTableSO`
- [[SynergyTableSO]] — tabla de reglas
- [[SynergyEffectBase]] — efectos polimórficos
- [[CombatTurn]] — los procs grabados aquí terminan en `Turn.Procs`

## Conexiones

**Entrada:**
- `CombatService.SimulateCore()` → crea instancia, pasa `config.Synergies`
- Cada `CombatProcEffect.Apply(ICombatContext)` llama a métodos públicos
- `AddStatus()` → `CheckSynergies()` automático

**Salida:**
- Mutaciones a `Self.Hp`, `Opponent.Hp`, `Opponent.StunTurns`, `Opponent.Active`
- `CombatProcEvent` enumerados en `TurnProcs` → `CombatTurn.Procs` (incluye `ModifierEffectKind.Synergy` y `TargetStatusAfter`)

## Cambios S32

- **Nuevos métodos públicos:** `StunBearer()`, `DamageBearer()`, `HealBearer()`, `AddStatusTo()` — acceso genérico para `SynergyEffectBase.Apply()`
- **Refactor StunTarget():** Guard compartido extraído de `StunOpponent()`
- **CheckSynergies loop:** Detección FIFO + cap 8 iteraciones + anti-reentrada
- **Proc Synergy:** Graba `ModifierEffectKind.Synergy` cuando detona regla
- **Backward compat:** Si `Synergies == null`, CheckSynergies retorna sin hacer nada — feature completamente deshabilitada sin tocar código

## Cambios S35

- **StatusMarks static method:** Calcula snapshot de stacks activos + stun para un Combatant
- **Record() captura automática:** `TargetStatusAfter = StatusMarks(target)` en cada proc grabado
- **CheckSynergies order:** ConsumeStacks ANTES de Record, para que `TargetStatusAfter` refleje estado post-quema (importante para sincronización de UI)

## Notas

- No es stateless: acumula `TurnProcs` durante el turno (se resetea cada turno).
- El anti-permastun, stacking, **y ahora sinergias** viven aquí, no en el DNA o el equipment.
- Todos los logs van a `Result.Log` para traza debug completa.
- Método `DamageBearer()` graba `Synergy` (no `ReturnDamage`) porque estos daños vienen de sinergias, no de procs de equipo.
- **S35:** La captura automática de `TargetStatusAfter` garantiza que el visualizador siempre tiene snapshots exactos del estado tras cada proc, sin gaps de sincronización.
