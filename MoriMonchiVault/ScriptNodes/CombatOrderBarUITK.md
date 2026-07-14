---
tags: [script, ui, uitk, combat, visualization, order-bar]
---

# CombatOrderBarUITK

**Ruta:** `UI/CombatOrderBarUITK.cs`

**Responsabilidad:** Barra superior de orden de acción para el replay 3v3 — visualiza equipos A/B con cartas de cada unidad. **S44:** Rediseño visual: order-bar = teamA + gap + teamB; cada slot contiene carta + fila de estados. **S45:** Nuevo banner de equipo por carta. Handler `HandleUnitElement()` suscribe a `OnUnitElement` y muta listas runtime `Marks`/`States` por-proc. **S46:** `HandleAffinity` firma cambió (sin parámetro energy). En `HandleUnitElement`, caso `Reaction` parsea `ReactionName` a `ElementalState` e inserta en `States` (para estados INSTANTÁNEOS que nunca quedan armados, pero el visualizador debe dibujarlos).

## Enums / Constantes

| Nombre | Descripción |
|--------|-------------|
| `NegativeStates` (HashSet) | Estados rojo: Boiling, Debilidad, Confuso, Leech, Mareado, PisoTierra |

## Clase Interna: OrderCard

Descriptor de una tarjeta de unidad.

```csharp
private class OrderCard
{
    public VisualElement Root;                           // Contenedor raíz
    public VisualElement Slot;                           // Slot padre (cv-ob-slot)
    public VisualElement AllyMarksRow;                   // Marcas aliadas
    public VisualElement EnemyMarksRow;                  // Marcas enemigas
    public VisualElement StatesRow;                      // Fila de estados armados
    public VisualElement AffinityDot0;                   // Primer círculo afinidad
    public VisualElement AffinityDot1;                   // Segundo círculo afinidad
    public List<CombatElementMark> Marks  = new List<CombatElementMark>();  // Marcas actuales (runtime)
    public List<ElementalState>    States = new List<ElementalState>();     // Estados actuales (runtime)
}
```

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe a eventos: Start, Order, Affinity, ActiveUnit, UnitElement |
| `OnDisable()` | Desuscribe eventos |
| `Start()` | Inicializa referencias, oculta barra |

## Métodos Privados Clave

| Método | Descripción |
|--------|-------------|
| `EnsureRefs()` | Localiza UIDocument → orderBar, crea tooltip dinámico |
| `HandleStart(CombatVisualContext)` | Almacena contexto, construye cartas A y B |
| `BuildCards()` | Limpia orderBar, construye equipos |
| `BuildTeam(side, snapshots, dnas)` | Itera snapshots, crea OrderCard por unidad |
| `CreateCard(side, snapshot, element)` | Arma tarjeta: banner equipo + body + affinityRow + marksSplit |
| `HandleOrder(List<CombatOrderEntry>)` | Recibe orden, reordena DOM, re-sincroniza Marks/States desde snapshot |
| `HandleActiveUnit(side, index)` | Marca tarjeta activa (borde dorado) |
| `HandleAffinity(side, index, affinity)` | **S46 FIRMA CAMBIÓ** Sin parámetro energy. Actualiza circulitos afinidad. |
| `HandleUnitElement(CombatElementEventData)` | **S45** Muta Marks/States por-proc, llama RebuildElements. **S46:** Parsea ReactionName a ElementalState para Reaction event. |
| `ApplyState(card, entry)` | Re-sincroniza Marks/States desde snapshot (fin-de-turno resync) |
| `RemoveMark(marks, element, ally)` | **S45** Helper — busca y quita marca |
| `RebuildElements(card)` | **S45** Reconstruye filas de marcas/estados |
| `BuildMarkRow(row, marks, ally)` | Limpia fila, agrega chips de marcas |
| `CreateMarkChip(mark)` | Label(DisplayName) en blanco sobre fondo elemento |
| `BuildStatesRow(row, states)` | Limpia fila, agrega estados |
| `CreateStateChip(state)` | Label(DisplayName) + clase negativa/positiva |
| `SetAffinity(card, affinity)` | Llena circulitos si affinity >= 1 ó >= 2 |
| `Identity(element)` | Lee ElementTableSO → DisplayName + UiColor |
| `StateOf(state)` | Lee ElementTableSO → DisplayName + Description |
| `MarkColor(element)` | Retorna UiColor de elemento |
| `RoleText(role)` | Role → "Protector" / "Agresivo" / "Empático" |
| `RoleInitial(role)` | Role → "P" / "A" / "E" |
| `SetVisible(bool)` | Muestra/oculta orderBar |

