---
tags: [scriptable-object, combate, tuning]
---

# CombatTuningSO.cs

**Ruta:** `Systems/Combat/CombatTuningSO.cs`

**Responsabilidad:** Parámetros de tuning del combate Dragon RPS (SO sin Odin, simple [CreateAssetMenu]). Campos: `CooldownMinutes` (20 por defecto), `MaterialPerWin` (3 material por victoria), `BudgetTolerance` (1 tolerancia presupuesto rival), `MinEnergyToFight` (0.0 energía mínima requerida). Asset por defecto: `ScriptableObjects/Combat/CombatTuning.asset` (20/3/1/0).

**Vinculado a:** [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsService]], [[DragonRpsRival]], [[DragonRpsGenes]], [[CombatPanelUITK]], [[DevToolsConsole]]
