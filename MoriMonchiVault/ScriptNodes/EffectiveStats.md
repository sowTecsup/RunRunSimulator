---
tags: [combat, data, stats, readonly]
---

# EffectiveStats

**Ruta:** `Data/Combat/EffectiveStats.cs`

**Responsabilidad:** Struct readonly que almacena los 6 stats finales de una criatura en combate, derivados de DNA base + partes + equipment. Inmutable una vez construido.

## Estructura

```csharp
public readonly struct EffectiveStats
{
    public readonly float Constitution;
    public readonly float Attack;
    public readonly float Speed;
    public readonly float Defense;
    public readonly float Luck;
    public readonly float Evasion;
}
```

**Constructor:**
```csharp
public EffectiveStats(float con, float atk, float spd, float def, float lck, float eva)
{
    Constitution = con;
    Attack = atk;
    Speed = spd;
    Defense = def;
    Luck = lck;
    Evasion = eva;
}
```

## Campos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Constitution` | `float` | Resistencia base (escalada a HP via BaseHpCombatMultiplier) |
| `Attack` | `float` | Daño por golpe |
| `Speed` | `float` | Orden de turno en combate |
| `Defense` | `float` | Reducción de daño entrante |
| `Luck` | `float` | Incremento de crit chance |
| `Evasion` | `float` | Chance de esquivar |

## Cálculo

Producido por `CombatStats.GetEffectiveStats(dna, db)`:
1. DNA base (6 campos)
2. Suma acumulativa de partes Body/Arm/Eye/Mouth según tier
3. Mod de equipment aplicado por `EquipmentStats.Apply(baseStats, dna, equipDb)`

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatStats]] — calcula stats base+partes → retorna como `EffectiveStats`
- [[EquipmentStats]] — aplica equipment mods a un `EffectiveStats`
- [[CombatService]] — accede en `BuildCombatant()`

## Conexiones

**Entrada:**
- `CombatStats.GetEffectiveStats()` → retorna nueva instancia
- `EquipmentStats.Apply(EffectiveStats, dna, equipDb)` → mutación funcional (retorna struct nuevo)

**Salida:**
- Pasado a `EquipmentStats.Apply()` para aplicar mods de equipment
- Resultado final usado para inicializar `Combatant.{Attack,Speed,Defense,Luck,Evasion}`

## Notas

- Es `readonly struct`, por lo que es value type y copiable.
- Inmutable tras construcción (ningún método muta).
- No se serializa; es un cálculo en tiempo real.
- **Constitution** se convierte a HP via `Combatant.MaxHp = eff.Constitution * BaseHpCombatMultiplier`.
