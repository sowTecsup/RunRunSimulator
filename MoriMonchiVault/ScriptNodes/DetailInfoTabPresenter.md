---
tags: [script, ui, presenter]
---

# DetailInfoTabPresenter.cs

**Ruta:** `UI/DetailInfoTabPresenter.cs`

**Responsabilidad:** Presenter UITK para tab Info (5 filas de partes genéticas, stats via `CreatureStats`, progresión BreedCount). **S93:** Usa `CreatureDisplay.StateOf()`. **S95:** AddPartRow recibe `int potential` en lugar de `Tier`; muestra potencial para Horn/Back/Wing si > 0, o "empty" si sin potencial (Body/Face nunca tienen potencial).

**Vinculado a:** [[Index/05 - UI System]], [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[CreatureStats]], [[CreatureDNA]], [[CreatureDisplay]], [[DetailInfoTabUITK]]

