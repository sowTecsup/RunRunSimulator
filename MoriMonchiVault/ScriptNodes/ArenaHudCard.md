---
tags: [script, ui, arena, expedition, presentation]
---

# ArenaHudCard.cs

**Ruta:** `World/Expedition/ArenaHudCard.cs` (presentador, no MonoBehaviour)

**Responsabilidad:** Constructor de tarjeta de HUD para un agente en la arena. Genera VisualElement con nombre, color, órdenes (3 pilares Loot/Contact/Posture), intención actual, carreo de minerales con barra de progreso (Taking), y 3 slots radiales de habilidades. S109: Habilidades pasivas renderizadas con clase CSS `hud-power--passive` y RadialSlot con color semi-transparente (alfa 0.55) para diferenciación visual. Maneja estado visual dinámico: pulsación por golpe recibido, dim de poderes Damage si Flee activo, índice de carga, carga de habilidades. S115: Campo `lastDazed` detecta transición a/desde intent Dazed/Tumbling; alterna clase USS `hud-card--dazed` (opacidad 0.4) para feedback visual de noqueado. **S118:** Super abilities muestran Armed state (doble anillo en RadialSlot) cuando solicitado por jugador.

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
   - S109: Si ability.Kind == AbilityKind.Passive:
     - Agrega clase CSS `hud-power--passive`
     - Asigna `radial.ReadyColor = Color(ability.Color.r/g/b, 0.55f)` (semi-transparente)

**Estados internos (Refresh):**
- `lastAction` — intención renderizada
- `lastCapacity`, `lastCarried`, `lastMining`, `lastMiningProgress` — carry
- `lastFired[3]`, `lastDim[3]` — abilities (cooldown y dim)
- `lastKnocked` — feedback visual de golpe
- `lastSelected`, `lastChase` — estilos CSS
- `lastDazed` — S115: estado noqueado (Dazed/Tumbling)

**Métodos Públicos:**
- `Refresh(bool selected)` — actualiza acción, carry, poderes, feedback hit, clases CSS de selección/chase/dazed

**Internals:**

- `RefreshCarry()` — remapea slots si capacity cambió, renderiza progreso de minado

- `RefreshPowers()` — sincroniza carga de 3 radials, dispara `Pulse()` si ability fired, dim si Flee, **S109:** renderiza pasivas con color semi-transparente, **S118:** sincroniza Armed state

**Callback (wired en constructor):**
- Click en Root → `onTapped?.Invoke(Agent)`
- Usado por ArenaRoundHud para TogglePin

**Integración:**
- Creado por ArenaRoundHud.RefreshRoster() para cada agente jugador
- Refrescado cada frame en ArenaRoundHud.Update()

**S107 (Inicial):**
- Extrae del interior de ArenaRoundHud la lógica de tarjeta de equipo, permitiendo reutilización
- Usada por ArenaRoundHud para team Player, mientras que Rival usa RivalChip interno más simple

**S109 Cambios:**

- Constructor: al iterar abilities (línea de powers):
  - Si `ability != null && ability.Kind == AbilityKind.Passive`:
    - `power.AddToClassList("hud-power--passive")` — marca visualmente como pasiva
    - `radial.ReadyColor = new Color(ability.Color.r, ability.Color.g, ability.Color.b, 0.55f)` — color semi-transparente
  - Diferencia: Damage/Mobility radial muestra carga (desde 0 a 1); Passive radial siempre semi-opaco (nunca carga)
- CSS class `hud-power--passive` permite styling distinto (ej: borde, fondo tenue, sin animación de carga)

**S115 Cambios:**

Campos línea 33:
```csharp
private bool lastDazed;
```
- Nuevo: bandera de estado noqueado detectada

Refresh() línea 154-159:
```csharp
bool dazed = agent.Intent == CreatureIntent.Dazed || agent.Intent == CreatureIntent.Tumbling;
if (dazed != lastDazed)
{
    lastDazed = dazed;
    Root.EnableInClassList("hud-card--dazed", dazed);
}
```
- Calcula si agente está noqueado (Dazed O Tumbling)
- Si cambio: actualiza `lastDazed` y alterna clase USS `hud-card--dazed`
- Clase CSS aplicada cuando `dazed = true`, removida cuando `false`

Clase USS `hud-card--dazed` (styling):
- Opacidad 0.4 cuando aplicada (criatura está "aturdida", visual atenuado)
- Transición suave vía CSS transitions (si están configuradas en stylesheet)
- Diferencia visual clara de estado noqueado vs. normal

Impacto S115:
- Card de agente noqueado se oscurece/atenúa visualmente
- Transición instantánea al entrar/salir de Dazed
- Feedback visual que complementa a MonchiMoodDriver (expresión facial) y ArenaCueOverlay (sin guías)
- Indica al jugador que criatura está temporalmente fuera de acción

**S118 Cambios:**

RefreshPowers() sincroniza Armed para cada radial:
```csharp
radials[i].Armed = agent.abilities.IsRequested(i) && agent.AbilityCharge01(i) >= 0.999f;
```

- Si habilidad i es Super (Kind == Damage, Role == Super) y Charge >= 1.0 y Requested:
  - `Armed = true` → RadialSlot dibuja doble anillo interior (visual de "armado")
- Si Super no solicitado O Charge < 1.0:
  - `Armed = false` → RadialSlot sin anillo interior

- Integración con UI:
  - Jugador toca RadialSlot en pantalla → llama onPowerTapped(agent, i)
  - ArenaRound.Request(i) → agent.abilities.Request(i) → Requested = true
  - RefreshPowers() detecta Requested && Charge >= 1.0 → Armed = true
  - Visual: anillo interior muestra Super "armado y listo a disparar por jugador"

## Invariantes S109-S118

- Un card por agente Player (rival tiene chip más simple)
- Pasivas nunca muestran barra de carga (siempre listas, semi-transparente)
- Damage Basic/Mobility muestran carga 0→1 según Charge01, dim si Flee
- Damage Super muestran carga acumulable, Armed cuando solicitado+listo
- Clase CSS permite UX diferenciado: pasivas como "siempre activas", activas como "cargan/disparan"
- Armed state es visual-only (no afecta lógica, solo UI feedback de intención del jugador)

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion]], [[Index/22 - Bajada Nocturna y Linaje]], S118

**Conexiones:** [[ArenaRoundHud]], [[RadialSlot]], [[AgentAbilities]], [[AbilitySO]], [[MoriMochiAgent]], [[ArenaOrderCatalog]]
