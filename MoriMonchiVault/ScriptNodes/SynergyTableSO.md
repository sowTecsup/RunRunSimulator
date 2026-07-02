---
tags: [scriptable-object, combat, synergy, config]
---

# SynergyTableSO

**Ruta:** `Data/Combat/SynergyTableSO.cs`

**Responsabilidad:** ScriptableObject autoreable (Odin `SerializedScriptableObject`) que centraliza todas las recetas de sinergia del proyecto. Patrón "un único asset" análogo a `BreedingAffinityTableSO`. Referenciado por `CombatManagerSO.Synergies`; si es null, las sinergias están deshabilitadas.

## Campos Públicos

| Campo | Tipo | Acceso | Descripción |
|-------|------|--------|-------------|
| `Rules` | `List<SynergyRule>` | [OdinSerialize] | Lista de recetas de sinergia autorables |

## Propiedades (Odin Inspector)

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `RulesSummary` | `string` (computed, ReadOnly) | Resumen textual de todas las reglas, línea por línea, para validación visual rápida |

## Métodos Privados (Odin)

| Método | Descripción |
|--------|-------------|
| `AddExampleRule()` | Botón [Odin]: agrega un ejemplo ("Explosión tóxica: 3x Poison → 10 daño") para referencia |

## CreateAssetMenu

**Menu path:** `RunRunSimulator/Combat/Synergy Table`  
**File name:** `SynergyTable`

## Flujo de Uso

1. **Setup:** Juan crea instancia en `Resources/Combat/SynergyTable` (u otro path)
2. **Edición:** Expande `Rules` en inspector, agrega `SynergyRule` instances
3. **Autoreo Inline:** Cada regla tiene `Name`, `Requirements` (list de tipos+stacks), `Effects` (list de subclases `SynergyEffectBase`)
4. **Validación:** Botón "Receta ejemplo" o `RulesSummary` para ver qué reglas están configuradas
5. **Asignación:** Arrastra el SO a `CombatManagerSO.Synergies`
6. **Ejecución:** En combate, `CombatResolver.CheckSynergies()` lee desde `config.Synergies` y ejecuta reglas

## Integración con CombatResolver

```csharp
// En CombatResolver constructor (S32)
public SynergyTableSO Synergies;

// En AddStatus, tras aplicar un nuevo stack:
CheckSynergies(bearer);

// En CheckSynergies:
if (resolvingSynergies || Synergies == null) return;
// Itera Synergies.Rules, busca la primera satisfecha,
// quema sus stacks, aplica sus Effects
```

## Sin Tabla = Sin Sinergias

Si `CombatManagerSO.Synergies == null`, el campo `CheckSynergies()` retorna temprano sin hacer nada. Permite deshabilitarlas sin tocar código.

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatManagerSO]] — campo `Synergies: SynergyTableSO`
- [[CombatResolver]] — lector de tabla, ejecutor de reglas
- [[SynergyRule]] — contenida en `Rules`
- [[SynergyEffectBase]] — efectos autorables en cada regla

## Conexiones

**Entrada:**
- `CombatManagerSO.Synergies` — referencia única

**Salida:**
- `CombatResolver.CheckSynergies()` → lee `Synergies.Rules` cada vez que se agrega un status

## Notas

- **Odin [Title(), ListDrawerSettings]:** Expandido por defecto para fácil autoreo.
- **Backward compat:** Si ausente/null, combate funciona sin sinergias.
- **Patrón centralizado:** Una única tabla de reglas, editada desde Inspector.
- **NUEVO S32:** Parte de la fase de sinergias del balance de combate; deuda futura S33+: configuración granular de reglas (activable/desactivable por regla).
