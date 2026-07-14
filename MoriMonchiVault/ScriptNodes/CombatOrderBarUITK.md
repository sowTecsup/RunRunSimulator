---
tags: [script, ui, uitk, combat, visualization, order-bar]
---

# CombatOrderBarUITK

**Ruta:** `UI/CombatOrderBarUITK.cs`

**Responsabilidad:** Barra superior de orden de acción para el replay 3v3 — visualiza equipos A/gap/B con cartas de cada unidad. **S44:** Rediseño visual sin tamaños fijos px: order-bar = teamA (flex-grow) + gap + teamB; cada unidad vive en slot cv-ob-slot (flex-grow max 240px) que contiene carta + fila de estados. Carta comprimida: (1) label cv-ob-order-num con número de orden (1ero/2do/3ero/4to…); (2) header nombre + chip rol (sin swatch de color ni mini-chip elemento, nombre coloreado con UiColor del elemento); (3) circulitos de afinidad (energía ⚡ eliminada); (4) marksSplit aliadas(izq)/enemigas(der) — chips son Labels con DisplayName en blanco sobre fondo de color. Turno activo indicado solo por borde dorado (cv-order-card--active, sin TurnMarker ▼). Suscriptor de eventos visuales (OnVisualCombatStart, OnActionOrder, OnUnitAffinity, OnActiveUnit). **S45:** Nuevo banner de equipo (`cv-ob-team-banner`, azul aliado/rojo enemigo) en cada carta. Nuevo handler `HandleUnitElement(CombatElementEventData)` que suscribe a `OnUnitElement` y muta listas runtime `Marks`/`States` por-proc (MarkApplied add, Reaction quita ambos elementos, StateArmed add, Consumed/Removed quita), con helpers `RemoveMark` y `RebuildElements`. `ApplyState` re-sincroniza las listas desde snapshot. Energía (parámetro energy en OnUnitAffinity) eliminado completamente del display.

## Enums / Constantes

| Nombre | Descripción |
|--------|-------------|
| `NegativeStates` (HashSet) | Estados que se marcan con color rojo: Boiling, Debilidad, Confuso, Leech, Mareado, PisoTierra |

## Clase Interna: OrderCard

Descriptor de una tarjeta de unidad (S44, S45: con Marks/States).

```csharp
private class OrderCard
{
    public VisualElement Root;                           // Contenedor raíz de la tarjeta
    public VisualElement Slot;                           // **S45** Slot padre (cv-ob-slot)
    public VisualElement AllyMarksRow;                   // Columna izq: marcas aliadas
    public VisualElement EnemyMarksRow;                  // Columna der: marcas enemigas
    public VisualElement StatesRow;                      // Fila de estados armados (fuera de la carta, debajo)
    public VisualElement AffinityDot0;                   // Primer círculo afinidad
    public VisualElement AffinityDot1;                   // Segundo círculo afinidad
    public List<CombatElementMark> Marks  = new List<CombatElementMark>();  // **S45 NEW** Marcas actuales (se actualiza por-proc)
    public List<ElementalState>    States = new List<ElementalState>();     // **S45 NEW** Estados armados actuales (se actualiza por-proc)
}
```

