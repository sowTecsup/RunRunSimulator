---
tags: [script, ui, arena, expedition, presentation]
---

# ArenaHudCard.cs

**Ruta:** `UI/ArenaHudCard.cs` (presentador, no MonoBehaviour)

**Responsabilidad:** Constructor de tarjeta de HUD para un agente en la arena. Genera VisualElement con nombre, color, órdenes (3 pilares Loot/Contact/Posture), intención actual, carreo de minerales con barra de progreso (Taking), y 3 slots radiales de habilidades. Maneja estado visual dinámico: pulsación por golpe recibido, dim de poderes Damage si Flee activo, índice de carga, carga de habilidades.

**Responsabilidad:** Presentación pura; lee estado vivo del agente y actualiza UIElements cada frame sin emitir eventos.

**Campos Públicos:**
- `Agent` (MoriMochiAgent) — referencia al agente
- `Root` (VisualElement) — contenedor raíz del card (PickingMode.Position)

**Construcción (ArenaHudCard constructor):**
1. Header: swatch de color + nombre + postura (archetype short label)
2. Pillars: 3 chips (Loot/Contact/Posture) con labels de Choice
3. Action: label de intención actual
4. Carry: N slots visuales (N = capacity) con barra de progreso si Taking
5. Powers: 3 RadialSlot (uno por habilidad) con nombre y color

**Estados internos (Refresh):**
- `lastAction` — intención renderizada
- `lastCapacity`, `lastCarried`, `lastMining`, `lastMiningProgress` — carry
- `lastFired[3]`, `lastDim[3]` — abilities (cooldown y dim)
- `lastKnocked`, `hitUntil` — feedback visual de golpe
- `lastSelected`, `lastChase` — estilos CSS

**Métodos Públicos:**
- `Refresh(bool selected)` — actualiza acción, carry, poderes, feedback hit, clases CSS de selección/chase

**Internals:**
- `RefreshCarry()` — remapea slots si capacity cambió, renderiza progreso de minado
- `RefreshPowers()` — sincroniza carga de 3 radials, dispara `Pulse()` si ability fired, dim si Flee

**Callback (wired en constructor):**
- Click en Root → `onTapped?.Invoke(Agent)`
- Usado por ArenaRoundHud para TogglePin

**Integración:**
- Creado por ArenaRoundHud.RefreshRoster() para cada agente jugador
- Refrescado cada frame en ArenaRoundHud.Update()

**S107 (NUEVO):**
- Extrae del interior de ArenaRoundHud la lógica de tarjeta de equipo, permitiendo reutilización
- Usada por ArenaRoundHud para team Player, mientras que Rival usa RivalChip interno más simple

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaRoundHud]], [[RadialSlot]], [[MoriMochiAgent]], [[AgentAbilities]], [[ArenaOrderCatalog]], [[LocEnumMaps]]
