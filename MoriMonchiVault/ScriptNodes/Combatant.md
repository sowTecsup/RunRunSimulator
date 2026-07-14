---
tags: [combat, data, mutable-state]
---

# Combatant

**Ruta:** `Systems/Combat/Combatant.cs`

**Responsabilidad:** Modelo mutable de un combatiente *durante* la simulación 3v3. Almacena snapshot de DNA, stats finales (después de equipment + role mods), HP presente, escudo (S37), rol (S37), fila (S37), índice (S37), elemento (S39), afinidad de elemento (S39), stun/immunity counters, y listas de usos de equipo y efectos activos. **S35:** Propiedades dinámicas (`EffDefense`, `EffEvasion`, `EffSpeed`, `LifestealPercent`) que suman stacks de elementos activos en tiempo real. **S39:** ItemUseState reemplaza CombatProcEffect; lista de `Marks` (ElementMark) y `States` (ElementalState) para sistema elemental. **S46:** Campo `Energy` eliminado; solo queda `Affinity`.

## Estructura

### Combatant (clase pública)

**Campos públicos:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Dna` | `CreatureDNA` | Referencia a la criatura (se mutará si gana/muere) |
| `Name` | `string` | Nombre para logging y snapshots |
| `IsA` | `bool` | Si true, pertenece al equipo A (vs B). Usado en ambos 1v1 legacy y 3v3. |
| `Role` | `Role` | **S37** Rol de combate (Protector, Agresivo, Empático). Determina efectos de rol en TakeTurn. |
| `Row` | `CombatRow` | **S37** Fila ocupada (Front=0, Mid=1, Back=2). Define quién puede ser target por backline/frontline hits. |
| `Index` | `int` | **S37** Índice dentro del equipo (0..2). Usado en CombatTurn para identificar al unit. |
| `Hp` | `float` | HP actual durante combate |
| `MaxHp` | `float` | HP máximo = (Constitution + RoleConMod) * BaseHpCombatMultiplier |
| `Shield` | `float` | **S37** Escudo de rol Protector (acumula ShieldPerTurn, absorbido antes de Hp, decrementa por daño) |
| `Attack` | `float` | Ataque total (base + role mods + equipment) |
| `Speed` | `float` | Velocidad total (base + role mods + equipment) |
| `Defense` | `float` | Defensa total (base + equipment) |
| `Luck` | `float` | Suerte total |
| `Evasion` | `float` | Evasión total base |
| `StunTurns` | `int` | Turnos de stun activos (decrementa cada turno) |
| `StunImmunityTurns` | `int` | Turnos de inmunidad a stun post-despertar (decrementa) |
| `Element` | `Element` | **S39** Elemento de la criatura (innato del DNA) |
| `Affinity` | `int` | **S46** Afinidad de elemento (0-1, llega a 2 y dispara auto-marca). Recurso central (reemplaza Energy). |
| `Uses` | `List<ItemUseState>` | **S39** Todos los usos de equipment equipado (cada uno tiene Effect + Remaining) |
| `Active` | `List<ActiveEffect>` | Estados en curso (Poison, Burn, Regen, Static, Pulse, Steel, Mist, Lifesteal, etc.) |
| `Marks` | `List<ElementMark>` | **S39** Marcas elementales acumuladas |
| `States` | `List<ElementalState>` | **S39** Estados elementales activos (enum: Quemado, Envenenado, etc.) |

**Propiedades y métodos:**

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `IsAlive` | `bool` | Hp > 0f |
| `EffDefense` | `float` | Defense + suma de Magnitude de stacks Steel activos |
| `EffEvasion` | `float` | Evasion + suma de Magnitude de stacks Mist activos |
| `EffSpeed` | `float` | Speed - suma de Magnitude de stacks Static, clamped a 0 |
| `LifestealPercent` | `float` | Suma de Magnitude de stacks Lifesteal / 100f, clamped a 1 |
| `HasState(ElementalState s)` | `bool` | Retorna si s está en la lista States |
| `ConsumeState(ElementalState s)` | `bool` | Remueve s de States y retorna success |

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

### ItemUseState (clase interna, S39 nueva)

Estructura de un uso de equipo durante combate, reemplazando CombatProcEffect.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Effect` | `ItemUseEffect` | Referencia al efecto del item equipado |
| `Remaining` | `int` | Usos restantes de este efecto (decrementa) |

## Ciclo de Vida (S37)

