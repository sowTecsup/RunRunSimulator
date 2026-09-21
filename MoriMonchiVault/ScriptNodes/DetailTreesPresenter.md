---
tags: [script, ui, presenter]
---

# DetailTreesPresenter.cs

**Ruta:** `UI/DetailTreesPresenter.cs`

**Responsabilidad (S54):** Presenter colaborador de MorimonchiDetailInfoUITK — cubre DOS tabs (Linaje + Descendencia) por ser un dominio único (comparten `MakeChip()` y `ParseGenetics()`). Implementa ro `Rebuild(dna)` — no navegación. **S68:** Strings de labels role extraídos a Loc.Tr (sin cambio de contrato). **S129:** Descendencia pasa a `GetAllKnown()` (vivas + idas) para mostrar el árbol genealógico completo.

**Tab 1: Linaje (árbol ancestral 2 generaciones)**
- Construye bloques recursivos: self (chip) → [padres row + conector V] → [abuelos row + conector V]
- Resuelve ancestros vivos desde registry (full recursión upward si existe) o parsea genética de ID (muerto, solo display)
- Chips: "Tú" (self, highlighted), "Madre", "Padre" (labels role), retrato fotomatón vía [[MonchiPortraitUI]].Apply(), tachado si dead

**Tab 2: Descendencia (árbol de cría) — S129**
- Escanea registry por `GetAllKnown()` (vivas + idas) — cualquiera cuyo MotherID o FatherID sea self
- Agrupa por pareja (padre/madre alternativo), ordena por discovery
- Árbol downward: self → [pareja row + conector] → [hijos como chips]
- Chips: "Pareja" (padre/madre en descendencia), "Cría" (hijos), retrato fotomatón vía [[MonchiPortraitUI]].Apply()
- Muestra criaturas vendidas y muertas (decision de Juan S129)

**Estructura visual:**
- `tree-block` (contiene parents row + connector V + self chip, o self + connector + children)
- `tree-branch` (wrapper para agregar separadores)
- `tree-chip` (círculo retrato + nombre + role label, con clases `tree-chip--self`, `tree-chip--unknown`, `tree-dead`)
- `tree-swatch` (retrato vía MonchiPortraitUI.Apply)
- Conectores verticales `tree-connector-v`

**Métodos privados:**
- `BuildBlock(dna, role, depth, isSelf)` — recursión upward (depth=2 → self+parents+abuelos)
- `BuildAncestor(id, role, depth)` — resuelve ancestro vivo (full) o muerto (ParseGenetics)
- `ParseGenetics(uniqueId)` — extrae substring antes del último "-" (timestamp), crea dummy DNA coloreado
- `BuildLineage()` — llamada una vez desde Rebuild, construye upward tree
- `BuildBreed()` — **(S129)** llamada una vez desde Rebuild, construye downward tree con `GetAllKnown()` para incluir idas
- `BuildPartnerBranch()` — columna partner → conector → hijos
- `MakeChip(dna, role, isSelf, isDead)` — crea VisualElement chip con retrato vía [[MonchiPortraitUI]].Apply()

**Métodos públicos:**
- `Rebuild(dna)` — limpia ambos trees, invoca BuildLineage + BuildBreed

**Cambios S129:** BuildBreed usa `GetAllKnown()` en lugar de `GetAll()` para mostrar criaturas vendidas y muertas en el árbol de descendencia.

**Conexiones:** [[MorimonchiDetailInfoUITK]], [[CreatureDatabaseSO]], [[CreatureRegistrySO]], [[MonchiPortraitUI]], [[Loc]]
