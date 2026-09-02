---
tags: [script, combate, dragon-rps]
---

# DragonRpsRival.cs

**Ruta:** `Systems/Combat/DragonRpsRival.cs`

**Responsabilidad:** Generador de rival para demo combate. Método `Generate()` clona una criatura viva aleatoria (excepto jugador), re-coloriza deterministamente (RandomBase + DeriveSecondary), copia pelaje y tier, renombra con prefijo "Salvaje". Regenéra potenciales en rango [min-1, max+1] (donde min/max = jugador) hasta 32 intentos para igualar presupuesto ±tolerancia; fallback = potenciales del jugador. Nunca genera Timestamp ni registra (es rival temporal de sesión, no persistente).

**Vinculado a:** [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsGenes]], [[CreatureDNA]], [[ColorGenetics]], [[CreatureRegistrySO]], [[CombatTuningSO]]