**S45 NUEVO:** Campos `Marks` y `States` son listas que se actualizan en vivo por HandleUnitElement, independiente de ApplyState que los re-sincroniza a fin de turno.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe a eventos visuales (Start, Order, Affinity, ActiveUnit, **S45: UnitElement**) |
| `OnDisable()` | Desuscribe eventos |
| `Start()` | Inicializa referencias y oculta la barra |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `EnsureRefs()` | Localiza UIDocument → orderBar, crea tooltip dinámico si falta; retorna true si referencias válidas |
| `HandleStart(CombatVisualContext)` | Almacena contexto, construye cartas estáticas de equipos A y B |
| `BuildCards()` | Limpia orderBar, construye equipo A, agrega gap, construye equipo B |
| `BuildTeam(side, snapshots, dnas)` | Itera snapshots, crea OrderCard por unidad en slot cv-ob-slot, agrega a dictionary y teamContainer |
| `CreateCard(side, snapshot, element)` | **S44/S45:** Arma tarjeta: (1) banner equipo (azul aliado/rojo enemigo); (2) body (nombre coloreado + chip rol P/A/E); (3) affinityRow (dots); (4) marksSplit (aliadas/enemigas) |
| `HandleOrder(List<CombatOrderEntry>)` | Recibe orden de próxima acción, reordena cartas en DOM, itera entries para re-sincronizar ApplyState (Marks/States desde snapshot) |
| `HandleActiveUnit(side, index)` | Marca tarjeta activa con clase "cv-order-card--active" (borde dorado); otros pierden clase |
| `HandleUnitElement(CombatElementEventData)` | **S45 NEW** Handler para OnUnitElement — muta Marks/States por-proc (MarkApplied add, Reaction quita ambos, StateArmed add, StateConsumed/Removed quita), llama RebuildElements |
| `ApplyState(card, entry)` | Aplica clase "dead" si no vivo, re-sincroniza Marks/States desde entry.State (snapshot), actualiza afinidad, llama RebuildElements |
| `RemoveMark(marks, element, ally)` | **S45 NEW** Helper — busca y quita la primera marca que coincida element+ally |
| `RebuildElements(card)` | **S45 NEW** Helper — reconstruye AllyMarksRow/StatesRow/EnemyMarksRow desde Marks/States actuales |
| `HandleAffinity(side, index, affinity, energy)` | Ignora parámetro energy; actualiza circulitos con affinity >= 1 ó >= 2 |
| `BuildMarkRow(row, marks, ally)` | Limpia fila, agrega chips de marcas aliadas XOR enemigas |
| `CreateMarkChip(mark)` | **S44:** Label(DisplayName) en blanco sobre fondo MarkColor(element), tooltip "Marca elemento (fuente) — reacciona…" |
| `BuildStatesRow(row, states)` | Limpia fila, agrega labels con DisplayName, clase negativa/positiva, tooltip Description |
| `CreateStateChip(state)` | Label(DisplayName) + classe negativa/positiva, tooltip Description |
| `SetAffinity(card, affinity)` | Actualiza circulitos (filled si >= 1 ó 2) |
| `RegisterTooltip(element, text)` | Eventos PointerEnter/Leave para mostrar/ocultar tooltip dinámico |
| `ShowTooltip(anchor, text)` | Posiciona tooltip bajo el elemento |
| `HideTooltip()` | Oculta tooltip |
| `Identity(element)` | Lee ElementTableSO → DisplayName + UiColor |
| `StateOf(state)` | Lee ElementTableSO → DisplayName + Description |
| `MarkColor(element)` | Retorna UiColor de elemento o white si alpha 0 |
| `RoleText(role)` | Mapea Role → "Protector" / "Agresivo" / "Empático" |
| `RoleInitial(role)` | Mapea Role → "P" / "A" / "E" |
| `SetVisible(bool)` | Muestra/oculta orderBar |

## Flujo de Construcción y Actualización

**Evento OnVisualCombatStart:**
1. EnsureRefs → localiza UIDocument
2. BuildCards:
   - Limpia orderBar
   - Crea teamA (flex-grow)
   - BuildTeam(A): itera SnapsA, crea OrderCards, cada una en un slot cv-ob-slot (flex-grow max 240px) que contiene Root + StatesRow
   - Crea gap
   - Crea teamB y construye igual
3. SetVisible(true) → muestra barra

**Evento OnActionOrder (S45: reordenación de DOM):**
- Itera entries en el orden nuevo
- Por cada entry: obtiene/crea card, llama ApplyState (re-sincroniza Marks/States desde snapshot), agrega Slot al orderBar
- Resultado: cartas reordenadas en DOM para reflejar orden de atacantes (estable dentro ronda)

**Evento OnUnitElement (S45 NEW):**
- Encuentra card correspondiente a (Side, Index)
- Muta Marks/States según Kind:
  - MarkApplied: agrega marca a Marks
  - MarkRemoved: quita marca de Marks
  - Reaction: quita Element y ElementB de Marks
  - StateArmed: agrega State a States
  - StateConsumed/StateRemoved: quita State de States
- Llama RebuildElements para re-render AllyMarksRow/StatesRow/EnemyMarksRow

**Evento OnActiveUnit:**
- Recorre todas las cartas
- Marca solo (side, index) activa con clase "cv-order-card--active" (borde dorado)
- Otros pierden clase

**Evento OnUnitAffinity:**
- Llama HandleAffinity ignorando parámetro energy
- Actualiza solo los dots de afinidad

## Estructura Layout CSS S44/S45

**order-bar (raíz, flex-row):**
- `.cv-ob-team` (A, flex-grow): equipos aliados
- `.cv-ob-team-gap`: espaciador
- `.cv-ob-team` (B, flex-grow): equipos enemigos

**slot (.cv-ob-slot, flex-column, flex-grow max 240px):**
- `cv-order-card` (Root):
  - `.cv-ob-team-banner` (azul aliado / rojo enemigo) — **S45 NEW**
  - `.cv-ob-body-row` (nombre + rol)
  - `.cv-ob-affinity-row` (dots)
  - `.cv-ob-marks-split` (aliadas izq | enemigas der)
- `.cv-ob-states-row` (StatesRow, fuera de Root, debajo)

