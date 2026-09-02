---
tags: [script, combate, dragon-rps, genetics]
---

# DragonRpsGenes.cs

**Ruta:** `Systems/Combat/DragonRpsGenes.cs`

**Responsabilidad:** Conversión de genética de criatura (`CreatureDNA`) a dragón de combate (`DragonRpsDragon`). Métodos: `PowerOf()` clampea potencial 1-10 a rango válido; `ToDragon()` convierte DNA a dragón con potencias mapeadas a tipos (Cuernos/Alas/Espalda); `Budget()` suma las 3 potencias; `CanFight()` valida si una criatura puede pelear (no muerta, no vendida, no ocupada, fuera cooldown, energía suficiente).

**Vinculado a:** [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsDragon]], [[CreatureDNA]], [[CreatureGenerator]], [[CombatTuningSO]], [[DragonRpsService]]
