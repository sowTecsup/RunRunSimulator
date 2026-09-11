---
tags: [script, world, ui, uitk, expedition]
---

# ArenaPlanPanel.cs

**Ruta:** `World/Expedition/ArenaPlanPanel.cs`

**Responsabilidad:** Panel UITK de planificación pre-ronda. Permite elegir órdenes (S104) por criatura, alternar save local vs roster. Integra picker (S103) y resultado (S103). **S104:** muestra tres pilares (Botín/Encuentro/Equipo) con bloqueos visuales, arquetipo+descripción, lectura de sala. **S107:** Integra MonchiTurntable para preview spinning 3D. **S111:** Muestra nombre de forma en cabecera. **S115:** Tarjeta rival muestra naturaleza primero, nombre debajo (reordenado); se eliminó label "→ RivalRead" (lectura rival se integra en el card directamente).

**Métodos públicos:**
- `void Update()` — gestiona visible/oculto, delay resultado, sincroniza turntable

**UI Structure (S104-S107-S111-S115):**
- `plan-root`
  - `plan-header` — **S111:** "Forma · sala NNNN · entrada · paleta"
  - `plan-room-read` — descripción de sala (S104)
  - `cast-list` (ScrollView) — tarjetas criaturas player
    - Cada `cast-card`:
      - Slot preview turntable (S107)
      - Swatch + nombre + stats
      - Tres filas de pilares (Botín / Encuentro / Equipo)
        - Cada pilar: 2 pills (bloqueo visual si forzado)
      - Nombre del plan
      - Lectura de rival
      - Contra sugerido
  - `plan-rival` — **S115:** naturaleza + nombres arquetipos rivales (reordenado)
  - Botones: cast, pick, shuffle, palette, room, play

**Campos Serializados (S107):**
- `turntable` (MonchiTurntable, Optional) — renderizador de previews spinning

**Métodos Privados (S104+S107+S111-S115):**
- **S111:** `Refresh()` — actualiza cabecera con "ShapeName · sala NNNN · entrada · paleta"
- `BuildCard(...)` → VisualElement — muestra 3 pilares + turntable preview
- `ChoosePillar(Card, OrderPillar, int)` — actualiza sandbox.SetPlayerOrders()
- `RefreshPills(Card)` — destaca pills activas
- `RefreshTeamLine()` — **S111:** incluye ShapeName en cabecera (refreshRoomLabel)
- `RefreshRivalLine()` — **S115:** muestra naturales primero, nombres debajo (sin label "→ RivalRead")
- `ToggleCastMode()` — Roster ↔ LocalSave
- `OpenPicker()` — picker.Open
- `Shuffle()` — sandbox.ShuffleCast
- `CyclePalette()` — sandbox.CyclePalette
- `NewRoom()` — round.Reset(true)
- `Play()` — round.Launch

**RefreshTeamLine (S111):**
1. Itera PlannedCast, colecta órdenes de player
2. plan = ArenaOrderCatalog.TeamPlanName(orders)
3. shape = sandbox.ShapeName (o "")
4. room = f"sala {ActiveSeed}{shape} · {PaletteName} · entrada {EntryName}"
5. read = ReadRoom(Player) con descripción (obstáculos, botín)
6. Muestra: room + read + "Tu plan: " + plan

**RefreshRivalLine (S115 ACTUALIZADO):**
1. Itera PlannedRivals, colecta naturales + nombres arquetipos
2. **S115 CAMBIO:** orden = naturaleza primero, nombre debajo (reordenado)
3. **S115 CAMBIO:** se eliminó label "→ RivalRead" — lectura se integra en card (sin separación explícita)
4. Muestra: naturales + arquetipos en dos líneas (naturaleza + nombre)
5. Contexto: simplifica UI, evita redundancia de etiqueta

**Turntable Integration (S107):**
- Si turntable != null: turntable.Show(slotIndex, dna, previewElement)
- OnDisable: turntable.HideAll()

**S103 Cambios:**
- Picker integrado
- Resultado integrado

**S104 Cambios:**
- Tres pilares (Botín/Encuentro/Equipo)
- Bloqueos visuales
- Arquetipos + descripciones
- Lectura de sala detallada

**S107 Cambios:**
- Campo `turntable` para preview spinning 3D
- BuildCard() asigna slot turntable.Show()

**S111 Cambios:**
- RefreshTeamLine() ahora incluye ShapeName en la cabecera
- Display: "sala NNNN {forma} · {paleta} · entrada {nombre}"

**S115 Cambios:**
- `RefreshRivalLine()` reordena: naturaleza (línea 1) + nombre (línea 2)
- Elimina label "→ RivalRead" (lectura se integra en card sin etiqueta separada)
- Simplifica visual: menos labels flotantes, información más compacta

**Invariantes:**
- Pilares forzados deshabilitados (feedback visual)
- Lectura completa: sala + rival + forma
- Turntable opcional (fallback OK)
- ScrollView para equipos grandes
- **S115:** Tarjeta rival compacta: naturaleza primero para claridad (prioridad visual)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], S115

**Conexiones:** [[ArenaSandbox]], [[ArenaRound]], [[ArenaCastPicker]], [[ArenaResultPanel]], [[ArenaCastEntry]], [[ArenaOrders]], [[ArenaOrderRules]], [[ArenaOrderCatalog]], [[ArenaRoomRead]], [[MonchiTurntable]], [[ArenaLayoutBuilder]]

