---
tags: [script, genetics]
---

# CreatureGenerator.cs

**Ruta:** `Core/CreatureGenerator.cs`

**Responsabilidad:** Generador estático de criaturas aleatorias. Crea `CreatureDNA` con partes aleatorias por slot (cada uno con su propio roll uniforme), color base aleatorio via `ColorGenetics.RandomBase()`, color secundario derivado deterministico via `ColorGenetics.DeriveSecondary(baseColor)`, `FurType` aleatorio (ponderado si se pasa `FurTypeDatabaseSO`, uniform si null). `RandomRole()` asigna rol no heredado (metadata, 1/3 aleatorio). `RandomElement()` asigna elemento no heredado (metadata, 1/4 aleatorio) para mint. `RandomBaseStats()` genera los 3 stats iniciales (Constitution/Attack/Speed) via point-buy: distribuye `StatBudget` (18 points) entre 3 stats, clampeados a [StatMin..StatMax] (1..10).

## Constantes

| Constante | Valor | Propósito |
|-----------|-------|----------|
| `StatBudget` | 18 | Puntos totales a distribuir entre los 3 stats iniciales. |
| `StatMin` | 1 | Mínimo por stat. |
| `StatMax` | 10 | Máximo por stat. |

## Métodos públicos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `GenerateRandom(CreatureDatabaseSO, FurTypeDatabaseSO)` | `CreatureDNA` | **S61** Partes uniforme + color base/secundario + FurType aleatorio (ponderado si furDb != null, uniforme si null) + IsShiny roll (sin stats base). Firma simplificada: eliminado `RarityOddsTableSO` (rareza reservada para gemas). |
| `RandomRole()` | `Role` | **S37** Role aleatorio no heredado (1/3). |
| `RandomElement()` | `Element` | **S39** Element aleatorio no heredado (1/4). |
| `RandomBaseStats()` | `(float, float, float)` | Point-buy: distribuye 18 puntos entre CON/ATK/SPD, cada uno 1–10. |

## Cambios S61

**GenerateRandom() firma actualizada:**
```csharp
public static CreatureDNA GenerateRandom(CreatureDatabaseSO database, FurTypeDatabaseSO furDb = null)
{
    // ... Pick<T>(db.BodyShapes) etc. → uniforme (sin rarity filter)
    var furValues = System.Enum.GetValues(typeof(FurType));
    
    return new CreatureDNA
    {
        BodyShapeID  = bodyShape?.ID ?? "",
        ArmID        = arm?.ID       ?? "",
        EyeID        = eye?.ID       ?? "",
        MouthID      = mouth?.ID     ?? "",
        BaseColor      = baseColor,
        SecondaryColor = ColorGenetics.DeriveSecondary(baseColor),
        FurType        = furDb != null ? furDb.RollMintFurType() : (FurType)furValues.GetValue(Random.Range(0, furValues.Length)),
        IsShiny        = ColorGenetics.RollShiny(),
    };
}
```

**Cambios principales:**
- **Eliminado parámetro `RarityOddsTableSO oddsTable`** — firma anterior: `GenerateRandom(CreatureDatabaseSO, RarityOddsTableSO, FurTypeDatabaseSO)`
- **Partes ahora uniform** — `Pick<T>(db)` llama `GetRandomPart()` sin filtro de rareza
- **Decisión de diseño:** Rareza por parte es irrelevante en mint (todas las partes spawn con igual probabilidad). Rareza reservada para gemas (IsShiny future).
- **FurType decision:** ponderado si `furDb` != null (consulta `furDb.RollMintFurType()`), uniforme fallback si null

**Consumo:**
- `GameManager.MintRandomCreature()` → llama `GenerateRandom(database, furTypeDatabase)` (sin rarityOddsTable)
- `GeneticsLabPreview.GenerateRandomCreature()` → llama `GenerateRandom(gameManager.Database)` (sin odds, sin fur type)

**Impacto S61:**
- Simplificación: una sola ruta de generación, sin branch de rareza
- Mint es ahora 100% uniforme por parte (si se pasa null, fur type también uniforme)
- RarityOddsTable en GameManager sigue serializado (reserva para gemas futuro)

## Cambios S57

**FurType ponderado en mint:**
- Si `furDb != null`: llama `furDb.RollMintFurType()` para FurType ponderado (mintWeights)
- Si `furDb == null`: FurType aleatorio uniforme (fallback legacy)
- Llama `ColorGenetics.RollShiny()` para `IsShiny` (0.5% probabilidad)
- Retorna DNA con `IsShiny` y `FurType` seteados

## Cambios S37

**Nuevo método `RandomRole()`:**
```csharp
public static Role RandomRole()
{
    var values = System.Enum.GetValues(typeof(Role));
    return (Role)values.GetValue(Random.Range(0, values.Length));
}
```

**Propósito:** Asigna rol aleatorio 1/3 (Protector, Agresivo, Empático) en mint. En breeding, el rol hereda 50/50 de padres vía `BreedingService` (NOT vía CreatureGenerator).

**Metadata:** Role es metadata (no genético), como Gender/Personality. Se asigna al azar en mint; se hereda en breeding por otra ruta (BreedingService roll 50/50 de padres).

**Consumo:**
- `GameManager.MintRandomCreature()` → llama `GenerateRandom()` + asigna stats base + **llama `RandomRole()`** → popula `Dna.Role`
- `BreedingService.Breed()` → hereda role 50/50 de padres (no llama RandomRole)

## Cambios S39

**Nuevo método `RandomElement()`:**
```csharp
public static Element RandomElement()
{
    var values = System.Enum.GetValues(typeof(Element));
    return (Element)values.GetValue(Random.Range(0, values.Length));
}
```

**Propósito:** Asigna afinidad elemental aleatoria 1/4 (Agua, Fuego, Electricidad, Planta) en mint. En breeding, el elemento hereda 50/50 de padres vía `BreedingService` con chance de mutación.

**Metadata:** Element es metadata (no genético), como Gender/Role. Se asigna al azar en mint; se hereda en breeding con chance de mutación.

**Consumo:**
- `GameManager.MintRandomCreature()` → llama `GenerateRandom()` + **llama `RandomElement()`** → popula `Dna.Element`
- `BreedingService.Breed()` → hereda element 50/50 de padres con mutación (no llama RandomElement)

## Vinculado a

[[Index/02 - Genetics & Breeding]], [[Index/03 - Combat System]], [[Index/13 - Combat Design Direction]]

## Conexiones

[[CreatureDNA]], [[PartDatabaseSO]], [[GameManager]], [[ColorGenetics]], [[FurType]], [[Enums]], [[Role]], [[Element]], [[BreedingService]], [[FurTypeDatabaseSO]]
