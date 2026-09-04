---
tags: [script, ui, world-ui]
---

# NameTag.cs

**Ruta:** `World/Creatures/NameTag.cs`

**Responsabilidad:** Placa flotante UITK sobre MoriMochi. Muestra nombre, gender, role, life stage, intent, precio (si está en venta), timer de cría (si está criando). **S97:** Expone `ShowDistance` como propiedad pública. **S98:** Renderiza gestos y beating vía cambios en arena (estado visual dinámico). **S99 NUEVO:** `ScreenSizeReferenceDistance` propiedad pública (si > 0, escala se multiplica por max(1, distancia-a-cámara/referencia)); colores de placa elegidos por `agent.Team`: `allyNameColor` verde pastel y `rivalNameColor` rojo pastel (fallback `GenderColor` si sin equipo); `RefreshDefault` re-aplica nombre si cambió, para sobrevivir rebuild del UIDocument.

## Campos Serializados

- **Visibility:**
  - `showDistance` (float, default 8f) — distancia máxima para mostrar la placa
  - `uprightOnly` (bool, default true) — mantiene etiqueta vertical (ignora camera pitch) en vez de facing completo

- **Pen Layout (cuando penned en el breeding):**
  - `penRaise` (float, default 0.6) — altura extra en metros cuando está en pen
  - `penScale` (float, default 0.8) — escala uniforme cuando penned (más compacta)

- **S99 NUEVOS:**
  - `screenSizeReferenceDistance` (float, min 0, default 0) — si > 0, escala se multiplica por max(1, distancia-a-cámara/referencia). Valor 0 = deshabilitado.
  - `allyNameColor` (Color, default verde pastel 0.76, 1, 0.6) — color del nombre si agent.Team == ExpeditionTeam.Player
  - `rivalNameColor` (Color, default rojo pastel 0.96, 0.6, 0.6) — color del nombre si agent.Team == ExpeditionTeam.Rival

## Campos Internos

- `document` (UIDocument) — componente del mismo GO
- `root` (VisualElement) — raíz resuelta del UIDocument
- `nameLabel`, `priceLabel`, `statusLabel`, `intentLabel`, `petHintLabel`, `genderLabel`, `roleLabel`, `stageLabel`, `breedLabel`, `heartLabel`, `timerLabel` (Label) — elementos queryados del UXML
- `agent` (MoriMochiAgent) — referencia al agente (wired en `Bind()`)
- `dna` (CreatureDNA) — referencia al DNA (wired en `Bind()`)
- `cam` (Transform) — transform de Camera.main (para LOD y facing)
- `shown` (bool) — bandera de si la placa está visible
- `baseLocalPos`, `baseLocalScale` (Vector3) — posición y escala guardadas en Awake para restaurar

## Propiedades Públicas (S97, S99)

- `ShowDistance { get; set; }` — S97 propiedad pública sobre `showDistance` para ajuste en runtime
- `ScreenSizeReferenceDistance { get; set; }` — S99 propiedad pública sobre `screenSizeReferenceDistance`

## Métodos Públicos

- `Bind(CreatureDNA creature, MoriMochiAgent agent)` — wireo del DNA y agente. Resuelve elementos UIDocument, carga nombre, aplica color via `NameColor()`, llama `Refresh()`.

## Métodos Privados

- `ResolveElements()` — queries al UIDocument por labels. Idempotente: si ya resuelto y no cambió root, retorna sin hacer nada. Inicia después de que el UIDocument se crea.
- `LateUpdate()` — tick principal:
  1. Adquiere cámara si falta (lazy ref a Camera.main)
  2. Calcula distancia a cámara, determina visibilidad según `showDistance`
  3. Si cambió visibilidad, llama `SetShown()` (actualiza DisplayStyle.Flex/None)
  4. Si no visible, retorna temprano
  5. Llama `Refresh()` para actualizar contenido dinámico
  6. Aplica escala pen si penned (localScale × penScale)
  7. **S99:** Multiplica escala por max(1, sqrt(distSqr) / screenSizeReferenceDistance) si screenSizeReferenceDistance > 0
  8. Reposiciona en altura: localPosition.y + penRaise si penned
  9. Rota hacia cámara (LookRotation), ignorando pitch si uprightOnly
