---
tags: [script, genetics, breeding-tuning]
---

# InheritanceOddsTableSO.cs

**Ruta:** `Data/Breeding/InheritanceOddsTableSO.cs`

**Responsabilidad:** ScriptableObject Odin con pesos para herencia genética de partes (5 slots) y parámetros de reproducción. Método `Roll()` devuelve un Slot según los pesos normalizados para herencia de partes. S69: Enum `DialSlot` (Average, Copy, Mutation) y pesos para herencia de diales genéticos (Sociability/Boldness). Método `RollDial()` devuelve DialSlot según pesos normalizados. BreedDurationMinutes es solo display (real está basado en GameClock). S39: `ElementMutationChance` controla probabilidad de mutación elemental. S131: Agregado `HatchCostBase` + `HatchCostPerPartLevel` para coste de eclosión en Minerita (método `HatchCost()`).

## Campos Principales

### Herencia de partes (5 slots)

| Campo | Tipo | Propósito |
|-------|------|----------|
| `ParentWeight` | float | Peso relativo para herencia de padres (default 40) |
| `GrandparentWeight` | float | Peso relativo para herencia de abuelos (default 20) |
| `GreatGrandparentWeight` | float | Peso relativo para herencia de bisabuelos (default 10) |
| `MutationWeight` | float | Peso relativo para mutación aleatoria (default 20) |
| `BaseWeight` | float | Peso relativo para fallback pool aleatorio (default 10) |

### Breeding timer

| Campo | Tipo | Propósito |
|-------|------|----------|
| `BreedDurationMinutes` | int | Duración de incubación en minutos de juego (default 360 = 6 horas juego) |

### Coste de eclosión (S131)

| Campo | Tipo | Propósito |
|-------|------|----------|
| `HatchCostBase` | int | Coste base en Minerita para ecloer (default 10) |
| `HatchCostPerPartLevel` | int | Coste adicional por cada level de tier en partes (default 5) |

**Fórmula:**
```
HatchCost = HatchCostBase + HatchCostPerPartLevel * (PartLevels(mother) + PartLevels(father))
PartLevels(dna) = (HornTier - 1) + (BackTier - 1) + (WingTier - 1)
```

Ejemplo: Ambos padres Tier2 en todas partes → PartLevels = 3 cada uno → HatchCost = 10 + 5 * 6 = 40 Minerita.

### Herencia elemental (S39)

| Campo | Tipo | Propósito |
|-------|------|----------|
| `ElementMutationChance` | float | Probabilidad (0–1) de que elemento hijo sea aleatorio vs heredado 50/50 (default 0.10 = 10%) |

### Herencia de diales genéticos (S69)

| Campo | Tipo | Propósito |
|-------|------|----------|
| `DialAverageWeight` | float | Peso relativo: hijo hereda promedio de padres ± jitter (default 50) |
| `DialCopyWeight` | float | Peso relativo: hijo copia exacto de un padre al azar (default 30) |
| `DialMutationWeight` | float | Peso relativo: hijo recibe RandomDial() independiente (default 20) |
| `DialJitter` | float | Varianza (0..0.2) aplicada a Average (default 0.05) |

## Métodos Públicos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `Roll()` | `Slot` | Devuelve un slot (Parent/Grandparent/GreatGrandparent/Mutation/Base) según pesos normalizados |
| `RollDial()` | `DialSlot` | **(S69)** Devuelve un DialSlot (Average/Copy/Mutation) según pesos normalizados |
| `HatchCost(CreatureDNA mother, CreatureDNA father)` | `int` | **(S131)** Calcula coste Minerita para eclosión basado en tiers de partes |

## Enums

```csharp
public enum Slot { Parent, Grandparent, GreatGrandparent, Mutation, Base }
public enum DialSlot { Average, Copy, Mutation }  // S69
```

## Métodos Privados

