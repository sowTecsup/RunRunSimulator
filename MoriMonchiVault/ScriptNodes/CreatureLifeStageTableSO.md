---
tags: [script, genetics]
---

# CreatureLifeStageTableSO.cs

**Ruta:** `Data/Breeding/CreatureLifeStageTableSO.cs`

**Responsabilidad:** ScriptableObject que mapea edad en días (`AgeDays`) a etapa de vida visible (`LifeStage`). `GetStage(ageDays)` devuelve la etapa más alta cuyo threshold se haya alcanzado. `Label(stage)` retorna string localizado (S68: ahora via `LocEnumMaps.LifeStageName(stage)`). Display-only — nunca parte del string genético. Referenciado por `BreedingController` (único owner), leído por `NameTag` via `BreedingController.Instance.LifeStageTable`.

## Cambios S68 (Localization-ready)

**Línea 36:**
```csharp
public string Label(LifeStage stage) => LocEnumMaps.LifeStageName(stage);
```
- Antes: string hardcodeado en español (e.g., `stage == LifeStage.Adult ? "Adulto" : ...`)
- Ahora: delega a `LocEnumMaps.LifeStageName(stage)` → `Loc.Tr("stage." + KeyOf(stage))`

**Datos:**
- Dictionary `entryDayThreshold` — mapea `LifeStage` → edad (días) en que se entra en esa etapa
- Default: Newborn (0d), Child (1d), Teen (3d), Adult (7d), Elder (20d)
- Botón `SeedDefaults()` reestablece valores default (Odin, solo editor)

**Métodos públicos:**
- `GetStage(int ageDays) → LifeStage` — devuelve la LifeStage más alta alcanzada (búsqueda lineal por threshold)
- `Label(LifeStage stage) → string` — traduce etapa a string localizado (S68)

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/14 - Localization]]

**Conexiones:** [[BreedingController]], [[NameTag]], [[LocEnumMaps]], [[Loc]], Enums (`Core/Enums/`, S93) (LifeStage)