- `SetShown(bool visible)` — actualiza DisplayStyle del root. Resuelve elementos antes de mutar.
- `Refresh()` — selecciona contexto y delega: `RefreshStore()` si IsForSale, `RefreshPenned()` si IsPenned, `RefreshDefault()` sino.
- `RefreshStore()` — muestra solo precio. Oculta gender, role, stage, breed, heart, timer. Calcula precio vía `CustomerService.EstimateAverage()`.
- `RefreshPenned()` — muestra gender, role, stage, breed count, heart+timer si criando. Oculta precio, status, intent, pet hint.
- `RefreshDefault()` — muestra nombre, status (si dead o breeding), intent (si interesante), pet hint (si tocando/facing). **S99:** Re-aplica nombre si cambió (null coalesce CustomName, compara con etiqueta actual antes de mutar).
- `NameColor(CreatureDNA) → Color` — **S99** elige color según team: `allyNameColor` si ExpeditionTeam.Player, `rivalNameColor` si ExpeditionTeam.Rival, fallback `GenderColor()`.
- `GenderColor(CreatureGender) → Color` — azul (macho), rosa (hembra), gris (desconocido).
- `StatusOf(CreatureDNA) → (string, Color)` — text+color del status (Dead = rojo, Breeding = rosa). Usa localization.
- `GenderGlyph(CreatureGender) → string` — ♂, ♀, ?.
- `StageText(int ageDays) → string` — "Life Stage, X días" o fallback "Xd".
- `CountdownText(long readyAtMs) → string` — "mm:ss" hasta readyAt, o "Ready" si vencido.
- `SetDisplay(Label, bool) → void` — helper para toggle DisplayStyle de un label.

## Ciclo de Contenido (Refresh)

### RefreshStore
- Muestra: price (Loc.Tr con CustomerService)
- Oculta: todo lo demás

### RefreshPenned
- Muestra: gender (glyph + color), role, stage, breed/maxBreed, heart+timer si breeding
- Oculta: price, status, intent, pet hint

### RefreshDefault
- Muestra: nombre (con `NameColor()` por team o gender)
  - **S99:** Re-aplica nombre si string cambió (para rebuild UIDocument)
- Status (Dead rojo / Breeding rosa): si no pet hint activo y texto no vacío
- Intent (verbo): si no pet hint, no status, e intent interesante (no Idle/Wandering)
- Pet hint ("Tocando" / "Tócame"): si being petted OR (friendly + facing player)
- Oculta: price, gender, role, breed, heart, timer, stage

## Invariantes S99

- **Team == color:** `allyNameColor` y `rivalNameColor` se aplican sin fallback si agente tiene equipo explícito (S99 feature de arena). Fallback a GenderColor solo si agent.Team no está seteado.
- **ScreenSizeReferenceDistance perezoso:** si = 0, deshabilitado (sin escalado por distancia). Si > 0, sí escala. Util para que placas lejanas sean legibles.
- **RefreshDefault preserva nombre:** compara nameLabel.text con dna.CustomName antes de reasignar, evitando dirty-mark innecesario del UIDocument. Necesario porque ResolveElements() a veces recrea el root (rebuild), y queremos mantener nombre si no cambió.
- **Bind() dispara Refresh:** al wirearse, se refresca inmediatamente para que la placa sea correcta ese frame.
- **Idempotencia de visibilidad:** SetShown() es seguro de llamar múltiples frames con el mismo valor.

## Vinculado a

- [[Index/05 - UI System]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S99: equipos, colores)

## Conexiones

- [[MoriMochiAgent]] — wired en `Bind()`, leído para agent.Team (S99), agent.IsPenned, agent.IsForSale, agent.IsBeingPetted, agent.IsInFriendlyReaction, agent.IsPlayerFacingMe(), agent.Intent
- [[CreatureDNA]] — wired en `Bind()`, leído para CustomName, Gender, Role, AgeDays, BusyState, BreedReadyAt, IsDead
- [[Perceivable]] — en el mismo GO del MoriMochi, para registrar perceptualmente
- [[LocEnumMaps]] — traducciones de role, intent, stage
- [[Loc]] — traducción de status, precio, timer, labels
- [[CustomerService]] — lectura de precio estimado si IsForSale
- [[BreedingController]] — acceso a LifeStageTable para stage text
- [[UiPanels]] — resolución de root del UIDocument
