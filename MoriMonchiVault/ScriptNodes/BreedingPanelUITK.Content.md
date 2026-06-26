---
tags: [script, ui, partial]
---

# BreedingPanelUITK.Content.md

**Ruta:** `UI/BreedingPanelUITK.Content.cs`

**Responsabilidad:** Contenido de las 2 pestañas del panel de crianza: Criar (padre + madre + preview + botón Breed) y Incubando (huevos en progreso + timers + botón Hatch).

**Vinculado a:** [[BreedingPanelUITK]], [[Index/05 - UI System]]

**Conexiones:** [[BreedingService]], [[BreedingController]], [[AsyncBreedingService]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[CreatureDNA]]

**Métodos principales:**

- `RebuildCandidates()`: lista padre (izquierda) + madre (derecha) vía `MakeCandidate()`
- `MakeCandidate(dna, bucket, isFather)`: fila con nombre + 6 stats (CON/ATK/SPD/DEF/LCK/EVA) + ratio crianza/límite. Usa fallback `new EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion)` si sin database
- `RebuildEggs()`: lista de criaturas en Breeding (hembra + timer + botón Hatch)
- `RefreshEggTimers()`: cuenta atrás a BreedReadyAt; muestra "¡Listo!" y habilita botón cuando llega
- `RefreshSlots()`: actualiza slots padre/madre + construye preview
- `BuildPreview()`: muestra resumen de ambos padres + duración esperada
- `ParentSummary(dna)`: columna con nombre, stats (6 campos CON/ATK/SPD/DEF/LCK/EVA), partes del padre
- `SelectFather()`, `SelectMother()`: cambia selección + busca foco en región Criar
- `TryBreed()`: envía request a `AsyncBreedingService.StartBreedingAsync(motherId, fatherId)`, espera respuesta antes de limpiar slots
- `DoHatch(motherId, btn)`: envía `HatchAsync()`, restaura botón si no_ready o lo deja orphaned si éxito

**Stats mostrados:**
- `MakeCandidate`: "CON X ATK Y SPD Z DEF A LCK B EVA C · X/MaxBreedCount"
- `ParentSummary`: "CON X   ATK Y   SPD Z   DEF A   LCK B   EVA C" (multiline con partes)
