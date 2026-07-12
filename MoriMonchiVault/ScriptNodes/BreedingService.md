---
tags: [script, genetics]
---

# BreedingService.cs

**Ruta:** `Systems/Breeding/BreedingService.cs`

**Responsabilidad:** Lógica local de cruce. Hereda partes desde el árbol genealógico (padres/abuelos/bisabuelos + mutación aleatoria), colores base via `ColorGenetics.Inherit(motherColor, fatherColor)` + color secundario derivado deterministico via `ColorGenetics.DeriveSecondary(childBase)`, `FurType` 50/50, stats base (Constitution/Attack/Speed) via `InheritStat()` que promedia padres y clampea a [StatMin..StatMax], género aleatorio, rol heredado 50/50 de padres vía herencia directa (si solo 1 padre, se hereda; si 0 padres, aleatorio), **S39:** elemento heredado 50/50 de padres con chance de mutación vía `ElementMutationChance` en `InheritanceOddsTableSO`. Valida género, muerte, busy state, `MaxBreedCount` (4). Valida herencia genealógica desde padres hasta bisabuelos; fallback a pool aleatorio si no hay ancestros.

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

## Algoritmo de Herencia (S37/S39)

1. **Partes:** Árbol genealógico (bisabuelos → mutación)
2. **Colores:** Interpolación determinista de padres
3. **FurType:** 50/50 padres
4. **Stats:** Promedio de padres, clampeo
5. **Género:** Aleatorio (no heredado)
6. **Role:** 50/50 padres (no genético, S37)
7. **Element:** 50/50 padres con chance de mutación (no genético, S39)

## Notas

- **Metadatas no genéticas:** Género, Role (S37), Element (S39) se asignan/heredan independientemente del genetic string. No contribuyen a la visual del creature.
- **Herencia 50/50:** Role y Element heredan simétricamente (no diferencia macho/hembra), igual que FurType.
- **Mutación elemental:** InheritanceOddsTableSO.ElementMutationChance controla probabilidad de que el hijo tenga elemento aleatorio vs heredado.
- **Impacto gameplay:** Rol heredado significa que un Protector + Agresivo pueden tener hijo Protector o Agresivo (50/50). Element heredado proporciona sabor táctico elemental adicional, con oportunidad de mutación.
