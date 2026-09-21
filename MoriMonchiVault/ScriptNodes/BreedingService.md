---
tags: [script, genetics]
---

# BreedingService

**Ruta:** `Systems/Breeding/BreedingService.cs`

**Responsabilidad:** Lógica local de cruce. Hereda 5 partes genéticas (BodyShape/Horn/Back/Wing/Face) desde árbol genealógico, colores, FurType, género, rol, elemento, IsShiny, diales (Sociability/Boldness), y potenciales de partes. Valida género, muerte, busy state, `MaxBreedCount = 4`. Retorna hijo `CreatureDNA` o null si falla validación. **S129:** Eliminada herencia de stats base.

## Método Principal

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

## Algoritmo de Herencia

### Partes Genéticas (5 slots)

Heredan del árbol genealógico vía `ResolveSlot(PartRole role, ...)` usando `odds.Roll()`:
- **Parent (40%):** Una parte del padre elegido al azar (50/50 madre/padre)
- **Grandparent (20%):** Una parte de un abuelo
- **GreatGrandparent (10%):** Una parte de bisabuelo
- **Mutation (20%):** Parte aleatoria del pool
- **Base (10%):** Fallback si no hay ancestros

**Slots:**
```
BodyShapeID, HornID, BackID, WingID, FaceID
```

### Otros Campos

- **Colores:** BaseColor heredado + SecondaryColor derivado
- **Género:** 50/50
- **Role:** 50/50 de padres
- **Element:** 50/50 de padres + mutación
- **IsShiny:** Roll nuevo 0.5%
- **FurType:** 50/50 de padres
- **Potenciales de partes:** Promedio ± 1, clamp 1-10
- **Diales:** Sociability/Boldness con 3 modos

## Cambios S129

- **ELIMINADO:** Herencia de stats base (Constitution, Attack, Speed, Defense, Luck, Evasion)
- **MANTIENE:** 5 partes genéticas, potenciales, diales

## Vinculado a

[[Index/02 - Genetics & Breeding]]
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[CreatureDNA]], [[InheritanceOddsTableSO]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[ColorGenetics]], [[CreatureGenerator]]
