---
tags: [script, genetics]
---

# CreatureGenerator.cs

**Ruta:** `Core/CreatureGenerator.cs`

**Responsabilidad:** Generador estático de criaturas aleatorias. Crea `CreatureDNA` con 5 partes aleatorias (Body/Horn/Back/Wing/Face), color base aleatorio, color secundario derivado, FurType aleatorio, IsShiny roll. Métodos para rol/elemento/diales aleatorios (metadata). Point-buy de stats base (Constitution/Attack/Speed). **S95** Genera potenciales de combate (HornPotential/BackPotential/WingPotential) en GenerateRandom vía RandomMintPotential().

## Constantes

| Constante | Valor | Propósito |
|-----------|-------|----------|
| `StatBudget` | 18 | Puntos totales a distribuir entre los 3 stats iniciales. |
| `StatMin` | 1 | Mínimo por stat. |
| `StatMax` | 10 | Máximo por stat. |
| `PotentialMin` | 1 | Mínimo potencial de combate |
| `PotentialMax` | 10 | Máximo potencial de combate |
| `MintPotentialMax` | 3 | Máximo potencial para criaturas generadas (1-3 range) |

## Métodos públicos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `GenerateRandom(CreatureDatabaseSO, FurTypeDatabaseSO)` | `CreatureDNA` | **S75** Genera 5 partes aleatorias (Body/Horn/Back/Wing/Face), colores, FurType, IsShiny. **S95** Genera 3 potenciales (1-3 range) vía RandomMintPotential(). Sin stats base ni metadata (los asigna GameManager). |
| `RandomRole()` | `Role` | Role aleatorio (1/3). |
| `RandomElement()` | `Element` | Element aleatorio (1/4). |
| `RandomDial()` | `float` | `Random.Range(0.15f, 0.85f)` para diales (Sociability/Boldness). |
| `RandomMintPotential()` | `int` | **S95** Genera potencial aleatorio en rango [PotentialMin, MintPotentialMax] = [1, 3] |
| `RandomBaseStats()` | `(float, float, float)` | Point-buy: distribuye 18 puntos entre CON/ATK/SPD. |

## GenerateRandom (S75 + S95)

**S75 ACTUALIZADO:** Reemplazó Arm/Eye/Mouth con Horn/Back/Wing/Face.

**S95 ACTUALIZADO:** Genera potenciales de combate (1-3 range mint).

```csharp
public static CreatureDNA GenerateRandom(CreatureDatabaseSO database, FurTypeDatabaseSO furDb = null)
{
    var bodyShape = Pick(database.BodyShapes);
    var horn      = Pick(database.Horns);
    var back      = Pick(database.Backs);
    var wing      = Pick(database.Wings);
    var face      = Pick(database.Faces);

    if (bodyShape == null || horn == null || back == null || wing == null || face == null)
        Debug.LogWarning("[CreatureGenerator] One or more part slots are empty.");

    return new CreatureDNA
    {
        BodyShapeID  = bodyShape?.ID ?? "",
        HornID       = horn?.ID ?? "",
        BackID       = back?.ID ?? "",
        WingID       = wing?.ID ?? "",
        FaceID       = face?.ID ?? "",
        BaseColor      = ColorGenetics.RandomBase(),
        SecondaryColor = ColorGenetics.DeriveSecondary(baseColor),
        FurType        = furDb != null ? furDb.RollMintFurType() : random uniform,
        IsShiny        = ColorGenetics.RollShiny(),
        HornPotential  = RandomMintPotential(),     // S95
        BackPotential  = RandomMintPotential(),     // S95
        WingPotential  = RandomMintPotential(),     // S95
    };
}
```

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[CreatureDNA]], [[CreatureDatabaseSO]], [[GameManager]], [[ColorGenetics]], [[BreedingService]], [[DragonRpsGenes]]

