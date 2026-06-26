---
tags: [script, genetics]
---

# CreatureGenerator.cs

**Ruta:** `Core/CreatureGenerator.cs`

**Responsabilidad:** Generador estático de criaturas aleatorias. Crea `CreatureDNA` con partes aleatorias por slot (cada uno con su propio roll de rareza), color base aleatorio via `ColorGenetics.RandomBase()`, color secundario derivado deterministico via `ColorGenetics.DeriveSecondary(baseColor)`, `FurType` aleatorio. `RandomPersonality()` asigna personalidad no heredada (metadata, misma fuente para Mint y Breed). `RandomBaseStats()` genera los 3 stats iniciales (Constitution/Attack/Speed) via point-buy: distribuye `StatBudget` (18 points) entre 3 stats, clampeados a [StatMin..StatMax] (1..10).

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
| `RandomPersonality()` | `Personality` | Personality aleatoria no heredada. |
| `RandomBaseStats()` | `(float, float, float)` | Point-buy: distribuye 18 puntos entre CON/ATK/SPD, cada uno 1–10. |

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[PartDatabaseSO]], [[RarityOddsTableSO]], [[CreatureNameBank]], [[PersonalityProfileSO]], [[GameManager]], [[ColorGenetics]], [[FurType]], [[Enums]]
