---
tags: [equipment, modifiers, stats]
---

# EquipmentStats

**Ruta:** `Systems/Stats/EquipmentStats.cs`

**Responsabilidad:** Clase estática pura que aplica modificadores de equipamiento a stats efectivos. Resuelve ítems equipados contra `EquipmentDatabaseSO`, extrae sus `StatModifierEffect` y los aplica escalonadamente: Flat (suma) → PercentAdd (suma %, mult 1+Σ/100) → PercentMult (compuesto, cada 1+v/100), con piso en 0.

## Método Público

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Apply(EffectiveStats baseStats, CreatureDNA dna, EquipmentDatabaseSO db)` | `EffectiveStats` | Aplica mods de equipment, retorna nuevo struct con 6 stats modificados |

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

## Firma Actual

```csharp
public static EffectiveStats Apply(EffectiveStats baseStats, CreatureDNA dna, EquipmentDatabaseSO db)
```

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[EffectiveStats]] — struct de entrada/salida
- [[CreatureStats]] — proporciona baseStats
- [[EquipmentDatabaseSO]] — resuelve ítems
- [[StatModifierEffect]] — tipo de modificador
- [[CreatureDNA]] — `.Equipped` dict

## Conexiones

**Entrada:**
- Consumidores futuros de stats efectivos (UI, sistemas de display, etc.)
- `CreatureDNA.Equipped` — dict de items equipados

**Salida:**
- `EffectiveStats` — stats finales con bonificadores aplicados

## Notas

- **No serializa:** Es cálculo puro en tiempo real.
- **Determinista:** Orden de aplicación de mods es fijo (Flat, PercentAdd, PercentMult).
- Movilidad de `Systems/Combat/` a `Systems/Stats/` refleja el foco en estadísticas fuera del combate.
