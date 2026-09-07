---
tags: [script, world, ui, uitk, expedition]
---

# ArenaPlanPanel.cs

**Ruta:** `World/Expedition/ArenaPlanPanel.cs`

**Responsabilidad:** Panel UITK de planificación pre-ronda. Permite elegir órdenes (S104) por criatura, alternar save local vs roster. Integra picker (S103) y resultado (S103). **S104:** muestra tres pilares (Botín/Encuentro/Equipo) con bloqueos visuales, arquetipo+descripción, contra sugerido, lectura de sala, nombre del plan, lectura de rival.

**Métodos públicos:**
- `void Update()` — gestiona visible/oculto y delay resultado

**UI Structure (S104 NUEVO):**
- `plan-root`
  - `plan-header` — sala, entrada, paleta
  - `plan-room-read` — (S104 NUEVO) descripción de sala: terreno, botín, vetas
  - `cast-list` (ScrollView) — tarjetas criaturas player
    - Cada `cast-card`:
      - Swatch + nombre + raza/nivel
      - **S104:** Tres filas de pilares (Botín / Encuentro / Equipo)
        - Cada pilar: 2-3 pills con bloqueo visual (deshabilitadas si forzado)
      - (S104) Nombre del plan ("Jauría del centro")
      - (S104) Lectura de rival ("puede hacer guardián o cazador")
      - (S104) Contra sugerido ("Frena cazadores...")
  - `plan-rival` — nombres/arquetipos rivales
  - Botones: cast, pick (si LocalSave), shuffle, palette, room (nueva semilla), play

**Métodos Privados (S104 actualizado):**
- `Refresh()` — actualiza room, cast button, construye cards con lectura de sala
- `BuildCard(int index, ArenaCastEntry entry)` → VisualElement — (S104) muestra 3 pilares, bloqueos, arquetipo, contra
- `ChoosePillar(Card state, OrderPillar pillar, int choice)` — (S104 NUEVO) actualiza `sandbox.SetPlayerOrders()`
- `RefreshPillars(Card)` — destaca pills activas, deshabilita si forzado (S104)
- `RefreshArchetype(Card)` — muestra ArchetypeName, ArchetypeDescription, CounterHint (S104)
- `RefreshRoomRead()` — muestra ArenaRoomRead completo (S104)
- `RefreshRivalLine()` — muestra RivalRead (qué pueden hacer) + UnlockRead (S104)
- `ToggleCastMode()` — Roster ↔ LocalSave
- `OpenPicker()` — picker.Open
- `Shuffle()` — sandbox.ShuffleCast
- `CyclePalette()` — sandbox.CyclePalette
- `NewRoom()` — round.Reset(true)
- `Play()` — round.Launch

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

**Invariantes:**
- Pilares forzados deshabilitados (visual feedback claro)
- Lectura de sala + rival: información completa antes de launch
- Nombre del plan ayuda estrategia (equipo cohesivo vs disperso)
- ScrollView para equipos grandes (no hardcodea 3 criaturas)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[ArenaRound]], [[ArenaCastPicker]], [[ArenaResultPanel]], [[ArenaCastEntry]], [[ArenaOrders]], [[ArenaOrderRules]], [[ArenaOrderCatalog]], [[ArenaRoomRead]], [[ArenaLayoutBuilder]]
