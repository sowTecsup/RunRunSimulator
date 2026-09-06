---
tags: [script, ui, expedition, presentation]
---

# ArenaRoundHud.cs

**Ruta:** `World/Expedition/ArenaRoundHud.cs`

**Responsabilidad:** Presentador de UI dinámica vía UIToolkit (sin UXML, construcción completa por código en OnEnable). Paneles: scoreboard (Player | Timer | Rival en fila, arriba centro), roster (nombres + verbos de ocupación de Monchis activos, por equipo), resultado (Gana tu equipo / Gana el rival / Empate, visible solo post-cierre), etiqueta de semilla (esquina arriba-izq). Actualiza cada frame solo si hay cambio (caché de últimos textos para minimizar ediciones DOM). Renderiza ocupaciones con `Verb()` (vigila/rompe/distrae/explora/recolecta).

## Campos serializados

- **round:** referencia a [[ArenaRound]] para lectura de estado vivo
- **playerColor:** color para texto Player (default #C2FF99, RGB 0.76, 1, 0.6)
- **rivalColor:** color para texto Rival (default #F49999, RGB 0.96, 0.6, 0.6)
- **timeColor:** color para Timer (default white)

## Elementos UIToolkit (instanciados en OnEnable)

- **scoreboard:** VisualElement flex row, posición absoluta arriba centro (top: 14px)
  - playerLabel: Label 30pt, bold, playerColor
  - timeLabel: Label 36pt, bold, timeColor (margen 28px a cada lado)
  - rivalLabel: Label 30pt, bold, rivalColor
- **rosterRoot:** VisualElement flex row, debajo de scoreboard (top: 60px)
  - playerRoster: Label 19pt, bold, playerColor, alineación derecha
  - rivalRoster: Label 19pt, bold, rivalColor, alineación izquierda
- **seedLabel:** Label 16pt, esquina arriba-izq (top: 14px, left: 16px), opacidad 0.7
- **resultRoot:** VisualElement flex center
  - resultLabel: Label 26pt, bold, oculto por defecto (display: None hasta IsOver)

## Métodos privados

- `RefreshRoster()` — itera `round.Sandbox.Spawned`, agrupa por Team, construye strings con nombre + Verb(ocupación)
- `RefreshSeed()` — lee `sandbox.ActiveSeed`, renderiza "sala NNNN"
- `Verb(Occupation) → string` — mapea ocupación a verbo: Guard→"vigila", Break→"rompe", Decoy→"distrae", Explore→"explora", default→"recolecta"
- `Update()` — tick principal:
  - RefreshRoster() si cambió count de Spawned
  - RefreshSeed() si cambió ActiveSeed
  - actualiza playerLabel si round.PlayerSecured cambió (cache lastPlayerText)
  - actualiza rivalLabel si round.RivalSecured cambió
  - actualiza timeLabel con formato MM:SS si round.Remaining cambió
  - si round.IsOver y no resultShown:
    - resultLabel.display = Flex
    - switch round.Winner: determina texto y color (Gana tu equipo / Gana el rival / Empate)
    - actualiza solo si texto cambió (cache lastResultText)

## Ciclo de vida

1. **OnEnable:** instancia UI tree completa (scoreboard, rosterRoot, seedLabel, resultRoot), limpia versiones anteriores
2. **Update:** cada frame chequea cambios, actualiza solo labels que cambiaron
3. **OnDisable:** limpia todos elementos (RemoveFromHierarchy)

## Invariantes S101

- UI se construye completamente en OnEnable (no hay archivo UXML/USS)
- Cache de strings (lastPlayerText, lastTimeText, etc.) previene ediciones innecesarias de DOM
- resultLabel está oculto hasta que round.IsOver = true
- Verb() es función pura (sin estado)
- RefreshRoster() no hace nada si count == lastRosterCount (evita trabajo inútil)
- RefreshSeed() no hace nada si seedText == lastSeedText

## Conexiones

**Entrada:**
- Lectura: `round.PlayerSecured`, `round.RivalSecured`, `round.Remaining`, `round.IsOver`, `round.Winner`
- Lectura: `round.Sandbox.Spawned`, `round.Sandbox.ActiveSeed`
- Lectura: cada `agent.Team`, `agent.DNA.CustomName`, `agent.Occupation`

**Salida:**
- Actualización de Labels de UIToolkit (rasterización en pantalla)

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[ArenaRound]]
- [[ArenaSandbox]]
- [[MoriMochiAgent]]
- [[CreatureDNA]]
