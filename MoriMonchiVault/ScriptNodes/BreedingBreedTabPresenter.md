---
tags: [script, ui, presenter]
---

# BreedingBreedTabPresenter.cs

**Ruta:** `UI/BreedingBreedTabPresenter.cs`

**Responsabilidad (S54):** Presenter de Tab 0 "Criar" (seleccionar padre + madre, preview ambos, ver duración, iniciar breed async). Implementa `ITabPresenter`. Almacena estado UI-only: foco interno entre 3 SubFocus (Slots, FatherList, MotherList), índices de selección + IDs padre/madre guardados. 

**Navegación (jerarquía de foco):**
- **SubFocus.Slots** (3 slots: padre, madre, botón Breed) — h/v se mueven entre slots, v-down entra a lista correspondiente
- **SubFocus.FatherList** (scroll left) / **SubFocus.MotherList** (scroll right) — h/v navegan lista, Submit selecciona, Cancel vuelve a Slots
- Al seleccionar padre/madre, el foco se devuelve a Slots (SubFocus.Slots) para permitir cambios rápidos antes de pulsar Breed

**Estado de bloqueo (Busy):**
- Campo público `Busy:bool` — `true` durante `StartBreedingAsync()` en vuelo (congelados todos los inputs, botón gris "Breeding...")
- `Navigate()`, `Submit()`, `Cancel()` devuelven sin cambio de estado si Busy==true (consume input sin hacer nada)
- Core (BreedingPanelUITK) chequea `breed.Busy` antes de procesar input global

**Datos UI:**
- `fatherSlot`, `motherSlot` (botones/clicks → abren lista), `preview` (resumen padres + duración)
- `fatherList`, `motherList` (ScrollView con candidatos elegibles: vivos, no ocupados, BreedCount < Max)
- `breedButton` (dispara TryBreed async)

**Métodos de interfaz:**
- `Enter()` — resetea foco a criarIndex=0 (padre)
- `Navigate(h,v):bool` — maneja subfocus + índices, retorna false si sale del tab (v-up desde Slots)
- `Submit()` — En Slots: abre lista o dispara TryBreed. En listas: selecciona candidato
- `Cancel():bool` — Si lista: cierra y vuelve a Slots (true). Si Slots: retorna false (cierra tab)
- `ClearFocus()` — limpia clases visuales
- `Rebuild()` — rebuildCandidates + refreshSlots (datos + UI)
- `Teardown()` — desuscribe breedButton.clicked

**Métodos privados:**
- `MakeCandidate(dna, bucket, isFather)` — fila con nombre + 6 stats + contador BreedCount/Max. **S75:** usa `CreatureStats.GetEffectiveStats()` en lugar de CombatStats. Retrato fotomatón vía [[MonchiPortraitUI]].Apply()
- `RefreshSlots()` — SetSlot (nombre + retrato fotomatón) + BuildPreview
- `BuildPreview()` — muestra resumen columnar de padre/madre + duración ≈X min via InheritanceOdds. **S75:** Agrega 5 filas de partes genéticas: `GetBodyShape()`, `GetHorn()`, `GetBack()`, `GetWing()`, `GetFace()` (cada una es un BodyPart con swatch color Set + nombre + Set name)
- `ParentSummary()` — resumen de una criatura para preview (nombre + 6 stats + 5 partes)
- `AddPartRow()` — fila visual de parte: swatch (color Set) + nombre parte y Set
- `TryBreed()` — await `asyncBreedingService.StartBreedingAsync()`, clearear slots, invocar `onBred()` callback si éxito (madre en estado Breeding)

**Conexiones:** [[ITabPresenter]], [[BreedingPanelUITK]], [[AsyncBreedingService]], [[CreatureStats]], [[CreatureDatabaseSO]], [[MonchiPortraitUI]], [[BodyPart]]
