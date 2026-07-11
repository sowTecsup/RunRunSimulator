---
tags: [script, genetics]
---

# BreedingService.cs

**Ruta:** `Systems/Breeding/BreedingService.cs`

**Responsabilidad:** Lógica local de cruce. Hereda partes desde el árbol genealógico (padres/abuelos/bisabuelos + mutación aleatoria), colores base via `ColorGenetics.Inherit(motherColor, fatherColor)` + color secundario derivado deterministico via `ColorGenetics.DeriveSecondary(childBase)`, `FurType` 50/50, stats base (Constitution/Attack/Speed) via `InheritStat()` que promedia padres y clampea a [StatMin..StatMax], personalidad no heredada via `CreatureGenerator.RandomPersonality()`, **S37:** rol heredado 50/50 de padres via `InheritRole()` (si solo 1 padre, se hereda; si 0 padres, aleatorio). Valida género, muerte, busy state, `MaxBreedCount` (4). Valida herencia genealógica desde padres hasta bisabuelos; fallback a pool aleatorio si no hay ancestros.

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/13 - Combat Design Direction]]

**Conexiones:** [[CreatureDNA]], [[InheritanceOddsTableSO]], [[BreedingAffinityTableSO]], [[GameEvents]], [[BreedingContainer]], [[BreedingController]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[ColorGenetics]], [[FurType]], [[CreatureGenerator]], [[Enums]], [[Role]], [[RoleTableSO]]

## Cambios S37

**Nuevo método `InheritRole()`:**
```csharp
private static Role InheritRole(CreatureDNA mother, CreatureDNA father)
{
    if (mother != null && father != null)
    {
        // 50/50 de los padres
        return Random.value < 0.5f ? mother.Role : father.Role;
    }
    else if (mother != null)
    {
        return mother.Role;  // Solo madre, hereda su rol
    }
    else if (father != null)
    {
        return father.Role;  // Solo padre, hereda su rol
    }
    else
    {
        return CreatureGenerator.RandomRole();  // Sin padres, aleatorio
    }
}
```

**Integración en `Breed()`:**
```csharp
var babyDna = new CreatureDNA
{
    // ... partes, colores, FurType, stats, personalidad ...
    Personality = CreatureGenerator.RandomPersonality(),  // No heredada
    Role = InheritRole(mother, father),  // **S37 NEW** Hereda 50/50 o aleatorio
    // ... resto de campos ...
};
```

**Metadata:** Role es metadata (no genético), como Gender/Personality. Se hereda en breeding pero NO es parte del string genético.

## Algoritmo de Herencia (S37)

1. **Partes:** Árbol genealógico (bisabuelos → mutación)
2. **Colores:** Interpolación determinista de padres
3. **FurType:** 50/50 padres
4. **Stats:** Promedio de padres, clampeo
5. **Personalidad:** Aleatorio (no heredada)
6. **Role:** 50/50 padres (o aleatorio si no hay padres)

## Notas

- **Metadatas no genéticas:** Personalidad y Role se asignan/heredan independientemente del genetic string. No contribuyen a la visual del creature, solo a comportamiento/combate.
- **Herencia 50/50:** Role hereda simétricamente (no diferencia macho/hembra), igual que FurType. Probabilidad 50% de cada padre, o aleatorio si falta alguno.
- **Impacto gameplay:** Rol heredado significa que un Protector + Agresivo pueden tener hijo Protector o Agresivo (50/50). Combinación genética aún válida; rol proporciona sabor táctico adicional.