1. `CombatService.BuildCombatant(dna, db, equipDb, isA, row, index)` — crea instancia, carga DNA/equipment, asigna Role/Row/Index/Shield=0, copia Element/Affinity del DNA
2. `CombatService.SimulateCore()` — itera ambos equipos en orden de EffSpeed
3. `TakeTurn()` muta: `Hp`, `Shield` (role Protector), `Affinity`, `StunTurns`, `StunImmunityTurns`, `Active` (lista crece/shrinks), `States` (decrementa), `Marks` (acumula)
4. Las propiedades dinámicas (`EffSpeed`, `EffDefense`, etc.) se leen durante orden de turno y en fórmulas de daño
5. `EmitTurn()` captura estado final de `Hp`/`Shield`/`Affinity`/`Active` en `CombatUnitState` (S37)
6. Final de combate: si ganó/perdió, mutaciones vuelven al DNA persistente via `CombatEvolution.AdvanceTier()` o muerte

## Cambios S46

**Energy eliminado:**
- Campo `Energy` removido completamente.
- Gates `if (actor.Energy > 0)` eliminados de pasivas (ShieldAllyPassive, HealLowestAllyOnHitPassive, BacklineHunterActive).
- Las pasivas ahora se aplican SIEMPRE (sin gate de recurso).

**Affinity refactorizado:**
- Sigue siendo `int`, rango 0-2.
- Cada turno, `GainAffinity()` incrementa +1; al alcanzar 2, se resetea a 0 y dispara `CombatElements.AddMark(actor, actor.Element, true, ...)` (auto-marca, mismo turno).
- El "beat" visual (UI: 2 llenos → se vacían) ahora emite dos eventos: `AffinityGained` con 2, y luego `AffinityGained` con 0 (post-marca).

## Cambios S39

**ItemUseState (nuevo):**
- `List<ItemUseState> Uses` reemplaza `List<CombatProcEffect> Procs`
- Estructura: `{ Effect: ItemUseEffect, Remaining: int }`
- Sistema elemental integrado: Equipment ahora lleva efectos polimórficos vía `ItemUseEffect` base

**Campos elementales nuevos:**
- `Element` — elemento innato del DNA (proviene de `CreatureDNA.Element`)
- `Affinity` — afinidad de elemento (valor numérico)
- `Marks` — lista de marcas elementales (ElementMark: tipo + stack count)
- `States` — lista de estados elementales activos (Quemado, Envenenado, etc.)

**Métodos nuevos:**
- `HasState(ElementalState s)` — chequea si un estado está activo
- `ConsumeState(ElementalState s)` — consume un estado activo y retorna success

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[CombatService]] — construye y muta durante simulación
- [[CombatResolver]] — accede para aplicar acciones
- [[ActiveEffect]] — lista de efectos activos
- [[ItemUseState]] — lista de usos de equipo (S39)
- [[ModifierEffectKind]] — enum de tipos de efectos
- [[Role]] — enum, determina comportamiento
- [[CombatRow]] — enum, fila en grid
- [[RoleTableSO]] — perfil de role, mods de stats
- [[Element]] — enum de elementos (S39)
- [[ElementMark]] — marca elemental (S39)
- [[ElementalState]] — estado elemental (S39)

## Conexiones

**Entrada:**
- `CombatService.BuildCombatant(dna, db, equipDb, isA, row, index)` — crea instancia con Role/Row/Index del DNA y Element/Affinity
- `CombatResolver.AddStatus()` — agrega a `Active` list
- `CombatResolver.ShieldTarget()` — suma a `Shield` (role Protector)
- `CombatResolver.ApplyMark()` — agrega a `Marks` (S39)
- `CombatResolver.ApplyState()` — agrega a `States` (S39)

**Salida:**
- Cambios en `Hp`, `Shield`, `Affinity`, stun counters, `Active`, `Marks`, `States` vía CombatResolver
- Las propiedades dinámicas se leen en Speed order, daño, evasión, lifesteal
- Final de combat: mutation vuelve a DNA vía CombatEvolution (tiers) o CombatRecord (historia)

## Notas

- No se serializa; es exclusivamente un modelo de simulación en tiempo real.
- Su `Dna` apunta a la misma criatura que en la registry, y se mutará solo si el combate modifica tiers/muerte.
- `StunImmunityTurns` implementa anti-permastun (ver `CombatManagerSO.StunImmunityTurns`).
- **S35:** Las propiedades dinámicas permiten que stacks de elementos *en curso* modifiquen el comportamiento en tiempo real. No hay pre-cálculo; se leen on-demand cada turno.
- **S37:** Role/Row/Index definen semántica 3v3. Shield es pool de absorción (Protector role). IsA se mantiene para backward compat + identificación de equipo.
- **S39:** Element/Affinity/Marks/States integran el sistema elemental en cada combatiente. Los efectos del equipo usan `ItemUseState` en lugar de `CombatProcEffect`.
- **S46:** Energy completamente eliminado como recurso. Affinity es el único mecánica de acumulación para disparar auto-marcas.
