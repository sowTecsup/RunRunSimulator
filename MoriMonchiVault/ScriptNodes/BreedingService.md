---
tags: [script, genetics]
---

# BreedingService.cs

**Ruta:** `Systems/Breeding/BreedingService.cs`

**Responsabilidad:** Lógica local de cruce. Hereda partes desde el árbol genealógico (padres/abuelos/bisabuelos + mutación aleatoria), colores base via `ColorGenetics.Inherit(motherColor, fatherColor)` + color secundario derivado deterministico via `ColorGenetics.DeriveSecondary(childBase)`, `FurType` 50/50, stats base (Constitution/Attack/Speed) via `InheritStat()` que promedia padres y clampea a [StatMin..StatMax], género aleatorio, rol heredado 50/50 de padres vía herencia directa (si solo 1 padre, se hereda; si 0 padres, aleatorio), **S39:** elemento heredado 50/50 de padres con chance de mutación vía `ElementMutationChance` en `InheritanceOddsTableSO`, **S57:** `IsShiny` roll 0.5% via `ColorGenetics.RollShiny()` (cada hijo nuevo roll independiente, no hereda de padres), **S69:** Sociability y Boldness heredan via `InheritDial()` con tres modos (Average/Copy/Mutation) controlados por `InheritanceOddsTableSO.RollDial()`. Valida género, muerte, busy state, `MaxBreedCount` (4). Valida herencia genealógica desde padres hasta bisabuelos; fallback a pool aleatorio si no hay ancestros.

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/03 - Combat System]], [[Index/13 - Combat Design Direction]]

**Conexiones:** [[CreatureDNA]], [[InheritanceOddsTableSO]], [[BreedingAffinityTableSO]], [[GameEvents]], [[BreedingContainer]], [[BreedingController]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[ColorGenetics]], [[FurType]], [[CreatureGenerator]], [[Enums]], [[Role]], [[Element]], [[ElementalState]], [[RoleWorldProfileSO]]

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
5. Ambos por debajo de MaxBreedCount (4)

**Retorna:** CreatureDNA hijo si todos validations pasan, null si alguno falla.

## Algoritmo de herencia (S69)

### Partes genéticas

Heredan del árbol genealógico vía `ResolveSlot()` usando `odds.Roll()`:
- **Parent (40%):** Una parte del padre elegido al azar (50/50 madre/padre)
- **Grandparent (20%):** Una parte de un abuelo (rama madre o padre)
- **GreatGrandparent (10%):** Una parte de bisabuelo
- **Mutation (20%):** Parte aleatoria del pool
- **Base (10%):** Fallback si no hay ancestros

### Colores

```csharp
BaseColor = ColorGenetics.Inherit(mother.BaseColor, father.BaseColor);
SecondaryColor = ColorGenetics.DeriveSecondary(childBase);
```

Determinista: mismos padres → mismo color hijo.

### Identidad

```csharp
Gender = Random.value < 0.5f ? CreatureGender.Male : CreatureGender.Female;
```

Aleatorio 50/50, no heredado.

### Metadata no genética

**Role (S37):**
```csharp
Role = Random.value < 0.5f ? mother.Role : father.Role;
```
50/50, no genético.

**Element (S39 con mutación):**
```csharp
Element = Random.value < odds.ElementMutationChance
    ? CreatureGenerator.RandomElement()
    : (Random.value < 0.5f ? mother.Element : father.Element),
```
50/50 de padres, O aleatorio con chance `ElementMutationChance` (default 10%).

**IsShiny (S57):**
```csharp
IsShiny = ColorGenetics.RollShiny();  // 0.5% roll nuevo
```
Roll independiente cada hijo, no hereda de padres.

### Stats base (Constitution/Attack/Speed)

```csharp
BaseConstitution = InheritStat(mother.BaseConstitution, father.BaseConstitution);
BaseAttack       = InheritStat(mother.BaseAttack,       father.BaseAttack);
BaseSpeed        = InheritStat(mother.BaseSpeed,        father.BaseSpeed);
```

