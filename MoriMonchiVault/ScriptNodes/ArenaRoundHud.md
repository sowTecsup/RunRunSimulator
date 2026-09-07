---
tags: [script, world, ui, exposition, expedition]
---

# ArenaRoundHud.cs

**Ruta:** `World/Expedition/ArenaRoundHud.cs`

**Responsabilidad:** HUD de ronda UITK en vivo. Muestra tarjetas estilo Pokémon Quest por equipo (arriba/abajo o lado), score, timer (S104: con control de pausa/velocidad vía ArenaClockControl), equipo rival como fichas. Cada tarjeta: swatch, nombre, ocupación+intención, minería en progreso, carga. Cache de strings y valores para evitar DOM ediciones innecesarias. **S104:** tarjetas rediseñadas, control de tiempo (1x/2x/5x/10x + pausa con Space/1234), filas/fichas de rivales.

**Métodos públicos:**
- `void Update()` — tick principal

**Métodos privados:**
- `RefreshSeed()` — "sala NNNN"
- `RefreshScores()` — Player score | Rival score
- `RefreshTime()` — cronómetro con warn
- `RefreshRoster()` — construye tarjetas/fichas por equipo
- `BuildCard(MoriMochiAgent agent)` → VisualElement — (S104 reescritura)
- `BuildRivalCard(MoriMochiAgent agent)` → VisualElement — (S104 NUEVO) fichas compactas
- `Verb(Occupation)` → string — descripción de rol

**UI Structure (S104 reescrito):**
- `hud-root`
  - `hud-header`
    - `hud-seed`, `hud-scores`, `hud-timer` con `hud-speed-label` (S104)
  - `hud-player-cards` — tarjetas player (Pokémon Quest style)
  - `hud-rival-cards` — fichas rival (compactas) (S104)
  - Overlay resultado si IsOver

**Tarjeta Pokémon Quest (S104):**
- Forma redondeada
- Swatch color + borde
- Nombre encima
- Ocupación/Intención debajo
- Barra de minería con animación
- Cristales llevados (ej. "◆◆◆")

**Ficha Rival (S104 NUEVO):**
- Compacta, sin minería (no necesaria)
- Nombre + Orders resume (ej. "Guardián")
- Color por equipo

**Control de Tiempo (S104 NUEVO via ArenaClockControl):**
- 1-4 keys: cambia velocidad
- Space: pausa/resume
- Display "1x | 2x | 5x | 10x | ⏸ PAUSE"

**Campos Serializados:**
- `round` [Required]
- `warnSeconds` [Min(0)] = 15
- `clockControl` [Required] — (S104) para leer Speed/Paused

**Invariantes:**
- Caché de strings evita spam DOM
- Tarjetas por equipo separadas (arriba/abajo o lado)
- Rival cards sin barras de minería (simplificadas)
- Timer con coloración dinámica (rojo si ≤ warn)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaRound]], [[ArenaClockControl]], [[MoriMochiAgent]], [[CreatureDNA]], [[ArenaOrders]], [[Occupation]], [[CreatureIntent]]