**PartLevels(CreatureDNA):**
```csharp
private static int PartLevels(CreatureDNA dna) =>
    dna == null ? 0 : ((int)dna.HornTier - 1) + ((int)dna.BackTier - 1) + ((int)dna.WingTier - 1);
```

Suma los niveles (Tier1=0, Tier2=1, Tier3=2) de 3 partes evolucionables.

## Implementación Roll()

```csharp
public Slot Roll()
{
    float total = TotalWeight;
    if (total <= 0f) return Slot.Parent;
    float roll = UnityEngine.Random.Range(0f, total);
    float c = 0f;
    c += ParentWeight;           if (roll < c) return Slot.Parent;
    c += GrandparentWeight;      if (roll < c) return Slot.Grandparent;
    c += GreatGrandparentWeight; if (roll < c) return Slot.GreatGrandparent;
    c += MutationWeight;         if (roll < c) return Slot.Mutation;
    return Slot.Base;
}
```

## Implementación RollDial() (S69)

```csharp
public DialSlot RollDial()
{
    float total = DialAverageWeight + DialCopyWeight + DialMutationWeight;
    if (total <= 0f) return DialSlot.Average;
    float roll = UnityEngine.Random.Range(0f, total);
    if (roll < DialAverageWeight) return DialSlot.Average;
    if (roll < DialAverageWeight + DialCopyWeight) return DialSlot.Copy;
    return DialSlot.Mutation;
}
```

## Herencia de Diales (S69 detalles)

**Average (50% default):** Hijo = (madre + padre) / 2 ± jitter.
**Copy (30% default):** Hijo copia exacto dial de un padre al azar.
**Mutation (20% default):** Hijo recibe `RandomDial()` independiente.

## S131: HatchCost

**Propósito:** Scaleo de coste de eclosión con rareza de padres. Padres Tier1 = coste bajo; Tier3 = coste alto.

**Parámetros tuneables:**
- `HatchCostBase` = piso mínimo
- `HatchCostPerPartLevel` = escalada por tier

**Ejemplo de costes:**
| Padre(s) | Tier | PartLevels | Coste |
|----------|------|-----------|-------|
| Tier1 x Tier1 | 1 | 0 + 0 | 10 |
| Tier2 x Tier1 | 2 | 3 + 0 | 25 |
| Tier2 x Tier2 | 2 | 3 + 3 | 40 |
| Tier3 x Tier3 | 3 | 6 + 6 | 70 |

## Cambios S131

**Nuevo método y campos:**
```csharp
[LabelWidth(190)] public int HatchCostBase = 10;
[LabelWidth(190)] public int HatchCostPerPartLevel = 5;

public int HatchCost(CreatureDNA mother, CreatureDNA father) =>
    HatchCostBase + HatchCostPerPartLevel * (PartLevels(mother) + PartLevels(father));
```

**Integración:** IncubationService consulta este método para deducir Minerita en TryHatch().

## Cambios Históricos

**S39:** Elemental mutation chance.
**S69:** Herencia de diales (Sociability/Boldness).
**S131:** HatchCost para eclosión local con Minerita.

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[Index/28 - Cimientos y camino a Game Ready]]

## Conexiones

**Sistemas:**
- [[BreedingService]] — consulta Roll() para herencia de partes
- [[BreedingController]] — proporciona acceso a este SO
- [[IncubationService]] — consulta HatchCost() para deducir Minerita
- [[CreatureGenerator]] — herencia de diales en breeding

**Data:**
- [[CreatureDNA]] — input para PartLevels y HatchCost

## Notas (S131 HC-4)

- **Tier mapping:** Tier1=0, Tier2=1, Tier3=2 para PartLevels (cálculo simple).
- **HatchCost null-safe:** Si un padre es null, PartLevels retorna 0.
- **Balance:** Coste escala con rareza de padres; incentiva crianza de buenos genéticos.
- **Metadata:** PartLevels cuenta solo partes evolucionables (Horn, Back, Wing), no BodyShape/Face.
