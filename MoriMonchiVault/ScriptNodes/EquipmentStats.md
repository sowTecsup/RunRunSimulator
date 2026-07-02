---
tags: [combat, equipment, modifiers]
---

# EquipmentStats

**Ruta:** `Systems/Combat/EquipmentStats.cs`

**Responsabilidad:** Clase estática pura que aplica modificadores de equipo a stats base. Resuelve ítems equipados contra `EquipmentDatabaseSO`, extrae sus `StatModifierEffect` y los aplica escalonadamente: Flat (suma) → PercentAdd (suma %, mult 1+Σ/100) → PercentMult (compuesto, cada 1+v/100), con piso en 0. Motor del "StatSheet" de visualización y del pipeline de combate. Usado por `CombatService.BuildCombatant()` para stats finales.

## Método Público

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Apply(EffectiveStats baseStats, CreatureDNA dna, EquipmentDatabaseSO db)` | `EffectiveStats` | Aplica mods de equipment, retorna nuevo struct (S32: firma top-level `EffectiveStats`) |

## Algoritmo

1. **Resolución:** Itera `dna.Equipped` (dict slot→id), resuelve cada item de `db`
2. **Extracción:** Lista cada `StatModifierEffect` del item
3. **Aplicación escalonada:**
   - Acumula todos los Flat `+cantidad` por stat
   - Acumula PercentAdd `(1 + Σ%/100)`
   - Acumula PercentMult `∏(1 + v%/100)` (orden importa para float precision)
   - Resultado: `(base + flats) * percentAdd * percentMult`
4. **Piso:** `Mathf.Max(0f, valor)` para prevenir stats negativos
5. **Retorna:** Nuevo `EffectiveStats` con todos los 6 campos modificados

## Firma Actualizada (S32)

```csharp
public static EffectiveStats Apply(EffectiveStats baseStats, CreatureDNA dna, EquipmentDatabaseSO db)
```

**Antes:** `public static CombatService.EffectiveStats Apply(...)`

**Ahora:** `public static EffectiveStats Apply(...)` (struct público top-level)

## Vinculado a

- [[Index/03 - Combat]]
- [[EffectiveStats]] — struct de entrada/salida (S32)
- [[CombatStats]] — proporciona baseStats
- [[EquipmentDatabaseSO]] — resuelve ítems
- [[StatModifierEffect]] — tipo de modificador
- [[CreatureDNA]] — `.Equipped` dict

## Conexiones

**Entrada:**
- `CombatService.BuildCombatant()` → llama `EquipmentStats.Apply(baseStats, dna, equipDb)`
- MoriMochiAgent.Tuning (display UI) → llama para readout

**Salida:**
- `EffectiveStats` → usado para `Combatant.{Attack,Speed,Defense,Luck,Evasion}` inicialización
- HP = `EffectiveStats.Constitution * BaseHpCombatMultiplier`

## Notas

- **No serializa:** Es cálculo puro en tiempo real.
- **Determinista:** Orden de aplicación de mods es fijo (Flat, PercentAdd, PercentMult).
- **S32:** Refactor extrajo `EffectiveStats` a struct top-level; firma simplificada.
