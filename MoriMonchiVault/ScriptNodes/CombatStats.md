---
tags: [combat, stats, calculation]
---

# CombatStats

**Ruta:** `Systems/Combat/CombatStats.cs`

**Responsabilidad:** Clase estática que calcula stats efectivos (CON/ATK/SPD/DEF/LCK/EVA) de una criatura sumando base + acumulación de partes por tier. No aplica equipment aún (eso lo hace `EquipmentStats`).

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetEffectiveStats(CreatureDNA dna, CreatureDatabaseSO db)` | `EffectiveStats` | Suma DNA base + bonificaciones de Body/Arm/Eye/Mouth según tier |
| `BaseHpCombatMultiplier` | `const float = 5f` | HP en combate = Constitution * 5 |

## Algoritmo

```csharp
GetEffectiveStats(dna, db):
  con = dna.BaseConstitution
  atk = dna.BaseAttack
  spd = dna.BaseSpeed
  
  AccumulatePart(Body, con, atk, spd)
  AccumulatePart(Arm,  con, atk, spd)
  AccumulatePart(Eye,  con, atk, spd)
  AccumulatePart(Mouth, con, atk, spd)
  
  return EffectiveStats(con, atk, spd, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion)
```

**AccumulatePart:**
```csharp
  if (part == null) return
  bonus = (int)tier - 1    // Tier1=0, Tier2=1, Tier3=2
  con += part.HP     + bonus
  atk += part.Attack + bonus
  spd += part.Speed  + bonus
```

DEF/LCK/EVA no se acumulan de partes; vienen íntegros del DNA base.

## Vinculado a

- [[Index/03 - Combat]]
- [[CreatureDNA]] — fuente de stats base
- [[BodyPart]] — estructura de partes con HP/ATK/SPD
- [[EffectiveStats]] — struct de retorno
- [[CombatService]] — llama desde `BuildCombatant()`

## Conexiones

**Entrada:**
- `CreatureDatabaseSO.Get{BodyShape,Arm,Eye,Mouth}(id)` — resuelve partes por ID

**Salida:**
- `EffectiveStats` → pasado a `EquipmentStats.Apply(baseStats, dna, equipDb)`
- HP final = `EffectiveStats.Constitution * BaseHpCombatMultiplier`

## Notas

- Stats de equipamiento se aplican *después*, en `EquipmentStats`.
- No valida nulidad de partes (retorna early si null).
- Ordenamiento de acumulación (Body → Arm → Eye → Mouth) es arbitrario; orden no importa.
