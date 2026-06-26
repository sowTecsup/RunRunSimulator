---
tags: [script, combat, equipment]
---

# EquipmentStats.cs

**Ruta:** `Systems/Combat/EquipmentStats.cs`

**Responsabilidad:** Clase estática pura que aplica modificadores de equipo a los stats base de una criatura. Resuelve los IDs de ítems equipados contra `EquipmentDatabaseSO`, extrae sus `StatModifierEffect` y los aplica de forma escalonada: Flat (suma) → PercentAdd (suma de %, aplicada como multiplicador 1+Σ/100) → PercentMult (compuesto, cada uno 1+v/100), con piso en 0 para todos los stats. Es el motor del "StatSheet" de visualización: usado hoy en MoriMochiAgent.Tuning (readout en inspector) y MorimonchiDetailInfoUITK (panel de detalle + tab Equipo). NO está acoplado al pipeline de combate aún (ese cambio es Fase 2).

**Conexiones:** [[EquipmentDatabaseSO]], [[StatModifierEffect]], [[StatModifier]], [[CreatureDNA]], [[CombatService]]
