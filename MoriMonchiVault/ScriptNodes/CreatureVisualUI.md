---
tags: [script, ui]
---

# CreatureVisualUI.cs

**Ruta:** `UI/CreatureVisualUI.cs`

**Responsabilidad:** Renderiza criatura en 3D dentro de paneles UI (RenderTexture). **S57b:** Icono swatch ahora es retrato fotomatón vía [[MonchiPortraitUI]].Apply() en lugar de backgroundColor BaseColor. **S75:** Eliminado estado "In Queue" que requería QueuedForCombat (desapareció en demolición del combate). **S93:** Estado usa `CreatureDisplay.StateOf()`.

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[MorimonchiDetailInfoUITK]], [[MoriMonchiVisualizer]], [[MonchiPortraitUI]], [[CreatureDisplay]]
