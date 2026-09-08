---
tags: [script, ui, visualization, custom-element]
---

# RadialSlot.cs

**Ruta:** `UI/RadialSlot.cs`

**Responsabilidad:** Elemento UIElements personalizado que dibuja un indicador de carga radial (pie slice que se llena de 0° a 360°). Usado en ArenaHudCard para mostrar carga de 3 habilidades. Pinta círculo track + sector carga, con pulsación visual al disparar.

**Base:** VisualElement customizado con `generateVisualContent` callback

**Propiedades Públicas:**

- `float Charge01 { get; set; }` — carga normalizada [0,1]:
  - Setter clampea a [0,1], ignora cambios < 0.005f (hysteresis), marca MarkDirtyRepaint()
  - Getter retorna valor interno clamped

- `Color FillColor { get; set; }` — color del sector cargado (white defecto)

- `Color TrackColor { get; set; }` — color del círculo base (white 0.1 alpha defecto)

- `Color ReadyColor { get; set; }` — color cuando carga == 1.0 (white 0.22 alpha defecto)

**Constructor:**
- Suscribe a `generateVisualContent += OnGenerate`

**Método Privado:**

- `void OnGenerate(MeshGenerationContext mgc)` — dibuja:
  1. **Entrada:** painter2D, contentRect (tamaño del elemento)
  2. **Track círculo:** arco 0-360°, color TrackColor
  3. **Si Charge01 > 0.001%:**
     - Sector: MoveTo(center), Arc(-90° a -90°+Charge01*360°), LineTo(center), ClosePath
     - Color: ReadyColor si Charge01 >= 0.999, sino FillColor
  4. **Si Charge01 >= 0.999 (ready):**
     - Anillo de borde: Arc 0-360°, lineWidth=2, strokeColor=FillColor

**Métodos Públicos:**

- `void Pulse()` — anim visual de disparo:
  - Añade clase CSS "radial--fired"
  - Schedule.Execute() remueve clase en 320ms (0.32s)

**Integración:**
- Instanciado en ArenaHudCard (3 radiales, uno por habilidad)
- Carga sincronizada cada frame: `radial.Charge01 = agent.AbilityCharge01(i)`
- Pulse() disparado cuando AbilityFiredAt(i) > lastFired[i] (detector de flanco)
- CSS "radial--fired" anima pulsación (probablemente scale/opacity en stylesheet)

**S107 (NUEVO):**
- Elemento UIElements reutilizable para indicadores de carga radiales
- Renderizado procedural (Painter2D) sin texturas
- Hysteresis en Charge01 evita repaint excesivo

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaHudCard]], [[AgentAbilities]], [[UIElements]]
