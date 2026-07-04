---
tags: [combat, data, mutable-state]
---

# Combatant

**Ruta:** `Systems/Combat/Combatant.cs`

**Responsabilidad:** Modelo mutable de un combatiente *durante* la simulación. Almacena snapshot de DNA, stats finales (después de equipment), HP presente, stun/immunity counters, y lista de procs/efectos activos. **S35:** Provee propiedades dinámicas (`EffDefense`, `EffEvasion`, `EffSpeed`, `LifestealPercent`) que suman stacks de elementos activos en tiempo real.

## Estructura

### Combatant (clase pública)

**Campos públicos:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Dna` | `CreatureDNA` | Referencia a la criatura (se mutará si gana/muere) |
| `Name` | `string` | Nombre para logging |
| `IsA` | `bool` | Si true, es combatante A (vs B) |
| `Hp` | `float` | HP actual durante combate |
| `MaxHp` | `float` | HP máximo = Constitution * BaseHpCombatMultiplier |
| `Attack` | `float` | Ataque total (base + equipment) |
| `Speed` | `float` | Velocidad total base (base + equipment) |
| `Defense` | `float` | Defensa total base |
| `Luck` | `float` | Suerte total |
| `Evasion` | `float` | Evasión total base |
| `StunTurns` | `int` | Turnos de stun activos (decrementa cada turno) |
| `StunImmunityTurns` | `int` | Turnos de inmunidad a stun post-despertar (decrementa) |
| `Procs` | `List<CombatProcEffect>` | Todos los procs del equipment equipado |
| `Active` | `List<ActiveEffect>` | Estados en curso (Poison, Burn, Regen, Static, Pulse, Steel, Mist, etc.) |

**Propiedades dinámicas (S35):**

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `EffDefense` | `float` | Defense + suma de Magnitude de stacks Steel activos |
| `EffEvasion` | `float` | Evasion + suma de Magnitude de stacks Mist activos |
| `EffSpeed` | `float` | Speed - suma de Magnitude de stacks Static, clamped a 0 |
| `LifestealPercent` | `float` | Suma de Magnitude de stacks Lifesteal / 100f, clamped a 1 |

**Cálculo de propiedades dinámicas:**
```csharp
public float EffDefense => Defense + StackSum(ModifierEffectKind.Steel);
public float EffEvasion => Evasion + StackSum(ModifierEffectKind.Mist);
public float EffSpeed   => Mathf.Max(0f, Speed - StackSum(ModifierEffectKind.Static));
public float LifestealPercent => Mathf.Min(1f, StackSum(ModifierEffectKind.Lifesteal) / 100f);

private float StackSum(ModifierEffectKind kind)
{
    float sum = 0f;
    foreach (var a in Active)
        if (a.Kind == kind) sum += a.Magnitude;
    return sum;
}
```

Las propiedades se recalculan en cada acceso (no cacheadas) porque `Active` muta durante el turno.

### ActiveEffect (clase interna)

Estructura de un status activo durante combate.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Kind` | `ModifierEffectKind` | Tipo (Poison, Burn, Regen, Static, Pulse, Steel, Mist, Lifesteal, etc.) |
| `RemainingTurns` | `int` | Turnos restantes (decrementa) |
| `Magnitude` | `int` | Daño/curación/bonus por turno o para cálculos dinámicos |

## Ciclo de Vida

1. `CombatService.BuildCombatant()` — crea instancia, carga DNA y equipment
2. `CombatService.SimulateCore()` — pasa A y B a `TakeTurn()`
3. `TakeTurn()` muta: `Hp`, `StunTurns`, `StunImmunityTurns`, `Active` (la lista crece/shrinks)
4. Las propiedades dinámicas (`EffSpeed`, `EffDefense`, etc.) se leen durante orden de turno (Speed comparison) y en fórmulas de daño
5. Final de combate: si ganó/perdió, las mutaciones vuelven al DNA persistente via `CombatEvolution.AdvanceTier()` u otros

## Uso de Propiedades Dinámicas

### EffSpeed — Orden de turno

En `CombatService.SimulateCore()`, línea ~105:
```csharp
bool aFirst = A.EffSpeed > B.EffSpeed ||
              (Mathf.Approximately(A.EffSpeed, B.EffSpeed) && rng.NextFloat() < 0.5f);
```

Static reduce Speed en tiempo real, por lo que un combatiente con Static×2 puede perder el orden si Speed es similar.

### EffDefense — Mitigación de daño

En `CombatService.TakeTurn()`, línea ~239:
```csharp
float reduction = Mathf.Clamp01(def.EffDefense * config.DefenseReductionPerPoint);
damage          = raw * (1f - reduction);
```

Steel apila DEF dinámicamente, aumentando la mitigación.

### EffEvasion — Evasión de golpes

En `CombatService.TakeTurn()`, línea ~226:
```csharp
float evaChance = def.EffEvasion * config.EvasionPerPoint;
bool  dodged    = evaRoll < evaChance;
```

Mist apila EVA dinámicamente, aumentando chance de esquivar.

### LifestealPercent — Curación post-golpe

En `CombatService.TakeTurn()`, línea ~251:
```csharp
if (!dodged && damage > 0f && atk.LifestealPercent > 0f)
{
    float steal = damage * atk.LifestealPercent;
    atk.Hp = Mathf.Min(atk.MaxHp, atk.Hp + steal);
    result.Log.Add($"    [Lifesteal] {atk.Name} +{steal:F1} → {atk.Hp:F1}");
    r.Record(ModifierEffectKind.Lifesteal, atk, steal);
}
```

El % de daño infligido vuelve como cura al atacante (solo si golpea y no es esquivado).

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatService]] — construye y muta durante simulación
- [[CombatResolver]] — accede a Self/Opponent para aplicar acciones
- [[ActiveEffect]] — lista de efectos activos
- [[ModifierEffectKind]] — enum de tipos de efectos

## Conexiones

**Entrada:**
- `CombatService.BuildCombatant(dna, db, equipDb, isA)` — crea instancia
- `AddStatus()` en CombatResolver — agrega/muta `Active` list

**Salida:**
- Cambios en `Hp`, stun counters, `Active` vía métodos de `CombatResolver`
- Las propiedades dinámicas se leen en fórmulas (Speed order, DEF reduction, EVA chance, Lifesteal %)

## Notas

- No se serializa; es exclusivamente un modelo de simulación en tiempo real.
- Su `Dna` apunta a la misma criatura que en la registry, y se mutará solo si el combate modifica tiers/muerte.
- `StunImmunityTurns` implementa anti-permastun (ver `CombatManagerSO.StunImmunityTurns`).
- **S35:** Las propiedades dinámicas permiten que stacks de elementos *en curso* (no items fijos) modifiquen el comportamiento de combate en tiempo real. No hay pre-cálculo; se leen on-demand cada turno.
