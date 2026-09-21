---
tags: [script, genetics]
---

# CreatureGenerator

**Ruta:** `Core/CreatureGenerator.cs`

**Responsabilidad:** Generador estático de criaturas aleatorias. Crea `CreatureDNA` con 5 partes aleatorias (Body/Horn/Back/Wing/Face), color base aleatorio, color secundario derivado, FurType aleatorio, IsShiny roll. Métodos para rol/elemento/diales aleatorios (metadata). Point-buy de stats base.

## Constantes

| Constante | Valor | Propósito |
|-----------|-------|----------|
| `StatBudget` | 18 | Puntos totales a distribuir entre 3 stats |
| `StatMin` | 1 | Mínimo por stat |
| `StatMax` | 10 | Máximo por stat |
| `PotentialMin` | 1 | Mínimo potencial de parte |
| `PotentialMax` | 10 | Máximo potencial de parte |
| `MintPotentialMax` | 3 | Máximo al generar (1-3 range) |

## Métodos Públicos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `GenerateRandom(database, furDb)` | `CreatureDNA` | Genera 5 partes aleatorias, colores, FurType, IsShiny, potenciales (1-3 range) |
| `RandomRole()` | `Role` | Role aleatorio (1/3) |
| `RandomElement()` | `Element` | Element aleatorio (1/4) |
| `RandomDial()` | `float` | `Random.Range(0.15f, 0.85f)` para diales |
| `RandomMintPotential()` | `int` | Potencial aleatorio [1, 3] |
| `RandomBaseStats()` | `(float, float, float)` | Point-buy: distribuye 18 puntos entre CON/ATK/SPD |

## GenerateRandom

Genera 5 partes aleatorias, colores derivados, FurType random, IsShiny roll, y potenciales de partes (1-3 range).

**Campos mutados:**
- BodyShapeID, HornID, BackID, WingID, FaceID
- BaseColor, SecondaryColor
- FurType, IsShiny
- HornPotential, BackPotential, WingPotential (1-3)

**No incluye:** stats base, metadata (role/element/diales) — GameManager los asigna post-generación.

## Vinculado a

[[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[CreatureDatabaseSO]], [[GameManager]], [[ColorGenetics]], [[BreedingService]]

