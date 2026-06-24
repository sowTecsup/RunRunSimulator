---
tags: [script, genetics]
---

# CreatureDNA.cs

**Ruta:** `Data/Genetics/CreatureDNA.cs`

## Responsabilidad

Modelo central: string genético (`ToStringID()`/`FromID()`: `"BODYSHAPE-ARM-EYE-MOUTH-RRGGBB"`), identidad (`UniqueID` con timestamp), linaje (`MotherID`/`FatherID`/`ChildrenIDs`), género, personalidad, stats base (`BaseHP`/`BaseAttack`/`BaseSpeed`), combat history, needs, busy state (`BusyReason`), propiedad `IsSold` (true iff BusyState == Sold), timers de cría (`BreedReadyAt`/`BreedPartnerID`), ubicación anchada (`LocationKey`/`LocationSlot`), timestamps (`QueuedAt` enqueue async, `SaleDate` venta a NPC), `FurType` (metadata), y colores (`BaseColor` solo en la genetic string, `SecondaryColor` derivado determinista). `FromID()` parsea solo la parte genética; la deserialización JSON maneja el estado completo. `SecondaryColor` se regenera automático en `ReconcileColors()` (CreatureRegistrySO).

## Cambios en S21

- Renombrado: `HomePenKey` → `LocationKey`, `HomePenSlot` → `LocationSlot` (genéricos, aplican a cualquier lugar anclado: corral de cría, estante, corral normal).
- `LocationKey` persiste dónde la criatura está colocada (clave del lugar en `AnchorRegistry`); "" = suelta.
- `LocationSlot` persiste un índice opcional del lugar (ej: slot de breeding, slot de estante); -1 = sin asignar.

## Campos principales (seleccionados)

| Campo | Tipo | Propósito |
|-------|------|----------|
| `BaseColor` | Color | Color genético (parte de la cadena genética, fuente de verdad). |
| `SecondaryColor` | Color | Derivado determinista de `BaseColor`. |
| `LocationKey` | string | Clave del lugar anclado ("x_y" del AnchorRegistry); "" = libre. |
| `LocationSlot` | int | Slot dentro del lugar (-1 = unassigned). |
| `BreedReadyAt` | long | Epoch ms del servidor cuando la cría lista; 0 = no incubando. |
| `BreedPartnerID` | string | UniqueID del otro padre. |
| `BusyState` | BusyReason | None / Breeding / Sold / etc. |
| `IsSold` { get; } | bool | `BusyState == BusyReason.Sold`. |

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureStats]], [[NeedsState]], [[CombatRecord]], [[MoriMochiAgent]], [[BreedingService]], [[PartDatabaseSO]], [[CreatureDatabaseSO]], [[ColorGenetics]], [[FurTypeDatabaseSO]]
