---
tags: [script, genetics]
---

# CreatureDNA.cs

**Ruta:** `Data/Genetics/CreatureDNA.cs`

## Responsabilidad

Modelo central: string genético (`ToStringID()`/`FromID()`: `"BODYSHAPE-ARM-EYE-MOUTH-RRGGBB"`), identidad (`UniqueID` con timestamp), linaje (`MotherID`/`FatherID`/`ChildrenIDs`), género, rol de combate, afinidad elemental, **S69:** diales genéticos no-heredables (`Sociability`/`Boldness` float 0..1, default 0.5), stats base 3 iniciales (`BaseConstitution`/`BaseAttack`/`BaseSpeed`) heredables + 3 derivados de equipo (`BaseDefense`/`BaseLuck`/`BaseEvasion`), combat history, needs, busy state (`BusyReason`), propiedad `IsSold` (true iff BusyState == Sold), timers de cría (`BreedReadyAt`/`BreedPartnerID`), ubicación anchada (`LocationKey`/`LocationSlot`), timestamps (`QueuedAt` enqueue async, `SaleDate` venta a NPC), `FurType` (metadata), `IsShiny` (bool 0.5% en mint/breed, reemplaza todo el tintado normal con gema), equipo equipado (`Equipped` dict de `EquipmentSlot` → ID string), y colores (`BaseColor` solo en la genetic string, `SecondaryColor` derivado determinista). `FromID()` parsea solo la parte genética; la deserialización JSON maneja el estado completo. `SecondaryColor` se regenera automático en `ReconcileColors()` (CreatureRegistrySO).

## Cambios en S69

- **NUEVO:** Campos `Sociability` y `Boldness` (float, rango 0..1, default 0.5 cada uno). Metadata NO genética (fuera de `ToStringID()`), heredable en breeding vía `BreedingService.InheritDial()` con herencia por Average/Copy/Mutation.
- Sociability modula afinidad social (Approach/PlayChase/SleepTogether) y cooldown entre interacciones (via `SocialTuningSO.DialShift()` y `ScaledSocialCooldown()`)
- Boldness modula agresividad en peleas + evitación social (via `SocialTuningSO.DialShift()`)
- Ambos diales se asignan al random en `GameManager.MintRandomCreature()` vía `CreatureGenerator.RandomDial()`

## Cambios en S21

- Renombrado: `HomePenKey` → `LocationKey`, `HomePenSlot` → `LocationSlot` (genéricos, aplican a cualquier lugar anclado: corral de cría, estante, corral normal).
- `LocationKey` persiste dónde la criatura está colocada (clave del lugar en `AnchorRegistry`); "" = suelta.
- `LocationSlot` persiste un índice opcional del lugar (ej: slot de breeding, slot de estante); -1 = sin asignar.

## Cambios en S37

- **NUEVO:** Campo `Role` (enum, metadata heredable no genética). Asignado al azar en mint (1/3), hereda 50/50 en breeding. Determina modificadores de stats y efectos de rol en combate 3v3 vía `RoleWorldProfileSO.GetProfile(Role)`.
- Rol NO es parte del string genético (`ToStringID()` no lo incluye); es metadata como `Gender` y `Personality`.

## Cambios en S39

- **NUEVO:** Campo `Element` (enum, metadata heredable no genética). Afinidad elemental innata (Agua, Fuego, Electricidad, Planta). Asignado al azar en mint, hereda 50/50 en breeding con chance de mutación. NO es parte del genetic string. Conduce reacciones elementales en combate vía `CombatElements.AddMark()` / `CombatElements.ReactionFor()`.

## Cambios en S57

- **NUEVO:** Campo `IsShiny` (bool, metadata no heredable). Probabilidad 0.5% en mint (CreatureGenerator.RollShiny()) y breeding (BreedingService.Breed() roll al nacer). Default false. Si true, MonchiVisualizer reemplaza todo tintado normal con material gema determinístico por hash de UniqueID (via MonchiVisualBankSO.GetGem()).
- **ACTUALIZADO `FurType`:** Expandido de 5 valores (Smooth, Fluffy, Spiky, Shaggy, Scaly) a 33 patrones (Pattern00-Pattern32). Hereda 50/50 de padres en breeding (sin pesar). Al mintear, tabla de pesos opcional en FurTypeDatabaseSO determina probabilidad relativa de cada patrón.
- Default: `FurType.Pattern00`, `IsShiny.false`

## Campos principales (seleccionados)

