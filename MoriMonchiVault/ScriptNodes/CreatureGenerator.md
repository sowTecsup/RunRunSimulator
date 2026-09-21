---
tags: [script, genetics]
---

# CreatureGenerator

**Ruta:** `Core/CreatureGenerator.cs`

**Responsabilidad:** Generador estático de criaturas aleatorias. Crea `CreatureDNA` con 5 partes aleatorias (Body/Horn/Back/Wing/Face), color base aleatorio, color secundario derivado, FurType aleatorio, IsShiny roll, y potenciales de partes (1-3). **S129:** Eliminada generación de stats base (removidos de DNA).

## Constantes

| Constante | Valor | Propósito |
|-----------|-------|----------|
| `PotentialMin` | 1 | Mínimo potencial de parte |
| `PotentialMax` | 10 | Máximo potencial de parte |
| `MintPotentialMax` | 3 | Máximo al generar (1-3 range) |

## Métodos Públicos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `GenerateRandom(database, furDb)` | `CreatureDNA` | Genera 5 partes aleatorias, colores, FurType, IsShiny, potenciales (1-3 range) |
| `RandomRole()` | `Role` | Role aleatorio (1/3) |
| `RandomElement()` | `Element` | Element aleatorio (1/4) |
| `RandomDial()` | `float` | `Random.Range(0.15f, 0.85f)` para diales (Sociability, Boldness) |
| `RandomMintPotential()` | `int` | Potencial aleatorio [1, 3] |

## GenerateRandom

Genera 5 partes aleatorias, colores derivados, FurType random, IsShiny roll, y potenciales de partes (1-3 range).

**Campos mutados:**
- BodyShapeID, HornID, BackID, WingID, FaceID (aleatorios)
- BaseColor, SecondaryColor (derivados)
- FurType, IsShiny
- HornPotential, BackPotential, WingPotential (1-3)
- Gender (Unknown por defecto)

**No incluye:** stats base (S129 removidos), metadata (role/element/diales) — GameManager los asigna post-generación.

## Cambios S129

- **ELIMINADO:** `RandomBaseStats()` (stats base removidos de CreatureDNA)
- **ELIMINADO:** Generación de Constitution/Attack/Speed/Defense/Luck/Evasion
- **MANTIENE:** Generación de potenciales de partes (1-3 range para evolución HC)

## Vinculado a

[[Index/02 - Genetics & Breeding]]
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[CreatureDNA]], [[CreatureDatabaseSO]], [[GameManager]], [[ColorGenetics]], [[BreedingService]]
