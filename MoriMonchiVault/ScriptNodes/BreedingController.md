---
tags: [script, singleton, breeding]
---

# BreedingController

**Ruta:** `Systems/Breeding/BreedingController.cs`

**Responsabilidad:** Apex del sistema de cría (domain owner). Singleton: `static Instance`. Posee `inheritanceOddsTable`, `affinityTable`, `lifeStageTable` y `asyncBreedingService` (todos serializados). Getters públicos: `InheritanceOdds`, `LifeStageTable`. Public API: `GetAffinity(Role a, Role b)` (S39 cambio), `BreedCreatures()` (devuelve ID hijo), wrappers async (StartBreedingAsync, HatchAsync, CancelBreedingAsync, CancelAllBreedingAsync). Las BreedingContainer piden servicios vía `Instance`. Resuelve registry y database de `GameManager.Instance` en Awake.

## Método GetAffinity (S39 cambio)

```csharp
public float GetAffinity(Role a, Role b) =>
    affinityTable?.GetAffinity(a, b) ?? 0.5f;
```

**Cambio S39:** Firma cambió de `GetAffinity(Personality, Personality)` → `GetAffinity(Role, Role)`. Refleja re-keyeado de la matriz en `BreedingAffinityTableSO`.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `inheritanceOddsTable` | `InheritanceOddsTableSO` | Tabla de probabilidades de herencia de partes |
| `asyncBreedingService` | `AsyncBreedingService` | Ref al servicio async (server-side egg incubation) |
| `affinityTable` | `BreedingAffinityTableSO` | Matriz de afinidad Role → Role (S39) |
| `lifeStageTable` | `CreatureLifeStageTableSO` | Umbrales de edad → etapa de vida |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Instance` | `static BreedingController` | Singleton |
| `InheritanceOdds` | `InheritanceOddsTableSO` | Getter (encapsulación) |
| `LifeStageTable` | `CreatureLifeStageTableSO` | Getter (encapsulación) |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetAffinity(Role a, Role b)` | `float` | Afinidad entre dos roles (S39); default 0.5f si tabla ausente |
| `BreedCreatures(string motherID, string fatherID)` | `string` | Cría local: retorna ID hijo (criatura nueva, namada, stampada, registrada) o null si falla |
| `StartBreedingAsync(string motherID, string fatherID)` | `Task` | Wrapper → `AsyncBreedingService.StartBreedingAsync()` |
| `HatchAsync(string motherID, string fatherID)` | `Task` | Wrapper → `AsyncBreedingService.HatchAsync()` |
| `CancelBreedingAsync(string motherID, string fatherID)` | `Task` | Wrapper → `AsyncBreedingService.CancelBreedingAsync()` |
| `CancelAllBreedingAsync()` | `Task` | Wrapper → `AsyncBreedingService.CancelAllBreedingAsync()` |

## Ciclo de Cría Local (BreedCreatures)

1. Validación: `inheritanceOddsTable != null`
2. Llama `BreedingService.Breed(motherID, fatherID, registry, database, odds)` → retorna `CreatureDNA` hijo
3. Asigna nombre aleatorio vía `CreatureNameBank.GetRandomName()`
4. Estampa timestamp vía `child.Stamp()`
5. Registra en `registry.Register(child)`
6. Agrega ID hijo a `mother.ChildrenIDs` y `father.ChildrenIDs`
7. Dispara `GameEvents.BreedingCompleted(mother, father, child)`
8. Dispara `GameEvents.RegistryChanged(registry)` → persistencia
9. Retorna `child.UniqueID`

## Cambios S39

**GetAffinity firma:**
- Antes: `public float GetAffinity(Personality a, Personality b)`
- Ahora: `public float GetAffinity(Role a, Role b)`

**Llamadores impactados:**
- `BreedingContainer.TryRollPair()` — ahora pasa `dna.Role` en lugar de `dna.Personality`
- `BreedingService.Breed()` — puede consultar afinidad via `BreedingController.Instance.GetAffinity()`

**Migration:** Todos los DNAs usan `.Role` (S37/S39); `.Personality` fue deprecated.

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[BreedingContainer]] — solicita servicios vía Instance
- [[BreedingService]] — logic de herencia
- [[BreedingAffinityTableSO]] — tabla de afinidad Role → Role
- [[InheritanceOddsTableSO]] — tabla de herencia de partes
- [[AsyncBreedingService]] — orchestrator de cría async
- [[CreatureRegistrySO]] — consulta/mutación de DNAs
- [[CreatureDatabaseSO]] — resolución de partes
- [[GameManager]] — proporciona registry/database
- [[GameEvents]] — dispara eventos
- [[Role]] — enum Protector/Agresivo/Empático

## Conexiones

**Entrada:**
- Serialized refs en inspector (inheritanceOddsTable, affinityTable, asyncService, lifeStageTable)
- `GameManager.Instance` resuelve en Awake
- `BreedingContainer` solicita servicios via `Instance.GetAffinity()`, `Instance.StartBreedingAsync()`, etc.

**Salida:**
- `GameEvents.BreedingCompleted()` — notifica que nació criatura
- `GameEvents.RegistryChanged()` → persistencia
- Retorna ID hijo de `BreedCreatures()`

## Notas (S32 + S39)

- **Singleton:** Una única instancia de BreedingController en escena, attachment GameManager GameObject.
- **S39 re-keyeado:** Afinidad now Role-based, no Personality-based. Separación de concerns: Role = combate, Personality = comportamiento (deprecado).
- **Backward compat:** Si affinityTable == null, GetAffinity() retorna 0.5f.
- **Async breeding:** El servidor maneja egg timer; cliente consulta vía wrappers.
- **Local breeding:** BreedCreatures() es síncrono, cría inmediatamente; async methods usan server-side incubation.