| Campo | Tipo | Propósito |
|-------|------|----------|
| `BaseConstitution` | float | HP base point-buy (heredable). |
| `BaseAttack` | float | ATK base point-buy (heredable). |
| `BaseSpeed` | float | SPD base point-buy (heredable). |
| `BaseDefense` | float | DEF base (0 al mint; derivado de equipo). |
| `BaseLuck` | float | LCK base (0 al mint; derivado de equipo). |
| `BaseEvasion` | float | EVA base (0 al mint; derivado de equipo). |
| `Equipped` | `Dict<EquipmentSlot, string>` | Items equipados por slot → ID ("EQ0", "EQ1"…). |
| `BaseColor` | Color | Color genético (parte de la cadena genética, fuente de verdad). |
| `SecondaryColor` | Color | Derivado determinista de `BaseColor`. |
| `FurType` | `FurType` | **S57** Patrón de pelaje (Pattern00-32), metadata NO genética. |
| `IsShiny` | bool | **S57** 0.5% rarity flag; reemplaza tintado con gema si true. |
| `Gender` | `CreatureGender` | Metadata (Unknown, Male, Female). No genético. |
| `Role` | `Role` | **S37** Rol de combate 3v3 (Protector, Agresivo, Empático). No genético, al azar 1/3 en mint, 50/50 padres en breeding. |
| `Element` | `Element` | **S39** Afinidad elemental (Agua, Fuego, Electricidad, Planta). No genético, al azar en mint, 50/50 padres en breeding. |
| `Sociability` | float | **S69** Dial genético 0..1 (default 0.5). Modula afinidad social + cooldown. No genético (fuera de ToStringID). |
| `Boldness` | float | **S69** Dial genético 0..1 (default 0.5). Modula agresividad en pelea + evitación. No genético (fuera de ToStringID). |
| `LocationKey` | string | Clave del lugar anclado ("x_y" del AnchorRegistry); "" = libre. |
| `LocationSlot` | int | Slot dentro del lugar (-1 = unassigned). |
| `BreedReadyAt` | long | Epoch ms del servidor cuando la cría lista; 0 = no incubando. |
| `BreedPartnerID` | string | UniqueID del otro padre. |
| `BusyState` | BusyReason | None / Breeding / Sold / etc. |
| `IsSold` { get; } | bool | `BusyState == BusyReason.Sold`. |
| `CombatHistory` | `List<CombatRecord>` | Historial replayable de todos los combates (local + async). |
| `IsDead` | bool | Muerte permanente (5% en combate perdedor). |
| `Needs` | `NeedsState` | Wellbeing runtime (Health, Energy, Affect). |

## Métodos Clave

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Stamp()` | `void` | Asigna Timestamp (UTC ticks) y BirthDate antes de registrar en CreatureRegistry |
| `ToStringID()` | `string` | Retorna genetic string: `"BODY-ARM-EYE-MOUTH-RRGGBB"` (no incluye Role/Gender/Element/FurType/IsShiny/Sociability/Boldness) |
| `UniqueID` { get; } | `string` | Retorna `"{ToStringID()}-{Timestamp}"` (clave del registry); "" si no stampado |
| `AgeDays` { get; } | `int` | Retorna días vivos desde BirthDate (0 si no stampado) |
| `GetDisplayName(db)` | `string` | Retorna nombres temáticos de partes (body + arm + eye + mouth) vía PartNameBank |

## Backward Compatibility

- **S21:** LocationKey/LocationSlot son additive, no rompen records viejos
- **S37:** Role también additive (default Protector si no serializado)
- **S39:** Element también additive (default Agua si no serializado)
- **S57:** IsShiny y FurType son additive (default false/Pattern00 si no serializados)
- **S69:** Sociability/Boldness son additive (default 0.5 si no serializados)

## Serialización

JSON con Newtonstein.Json, campos PascalCase (match contrato JS). `Role`, `Element`, `FurType` se serializan como int (enum values). `IsShiny` como bool. `Sociability` y `Boldness` como float. Diccionario `Equipped` se serializa con clave string (EquipmentSlot enum convertido).

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/03 - Combat System]], [[Index/10 - Visualization]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureStats]], [[NeedsState]], [[CombatRecord]], [[MoriMochiAgent]], [[BreedingService]], [[PartDatabaseSO]], [[CreatureDatabaseSO]], [[ColorGenetics]], [[FurTypeDatabaseSO]], [[MonchiVisualBankSO]], [[EquipmentSO]], [[EquipmentDatabaseSO]], [[Enums]], [[RoleWorldProfileSO]], [[CombatElements]], [[Element]], [[ElementalState]], [[SocialTuningSO]], [[InheritanceOddsTableSO]]
