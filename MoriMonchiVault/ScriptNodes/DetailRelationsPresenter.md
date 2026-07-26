---
tags: [script, ui, presenter]
---

# DetailRelationsPresenter.cs

**Ruta:** `UI/DetailRelationsPresenter.cs`

**Responsabilidad (S67):** Presenter colaborador de MorimonchiDetailInfoUITK (patrón S54 — no implementa ITabPresenter, sin navegación) — tab "Relaciones" (visualizador del SocialGraph). Renderiza dos listas de monchis vivos según afinidad efectiva (seed + historia): "Le caen bien" (afinidad ≥ RelationsFriendThreshold, default 0.25) y "Le caen mal" (≤ RelationsFoeThreshold, default 0.05), ordenadas por intensidad. Chips con retrato fotomatón (MonchiPortraitUI.Apply), nombre (custom o ToStringID) y glifo ❤/✖. Placeholder si ambas vacías. **S68:** Strings de labels extraídos a Loc.Tr (sin cambio de contrato).

**Métodos públicos:**
- `Rebuild(dna)` — limpia listas, recalcula afinidades efectivas via SocialGraphService, popula chips ordenados, muestra/oculta placeholder

**Datos UI:**
- `good` — VisualElement para chips "Le caen bien" (amigos)
- `bad` — VisualElement para chips "Le caen mal" (enemigos)
- `empty` — Label placeholder (visible si no hay relaciones)

**Construcción del chip:**
- `BuildChip(dna, isGood)` — crea VisualElement con 3 hijos:
  - `relation-swatch` — VisualElement con retrato fotomatón via MonchiPortraitUI.Apply
  - `relation-name` — Label nombre criatura (custom o ID)
  - `relation-glyph` — Label glifo (❤ si good, ✖ si bad) con clase CSS por estado

**Lógica de filtrado:**
1. Itera registry.GetAll() excluye: self (UniqueID), null, muertos (IsDead)
2. Calcula `SocialGraphService.EffectiveAffinity(dna, other, tuning)` (seed + historia)
3. Clasifica: amigos si aff ≥ threshold, enemigos si aff ≤ threshold (ni en medio → no aparece)
4. Ordena: amigos descendente por afinidad, enemigos ascendente
5. Si vacías ambas: muestra empty label

**Dependencias inyectadas:**
- `Func<CreatureRegistrySO> getRegistry` — resuelve registry en tiempo de rebuild (lazy)

**Cambios S68:** Strings de labels ("Le caen bien", "Le caen mal", placeholder) extraídos a Loc.Tr sin cambio de interfaz pública.

**Conexiones:** [[MorimonchiDetailInfoUITK]], [[SocialGraphService]], [[SocialTuningSO]], [[MonchiPortraitUI]], [[CreatureDNA]], [[Loc]]
