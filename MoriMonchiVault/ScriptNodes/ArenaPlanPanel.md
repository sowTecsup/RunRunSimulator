---
tags: [script, world, ui, uitk, expedition]
---

# ArenaPlanPanel.cs

**Ruta:** `World/Expedition/ArenaPlanPanel.cs`

**Responsabilidad:** Panel UITK de planificación pre-ronda y resultado post-ronda. Permite elegir órdenes (S104) por criatura, alternar save local vs roster. Integra picker (S103) y resultado (S103). **S104:** muestra tres pilares (Botín/Encuentro/Equipo) con bloqueos visuales, arquetipo+descripción, lectura de sala. **S107:** Integra MonchiTurntable para preview spinning 3D. **S111:** Muestra nombre de forma en cabecera. **S115:** Tarjeta rival muestra naturaleza primero, nombre debajo (reordenado); se eliminó label "→ RivalRead" (lectura rival se integra en el card directamente). **S119:** Método ReturnToStore() usa ExpeditionHandoff.ReturnToStore(lastResult) para viajar a tienda.

**Métodos públicos:**
- `void Update()` — gestiona visible/oculto, delay resultado, sincroniza turntable

**Métodos Privados (S119 NUEVO):**
- `void ReturnToStore()` — dispara ExpeditionHandoff.ReturnToStore(lastResult); limpia lastResult

**UI Structure (S104-S107-S111-S115):**
- `plan-root`
  - `plan-header` — S111: "Forma · sala NNNN · entrada · paleta"
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
  - `plan-rival` — S115: naturaleza + nombres arquetipos rivales (reordenado)
  - Botones: cast, pick, shuffle, palette, room, play, **return** (S119 NUEVO)

**Campos Serializados (S107):**
- `turntable` (MonchiTurntable, Optional) — renderizador de previews spinning

**Campos Privados (S119 ACTUALIZADO):**
- `lastResult` (ExpeditionResult?, S119) — resultado de última ronda, used por ReturnToStore()

**Métodos Privados (S104+S107+S111-S115):**
- `void SetVisible(bool value)` — oculta/muestra panel, limpia turntable si oculto
- **S111:** `void Refresh()` — actualiza cabecera con "ShapeName · sala NNNN · entrada · paleta"
- `VisualElement BuildCard(...)` → construye tarjeta de criatura con 3 pilares + turntable preview
- `void ChoosePillar(Card, OrderPillar, int)` — actualiza sandbox.SetPlayerOrders()
- `void RefreshPills(Card)` — destaca pills activas
- `void RefreshTeamLine()` — **S111:** incluye ShapeName en cabecera (refreshRoomLabel)
- `void RefreshRivalLine()` — **S115:** muestra naturales primero, nombres debajo (sin label "→ RivalRead")
- `void ToggleCastMode()` — Roster ↔ LocalSave
- `void OpenPicker()` — picker.Open
- `void Shuffle()` — sandbox.ShuffleCast
- `void CyclePalette()` — sandbox.CyclePalette
- `void NewRoom()` — round.Reset(true)
- `void Play()` — round.Launch
- **S119 NUEVO:** `void ReturnToStore()` — ExpeditionHandoff.ReturnToStore(lastResult)

**RefreshTeamLine (S111):**
1. Itera PlannedCast, colecta órdenes de player
2. plan = ArenaOrderCatalog.TeamPlanName(orders)
3. shape = sandbox.ShapeName (o "")
4. room = f"sala {ActiveSeed}{shape} · {PaletteName} · entrada {EntryName}"
5. read = ReadRoom(Player) con descripción (obstáculos, botín)
6. Muestra: room + read + "Tu plan: " + plan

**RefreshRivalLine (S115 ACTUALIZADO):**
1. Itera PlannedRivals, colecta naturales + nombres arquetipos
2. S115 CAMBIO: orden = naturaleza primero, nombre debajo (reordenado)
3. S115 CAMBIO: se eliminó label "→ RivalRead" — lectura se integra en card (sin separación explícita)
4. Muestra: naturales + arquetipos en dos líneas (naturaleza + nombre)
5. Contexto: simplifica UI, evita redundancia de etiqueta

**Turntable Integration (S107):**
- Si turntable != null: turntable.Show(slotIndex, dna, previewElement)
- OnDisable: turntable.HideAll()

**Flujo Round Finish (S119 ACTUALIZADO):**

1. ArenaRound.IsRunning → SetVisible(false), esconde panel
2. ArenaRound.IsOver → captura Winner/PlayerSecured/RivalSecured
3. Construye lastResult con Seed, Winner, materiales, Stats (S119)
4. Espera resultHoldSeconds (display de resultado visual)
5. Llama resultPanel.Show() con los datos
6. SetVisible(true) — retorna a panel plan
7. Botón "Volver a tienda" llama ReturnToStore() → ExpeditionHandoff.ReturnToStore(lastResult)

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

**S119 Cambios:**
- **ReturnToStore() método nuevo:** Dispara ExpeditionHandoff.ReturnToStore(lastResult)
- lastResult capturado en Update() cuando round.IsOver
- lastResult limpiado tras ReturnToStore() o al lanzar nueva ronda
- returnButton wiring: `returnButton.clicked += ReturnToStore`

**Invariantes:**
- Pilares forzados deshabilitados (feedback visual)
- Lectura completa: sala + rival + forma
- Turntable opcional (fallback OK)
- ScrollView para equipos grandes
- S115: Tarjeta rival compacta: naturaleza primero para claridad (prioridad visual)
- S119: lastResult preservado hasta ser consumido por ExpeditionHandoff

**Vinculado a:** [[Index/22 - Bajada Nocturna y Linaje]], [[Index/23 - Arena Sandbox y Expedicion]], [[Index/24 - Puente Tienda-Arena]], S115, S119

**Conexiones:** [[ArenaSandbox]], [[ArenaRound]], [[ArenaCastPicker]], [[ArenaResultPanel]], [[ArenaCastEntry]], [[ArenaOrders]], [[ArenaOrderRules]], [[ArenaOrderCatalog]], [[ArenaRoomRead]], [[MonchiTurntable]], [[ArenaLayoutBuilder]], [[ExpeditionHandoff]], [[ExpeditionResult]]