**Estados tarjeta:**
- `.cv-order-card--self` (equipo A, aliado)
- `.cv-order-card--opp` (equipo B, enemigo)
- `.cv-order-card--active` (borde dorado, turno activo)
- `.cv-order-card--dead` (muerto, gris)

**Team banner (S45 NEW):**
- `.cv-ob-team-banner--self` (azul claro para aliado)
- `.cv-ob-team-banner--opp` (rojo claro para enemigo)

## S44 Cambios

**Rediseño layout flexible (sin px fijos):**
- order-bar = teamA (flex-grow) + gap + teamB
- .cv-ob-slot: flex-grow max 240px (no tamaño fijo)
- Tooltip dinámico ahora sobre nombre (elemento)

**Eliminar:**
- TurnMarker ▼ (indicador ▼ desaparece; turno activo = borde dorado)
- EnergyLabel ⚡ (energía no se muestra en UI)
- Swatch de color de criatura (quita visual antes de nombre)
- Mini-chip de elemento (quita cuadrado color, indicador va en nombre coloreado)
- Helper `SnapshotColor` (nunca usado)

**Nuevos elementos:**
- OrderLabel: mostrar número de orden (1ero/2do/3ero…) solo si Alive
- Nombre coloreado con UiColor del elemento → indicador de elemento integrado
- Chip de rol siempre visible (P/A/E con tooltip)

**CreateMarkChip S44:**
- Antes (S43): chips de marca sin texto especial
- Ahora (S44): Label(DisplayName) en blanco, fondo MarkColor(element)

**OrderCard S44:**
- Borrados: TurnMarker, EnergyLabel
- Agregados: OrderLabel

**Invariante:** Eventos, flujo de estado, tolerancia para estados muertos (texto vacío en lugar de valor), negativeStates sin cambios.

## S45 Cambios

**Aditivos (append-only):**
- **Nuevo campo serializado:** None (solo internos)
- **Nuevos campos en OrderCard:**
  - `Marks: List<CombatElementMark>` — lista de marcas que se actualiza en vivo por HandleUnitElement
  - `States: List<ElementalState>` — lista de estados que se actualiza en vivo por HandleUnitElement
- **Nueva suscripción:** `OnUnitElement += HandleUnitElement` (OnEnable/OnDisable)
- **Nuevo método privado:** 
  - `HandleUnitElement(CombatElementEventData d)` — maneja eventos elementales por-proc (muta Marks/States)
  - `RemoveMark(List<CombatElementMark> marks, Element element, bool ally)` — helper para quitar marca específica
  - `RebuildElements(OrderCard card)` — helper para reconstruir filas de marcas/estados desde listas actuales
- **Banner de equipo (S45 NEW):**
  - Nuevo VisualElement `.cv-ob-team-banner` (azul si self, rojo si opp) agregado a cada CreateCard
  - CSS: altura pequeña, ancho 100%, fondo semitransparente
- **ApplyState actualizado (S45):**
  - Ahora re-sincroniza Marks/States desde entry.State (snapshot) — permite que tras DisplayOrder se corrija si hubo desincronización
  - Luego llama `RebuildElements(card)`
- **HandleAffinity actualizado (S45):**
  - Ignora completamente parámetro `energy` (antes se ignoraba)
  - Solo actualiza dots de afinidad

**Invariante:** Eventos OnActionOrder/OnActiveUnit/OnUnitAffinity siguen sin cambios; nuevas capas HandleUnitElement sólo mutan Marks/States listas en paralelo.

## Notas S45

- **Por-proc updates:** Cuando PlayProc emite OnUnitElement por MarkApplied, HandleUnitElement agrega la marca a card.Marks; RebuildElements reconstruye AllyMarksRow
- **Re-sincronización:** Al cambiar de turno, OnActionOrder dispara ApplyState que re-sincroniza Marks/States desde snapshot (entry.State), cerrando cualquier desvío acumulado por errores de proc
- **Energía eliminada:** Energy indicator (⚡ label) no existe en S45; OnUnitAffinity parámetro energy ignorado por completo
- **Orden estable:** Cartas reordenadas en DOM por HandleOrder (OnActionOrder), reflejando orden.TurnNumber estable desde roundOrders en CombatVisualizerService
- **Dead-actors:** Si unidad muere, estado "dead" se marca vía clase css, pero Marks/States listos siguen actualizando (silenciosamente, para snapshots históricos)

## Vinculado a

- [[CombatVisualEvents]] — suscriptor (OnVisualCombatStart, OnActionOrder, OnUnitAffinity, OnActiveUnit, **S45: OnUnitElement**)
- [[CombatVisualizerService]] — publicador de eventos
- [[ElementTableSO]] — fuente de DisplayName + UiColor + StateDefinition
- [[CombatVisualContext]] — contexto snapshot de combate
- [[Index/13 - Combat Design Direction]] — tabla de estados y estructura 3v3
