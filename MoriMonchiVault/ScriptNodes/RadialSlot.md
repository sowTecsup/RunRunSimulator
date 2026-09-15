---
tags: [script, ui, visualization, custom-element]
---

# RadialSlot.cs

**Ruta:** `UI/RadialSlot.cs`

**Responsabilidad:** Elemento UIElements personalizado que dibuja un indicador de carga radial (pie slice que se llena de 0° a 360°). Usado en ArenaHudCard para mostrar carga de 3 habilidades (Damage Basic/Super, Mobility, Passive). Pinta círculo track + sector carga, con borde de ready y anim pulsación al disparar. **S118:** Armed state muestra anillo interior cuando Super solicitado por jugador, combinado con Charge01 y Pulse.

**Base:** VisualElement customizado con `generateVisualContent` callback

**Propiedades Públicas:**

- `float Charge01 { get; set; }` — carga normalizada [0,1]:
  - Setter clampea a [0,1], ignora cambios < 0.005f (hysteresis), marca MarkDirtyRepaint()
  - Getter retorna valor interno clamped

- `Color FillColor { get; set; }` — color del sector cargado (white defecto)

- `Color TrackColor { get; set; }` — color del círculo base (white 0.1 alpha defecto)

- `Color ReadyColor { get; set; }` — color cuando carga == 1.0 (white 0.22 alpha defecto)

- **`bool Armed { get; set; }`** — **S118 NUEVO:** estado "armado" (Super solicitado):
  - Setter: si cambia, activa clase CSS "radial--armed", marca MarkDirtyRepaint
  - Getter: retorna bool interno
  - Visual: dibuja anillo interior adicional cuando Armed && Charge >= 1.0

**Constructor:**
- Suscribe a `generateVisualContent += OnGenerate`
- pickingMode = PickingMode.Position (interactuable)

**Método Privado:**

- `void OnGenerate(MeshGenerationContext mgc)` — dibuja:
  1. **Entrada:** painter2D, contentRect (tamaño del elemento)
  2. **Track círculo:** arco 0-360°, color TrackColor
  3. **Si Charge01 > 0.001:**
     - Sector: MoveTo(center), Arc(-90° a -90°+Charge01*360°), LineTo(center), ClosePath
     - Color: ReadyColor si (Charge01 >= 0.999 O Armed), sino FillColor
  4. **Si Charge01 >= 0.999 (ready):**
     - Anillo de borde: Arc 0-360°, lineWidth=2, strokeColor=FillColor
  5. **Si Armed** — **S118 NUEVO:**
     - Anillo interior: Arc(radius-4, 0-360°), lineWidth=1.5, strokeColor=FillColor
     - Visual: doble anillo, mayor al exterior

**Métodos Públicos:**

- `void Pulse()` — anim visual de disparo:
  - Añade clase CSS "radial--fired"
  - Schedule.Execute() remueve clase en 320ms (0.32s)

**Integración:**

- Instanciado en ArenaHudCard (3 radiales, uno por habilidad)
- Carga sincronizada cada frame: `radial.Charge01 = agent.AbilityCharge01(i)`
- Pulse() disparado cuando AbilityFiredAt(i) > lastFired[i] (detector de flanco)
- **S118:** Armed sincronizado: `radial.Armed = agent.abilities.IsRequested(i) && agent.AbilityCharge01(i) >= 0.999`
- CSS "radial--fired" anima pulsación (probablemente scale/opacity en stylesheet)
- CSS "radial--ready" activa cuando Charge01 >= 0.999
- CSS "radial--armed" activa cuando Armed (doble anillo visual)

**S107 (Inicial):**
- Elemento UIElements reutilizable para indicadores de carga radiales
- Renderizado procedural (Painter2D) sin texturas
- Hysteresis en Charge01 evita repaint excesivo

**S118 Cambios:**

- **Armed property:** nuevo estado booleano reflejando Super solicitado
- **Doble anillo visual:** outer ring en ready, inner ring en armed
- **Color ReadyColor:** ahora se aplica también cuando Armed (readiness)
- **Integración con Pulse:** pulsación visible en combo Armed + Ready

**Invariantes:**

- Charge01 siempre [0,1]
- Armed solo tiene sentido si Charge01 ~ 1.0 (Super listo)
- Pulse dura 320ms (timing fijo)
- Hysteresis: delta < 0.005f se ignora (evita oscilación)
- CSS classes mutables: fired, ready, armed

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion]], [[Index/22 - Bajada Nocturna y Linaje]], S118

**Conexiones:** [[ArenaHudCard]], [[AgentAbilities]], [[AbilitySO]], [[UIElements]]
