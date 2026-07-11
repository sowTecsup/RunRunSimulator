---
tags: [combat, stats, calculation]
---

# CombatStats

**Ruta:** `Systems/Combat/CombatStats.cs`

**Responsabilidad:** Clase estática que calcula stats efectivos (CON/ATK/SPD/DEF/LCK/EVA) de una criatura sumando base + **S37** modificadores de rol (si tabla pasada) + acumulación de partes por tier. No aplica equipment (eso lo hace `EquipmentStats`). Dos sobrecargas: `GetEffectiveStats(dna, db)` sin roles, `GetEffectiveStats(dna, db, roles)` con mods de rol.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetEffectiveStats(CreatureDNA dna, CreatureDatabaseSO db)` | `EffectiveStats` | Suma DNA base + bonificaciones de Body/Arm/Eye/Mouth según tier (sin role mods). Delegación a sobrecarga de 3 args con null. |
| `GetEffectiveStats(CreatureDNA dna, CreatureDatabaseSO db, RoleTableSO roles)` | `EffectiveStats` | **S37** Suma DNA base + role mods (si roles != null) + bonificaciones de partes según tier |
| `BaseHpCombatMultiplier` | `const float = 5f` | HP en combate = Constitution * 5 |

## Algoritmo (S37)

```csharp
GetEffectiveStats(dna, db, roles):
  con = dna.BaseConstitution
  atk = dna.BaseAttack
  spd = dna.BaseSpeed
  
  IF roles != null:
    profile = roles.GetProfile(dna.Role)
    con = Clamp(con + profile.ConMod, 1, 10)  // Aplica ANTES de acumular partes
    atk = Clamp(atk + profile.AtkMod, 1, 10)
    spd = Clamp(spd + profile.SpdMod, 1, 10)
  
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

## Cambios S37

**Sobrecarga con 3 args:**
```csharp
public static EffectiveStats GetEffectiveStats(CreatureDNA dna, CreatureDatabaseSO db, RoleTableSO roles)
{
    float con = dna.BaseConstitution;
    float atk = dna.BaseAttack;
    float spd = dna.BaseSpeed;

    // Aplica role mods ANTES de acumular partes (si tabla pasada)
    if (roles != null)
    {
        var p = roles.GetProfile(dna.Role);
        con = Mathf.Clamp(dna.BaseConstitution + p.ConMod, 
                          CreatureGenerator.StatMin, CreatureGenerator.StatMax);  // [1,10]
        atk = Mathf.Clamp(dna.BaseAttack + p.AtkMod, 
                          CreatureGenerator.StatMin, CreatureGenerator.StatMax);
        spd = Mathf.Clamp(dna.BaseSpeed + p.SpdMod, 
                          CreatureGenerator.StatMin, CreatureGenerator.StatMax);
    }

    // Luego acumula partes sobre los stats con mods ya aplicados
    AccumulatePart(db.GetBodyShape(dna.BodyShapeID), dna.BodyTier,  ref con, ref atk, ref spd);
    AccumulatePart(db.GetArm(dna.ArmID),             dna.ArmTier,   ref con, ref atk, ref spd);
    AccumulatePart(db.GetEye(dna.EyeID),             dna.EyeTier,   ref con, ref atk, ref spd);
    AccumulatePart(db.GetMouth(dna.MouthID),         dna.MouthTier, ref con, ref atk, ref spd);

    return new EffectiveStats(con, atk, spd, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);
}
```

**Consumo en BuildCombatant (S37):**
```csharp
// En CombatService.BuildCombatant():
var baseStats = CombatStats.GetEffectiveStats(dna, db, config.Roles);  // Incluye role mods
// baseStats.Constitution ya tiene role mod aplicado y clampeado [1,10]

c.MaxHp   = baseStats.Constitution * BaseHpCombatMultiplier;
c.Attack  = baseStats.Attack;
c.Speed   = baseStats.Speed;
c.Defense = baseStats.Defense;
c.Luck    = baseStats.Luck;
c.Evasion = baseStats.Evasion;
```

Luego `EquipmentStats.Apply()` suma mods de equipment sobre estos valores.

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/13 - Combat Design Direction]]
- [[CreatureDNA]] — fuente de stats base + Role metadata
- [[BodyPart]] — estructura de partes con HP/ATK/SPD
- [[EffectiveStats]] — struct de retorno
- [[CombatService]] — llama desde `BuildCombatant()` con `config.Roles`
- [[RoleTableSO]] — perfiles de role con mods [ConMod, AtkMod, SpdMod]

## Conexiones

**Entrada:**
- `CombatService.BuildCombatant(dna, db, equipDb, ...)` → llama `GetEffectiveStats(dna, db, config.Roles)`
- `CreatureDatabaseSO.Get{BodyShape,Arm,Eye,Mouth}(id)` — resuelve partes por ID

**Salida:**
- `EffectiveStats` → clampeados [1,10] para CON/ATK/SPD si roles aplicados → pasados a Combatant fields
- HP final (post-equipment) = `(EffectiveStats.Constitution + [equipement bonus]) * BaseHpCombatMultiplier`

## Notas

- **Pipeline S37:** base stats → apply role mods (si roles table) + clamp [1,10] → accumulate parts → return EffectiveStats → equipment stats applied later
- Stats de equipamiento se aplican *después* en `EquipmentStats.Apply()`.
- Sobrecarga de 2 args (`GetEffectiveStats(dna, db)`) delega a la de 3 args con `null` → sin role mods (backward compat).
- **Clampe crucial S37:** Role mods se clampean [1,10] INMEDIATAMENTE en esta función, asegurando que stats nunca caen por debajo de 1 ni suben sin límite superior por el mod solo.
- No valida nulidad de partes (retorna early si null).
- Ordenamiento de acumulación (Body → Arm → Eye → Mouth) es arbitrario; orden no importa (suma conmutativa).
