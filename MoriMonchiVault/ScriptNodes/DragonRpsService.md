---
tags: [script, combate, dragon-rps, core]
---

# DragonRpsService.cs

**Ruta:** `Systems/Combat/DragonRpsService.cs`

**Responsabilidad:** Orquestación de sesión de combate. Único dueño de mutación: `Seed()` deriva número aleatorio desde Timestamp DNA ⊕ now.Ticks; `Start()` crea sesión nueva con dragones convertidos y seed; `Resolve()` aplica resultado si sesión terminó: victoria → suma material a inventario + dispara `InventoryChanged`; derrota → establece cooldown en DNA + dispara `RegistryChanged`. Sesión no terminada retorna default sin mutar.

**Vinculado a:** [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsSession]], [[DragonRpsGenes]], [[CombatOutcome]], [[CreatureDNA]], [[PlayerInventorySO]], [[CombatTuningSO]], [[CreatureRegistrySO]], [[GameEvents]]
