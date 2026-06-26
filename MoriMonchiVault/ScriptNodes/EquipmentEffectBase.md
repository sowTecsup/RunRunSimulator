---
tags: [script, equipment]
---

# EquipmentEffectBase.cs

**Ruta:** `Data/Equipment/EquipmentEffectBase.cs`

**Responsabilidad:** Clase abstracta (Odin-serializable polimorfa) que define qué puede hacer un item de equipo. Cada subclase es un efecto cerrado y parametrizado (sin lógica ad hoc) que el motor de combate en JavaScript puede replicar para async parity. Actualmente contiene `StatModifierEffect` (modificadores pasivos de stat: +10 ATK, -5 SPD, etc.). Los hooks de combate (procs, efectos de golpe) son Etapa 2.

## Subclases

| Clase | Propósito |
|-------|-----------|
| `StatModifierEffect` | Porta una `List<StatModifier>` aplicados al pipeline de stats. Sin hooks de combate. |

**Métodos públicos**

| Método | Retorna | Propósito |
|--------|---------|----------|
| `Summary()` | `string` | Resumen legible (ej: "+10 ATK, +5% SPD"). |

**Vinculado a:** [[Index/04 - Combat]] (sistema de modificadores)

**Conexiones:** [[EquipmentSO]], [[StatModifier]], [[CombatService]]
