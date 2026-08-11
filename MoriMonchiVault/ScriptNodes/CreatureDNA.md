---
tags: [script, genetics]
---

# CreatureDNA.cs

**Ruta:** `Data/Genetics/CreatureDNA.cs`

## Responsabilidad

Modelo central de genética y estado de criatura: string genético (`ToStringID()`/`FromID()`: `"BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB"` con 5 slots genéticos + color hex), identidad (`UniqueID` con timestamp), linaje (`MotherID`/`FatherID`/`ChildrenIDs`), género, rol, afinidad elemental, diales genéticos (`Sociability`/`Boldness`), stats base heredables (`BaseConstitution`/`BaseAttack`/`BaseSpeed`) + derivados de equipo (`BaseDefense`/`BaseLuck`/`BaseEvasion`), needs runtime, busy state (`BusyReason`), timers de cría, ubicación anchada, timestamps, `FurType` (metadata), `IsShiny`, equipo equipado (`Equipped` dict), **S75:** marcas distintivas (`CutieMarks` lista, max 2), **S75:** item sostenido (`HeldItemId`), y colores. `FromID()` parsea solo la parte genética; la deserialización JSON maneja el estado completo. **S75:** Sin combate (sin `FightCount`, `WinCount`, `CombatHistory`, `QueuedAt`, `QueuedForCombat`).

## Cambios en S75 (Demolición de combate + migración de genes)

- **Genetic string format NUEVO:** `"BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB"` (5 partes en lugar de 4: Body/Horn/Back/Wing/Face).
- **Nuevos campos de genes:** `HornID`, `BackID`, `WingID`, `FaceID` (reemplazan `ArmID`, `EyeID`, `MouthID`).
- **Tiers actualizados:** `BodyTier`, `HornTier`, `BackTier`, `WingTier` (sin `FaceTier`; Face es la 5ta parte sin tier).
- **ELIMINADOS:** `FightCount`, `WinCount`, `CombatHistory`, `QueuedAt`, `QueuedForCombat` (relacionados con combate async).
- **NUEVOS:** `MaxCutieMarks = 2`, `CutieMarks` (lista de IDs de marcas), `HeldItemId` (string, item sostenido).
- `BusyReason` simplificado: solo `None`, `Breeding`, `Sold` (sin `QueuedForCombat`).
- `GetDisplayName(db)` ahora retorna nombres de Body/Horn/Back/Wing (sin Face).

## Cambios en S69

- **NUEVO:** Campos `Sociability` y `Boldness` (float, rango 0..1, default 0.5 cada uno). Metadata NO genética (fuera de `ToStringID()`), heredable en breeding vía `BreedingService.InheritDial()` con herencia por Average/Copy/Mutation.

## Cambios en S37

- **NUEVO:** Campo `Role` (enum, metadata heredable no genética). Asignado al azar en mint (1/3), hereda 50/50 en breeding.

## Cambios en S39

- **NUEVO:** Campo `Element` (enum, metadata heredable no genética). Afinidad elemental innata (Agua, Fuego, Electricidad, Planta). Asignado al azar en mint, hereda 50/50 en breeding con chance de mutación.

## Cambios en S57

- **NUEVO:** Campo `IsShiny` (bool, metadata no heredable). Probabilidad 0.5% en mint y breeding. Si true, reemplaza tintado normal con material gema.
- **ACTUALIZADO `FurType`:** 33 patrones (Pattern00-32), hereda 50/50 de padres en breeding.

## Campos principales

| Campo | Tipo | Propósito |
|-------|------|----------|
| `BodyShapeID` | string | ID de cuerpo (parte genética #1) |
| `HornID` | string | ID de cuerno (parte genética #2) |
| `BackID` | string | ID de dorso/espalda (parte genética #3) |
| `WingID` | string | ID de ala (parte genética #4) |
| `FaceID` | string | ID de cara/rostro (parte genética #5) |
| `BaseColor` | Color | Color genético (parte de genetic string) |
| `SecondaryColor` | Color | Derivado determinista de BaseColor |
| `FurType` | `FurType` | Patrón de pelaje (Pattern00-32), metadata |
| `IsShiny` | bool | 0.5% rarity flag |
| `BaseConstitution` | float | CON base heredable |
| `BaseAttack` | float | ATK base heredable |
| `BaseSpeed` | float | SPD base heredable |
| `BaseDefense` | float | DEF base (derivado de equipo) |
| `BaseLuck` | float | LCK base (derivado de equipo) |
| `BaseEvasion` | float | EVA base (derivado de equipo) |
| `BodyTier` | `Tier` | Tier de Body (Tier1, Tier2, Tier3) |
| `HornTier` | `Tier` | Tier de Horn |
| `BackTier` | `Tier` | Tier de Back |
| `WingTier` | `Tier` | Tier de Wing |
| `Gender` | `CreatureGender` | Metadata (Unknown, Male, Female) |
| `Role` | `Role` | Rol (Protector, Agresivo, Empático) |
| `Element` | `Element` | Afinidad elemental |
| `Sociability` | float | Dial social (0..1) |
| `Boldness` | float | Dial agresividad (0..1) |
| `Equipped` | `Dict<EquipmentSlot, string>` | Items equipados |
| `CutieMarks` | `List<string>` | IDs de marcas (max 2) |
| `HeldItemId` | string | ID del item sostenido |
| `BusyState` | `BusyReason` | None / Breeding / Sold |
| `Needs` | `NeedsState` | Wellbeing runtime |
| `BreedCount` | int | Veces criada |
| `IsDead` | bool | Muerte permanente |

## Métodos Clave

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Stamp()` | `void` | Asigna Timestamp (UTC ticks) y BirthDate |
| `ToStringID()` | `string` | `"BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB"` |
| `FromID(string)` | `CreatureDNA` | Parsea genetic string, retorna new DNA |
| `UniqueID` { get; } | `string` | `"{ToStringID()}-{Timestamp}"` |
| `AgeDays` { get; } | `int` | Días vivos desde BirthDate |
| `GetDisplayName(db)` | `string` | "body horn back wing" vía PartNameBank |

## Backward Compatibility

- S75 es un breaking change: genetic string de 4 a 5 partes, eliminación de combate-related fields
- Migraciones esperadas en `CreatureRegistrySO.ReconcileGenes()` o similar

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[Index/04 - World & AI]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureStats]], [[EffectiveStats]], [[NeedsState]], [[MoriMochiAgent]], [[BreedingService]], [[CreatureDatabaseSO]], [[PartDatabaseSO]], [[ColorGenetics]], [[FurTypeDatabaseSO]], [[EquipmentSO]], [[EquipmentDatabaseSO]], [[Enums]], [[CutieMarkDatabaseSO]]
