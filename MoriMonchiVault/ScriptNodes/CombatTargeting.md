---
tags: [script, combat, targeting, deterministic]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatTargeting.cs

**Ruta:** `Systems/Combat/CombatTargeting.cs`

**Responsabilidad:** Utilitarios estáticos deterministas para selección de objetivos en simulador 3v3. Todo roll sale de `CombatRng` inyectado por el caller — nunca `UnityEngine.Random`. Soporta dos estrategias: golpear la fila frontal (por defecto) o backdoor la backline (rol Agresivo). Simétrico entre ambos clientes (local + async) porque comparten seed y equipo. **S62:** Nuevo método `LowestHpPercentAlly()` que busca aliado con menor % de vida (tiebreak por Index) — usado por ShieldAllyPassive como targeting inteligente para escudos.

## Cambios S62

**Nuevo método LowestHpPercentAlly:**
- Busca el aliado vivo con menor porcentaje de HP (Hp / MaxHp)
- Tiebreak por Index ascendente (determinista)
- Retorna null si no hay aliados vivos
- Usado por ShieldAllyPassive para identificar el aliado con menor vida relativa

## Cambios S37

**Nuevo en S37:** Reemplazo de sistema 1v1 con selección de objetivos orientada a equipos. Introduce concepto de filas (`CombatRow`: Front/Mid/Back), evaluación de fila "viva más adelantada", y tres estrategias de targeting.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `FrontRow(team)` | `CombatRow` | Retorna la fila **viva más adelantada** del equipo (itera, retorna lowest row vivo, default Front si vacío) |
| `PickFrontTarget(team, rng)` | `Combatant` | Itera candidatos vivos en fila frontal, elige uniforme con rng.Range, retorna null si none |
| `PickBacklineTarget(team, rng)` | `Combatant` | Itera candidatos vivos en filas NO-frontal (Mid/Back), elige uniforme, retorna null si none |
| `PickAlly(team, rng)` | `Combatant` | Elige aliado vivo al azar uniforme, retorna null si none |
| `LowestHpAlly(team)` | `Combatant` | Itera aliados vivos, retorna lowest HP (tiebreak por Index asc), null si none |
| `LowestHpPercentAlly(team)` | `Combatant` | **S62 NEW** Itera aliados vivos, retorna lowest HP% (tiebreak por Index asc), null si none. Usado por ShieldAllyPassive. |

## Algoritmo: FrontRow (S37)

```csharp
CombatRow best = CombatRow.Back;  // worst-case init
bool found = false;
foreach (var c in team)
{
    if (!c.IsAlive) continue;
    if (!found || c.Row < best)
    {
        best = c.Row;
        found = true;
    }
}
return found ? best : CombatRow.Front;
```

**Semántica:** Front < Mid < Back (valores enum); este código encuentra la fila con el `int` value más bajo que tiene al menos una criatura viva.

**Ejemplo:** Si team = [Back vivo, Back vivo, Mid vivo], retorna Mid (la fila más adelantada). Si team = [todos muertos], retorna Front (default fallback).

## Algoritmo: PickFrontTarget (S37)

```csharp
var front = FrontRow(team);
var candidates = new List<Combatant>();
foreach (var c in team)
    if (c.IsAlive && c.Row == front) candidates.Add(c);

if (candidates.Count == 0) return null;
int idx = rng.Range(0, candidates.Count);
return candidates[idx];
```

**Semántica:** Recolecta todos los vivos en la fila frontal, luego elige uniforme entre ellos. Si no hay candidatos (equipo destruido o anomalía), retorna null (el caller debe manejar — típicamente skip turn).

## Algoritmo: PickBacklineTarget (S37)

```csharp
var front = FrontRow(team);
var candidates = new List<Combatant>();
foreach (var c in team)
    if (c.IsAlive && c.Row != front) candidates.Add(c);

if (candidates.Count == 0) return null;
int idx = rng.Range(0, candidates.Count);
return candidates[idx];
```

**Semántica:** Opuesto de PickFrontTarget — recolecta vivos NOT en fila frontal. Usado por rol Agresivo (50% chance de backline hit en lugar de frontline). Si no hay backline vivo, retorna null (fallback a frontline en caller).

## Algoritmo: PickAlly (S37)

```csharp
var candidates = new List<Combatant>();
foreach (var c in team)
    if (c.IsAlive) candidates.Add(c);

if (candidates.Count == 0) return null;
int idx = rng.Range(0, candidates.Count);
return candidates[idx];
```

