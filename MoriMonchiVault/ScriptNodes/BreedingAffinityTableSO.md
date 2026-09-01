---
tags: [scriptable-object, genetics, breeding]
---

# BreedingAffinityTableSO

**Ruta:** `Data/Breeding/BreedingAffinityTableSO.cs`

**Responsabilidad:** Matriz simétrica `(Role, Role)` → `float` de afinidad (0..1). **S39 cambio:** re-keyeada de `(Personality, Personality)` a `(Role, Role)`, cambio desde 21 pares (6 Personalities × 6) a 9 pares (3 Roles × 3: Protector/Agresivo/Empático). `SerializedScriptableObject` con `OdinSerialize Dictionary<(Role, Role), float>`. Sin `static Current`; lo posee `BreedingController`, accedible vía `BreedingController.Instance.GetAffinity(roleA, roleB)`. Devuelve 0.5 por defecto si falta par. Botón `SeedDefaults` llena los 9 pares con valores balanceados.

## Campos Públicos

| Campo | Tipo | Acceso | Descripción |
|-------|------|--------|-------------|
| `affinities` | `Dictionary<(Role, Role), float>` | [OdinSerialize] private | Matriz simétrica Role → Role → afinidad (0..1) |

## Propiedades Públicas

| Propiedad | Firma | Descripción |
|-----------|-------|-------------|
| `GetAffinity(Role a, Role b)` | `float` | Retorna afinidad entre dos roles. Simétrica: `GetAffinity(A, B) == GetAffinity(B, A)`. Default 0.5f si falta par. |

## CreateAssetMenu

**Menu path:** `RunRunSimulator/Breeding/Breeding Affinity Table`  
**File name:** `BreedingAffinityTable`

## Matriz Default (S39)

El botón "Seed Defaults" puebla 9 pares (3×3 Roles) con valores balanceados:

| Par | Afinidad | Notas |
|-----|----------|-------|
| (Protector, Protector) | 0.60 | Protectores compatibles |
| (Protector, Agresivo) | 0.40 | Tensión: defensa vs ataque |
| (Protector, Empático) | 0.60 | Sinergia: protección + apoyo |
| (Agresivo, Agresivo) | 0.55 | Agresivos moderadamente compatibles |
| (Agresivo, Empático) | 0.50 | Neutral |
| (Empático, Empático) | 0.80 | Empaticos muy compatibles |

Matriz es simétrica: `GetAffinity(Protector, Agresivo) == GetAffinity(Agresivo, Protector) == 0.40`.

## Método GetAffinity (S39)

```csharp
public float GetAffinity(Role a, Role b)
{
    var key = a <= b ? (a, b) : (b, a);
    return affinities != null && affinities.TryGetValue(key, out var chance) 
        ? chance 
        : 0.5f;
}
```

**Lógica de simetría:** Normaliza el par `(a, b)` a `(menor, mayor)` antes de búsqueda, garantiza simetría sin duplicar pares.

## Cambios S39

**Re-keyeado de Personality → Role:**
- Antes: Dictionary<(Personality, Personality), float> con 21 pares (6×6 Personalities)
- Ahora: Dictionary<(Role, Role), float> con 9 pares (3×3 Roles: Protector/Agresivo/Empático)
- Afinidad vinculada ahora al Role de combate, no a Personality de comportamiento (separación de concerns)

**Cambio en caller (BreedingController):**
```csharp
// Antes: GetAffinity(motherPersonality, fatherPersonality)
// Ahora: GetAffinity(motherRole, fatherRole)
public float GetAffinity(Role a, Role b) =>
    affinityTable?.GetAffinity(a, b) ?? 0.5f;
```

**Migration:** Los DNAs ahora usan `.Role` (enum Role con 3 valores) en lugar de `.Personality` (que quedó deprecated/removido).

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[BreedingController]] — caller principal via `GetAffinity()`
- [[BreedingService]] — usa afinidad en `Breed()`
- [[BreedingContainer]] — llama `GetAffinity()` en dice roll
- [[CreatureDNA]] — campo `.Role` (source de verdad)
- [[GeneticsEnums]] — enum Protector/Agresivo/Empático (S37)

## Conexiones

**Entrada:**
- `BreedingController.GetAffinity(Role a, Role b)` → consulta esta tabla
- Botón editor "Seed Defaults" para inicializar

**Salida:**
- Valor float de afinidad (0..1) usado en `BreedingContainer.TryRollPair()` y probabilidades de crianza

## Notas

- **Odin:** `[DictionaryDrawerSettings]` para UI inspector friendly.
- **Simetría garantizada:** El método `GetAffinity()` normaliza pares antes de búsqueda.
- **Backward compat:** Si tabla ausente/null, `BreedingController.GetAffinity()` retorna 0.5f por defecto.
- **Balance:** Default values balanceados para que Empaticos sean la pareja más compatible (0.80), Protector-Agresivo la menos (0.40).
- **S39 cambio crítico:** Roles reemplazan Personalities en breeding logic. Afecta a todos los DNAs cargados.
