---
tags: [script, genetics]
---

# InheritanceOddsTableSO.cs

**Ruta:** `Data/Breeding/InheritanceOddsTableSO.cs`

**Responsabilidad:** 5 slots (Parent, Grandparent, GreatGrandparent, Mutation, Base) con pesos para herencia genética de partes. SerializedScriptableObject sin `static Current`; lo posee BreedingController, accedible vía `BreedingController.Instance.InheritanceOdds`. Método `Roll()` devuelve un Slot según los pesos normalizados. BreedDurationMinutes es solo display (real está hardcodeado en server). **S39:** Nuevo campo `ElementMutationChance` (0–1) controla probabilidad de que un hijo herede elemento mutado aleatorio vs 50/50 de padres.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[BreedingController]], [[BreedingService]], [[InheritanceOddsTableSO]]

## Campos principales

| Campo | Tipo | Propósito |
|-------|------|----------|
| `ParentWeight` | float | Peso relativo para herencia de padres (default 40). |
| `GrandparentWeight` | float | Peso relativo para herencia de abuelos (default 20). |
| `GreatGrandparentWeight` | float | Peso relativo para herencia de bisabuelos (default 10). |
| `MutationWeight` | float | Peso relativo para mutación aleatoria (default 20). |
| `BaseWeight` | float | Peso relativo para fallback pool aleatorio (default 10). |
| `BreedDurationMinutes` | int | Display only; servidor tiene valor hardcodeado (default 30). |
| `ElementMutationChance` | float | **S39** Probabilidad (0–1) de que elemento hijo sea aleatorio vs heredado 50/50 (default 0.10 = 10%). |

## Métodos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `Roll()` | `Slot` | Devuelve un slot (Parent/Grandparent/GreatGrandparent/Mutation/Base) según pesos normalizados. |

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
