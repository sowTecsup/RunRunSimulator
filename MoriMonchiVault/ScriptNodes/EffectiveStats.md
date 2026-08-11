---
tags: [data, stats, readonly]
---

# EffectiveStats

**Ruta:** `Data/Genetics/EffectiveStats.cs`

**Responsabilidad:** Struct readonly que almacena los 6 stats finales de una criatura (CON, ATK, SPD, DEF, LCK, EVA), derivados de DNA base + acumulación de partes. Inmutable una vez construido.

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
| `Constitution` | `float` | Resistencia base (HP virtual) |
| `Attack` | `float` | Daño ofensivo |
| `Speed` | `float` | Velocidad / agilidad |
| `Defense` | `float` | Resistencia defensiva |
| `Luck` | `float` | Probabilidad de eventos favorables |
| `Evasion` | `float` | Chance de esquivar |

## Cálculo

Producido por `CreatureStats.GetEffectiveStats(dna, db)`:
1. DNA base (CON, ATK, SPD de `BaseConstitution`, `BaseAttack`, `BaseSpeed`)
2. Suma acumulativa de bonificadores por partes (BodyShape, Horn, Back, Wing) según tier
3. DEF, LCK, EVA provenientes de DNA base sin acumulación de partes

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[CreatureStats]] — calcula stats base+partes → retorna como `EffectiveStats`
- [[EquipmentStats]] — aplica equipment mods a un `EffectiveStats`

## Conexiones

**Entrada:**
- `CreatureStats.GetEffectiveStats(dna, db)` → retorna nueva instancia

**Salida:**
- Pasado a `EquipmentStats.Apply()` para aplicar mods de equipment
- Accedida por UI y sistemas de display de stats

## Notas

- Es `readonly struct`, por lo que es value type y copiable.
- Inmutable tras construcción (ningún método muta).
- No se serializa; es un cálculo en tiempo real.
- DEF/LCK/EVA NO se acumulan de partes; vienen íntegros del DNA base.