## Flujo de Construcción y Actualización

**Evento OnVisualCombatStart:**
1. EnsureRefs → localiza UIDocument
2. BuildCards → crea cartas A y B en slots
3. SetVisible(true)

**Evento OnActionOrder:**
- Itera entries en orden nuevo
- Por cada entry: obtiene card, re-sincroniza Marks/States desde snapshot, reordena DOM

**Evento OnUnitElement (S46):**
- Encuentra card correspondiente
- Muta Marks/States según `Kind`:
  - `MarkApplied`: agrega marca
  - `MarkRemoved`: quita marca
  - `Reaction`: quita Element y ElementB de Marks; **S46:** parsea ReactionName a ElementalState e inserta si no presente
  - `StateArmed`: agrega State
  - `StateConsumed` / `StateRemoved`: quita State
- Llama RebuildElements

**Evento OnUnitAffinity (S46 CAMBIÓ):**
- Recibe (side, index, affinity) — **sin energy**
- Llama SetAffinity(card, affinity)
- Circulitos se llenan si affinity >= 1 ó >= 2

## Cambios S46

**HandleAffinity firma cambió:**
- Antes (S42-S45): `void HandleAffinity(CombatVisualSide side, int index, int affinity, int energy)`
- Ahora (S46): `void HandleAffinity(CombatVisualSide side, int index, int affinity)`
- Ignora completamente energy (que ya no existe)

**HandleUnitElement para Reaction (S46 NUEVO):**
```csharp
case ElementEventKind.Reaction:
    RemoveMark(card.Marks, d.Element, d.AllySource);
    RemoveMark(card.Marks, d.ElementB, d.AllySource);
    if (System.Enum.TryParse<ElementalState>(d.ReactionName, out var reacted)
     && !card.States.Contains(reacted))
        card.States.Add(reacted);
    break;
```
- Parsea `ReactionName` (ej: "PisoTierra") a `ElementalState` enum
- Agrega estado a `card.States` si no está presente
- Permite que estados INSTANTÁNEOS (que nunca se arman en Combatant.States) se visualicen en la barra por un turno

**Energía completamente eliminada:**
- No hay circulitos de energía ⚡
- No hay parámetro energy en OnUnitAffinity
- Solo circulitos de afinidad (0-2)

## Ciclo de Vida

1. **OnVisualCombatStart:** Se crean las cartas estáticas
2. **OnActionOrder:** Cada turno, se reordenan las cartas en el DOM (orden estable dentro ronda)
3. **OnUnitElement (por-proc):** Conforme salen procs, se actualizan las marcas/estados en vivo
4. **OnUnitAffinity (fin-de-turno):** Se actualiza afinidad (sin energy)
5. **OnActiveUnit:** Se marca quién está en turno actual

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CombatVisualEvents]] — suscriptor (S46: OnUnitAffinity sin energy, HandleUnitElement parsea ReactionName)
- [[CombatVisualizerService]] — publisher de OnActionOrder/OnUnitAffinity/OnActiveUnit/OnUnitElement
- [[ElementTableSO]] — proveedor de DisplayName + UiColor + Description

## Notas S46

- Affinidad es el único recurso visible (2 circulitos)
- Energy⚡ fue completamente removido del display
- ReactionName parsing a ElementalState permite visualizar estados que se disparan instantáneamente (ej: PisoTierra)
- Backward compat: Energía simplemente no se utiliza en el UI (parámetro omitido de OnUnitAffinity)
