---
tags: [script, genetics]
---

# BreedingService.cs

**Ruta:** `Systems/Breeding/BreedingService.cs`

**Responsabilidad:** Lógica local de cruce. Hereda partes desde el árbol genealógico (padres/abuelos/bisabuelos + mutación aleatoria), colores base via `ColorGenetics.Inherit(motherColor, fatherColor)` + color secundario derivado deterministico via `ColorGenetics.DeriveSecondary(childBase)`, `FurType` 50/50, stats base (Constitution/Attack/Speed) via `InheritStat()` que promedia padres y clampea a [StatMin..StatMax], género aleatorio, rol heredado 50/50 de padres vía herencia directa (si solo 1 padre, se hereda; si 0 padres, aleatorio), **S39:** elemento heredado 50/50 de padres con chance de mutación vía `ElementMutationChance` en `InheritanceOddsTableSO`, **S57:** `IsShiny` roll 0.5% via `ColorGenetics.RollShiny()` (cada hijo nuevo roll independiente, no hereda de padres). Valida género, muerte, busy state, `MaxBreedCount` (4). Valida herencia genealógica desde padres hasta bisabuelos; fallback a pool aleatorio si no hay ancestros.

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/03 - Combat System]], [[Index/13 - Combat Design Direction]]

**Conexiones:** [[CreatureDNA]], [[InheritanceOddsTableSO]], [[BreedingAffinityTableSO]], [[GameEvents]], [[BreedingContainer]], [[BreedingController]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[ColorGenetics]], [[FurType]], [[CreatureGenerator]], [[Enums]], [[Role]], [[Element]], [[ElementalState]], [[RoleWorldProfileSO]]

## Cambios S37

**Herencia de Role:**
```csharp
Role = Random.value < 0.5f ? mother.Role : father.Role,
```

**Metadata:** Role es metadata (no genético), como Gender. Se hereda en breeding pero NO es parte del string genético. Herencia 50/50 simétrica.

## Cambios S39

**Herencia de Element con mutación:**
```csharp
Element = Random.value < odds.ElementMutationChance
    ? CreatureGenerator.RandomElement()
    : (Random.value < 0.5f ? mother.Element : father.Element),
```

**Propósito:** Element hereda 50/50 de padres, pero con probabilidad de mutación definida en `InheritanceOddsTableSO.ElementMutationChance`. Si muta, se asigna elemento aleatorio vía `CreatureGenerator.RandomElement()`.

**Metadata:** Element es metadata (no genético), como Gender/Role. Se hereda en breeding pero NO es parte del string genético.

## Cambios S57

**IsShiny roll al nacer:**
```csharp
IsShiny = ColorGenetics.RollShiny(),  // 0.5% new roll, independent per child
```

**Propósito:** Cada hijo recibe roll independiente 0.5% (no hereda shiny de padres). Tres criaturas shinys pueden tener hijo no-shiny y viceversa.

**Metadata:** IsShiny es metadata (no genético), puramente cosmético. No parte del string genético.

## Algoritmo de Herencia (S57/S39/S37)

1. **Partes:** Árbol genealógico (bisabuelos → mutación)
2. **Colores:** Interpolación determinista de padres
3. **FurType:** 50/50 padres
4. **IsShiny:** Roll independiente 0.5% (S57)
5. **Stats:** Promedio de padres, clampeo
6. **Género:** Aleatorio (no heredado)
7. **Role:** 50/50 padres (no genético, S37)
8. **Element:** 50/50 padres con chance de mutación (no genético, S39)

## Notas

- **Metadatas no genéticas:** Género, Role (S37), Element (S39), IsShiny (S57) se asignan/heredan independientemente del genetic string. No contribuyen a la visual del creature (salvo IsShiny que es cosmético).
- **Herencia 50/50:** Role, Element, FurType heredan simétricamente (no diferencia macho/hembra).
- **IsShiny NO hereditario:** Cada hijo obtiene roll nuevo; shiny de padres no afecta. Rarity independiente.
- **Mutación elemental:** InheritanceOddsTableSO.ElementMutationChance controla probabilidad de que el hijo tenga elemento aleatorio vs heredado.
- **Impacto gameplay:** Rol heredado significa que un Protector + Agresivo pueden tener hijo Protector o Agresivo (50/50). Element heredado proporciona sabor táctico elemental adicional, con oportunidad de mutación. IsShiny es cosmético puro.
