---
tags: [script, genetics]
---

# CreatureGenerator.cs

**Ruta:** `Core/CreatureGenerator.cs`

**Responsabilidad:** Generador estático de criaturas aleatorias. Crea `CreatureDNA` con 5 partes aleatorias (Body/Horn/Back/Wing/Face), color base aleatorio, color secundario derivado, FurType aleatorio, IsShiny roll. Métodos para rol/elemento/diales aleatorios (metadata). Point-buy de stats base (Constitution/Attack/Speed).

## Constantes

| Constante | Valor | Propósito |
|-----------|-------|----------|
| `StatBudget` | 18 | Puntos totales a distribuir entre los 3 stats iniciales. |
| `StatMin` | 1 | Mínimo por stat. |
| `StatMax` | 10 | Máximo por stat. |

## Métodos públicos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `GenerateRandom(CreatureDatabaseSO, FurTypeDatabaseSO)` | `CreatureDNA` | **S75** Genera 5 partes aleatorias (Body/Horn/Back/Wing/Face), colores, FurType, IsShiny. Sin stats base ni metadata (los asigna GameManager). |
| `RandomRole()` | `Role` | Role aleatorio (1/3). |
| `RandomElement()` | `Element` | Element aleatorio (1/4). |
| `RandomDial()` | `float` | `Random.Range(0.15f, 0.85f)` para diales (Sociability/Boldness). |
| `RandomBaseStats()` | `(float, float, float)` | Point-buy: distribuye 18 puntos entre CON/ATK/SPD. |

## GenerateRandom (S75)

**S75 ACTUALIZADO:** Reemplazó Arm/Eye/Mouth con Horn/Back/Wing/Face.

```csharp
public static CreatureDNA GenerateRandom(CreatureDatabaseSO database, FurTypeDatabaseSO furDb = null)
{
    var bodyShape = Pick(database.BodyShapes);
    var horn      = Pick(database.Horns);         // NUEVO S75
    var back      = Pick(database.Backs);         // NUEVO S75
    var wing      = Pick(database.Wings);         // NUEVO S75
    var face      = Pick(database.Faces);         // NUEVO S75

    if (bodyShape == null || horn == null || back == null || wing == null || face == null)
        Debug.LogWarning("[CreatureGenerator] One or more part slots are empty — ensure all databases are populated.");

    return new CreatureDNA
    {
        BodyShapeID  = bodyShape?.ID ?? "",
        HornID       = horn?.ID       ?? "",       // NUEVO S75
        BackID       = back?.ID       ?? "",       // NUEVO S75
        WingID       = wing?.ID       ?? "",       // NUEVO S75
        FaceID       = face?.ID       ?? "",       // NUEVO S75
        BaseColor      = ColorGenetics.RandomBase(),
        SecondaryColor = ColorGenetics.DeriveSecondary(baseColor),
        FurType        = furDb != null ? furDb.RollMintFurType() : random uniform,
        IsShiny        = ColorGenetics.RollShiny(),
    };
}
```

## Vinculado a

- [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[CreatureDatabaseSO]], [[GameManager]], [[ColorGenetics]], [[BreedingService]]
