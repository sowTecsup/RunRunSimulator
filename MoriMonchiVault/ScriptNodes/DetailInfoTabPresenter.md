---
tags: [script, ui, presenter]
---

# DetailInfoTabPresenter.cs

**Ruta:** `UI/DetailInfoTabPresenter.cs`

**Responsabilidad:** Presenter UITK para tab Info (5 filas de partes genéticas, stats via `CreatureStats`, progresión BreedCount).

## Cambios en S75

- **5 filas de partes:** Body/Horn/Back/Wing/Face (reemplazó Body/Arm/Eye/Mouth)
- **Stats:** Via `CreatureStats.GetEffectiveStats()`
- **Progresión:** Solo BreedCount (sin FightCount/WinCount)

## Vinculado a

- [[Index/05 - UI System]]

**Conexiones:** [[CreatureStats]], [[CreatureDNA]]
