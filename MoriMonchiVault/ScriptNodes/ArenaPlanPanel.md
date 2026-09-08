---
tags: [script, world, ui, uitk, expedition]
---

# ArenaPlanPanel.cs

**Ruta:** `World/Expedition/ArenaPlanPanel.cs`

**Responsabilidad:** Panel UITK de planificación pre-ronda. Permite elegir órdenes (S104) por criatura, alternar save local vs roster. Integra picker (S103) y resultado (S103). **S104:** muestra tres pilares (Botín/Encuentro/Equipo) con bloqueos visuales, arquetipo+descripción, contra sugerido, lectura de sala, nombre del plan, lectura de rival. **S107:** Integra MonchiTurntable para preview spinning 3D de criaturas seleccionadas.

**Métodos públicos:**
- `void Update()` — gestiona visible/oculto, delay resultado, sincroniza turntable

**UI Structure (S104-S107):**
- `plan-root`
  - `plan-header` — sala, entrada, paleta
  - `plan-room-read` — (S104) descripción de sala: terreno, botín, vetas
  - `cast-list` (ScrollView) — tarjetas criaturas player
    - Cada `cast-card`:
      - **S107:** Slot para preview turntable (background image slot)
      - Swatch + nombre + raza/nivel
      - Tres filas de pilares (Botín / Encuentro / Equipo)
        - Cada pilar: 2-3 pills con bloqueo visual (deshabilitadas si forzado)
      - Nombre del plan ("Jauría del centro")
      - Lectura de rival ("puede hacer guardián o cazador")
      - Contra sugerido ("Frena cazadores...")
  - `plan-rival` — nombres/arquetipos rivales
  - Botones: cast, pick (si LocalSave), shuffle, palette, room (nueva semilla), play

**Campos Serializados (S107):**
- `turntable` (MonchiTurntable, Optional) — renderizador de previews spinning

**Métodos Privados (S104 actualizado, S107 extendido):**
- `Refresh()` — actualiza room, cast button, construye cards con lectura de sala
- `BuildCard(int index, ArenaCastEntry entry)` → VisualElement — muestra 3 pilares, bloqueos, arquetipo, contra, **S107:** + turntable preview
- `ChoosePillar(Card state, OrderPillar pillar, int choice)` — actualiza `sandbox.SetPlayerOrders()`, **S107:** trigger refresh de preview
- `RefreshPillars(Card)` — destaca pills activas, deshabilita si forzado
- `RefreshArchetype(Card)` — muestra ArchetypeName, ArchetypeDescription, CounterHint
- `RefreshRoomRead()` — muestra ArenaRoomRead completo
- `RefreshRivalLine()` — muestra RivalRead (qué pueden hacer) + UnlockRead
- `ToggleCastMode()` — Roster ↔ LocalSave
- `OpenPicker()` — picker.Open
- `Shuffle()` — sandbox.ShuffleCast
- `CyclePalette()` — sandbox.CyclePalette
- `NewRoom()` — round.Reset(true)
- `Play()` — round.Launch

**Turntable Integration (S107 NUEVO):**
- Si turntable != null en Refresh():
  - Por cada card con DNA válido: turntable.Show(slotIndex, dna, previewElement)
  - Permite visualizar modelo 3D spinning antes de launch
  - Slot determina cual de los 3 disponibles usa (pooling)
- OnDisable: turntable.HideAll() para cleanup

**S103 Cambios:**
- Picker integrado
- Resultado integrado
- Explore ocupación + píldora

**S104 Cambios:**
- Tres pilares en lugar de ocupación/sitio (más visible)
- Bloqueos visuales (grises, deshabilitados si forzado por DNA)
- Arquetipos dinámicos + descripciones + contras (ArenaOrderCatalog)
- Lectura de sala detallada (terreno, obstáculos, botín, vetas)
- Lectura de rival (qué DNA permite, qué desbloquea con stats)
- Nombre del plan por composición de equipo
- SetPlayerOrders en lugar de SetPlayerPlan
- Sandbox.ReadRoom() para estadísticas

**S107 Cambios:**
- Campo `turntable` (MonchiTurntable, Optional) para preview spinning 3D
- BuildCard() ahora asigna slot turntable.Show() si turntable != null
- ChoosePillar() puede trigger refresh de turntable preview
- OnDisable() llama turntable.HideAll() para limpiar RenderTextures

**Invariantes:**
- Pilares forzados deshabilitados (visual feedback claro)
- Lectura de sala + rival: información completa antes de launch
- Nombre del plan ayuda estrategia (equipo cohesivo vs disperso)
- ScrollView para equipos grandes (no hardcodea 3 criaturas)
- Turntable opcional: si null, no renderiza previews (fallback OK)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[ArenaRound]], [[ArenaCastPicker]], [[ArenaResultPanel]], [[ArenaCastEntry]], [[ArenaOrders]], [[ArenaOrderRules]], [[ArenaOrderCatalog]], [[ArenaRoomRead]], [[MonchiTurntable]], [[ArenaLayoutBuilder]]
