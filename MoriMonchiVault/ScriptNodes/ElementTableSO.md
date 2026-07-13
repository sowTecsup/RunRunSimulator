---
tags: [scriptable-object, combat, elements, data]
---

# ElementTableSO.cs

**Ruta:** `Data/Combat/ElementTableSO.cs`

**Responsabilidad:** Asset Odin `SerializedScriptableObject` que centraliza toda la definición elemental del sistema 3v3: identidades de elementos, definiciones de estados (magnitudes en %), y 12 recetas de reacción (6 aliadas + 6 ofensivas) con efectos polimórficos. **S40:** Data-driven consolidación — antes los knobs estaban hardcoded en `CombatManagerSO` (8 floats) y `CombatElements` (switch de reacciones). Ahora un único asset editable sin recompilación. Wired vía `CombatManagerSO.Elements`.

## Estructura

### ElementIdentity

```csharp
public class ElementIdentity
{
    public string DisplayName;    // ej: "Agua"
    public Color  UiColor;         // ej: (0.2, 0.6, 1)
}
```

**Dict:** `Dictionary<Element, ElementIdentity> Identities`

### StateDefinition

```csharp
public class StateDefinition
{
    public string DisplayName;     // ej: "Vaporizado"
    public string Description;     // ej: "Estado armado aliado por Agua+Fuego"
    public float  Percent;         // Valor 0–1 para bonus/chance (ej: evasión)
    public float  Amount;          // Valor fijo para daño/cura (ej: Mareado damage)
}
```

**Dict:** `Dictionary<ElementalState, StateDefinition> States`

### ElementReaction

```csharp
public class ElementReaction
{
    public string  Name;                           // ej: "Cleanse"
    public Element A;                              // Primer elemento
    public Element B;                              // Segundo elemento
    public bool    AllySource;                     // true = aliada, false = ofensiva
    public List<ReactionEffectBase> Effects;       // Efectos polimórficos
}
```

**List:** `List<ElementReaction> Reactions`

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetState(state)` | `StateDefinition` | Busca definición en dict, retorna default vacío si falta. |
| `StatePercent(state)` | `float` | Retorna `Percent` del estado (0–1). |
| `StateAmount(state)` | `float` | Retorna `Amount` del estado (fijo). |
| `GetIdentity(element)` | `ElementIdentity` | Busca identidad elemento, retorna default (DisplayName = enum.ToString()) si falta. |
| `FindReaction(a, b, allySource)` | `ElementReaction \| null` | Busca reacción que combine elementos `a` y `b` con fuente. Retorna null si no existe o `a == b` (mismo elemento no reacciona). |
| `PopulateV1()` | `void` (Button) | Populate helper: llena Identities (4 elementos), States (12 estados), Reactions (12 recetas con efectos). |

## Reacciones V1 (12 Recetas)

### Aliadas (6)

| Nombre | Elementos | Efectos |
|--------|-----------|---------|
| Vaporizado | Agua + Fuego | ArmStateEffect(Vaporizado) |
| GolpePreciso | Agua + Electricidad | ArmStateEffect(GolpePreciso) |
| Cleanse | Agua + Planta | CleanseEffect(heal 20%) |
| Energizado | Fuego + Electricidad | ArmStateEffect(Energizado) |
| Charcoal | Fuego + Planta | ArmStateEffect(Charcoal) |
| OverGrow | Electricidad + Planta | DoubleShieldEffect() |

### Ofensivas (6)

| Nombre | Elementos | Efectos |
|--------|-----------|---------|
| Boiling | Agua + Fuego | ArmStateEffect(Boiling) |
| Confuso | Agua + Electricidad | ArmStateEffect(Confuso) |
| Leech | Agua + Planta | LeechEffect(drain 4 HP) |
| Mareado | Fuego + Electricidad | ArmStateEffect(Mareado) |
| Debilidad | Fuego + Planta | ArmStateEffect(Debilidad) |
| PisoTierra | Electricidad + Planta | RemoveRandomMarkEffect() |

## Estados V1 (12 Definiciones)

| Estado | Fuente | Percent/Amount | Descripción |
|--------|--------|---|-------------|
| Vaporizado | Agua+Fuego (aliada) | 0.30 | Bonus evasión 30% |
| GolpePreciso | Agua+Elec (aliada) | 0.25 | Bonus crit 25% |
| Boiling | Agua+Fuego (ofensiva) | 0.30 | Amplificación daño 30% |
| Charcoal | Fuego+Planta (aliada) | 0.50 | Reflejo daño 50% |
| Mareado | Fuego+Elec (ofensiva) | 0.50% chance, 3.0 damage | Golpe al azar |
| Energizado | Fuego+Elec (aliada) | — | Prioridad turno |
| Cleanse | Agua+Planta (aliada) | — | Purga o cura 20% |
| OverGrow | Elec+Planta (aliada) | — | Duplica escudo |
| Debilidad | Fuego+Planta (ofensiva) | — | Ignora DEF |
| Confuso | Agua+Elec (ofensiva) | — | Acción falla |
| Leech | Agua+Planta (ofensiva) | — | Drena 4 HP |
| PisoTierra | Elec+Planta (ofensiva) | — | Remueve marca |

## Flujo de Integración (S40)

**En `CombatManagerSO`:**
```csharp
[Title("Elemental")]
[InfoBox("Tabla elemental...")]
public ElementTableSO Elements;
```

**En `CombatElements.AddMark()`:**
```csharp
var reaction = config.Elements != null ? config.Elements.FindReaction(other.Element, element, allySource) : null;
if (reaction == null) return;
// Apply effects
foreach (var e in reaction.Effects) e.Apply(target, reactor, reaction.Name, result, rng);
```

**En `CombatStrike.Execute()`:**
```csharp
// Consume estados con magnitudes del table
evaChance += target.HasState(ElementalState.Vaporizado) 
    ? (config.Elements != null ? config.Elements.StatePercent(ElementalState.Vaporizado) : 0f) 
    : 0f;
```

## Determinismo

- **Sin herencia:** ElementTableSO es puro data, sin lógica RNG
- **Polimórfismo:** Efectos dentro de Reactions son polimórficos via ReactionEffectBase; pueden consumir RNG (RemoveRandomMarkEffect)
- **Editable:** PopulateV1 es helper para llenar defaults; usuarios pueden editar en inspector sin recompilar

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

- [[CombatManagerSO]] — `Elements` field asignado
- [[CombatElements]] — consume `FindReaction()` en `AddMark()`
- [[CombatService]] — acceso indirecto vía config.Elements en TakeTurn para magnitudes de estado
- [[ReactionEffectBase]] — efectos polimórficos en Reactions
- [[ElementalState]] (enum) — estados aplicables
- [[Element]] (enum) — elementos en reaction pairs
- [[Combatant]] — portador de marcas/estados
