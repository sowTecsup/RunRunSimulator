---
tags: [script, ui]
---

# MorimonchiDetailInfoUITK.md

**Ruta:** `UI/MorimonchiDetailInfoUITK.cs`

**Responsabilidad:** Panel de detalle de criatura: stats (6: CON/ATK/SPD/DEF/LCK/EVA), genética, historial, árbol. `IUINavigable`.

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[UIManager]], [[CreatureGridUITK]], [[CreatureDNA]], [[CreatureVisualUI]], [[CombatService]]

**Campos públicos / eventos:**
- `OnCreatureSelected` (evento de UIManager): trae `CreatureDNA` + `CreatureRegistrySO`
- Campos UI: `statCon`, `statAtk`, `statSpd`, `statDef`, `statLck`, `statEva` (Labels para los 6 stats)
- `portrait` (VisualElement, fondo ColorBase)
- `tabs` (TabView), `closeButton`

**Lógica clave:**
- `Wire()` busca elementos en UIDocument por `name`: "stat-con", "stat-atk", "stat-spd", "stat-def", "stat-lck", "stat-eva"
- `Populate()` llama `CombatService.GetEffectiveStats(dna, database)` o fallback `new EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion)`
- `SetStat(label, name, final, baseVal)` mostrará `CON 45 (40 + 5)` con el bonus calculado
- Los 6 `SetStat()` se llaman para CON/ATK/SPD/DEF/LCK/EVA
- `BuildCombatHistory()` muestra combates en pestaña "Combate" (uno por foldout, más reciente primero)

**Organización (partial class):**
- `MorimonchiDetailInfoUITK.cs` — núcleo + Info + Combat tab + SetStat
- `MorimonchiDetailInfoUITK.Trees.cs` — tabs Linaje/Descendencia