**InheritStat() privado:**
```csharp
private static float InheritStat(float motherStat, float fatherStat)
{
    return Mathf.Clamp(
        (motherStat + fatherStat) / 2f + Random.Range(-0.5f, 0.5f),
        CreatureGenerator.StatMin,
        CreatureGenerator.StatMax
    );
}
```

Promedia padres ± pequeño jitter, clampea [1, 10].

### Diales genéticos (S69 - Sociability/Boldness)

```csharp
Sociability = InheritDial(mother.Sociability, father.Sociability, odds);
Boldness    = InheritDial(mother.Boldness,    father.Boldness,    odds);
```

**InheritDial() privado:**
```csharp
private static float InheritDial(float motherDial, float fatherDial, InheritanceOddsTableSO odds)
{
    var slot = odds.RollDial();
    
    return slot switch
    {
        InheritanceOddsTableSO.DialSlot.Average =>
            Mathf.Clamp01((motherDial + fatherDial) / 2f + 
                          Random.Range(-odds.DialJitter, odds.DialJitter)),
        
        InheritanceOddsTableSO.DialSlot.Copy =>
            Random.value < 0.5f ? motherDial : fatherDial,
        
        InheritanceOddsTableSO.DialSlot.Mutation =>
            CreatureGenerator.RandomDial(),
        
        _ => (motherDial + fatherDial) / 2f
    };
}
```

**Tres modos:**
1. **Average (50% default):** Promedio ± jitter
2. **Copy (30% default):** Exacto de un padre
3. **Mutation (20% default):** RandomDial() nuevo

### FurType

```csharp
FurType = ColorGenetics.Inherit(mother.FurType, father.FurType);
```

50/50 de padres.

## Cambios S69

**Herencia de Sociability y Boldness:**
- Ambos diales heredan de padres con 3 modos posibles (Average/Copy/Mutation)
- Controlado por `InheritanceOddsTableSO.RollDial()`
- Pesos default: Average 50%, Copy 30%, Mutation 20%
- **Average:** (m+f)/2 ± DialJitter (default 0.05), luego clamp01
- **Copy:** Exacto de un padre al azar
- **Mutation:** RandomDial() independiente (sorpresa)
- Impacto: Permite criar Sociability alto/bajo e identificar heredabilidad; Boldness modula agresividad

**Uso:**
- Madre Sociability 0.8, Padre 0.4 → hijo puede ser 0.6 (average), 0.8 (copy madre), 0.4 (copy padre), o new random (mutation)
- Madre Boldness 0.3, Padre 0.7 → hijo puede ser 0.5 (average), 0.3 (copy madre), 0.7 (copy padre), o new random (mutation)

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

## Notas

- **Metadatas no genéticas:** Género, Role (S37), Element (S39), IsShiny (S57), Sociability/Boldness (S69) se asignan/heredan independientemente del genetic string. No contribuyen a la visual del creature (salvo IsShiny que es cosmético).
- **Herencia 50/50 + variance:** Stats/Diales heredan simétricamente (no diferencia macho/hembra), con pequeña varianza para evitar clones exactos.
- **IsShiny NO hereditario:** Cada hijo obtiene roll nuevo; shiny de padres no afecta. Rarity independiente.
- **Mutación elemental:** InheritanceOddsTableSO.ElementMutationChance controla probabilidad de que el hijo tenga elemento aleatorio vs heredado.
- **Impacto gameplay:** Rol heredado significa que un Protector + Agresivo pueden tener hijo Protector o Agresivo (50/50). Element heredado proporciona sabor táctico elemental adicional, con oportunidad de mutación. Diales heredables permiten selección artificial (cruzar Sociable x Sociable → más Sociable probable). IsShiny es cosmético puro.

## Vinculado a

[[Index/02 - Genetics & Breeding]]

## Conexiones

[[CreatureDNA]], [[InheritanceOddsTableSO]], [[CreatureGenerator]], [[CreatureRegistrySO]], [[ColorGenetics]]
