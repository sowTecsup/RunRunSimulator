---
tags: [script, ui, uitk, combat, visualization, order-bar]
---

# CombatOrderBarUITK

**Ruta:** `UI/CombatOrderBarUITK.cs`

**Responsabilidad:** Barra superior de orden de acción para el replay 3v3 — visualiza equipos A|gap|B con cartas ESTÁTICAS de cada unidad, indicador de turno activo (▼ + borde amarillo), chips de marcas (aliadas arriba/enemigas abajo), estados armados (★ con borde), 2 circulitos de afinidad + etiqueta energía, tooltips con Description del ElementTableSO, muertos en gris. Suscriptor de eventos visuales (OnVisualCombatStart, OnActionOrder, OnUnitAffinity, OnActiveUnit).

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
    public Label         TurnMarker;      // Marker ▼ (visible solo si activo)
    public VisualElement AllyMarksRow;    // Fila arriba con marcas aliadas
    public VisualElement EnemyMarksRow;   // Fila abajo con marcas enemigas
    public VisualElement StatesRow;       // Fila de estados armados
    public VisualElement AffinityDot0;    // Primer círculo afinidad
    public VisualElement AffinityDot1;    // Segundo círculo afinidad
    public Label         EnergyLabel;     // ⚡N (energía)
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
| `BuildTeam(side, snapshots, dnas)` | Itera snapshots, crea OrderCard por unidad, agrega a dictionary y orderBar |
| `CreateCard(side, snapshot, element)` | Arma tarjeta con nombre, rol, elemento, marcadores de turno, filas de marcas, afinidad |
| `HandleOrder(List<CombatOrderEntry>)` | Recibe orden de próxima acción, actualiza cada tarjeta con ApplyState |
| `HandleActiveUnit(side, index)` | Marca tarjeta activa con clase "active" (borde amarillo, ▼ visible) |
| `ApplyState(card, entry)` | Aplica clase "dead" si no vivo, construye filas de marcas/estados, actualiza afinidad |
| `HandleAffinity(side, index, affinity, energy)` | Actualiza circulitos + etiqueta energía sin reconstruir card |
| `BuildMarkRow(row, marks, ally)` | Limpia fila, agrega chips de marcas aliadas XOR enemigas |
| `CreateMarkChip(mark)` | Crea chip de marca (color por elemento, tooltip) |
| `BuildStatesRow(row, states)` | Limpia fila de estados, agrega chips armados (★) con etiqueta negativa/positiva |
| `CreateStateChip(state)` | Label con DisplayName + classe negativa/positiva, tooltip Description |
| `SetAffinity(card, affinity, energy)` | Actualiza circulitos (filled si >= 1 o 2) y energía |
| `RegisterTooltip(element, text)` | Eventos PointerEnter/Leave para mostrar/ocultar tooltip dinámico |
| `ShowTooltip(anchor, text)` | Posiciona tooltip bajo el elemento |
| `HideTooltip()` | Oculta tooltip |
| `Identity(element)` | Lee ElementTableSO → DisplayName + UiColor |
| `StateOf(state)` | Lee ElementTableSO → DisplayName + Description |
| `MarkColor(element)` | Retorna UiColor de elemento o white si alpha 0 |
| `SnapshotColor(snapshot)` | Parse ColorHex → Color, fallback gray |
| `RoleText(role)` | Mapea Role → "Protector" / "Agresivo" / "Empático" |
| `SetVisible(bool)` | Muestra/oculta orderBar |

## Flujo de Construcción

**Evento OnVisualCombatStart:**
1. EnsureRefs → localiza UIDocument
2. BuildCards:
   - Limpia orderBar
   - BuildTeam(A): itera SnapsA, crea OrderCards, agrega a orderBar
   - Crea gap visual (espaciador entre equipos)
   - BuildTeam(B): igual para equipo B
3. SetVisible(true) → muestra barra

**Evento OnActionOrder:**
- Itera entries de la orden futura
- Aplica estado (muerto, marcas, estados, afinidad) a cada tarjeta sin reconstruir visualmente

**Evento OnActiveUnit:**
- Recorre todas las cartas, marca solo la activa con clase "active" (borde amarillo, ▼ visible)
- Otros pierden clase

## S42 Cambios

**Nuevo en S42:**
- Clase completa nueva (no existía en S41)
- Bus visual aditivo: eventos OnActionOrder, OnUnitAffinity, OnActiveUnit
- Tooltip dinámico HTML con Description del estado
- Separación visual neta entre equipo A | gap | equipo B
- Indicador de turno (▼) + borde amarillo en tarjeta activa
- Chips de afinidad (2 círculitos) + energía (⚡N)
- Chips de marcas con color de elemento + tooltip
- Chips de estados (★) con clase negativa/positiva
- Integración con ElementTableSO para DisplayName y colors

## Vinculado a

- [[CombatVisualEvents]] — suscriptor (OnVisualCombatStart, OnActionOrder, OnUnitAffinity, OnActiveUnit)
- [[CombatVisualizerService]] — publicador de eventos
- [[ElementTableSO]] — fuente de DisplayName + UiColor + StateDefinition
- [[Index/13 - Combat Design Direction]] — tabla de estados y estructura 3v3
