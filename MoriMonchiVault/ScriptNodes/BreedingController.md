---
tags: [script, singleton, breeding]
---

# BreedingController

**Ruta:** `Systems/Breeding/BreedingController.cs`

**Responsabilidad:** Apex del sistema de cría (domain owner). Singleton: `static Instance`. Posee `inheritanceOddsTable`, `affinityTable`, `lifeStageTable` y `incubation` (IncubationService, todos serializados). Getters públicos: `InheritanceOdds`, `LifeStageTable`, `Incubation`. Public API: `GetAffinity(Role a, Role b)`, `StartBreeding()` (wrapper a incubation), `TryHatch()` (wrapper → retorna HatchResult), `BreedCreatures()` (local sync), `CancelBreeding()`, `CancelAllBreeding()`. BreedingContainer pide servicios vía `Instance`. Resuelve registry y database de `GameManager.Instance` en Awake.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `inheritanceOddsTable` | `InheritanceOddsTableSO` | Tabla de probabilidades de herencia de partes + HatchCost |
| `incubation` | `IncubationService` | Servicio de incubación local (S131, antes AsyncBreedingService) |
| `affinityTable` | `BreedingAffinityTableSO` | Matriz de afinidad Role → Role |
| `lifeStageTable` | `CreatureLifeStageTableSO` | Umbrales de edad (días) → etapa de vida |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Instance` | `static BreedingController` | Singleton |
| `InheritanceOdds` | `InheritanceOddsTableSO` | Getter (encapsulación) |
| `LifeStageTable` | `CreatureLifeStageTableSO` | Getter (encapsulación) |
| `Incubation` | `IncubationService` | Getter (acceso a servicio local) |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetAffinity(Role a, Role b)` | `float` | Afinidad entre dos roles (S39 cambio a Role-based); default 0.5f si tabla ausente |
| `StartBreeding(string motherID, string fatherID)` | `bool` | Wrapper → `incubation.StartBreeding()` |
| `TryHatch(string motherID, string fatherID)` | `HatchResult` | Wrapper → `incubation.TryHatch()` — retorna Hatched/NotReady/InsufficientMinerita/Invalid |
| `CancelBreeding(string motherID, string fatherID)` | `void` | Wrapper → `incubation.CancelBreeding()` |
| `CancelAllBreeding()` | `void` | Wrapper → `incubation.CancelAllBreeding()` |
| `BreedCreatures(string motherID, string fatherID)` | `string` | Cría local síncrona: retorna ID hijo o null si falla |

## Ciclo de Cría (BreedCreatures)

1. Validación: `inheritanceOddsTable != null`
2. Llama `BreedingService.Breed(motherID, fatherID, registry, database, odds)` → retorna `CreatureDNA` hijo
3. Asigna nombre aleatorio vía `CreatureNameBank.GetRandomName()`
4. Estampa timestamp vía `child.Stamp()`
5. Asigna BirthDay: `child.BirthDay = GameClock.Instance != null ? GameClock.Instance.Day : 1`
6. Registra en `registry.Register(child)`
7. Agrega ID hijo a `mother.ChildrenIDs` y `father.ChildrenIDs`
8. Dispara `GameEvents.BreedingCompleted(mother, father, child)`
9. Dispara `GameEvents.RegistryChanged(registry)` → persistencia
10. Retorna `child.UniqueID`

## S131 Cambio: AsyncBreedingService → IncubationService

**Renombrado:** Campo `asyncBreedingService` → `incubation`, tipo cambió a `IncubationService`.

**FormerlySerializedAs:** Usado para backward compatibility (Unity auto-migra refs serializadas).

```csharp
[FormerlySerializedAs("asyncBreedingService")]
[SerializeField] private IncubationService incubation;
public IncubationService Incubation => incubation;
```

**Razón:** IncubationService es local síncrono (no Cloud Code). Flujo:
- `StartBreeding()` marca padres, deduce energía, fija BreedReadyAt
- Esperar gametime (GameClock)
- `TryHatch()` hatcha si BreedReadyAt alcanzado, deduce Minerita

## Métodos Privados

**GetAffinity (S39 re-keyeado):**
```csharp
public float GetAffinity(Role a, Role b) =>
    affinityTable?.GetAffinity(a, b) ?? 0.5f;
```

Cambio S39: Firma antes era `GetAffinity(Personality, Personality)` → ahora `GetAffinity(Role, Role)`.

## Cambios S39

**GetAffinity firma:**
- Antes: `public float GetAffinity(Personality a, Personality b)`
- Ahora: `public float GetAffinity(Role a, Role b)`

**Llamadores impactados:**
- `BreedingContainer.TryRollPair()` — ahora pasa `dna.Role` en lugar de `dna.Personality`
- `BreedingService.Breed()` — puede consultar afinidad

**Migration:** Todos los DNAs usan `.Role` (S37/S39); `.Personality` fue deprecated.

## Ciclo de Vida

1. `Awake()` → `Instance = this`, resuelve `registry` y `database` de `GameManager.Instance`
2. `OnDestroy()` → Limpia `Instance` si es el mismo
3. `GetAffinity()` consultado por breeding logic (getter public)
4. `StartBreeding()` / `TryHatch()` / `CancelBreeding()` llamados desde BreedingContainer

## Invariantes S131

- Singleton: Una única instancia en escena, attachment GameObject de GameManager
- Breeding local: Síncrono, sin Cloud Code
- Incubation service: Maneja timers via GameClock
- Persistencia: Todos los cambios disparan RegistryChanged → SaveSystem

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[Index/09 - Active Context]]

## Conexiones

**Data/Services:**
- [[IncubationService]] — orquestador de cría local (S131)
- [[BreedingService]] — logic de herencia genética
- [[BreedingAffinityTableSO]] — matriz de afinidad Role → Role
- [[InheritanceOddsTableSO]] — tabla de herencia + HatchCost
- [[CreatureLifeStageTableSO]] — umbrales de edad

**Sistemas:**
- [[CreatureRegistrySO]] — consulta/mutación de DNAs
- [[CreatureDatabaseSO]] — resolución de partes
- [[GameManager]] — proporciona registry/database
- [[GameEvents]] — dispara eventos
- [[GameClock]] — lee día para BirthDay

**UI:**
- [[BreedingContainer]] — solicita servicios vía Instance
- [[BreedingEggsTabPresenter]] — llama TryHatch, procesa HatchResult

## Notas (S131 HC-4)

- **Backward compat:** Si affinityTable == null, GetAffinity() retorna 0.5f
- **Local breeding:** BreedCreatures() es síncrono, cría inmediatamente
- **Wrapped methods:** StartBreeding/TryHatch/CancelBreeding delegados a IncubationService
- **HatchCost:** Consultado a InheritanceOddsTableSO.HatchCost(mother, father) en IncubationService
- **BirthDay S131:** Ahora asignado tanto en BreedCreatures como en IncubationService.HatchLocally
