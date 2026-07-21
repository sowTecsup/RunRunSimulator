---
tags: [script, genetics]
---

# CreatureGenerator.cs

**Ruta:** `Core/CreatureGenerator.cs`

**Responsabilidad:** Generador estático de criaturas aleatorias. Crea `CreatureDNA` con partes aleatorias por slot (cada uno con su propio roll de rareza), color base aleatorio via `ColorGenetics.RandomBase()`, color secundario derivado deterministico via `ColorGenetics.DeriveSecondary(baseColor)`, `FurType` aleatorio (ponderado si se pasa `FurTypeDatabaseSO`, uniform si null). `RandomRole()` asigna rol no heredado (metadata, 1/3 aleatorio). `RandomElement()` asigna elemento no heredado (metadata, 1/4 aleatorio) para mint. `RandomBaseStats()` genera los 3 stats iniciales (Constitution/Attack/Speed) via point-buy: distribuye `StatBudget` (18 points) entre 3 stats, clampeados a [StatMin..StatMax] (1..10).

## Constantes

| Constante | Valor | Propósito |
|-----------|-------|----------|
| `StatBudget` | 18 | Puntos totales a distribuir entre los 3 stats iniciales. |
| `StatMin` | 1 | Mínimo por stat. |
| `StatMax` | 10 | Máximo por stat. |

## Métodos públicos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `GenerateRandom(CreatureDatabaseSO, RarityOddsTableSO, FurTypeDatabaseSO)` | `CreatureDNA` | Partes + color base/secundario + FurType aleatorios + IsShiny roll (sin stats base). |
| `RandomRole()` | `Role` | **S37** Role aleatorio no heredado (1/3). |
| `RandomElement()` | `Element` | **S39** Element aleatorio no heredado (1/4). |
| `RandomBaseStats()` | `(float, float, float)` | Point-buy: distribuye 18 puntos entre CON/ATK/SPD, cada uno 1–10. |

## Cambios S57

**Actualizado `GenerateRandom()`:**
- Parámetro nuevo opcional `FurTypeDatabaseSO furDb`
- Si `furDb != null`: llama `furDb.RollMintFurType()` para FurType ponderado (mintWeights)
- Si `furDb == null`: FurType aleatorio uniforme (fallback legacy)
- Llama `ColorGenetics.RollShiny()` para `IsShiny` (0.5% probabilidad)
- Retorna DNA con `IsShiny` y `FurType` seteados

**Consumo:**
- `GameManager.MintRandomCreature()` → llama `GenerateRandom(database, rarityOddsTable, furTypeDatabase)` con bank completo

**Impacto:** S57 — mint ahora es ponderado por tabla de pesos de FurType; 0.5% de criaturas nuevas (mint + breeding) serán shiny.

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

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/03 - Combat System]], [[Index/13 - Combat Design Direction]]

**Conexiones:** [[CreatureDNA]], [[PartDatabaseSO]], [[RarityOddsTableSO]], [[GameManager]], [[ColorGenetics]], [[FurType]], [[Enums]], [[Role]], [[Element]], [[BreedingService]], [[FurTypeDatabaseSO]]
