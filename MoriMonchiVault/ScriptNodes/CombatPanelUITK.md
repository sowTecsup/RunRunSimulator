---
tags: [script, ui]
---

# CombatPanelUITK.md

**Ruta:** `UI/CombatPanelUITK.cs`

**Responsabilidad:** Panel UI de combate (4 pestañas: Batalla Online, Combate Local, Resultados, Historial). Implementa `IUINavigable` (focus jerárquico). Obtiene config vía propiedad lazy `Config => CombatController.Instance?.Config`, registry de GameManager. Combate local vía `CombatService.Simulate()`, async vía `AsyncCombatService`. Muestra stats de 6 campos: CON/ATK/SPD/DEF/LCK/EVA.

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[UIManager]], [[CombatController]], [[CombatService]], [[AsyncCombatService]], [[GameManager]], [[CreatureDatabaseSO]]

**Método StatsOf():**
```csharp
private CombatService.EffectiveStats StatsOf(CreatureDNA dna) =>
    database != null ? CombatService.GetEffectiveStats(dna, database)
                     : new CombatService.EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);
```
Fallback con 6 args (sin database), devuelve `EffectiveStats` con campos: Constitution, Attack, Speed, Defense, Luck, Evasion.

**Organización (partial class):**
- `CombatPanelUITK.cs` — núcleo/lifecycle/wiring/data/StatsOf
- `CombatPanelUITK.Tabs.cs` — contenido de las 4 pestañas (MakeCandidate, UI building)
- `CombatPanelUITK.Navigation.cs` — IUINavigable + foco
