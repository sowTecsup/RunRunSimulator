---
tags: [script, genetics]
---

# BreedingService.cs

**Ruta:** `Systems/Breeding/BreedingService.cs`

**Responsabilidad:** Lógica local de cruce. Hereda 5 partes genéticas (BodyShape/Horn/Back/Wing/Face) desde árbol genealógico, colores, FurType, stats base, género, rol, elemento, IsShiny, diales (Sociability/Boldness). Valida género, muerte, busy state, `MaxBreedCount = 4`. Retorna hijo `CreatureDNA` o null si falla validación.

**S75 CAMBIOS:** Reemplazó 4 partes (Body/Arm/Eye/Mouth) con 5 partes (Body/Horn/Back/Wing/Face). ResolveSlot y switches correspondientes actualizados. Métodos `SlotPartID()` y `RandomPartID()` usan switch con 5 casos PartRole.

## Método principal

```csharp
public static CreatureDNA Breed(
    string                 motherID,
    string                 fatherID,
    CreatureRegistrySO     registry,
    CreatureDatabaseSO     partDb,
    InheritanceOddsTableSO odds)
```

**Validaciones:**
1. Ambos padres existen en registry
2. Ambos vivos (not IsDead)
3. Ninguno ocupado (not IsBusy)
4. Madre Female, padre Male
5. Ambos < MaxBreedCount (4)

**Retorna:** CreatureDNA hijo o null si falla.

## Algoritmo de herencia

### Partes genéticas (S75 - 5 slots)

Heredan del árbol genealógico vía `ResolveSlot(PartRole role, ...)` usando `odds.Roll()`:
- **Parent (40%):** Una parte del padre elegido al azar (50/50 madre/padre)
- **Grandparent (20%):** Una parte de un abuelo
- **GreatGrandparent (10%):** Una parte de bisabuelo
- **Mutation (20%):** Parte aleatoria del pool
- **Base (10%):** Fallback si no hay ancestros

**Slots S75:**
```csharp
BodyShapeID = ResolveSlot(PartRole.Body, motherID, fatherID, registry, partDb, odds),
HornID      = ResolveSlot(PartRole.Horn, motherID, fatherID, registry, partDb, odds),
BackID      = ResolveSlot(PartRole.Back, motherID, fatherID, registry, partDb, odds),
WingID      = ResolveSlot(PartRole.Wing, motherID, fatherID, registry, partDb, odds),
FaceID      = ResolveSlot(PartRole.Face, motherID, fatherID, registry, partDb, odds),
```

**Switches privados S75:**
```csharp
// Extrae ID de parte según PartRole
private static string SlotPartID(CreatureDNA dna, PartRole role) => role switch
{
    PartRole.Body => dna.BodyShapeID,
    PartRole.Horn => dna.HornID,
    PartRole.Back => dna.BackID,
    PartRole.Wing => dna.WingID,
    PartRole.Face => dna.FaceID,
    _             => ""
};

// Resuelve ID aleatorio según PartRole desde DB
private static string RandomPartID(PartRole role, CreatureDatabaseSO partDb) => role switch
{
    PartRole.Body => partDb.BodyShapes?.GetRandomPart()?.ID ?? "",
    PartRole.Horn => partDb.Horns?.GetRandomPart()?.ID      ?? "",
    PartRole.Back => partDb.Backs?.GetRandomPart()?.ID      ?? "",
    PartRole.Wing => partDb.Wings?.GetRandomPart()?.ID      ?? "",
    PartRole.Face => partDb.Faces?.GetRandomPart()?.ID      ?? "",
    _             => ""
};
```

### Otros campos

- **Colores:** BaseColor heredado + SecondaryColor derivado
- **Género:** 50/50
- **Role:** 50/50 de padres
- **Element:** 50/50 de padres + mutación (ElementMutationChance)
- **IsShiny:** Roll nuevo 0.5%
- **FurType:** 50/50 de padres
- **Stats base:** Promedio ± jitter
- **Diales:** Sociability/Boldness con 3 modos (Average/Copy/Mutation)

## Vinculado a

- [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[InheritanceOddsTableSO]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[ColorGenetics]], [[CreatureGenerator]], [[GeneticsEnums]]
