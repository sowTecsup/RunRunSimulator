---
tags: [script, equipment, items]
---

# ItemUseEffect.cs

**Ruta:** `Data/Equipment/ItemUseEffect.cs`

**Responsabilidad:** Clase base abstracta para efectos de items consumibles. Define campos `Uses` (contador de usos restantes), `Rule` (UseRule enum: Always, SelfHpBelow), `HpThreshold` (umbral de HP para la regla). Plantilla pura, sin lógica de ejecución (S75: sin Apply method, simplemente descriptor de efecto).

## Campos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Uses` | int | Usos restantes del item |
| `Rule` | UseRule | Cuándo activar: Always, SelfHpBelow |
| `HpThreshold` | float | Umbral de HP para regla SelfHpBelow |

## UseRule (enum)

- **Always** — Efecto se activa siempre
- **SelfHpBelow** — Solo si portador HP < HpThreshold

## Cambios en S75

- **ELIMINADO:** `Apply(ICombatContext)` method (demolición de combate)
- **MANTIENE:** Campos Uses/Rule/HpThreshold como descriptor de efecto

## Vinculado a

- [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[EquipmentSO]]
