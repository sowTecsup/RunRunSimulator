---
tags: [script, genetics]
---

# CreatureGenerator.cs

**Ruta:** `Core/CreatureGenerator.cs`

**Responsabilidad:** Generador estático de criaturas aleatorias. Crea `CreatureDNA` con partes aleatorias por slot (cada uno con su propio roll de rareza), color base aleatorio via `ColorGenetics.RandomBase()`, color secundario derivado deterministico via `ColorGenetics.DeriveSecondary(baseColor)`, `FurType` aleatorio. `RandomPersonality()` asigna personalidad no heredada (metadata, misma fuente para Mint y Breed). `RandomRole()` asigna rol no heredado (metadata, 1/3 aleatorio). `RandomBaseStats()` genera los 3 stats iniciales (Constitution/Attack/Speed) via point-buy: distribuye `StatBudget` (18 points) entre 3 stats, clampeados a [StatMin..StatMax] (1..10).

## Constantes

| Constante | Valor | Propósito |
|-----------|-------|----------|
| `StatBudget` | 18 | Puntos totales a distribuir entre los 3 stats iniciales. |
| `StatMin` | 1 | Mínimo por stat. |
| `StatMax` | 10 | Máximo por stat. |

## Métodos públicos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `GenerateRandom(CreatureDatabaseSO, RarityOddsTableSO)` | `CreatureDNA` | Partes + color base/secundario + FurType aleatorios (sin stats base). |
| `RandomPersonality()` | `Personality` | Personality aleatoria no heredada (1/6). |
| `RandomRole()` | `Role` | **S37** Role aleatorio no heredado (1/3). |
| `RandomBaseStats()` | `(float, float, float)` | Point-buy: distribuye 18 puntos entre CON/ATK/SPD, cada uno 1–10. |

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

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/13 - Combat Design Direction]]

**Conexiones:** [[CreatureDNA]], [[PartDatabaseSO]], [[RarityOddsTableSO]], [[CreatureNameBank]], [[PersonalityProfileSO]], [[GameManager]], [[ColorGenetics]], [[FurType]], [[Enums]], [[Role]], [[RoleTableSO]]
