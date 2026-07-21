---
tags: [script, ui, presenter]
---

# DetailTreesPresenter.cs

**Ruta:** `UI/DetailTreesPresenter.cs`

**Responsabilidad (S54):** Presenter colaborador de MorimonchiDetailInfoUITK — cubre DOS tabs (Linaje + Descendencia) por ser un dominio único (comparten `MakeChip()` y `ParseGenetics()`). Implementa ro `Rebuild(dna)` — no navegación.

**Tab 1: Linaje (árbol ancestral 2 generaciones)**
- Construye bloques recursivos: self (chip) → [padres row + conector V] → [abuelos row + conector V]
- Resuelve ancestros vivos desde registry (full recursión upward si existe) o parsea genética de ID (muerto, solo display)
- Chips: "Tú" (self, highlighted), "Madre", "Padre" (labels role), retrato fotomatón vía [[MonchiPortraitUI]].Apply(), tachado si dead

**Tab 2: Descendencia (árbol de cría)**
- Escanea registry por cualquiera cuyo MotherID o FatherID sea self
- Agrupa por pareja (padre/madre alternativo), ordena por discovery
- Árbol downward: self → [pareja row + conector] → [hijos como chips]
- Chips: "Pareja" (padre/madre en descendencia), "Cría" (hijos), retrato fotomatón vía [[MonchiPortraitUI]].Apply()

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
- `BuildBreed()` — llamada una vez desde Rebuild, construye downward tree con agrupación por pareja
- `BuildPartnerBranch()` — columna partner → conector → hijos
- `MakeChip(dna, role, isSelf, isDead)` — crea VisualElement chip con retrato vía [[MonchiPortraitUI]].Apply()

**Métodos públicos:**
- `Rebuild(dna)` — limpia ambos trees, invoca BuildLineage + BuildBreed

**Conexiones:** [[MorimonchiDetailInfoUITK]], [[CreatureDatabaseSO]], [[CreatureRegistrySO]], [[MonchiPortraitUI]]
