---
tags: [script, ui, uitk, combat, visualization, order-bar]
---

# CombatOrderBarUITK

**Ruta:** `UI/CombatOrderBarUITK.cs`

**Responsabilidad:** Barra superior de orden de acción para el replay 3v3 — visualiza equipos A|gap|B con cartas de cada unidad. **S44:** Rediseño visual sin tamaños fijos px: order-bar = teamA (flex-grow) + gap + teamB; cada unidad vive en slot cv-ob-slot (flex-grow max 240px) que contiene carta + fila de estados. Carta comprimida: (1) label cv-ob-order-num con número de orden (1ero/2do/3ero/4to…); (2) header nombre + chip rol (sin swatch de color ni mini-chip elemento, nombre coloreado con UiColor del elemento); (3) circulitos de afinidad (energía ⚡ eliminada); (4) marksSplit aliadas(izq)/enemigas(der) — chips son Labels con DisplayName en blanco sobre fondo de color. Turno activo indicado solo por borde dorado (cv-order-card--active, sin TurnMarker ▼). Suscriptor de eventos visuales (OnVisualCombatStart, OnActionOrder, OnUnitAffinity, OnActiveUnit).

## Enums / Constantes

| Nombre | Descripción |
|--------|-------------|
| `NegativeStates` (HashSet) | Estados que se marcan con color rojo: Boiling, Debilidad, Confuso, Leech, Mareado, PisoTierra |

## Clase Interna: OrderCard

Descriptor de una tarjeta de unidad.

```csharp
private class OrderCard
{
    public VisualElement Root;            // Contenedor raíz de la tarjeta
    public Label         OrderLabel;      // Número de orden (1ero/2do/3ero/4to…)
    public VisualElement AllyMarksRow;    // Columna izq: marcas aliadas
    public VisualElement EnemyMarksRow;   // Columna der: marcas enemigas
    public VisualElement StatesRow;       // Fila de estados armados (fuera de la carta, debajo)
    public VisualElement AffinityDot0;    // Primer círculo afinidad
    public VisualElement AffinityDot1;    // Segundo círculo afinidad
}
```

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe a eventos visuales (Start, Order, Affinity, ActiveUnit) |
| `OnDisable()` | Desuscribe eventos |
| `Start()` | Inicializa referencias y oculta la barra |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `EnsureRefs()` | Localiza UIDocument → orderBar, crea tooltip dinámico si falta; retorna true si referencias válidas |
| `HandleStart(CombatVisualContext)` | Almacena contexto, construye cartas estáticas de equipos A y B |
| `BuildCards()` | Limpia orderBar, construye equipo A, agrega gap, construye equipo B |
| `BuildTeam(side, snapshots, dnas)` | Itera snapshots, crea OrderCard por unidad en slot cv-ob-slot, agrega a dictionary y teamContainer |
| `CreateCard(side, snapshot, element)` | **S44:** Arma tarjeta: OrderLabel vacío + body (nombre coloreado + chip rol P/A/E) + affinityRow (dots) + marksSplit (aliadas/enemigas) |
| `HandleOrder(List<CombatOrderEntry>)` | Recibe orden de próxima acción, itera entries Alive, asigna Ordinal(pos) a OrderLabel; entradas Dead = texto vacío |
| `HandleActiveUnit(side, index)` | Marca tarjeta activa con clase "cv-order-card--active" (borde dorado); otros pierden clase |
| `ApplyState(card, entry)` | Aplica clase "dead" si no vivo, construye filas de marcas/estados, actualiza afinidad |
| `HandleAffinity(side, index, affinity, energy)` | **S44:** Ignora parámetro energy; actualiza circulitos con affinity >= 1 ó >= 2 |
| `BuildMarkRow(row, marks, ally)` | Limpia fila, agrega chips de marcas aliadas XOR enemigas |
| `CreateMarkChip(mark)` | **S44:** Label(DisplayName) en blanco sobre fondo MarkColor(element), tooltip "Marca elemento (fuente) — reacciona…" |
| `BuildStatesRow(row, states)` | Limpia fila, agrega labels con DisplayName sin prefijo, clase negativa/positiva, tooltip Description |
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
| `Ordinal(int)` | **S44:** Helper privado — retorna "1ero", "2do", "3ero", "Nto" (ej: "4to", "5to") |
| `SetVisible(bool)` | Muestra/oculta orderBar |

## Flujo de Construcción

**Evento OnVisualCombatStart:**
1. EnsureRefs → localiza UIDocument
2. BuildCards:
   - Limpia orderBar
   - Crea teamA (.cv-ob-team, flex-grow)
   - BuildTeam(A): itera SnapsA, crea OrderCards, cada una en un slot cv-ob-slot (flex-grow max 240px) que contiene Root + StatesRow
   - Crea gap (.cv-ob-team-gap)
   - Crea teamB y construye igual
3. SetVisible(true) → muestra barra

**Evento OnActionOrder:**
- Itera entries de la orden futura
- Por cada entry Alive: incrementa contador pos, asigna Ordinal(pos) a OrderLabel
- Por cada entry Dead: OrderLabel.text = ""
- Llama ApplyState (sin reconstruir) para actualizar marcas/estados/afinidad

**Evento OnActiveUnit:**
- Recorre todas las cartas
- Marca solo (side, index) activa con clase "cv-order-card--active" (borde dorado)
- Otros pierden clase

**Evento OnUnitAffinity:**
- Llama HandleAffinity ignorando parámetro energy
- Actualiza solo los dots de afinidad

## Estructura Layout CSS S44

**order-bar (raíz, flex-row):**
- `.cv-ob-team` (A, flex-grow): equipos aliados
- `.cv-ob-team-gap`: espaciador
- `.cv-ob-team` (B, flex-grow): equipos enemigos

**slot (.cv-ob-slot, flex-column, flex-grow max 240px):**
- `cv-order-card` (Root):
  - `.cv-ob-order-num` (OrderLabel, arriba)
  - `.cv-ob-body-row` (nombre + rol)
  - `.cv-ob-affinity-row` (dots)
  - `.cv-ob-marks-split` (aliadas izq | enemigas der)
- `.cv-ob-states-row` (StatesRow, fuera de Root, debajo)

**Estados tarjeta:**
- `.cv-order-card--self` (equipo A, aliado)
- `.cv-order-card--opp` (equipo B, enemigo)
- `.cv-order-card--active` (borde dorado, turno activo)
- `.cv-order-card--dead` (muerto, gris)

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

## Vinculado a

- [[CombatVisualEvents]] — suscriptor (OnVisualCombatStart, OnActionOrder, OnUnitAffinity, OnActiveUnit)
- [[CombatVisualizerService]] — publicador de eventos
- [[ElementTableSO]] — fuente de DisplayName + UiColor + StateDefinition
- [[CombatVisualContext]] — contexto snapshot de combate
- [[Index/13 - Combat Design Direction]] — tabla de estados y estructura 3v3