**Semántica:** Elige aliado vivo cualquiera al azar uniforme. Usado por rol Protector (pick target para escudo cada turno).

## Algoritmo: LowestHpAlly (S37)

```csharp
Combatant best = null;
foreach (var c in team)
{
    if (!c.IsAlive) continue;
    if (best == null || c.Hp < best.Hp || (c.Hp == best.Hp && c.Index < best.Index))
        best = c;
}
return best;
```

**Semántica:** Encuentra el vivo con menor HP actual (tiebreak por Index ascendente para determinismo). Usado por rol Empático (cura al aliado más débil post-strike).

## Algoritmo: LowestHpPercentAlly (S62 NEW)

```csharp
Combatant best = null;
float bestPercent = 0f;
foreach (var c in team)
{
    if (!c.IsAlive) continue;
    float percent = c.Hp / c.MaxHp;
    if (best == null || percent < bestPercent || (percent == bestPercent && c.Index < best.Index))
    {
        best = c;
        bestPercent = percent;
    }
}
return best;
```

**Semántica:** Encuentra el vivo con menor porcentaje de vida (Hp / MaxHp), tiebreak por Index ascendente. Usado por rol Protector en S62 para identificar al aliado más vulnerable proporcionalmente (no absoluto). Si todos están a 100%, retorna null y el caller cae a `PickAlly` random.

## Consumo en CombatService (S37 + S62)

**TakeTurn() flow:**
1. Agresivo: `if (rng.NextFloat() < atk.Role == Agresivo ? 0.5f : 0)` → `target = PickBacklineTarget(team) ?? PickFrontTarget(team)`
2. Protector (S62): `ally = LowestHpPercentAlly(myTeam)`; si ally.Hp >= ally.MaxHp (está al 100%), `ally = PickAlly(myTeam)` random → `resolver.ShieldTarget(ally, ...)`
3. Empático: post-strike si golpea → `ally = LowestHpAlly(myTeam)` → cura `ally.Hp += damage * profile.HealPercentOfDamage`

## Determinismo (S37 + S62)

- **Cero randómicos globales:** Todo rng pasa explícitamente, inyectado por caller
- **Orden de consumo fijo:** FrontRow evalúa cada Combatant exactamente 1 vez; PickFrontTarget/Backline/Ally consumen 1 rng.Range() call; LowestHpAlly/LowestHpPercent sin rng
- **Simétrico:** Ambos clientes (local + async) corren mismo seed + mismas filas → mismos rolls → mismos objetivos
- **Eficiencia:** O(n) por pick (n = team size, típicamente 3)

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[Enums]] — `CombatRow` enum (Front=0, Mid=1, Back=2)
- [[CombatService]] — TakeTurn usa PickFrontTarget/Backline/LowestHpAlly/LowestHpPercent/PickAlly en rol logic
- [[Combatant]] — equipo = List<Combatant>; cada uno tiene IsAlive, Row, Index, Hp, MaxHp
- [[RoleTableSO]] — perfiles define BacklineHitChance

## Conexiones

**Entrada:**
- `CombatService.TakeTurn(atk, def, ..., rng)` → llama PickFrontTarget, PickBacklineTarget, PickAlly, LowestHpAlly, LowestHpPercentAlly con rng inyectado
- `RolePassiveBase.ShieldAllyPassive` → llama LowestHpPercentAlly primero, fallback a PickAlly (S62)

**Salida:**
- Retorna `Combatant` elegido (o null si no disponible)
- Caller procede a atacar ese objetivo o aplicar efecto
- No mutación de estado (lectura pura)

## Notas (S37 + S62)

- **Sin estado mutativo:** CombatTargeting es stateless utility class
- **Null-safe:** Todos los métodos retornan null si no hay candidatos válidos; caller maneja
- **Filas vivas:** Método `FrontRow` es clave — evaluado dinámicamente cada turno, reflejando cambios de muerte al pasar rondas
- **Paridad Row:** Los `Combatant` tienen campo `Row` asignado en `CombatService.SimulateCore()` antes de combate, con default 2-3-2 (Front-Front-Mid per equipo)
- **LowestHpAlly sin rng:** Selección determinista por HP + Index, sin roll (usado para curación Empático)
- **LowestHpPercentAlly sin rng (S62):** Selección determinista por HP% + Index, sin roll (usado para targeting de escudo Protector, con fallback a random si está al 100%)
