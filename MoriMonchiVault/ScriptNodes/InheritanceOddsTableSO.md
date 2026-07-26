---
tags: [script, genetics]
---

# InheritanceOddsTableSO.cs

**Ruta:** `Data/Breeding/InheritanceOddsTableSO.cs`

**Responsabilidad:** ScriptableObject Odin con pesos para herencia genética de partes (5 slots) y parámetros de reproducción. Método `Roll()` devuelve un Slot según los pesos normalizados para herencia de partes. **S69:** Enum `DialSlot` (Average, Copy, Mutation) y pesos `DialAverageWeight`/`DialCopyWeight`/`DialMutationWeight` + `DialJitter` para herencia de diales genéticos (Sociability/Boldness). Método `RollDial()` devuelve DialSlot según pesos normalizados. BreedDurationMinutes es solo display (real está hardcodeado en server). **S39:** Campo `ElementMutationChance` controla probabilidad de que un hijo herede elemento mutado aleatorio vs 50/50 de padres.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[BreedingService]], [[InheritanceOddsTableSO]], [[CreatureGenerator]]

## Campos principales

### Herencia de partes (5 slots)

| Campo | Tipo | Propósito |
|-------|------|----------|
| `ParentWeight` | float | Peso relativo para herencia de padres (default 40). |
| `GrandparentWeight` | float | Peso relativo para herencia de abuelos (default 20). |
| `GreatGrandparentWeight` | float | Peso relativo para herencia de bisabuelos (default 10). |
| `MutationWeight` | float | Peso relativo para mutación aleatoria (default 20). |
| `BaseWeight` | float | Peso relativo para fallback pool aleatorio (default 10). |

### Breeding timer

| Campo | Tipo | Propósito |
|-------|------|----------|
| `BreedDurationMinutes` | int | Display only; servidor tiene valor hardcodeado (default 30). |

### Herencia elemental (S39)

| Campo | Tipo | Propósito |
|-------|------|----------|
| `ElementMutationChance` | float | Probabilidad (0–1) de que elemento hijo sea aleatorio vs heredado 50/50 (default 0.10 = 10%). |

### Herencia de diales genéticos (S69)

| Campo | Tipo | Propósito |
|-------|------|----------|
| `DialAverageWeight` | float | Peso relativo: hijo hereda promedio de padres ± jitter (default 50). |
| `DialCopyWeight` | float | Peso relativo: hijo copia exacto de un padre al azar (default 30). |
| `DialMutationWeight` | float | Peso relativo: hijo recibe RandomDial() independiente (default 20). |
| `DialJitter` | float | Varianza (0..0.2) aplicada a Average: `(m+f)/2 ± Random.Range(-jitter, +jitter)`, luego clamp01 (default 0.05). |

## Enums

```csharp
public enum Slot { Parent, Grandparent, GreatGrandparent, Mutation, Base }
public enum DialSlot { Average, Copy, Mutation }  // S69
```

## Métodos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `Roll()` | `Slot` | Devuelve un slot (Parent/Grandparent/GreatGrandparent/Mutation/Base) según pesos normalizados. Usado para herencia de partes. |
| `RollDial()` | `DialSlot` | **S69** Devuelve un DialSlot (Average/Copy/Mutation) según pesos normalizados. Usado para herencia de Sociability/Boldness. |

## Cambios S69

**Nuevo enum y métodos:**
```csharp
public enum DialSlot { Average, Copy, Mutation }

[LabelWidth(190)] public float DialAverageWeight  = 50f;
[LabelWidth(190)] public float DialCopyWeight     = 30f;
[LabelWidth(190)] public float DialMutationWeight = 20f;
[LabelWidth(190), Range(0f, 0.2f)] public float DialJitter = 0.05f;

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

**Propósito:** Controla herencia de diales genéticos (Sociability/Boldness) en breeding.

**Tres modos de herencia:**
1. **Average (50% default):** Hijo = (madre + padre) / 2 ± jitter (varianza pequeña, Default mode es el más predecible)
2. **Copy (30% default):** Hijo copia exacto dial de un padre al azar (50/50 macho/hembra)
3. **Mutation (20% default):** Hijo recibe `RandomDial()` independiente (sorpresa)

**Jitter:** Pequeña varianza (±0.05 default, rango 0..0.2) aplicada al Average para evitar que siempre sea exactamente (m+f)/2. Resultado final siempre clampeado a [0, 1].

**Consumo:**
- `BreedingService.Breed()` → llama `InheritDial(mother.Sociability, father.Sociability, odds)` (2x, una por dial)
- `InheritDial()` privado en BreedingService aplica el roll de `RollDial()` para elegir herencia

## Cambios S39

**Nuevo campo `ElementMutationChance`:**
```csharp
[LabelWidth(190), Range(0f, 1f)] public float ElementMutationChance = 0.10f;
```

**Propósito:** En BreedingService.Breed(), si `Random.value < ElementMutationChance`, el hijo recibe elemento aleatorio vía `CreatureGenerator.RandomElement()`. De lo contrario, hereda 50/50 de padres.

**Tuning:** Default 10% de chance de mutación elemental. Controlable desde el asset en inspector.

**Uso en BreedingService:**
```csharp
Element = Random.value < odds.ElementMutationChance
    ? CreatureGenerator.RandomElement()
    : (Random.value < 0.5f ? mother.Element : father.Element),
```

## Implementación detallada (S69)

**Heredar Sociability:**
```csharp
child.Sociability = InheritDial(mother.Sociability, father.Sociability, odds);
```

**Heredar Boldness:**
```csharp
child.Boldness = InheritDial(mother.Boldness, father.Boldness, odds);
```

**Lógica de InheritDial (privada en BreedingService):**
```csharp
private static float InheritDial(float motherDial, float fatherDial, InheritanceOddsTableSO odds)
{
    var slot = odds.RollDial();
    
    return slot switch
    {
        InheritanceOddsTableSO.DialSlot.Average =>
            Mathf.Clamp01((motherDial + fatherDial) / 2f + 
                          UnityEngine.Random.Range(-odds.DialJitter, odds.DialJitter)),
        
        InheritanceOddsTableSO.DialSlot.Copy =>
            UnityEngine.Random.value < 0.5f ? motherDial : fatherDial,
        
        InheritanceOddsTableSO.DialSlot.Mutation =>
            CreatureGenerator.RandomDial(),
        
        _ => (motherDial + fatherDial) / 2f
    };
}
```

## Notas

- **Metadata no genética:** Diales (Sociability/Boldness) heredan en breeding pero no son parte del genetic string (`ToStringID()`). Son metadata como Gender/Role/Element.
- **Herencia 50/50 + variance:** Average es el modo default, garantiza hijos cercanos a padres pero no idénticos (gracias a jitter).
- **Copy garantiza herencia exacta:** Si se rolla Copy, el hijo es copia pixel-perfect del dial del padre elegido.
- **Mutation introduce sorpresas:** Inyecta variedad; probabilidad 20% default permite crianza competitiva (un padre puede tener hijo con trait sorpresa).

## Vinculado a

[[Index/02 - Genetics & Breeding]]

## Conexiones

[[BreedingService]], [[CreatureGenerator]], [[CreatureDNA]], [[GameManager]]
