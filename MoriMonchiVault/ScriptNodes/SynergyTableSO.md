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
| `AddElementRecipes()` | Botón [Odin] **(S35)**: agrega 3 recetas base de elementos (Regeneración, Cortocircuito, Robo de vida) |

## CreateAssetMenu

**Menu path:** `RunRunSimulator/Combat/Synergy Table`  
**File name:** `SynergyTable`

## Recetas v1 (Elementos) — S35

El botón "Recetas v1 (elementos)" crea 3 recetas de sinergia emergentes:

### Regeneración

```
Requerimientos: Pulse×3 + Steel×1
Efectos: Regen (3 turnos, 4 curación/turno)
```

**Lógica:** Cuando se aplican 3 stacks de Pulse (curación periódica) más 1 stack de Steel (defensa), se dispara esta sinergia que otorga un Regen más poderoso (4/turno vs. default 2/turno de Pulse).

### Cortocircuito

```
Requerimientos: Static×2 + Mist×1
Efectos: Stun (1 turno)
```

**Lógica:** Cuando se aplican 2 stacks de Static (reducción de velocidad) más 1 stack de Mist (evasión), la combinación causa un aturdimiento al rival.

### Robo de vida

```
Requerimientos: Pulse×2 + Mist×1
Efectos: Lifesteal (3 turnos, 30% de daño)
```

**Lógica:** Cuando se aplican 2 stacks de Pulse más 1 stack de Mist, se activa un efecto de Lifesteal: durante 3 turnos, el portador recupera el 30% del daño que inflige como HP.

## Flujo de Uso

1. **Setup:** Juan crea instancia en `Resources/Combat/SynergyTable` (u otro path)
2. **Edición:** Expande `Rules` en inspector, agrega `SynergyRule` instances, o usa botones "Receta ejemplo" / "Recetas v1 (elementos)"
3. **Autoreo Inline:** Cada regla tiene `Name`, `Requirements` (list de tipos+stacks), `Effects` (list de subclases `SynergyEffectBase`)
4. **Validación:** `RulesSummary` (ReadOnly field) muestra qué reglas están configuradas
5. **Asignación:** Arrastra el SO a `CombatManagerSO.Synergies`
6. **Ejecución:** En combate, `CombatResolver.CheckSynergies()` lee desde `config.Synergies` y ejecuta reglas satisfechas

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
- [[CombatProcEffect]] — los 4 nuevos (Static, Pulse, Steel, Mist) aplican stacks que disparan estas recetas (S35)

## Conexiones

**Entrada:**
- `CombatManagerSO.Synergies` — referencia única

**Salida:**
- `CombatResolver.CheckSynergies()` → lee `Synergies.Rules` cada vez que se agrega un status (S32)
- Sinergias disparadas aplican `SynergyEffectBase` polimórficamente (daño, curación, status, stun)

## Cambios S35

**Botón "Recetas v1 (elementos)":** Nuevo helper que puebla 3 recetas base de elementos (Regeneración, Cortocircuito, Robo de vida). Permite setup rápido del sistema de sinergias emergentes sin tener que configurar manualmente.

## Notas

- **Odin [Title(), ListDrawerSettings]:** Expandido por defecto para fácil autoreo.
- **Backward compat:** Si ausente/null, combate funciona sin sinergias.
- **Patrón centralizado:** Una única tabla de reglas, editada desde Inspector.
- **NUEVO S32:** Parte de la fase de sinergias del balance de combate.
- **ACTUALIZADO S35:** Botón "Recetas v1 (elementos)" proporciona 3 recetas de referencia. Deuda futura: configuración granular de reglas (activable/desactivable por regla), más recetas de balance.
