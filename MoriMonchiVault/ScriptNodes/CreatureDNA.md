---
tags: [script, genetics]
---

# CreatureDNA.cs

**Ruta:** `Data/Genetics/CreatureDNA.cs`

## Responsabilidad

Modelo central: string genético (`ToStringID()`/`FromID()`: `"BODYSHAPE-ARM-EYE-MOUTH-RRGGBB"`), identidad (`UniqueID` con timestamp), linaje (`MotherID`/`FatherID`/`ChildrenIDs`), género, rol de combate, afinidad elemental, stats base 3 iniciales (`BaseConstitution`/`BaseAttack`/`BaseSpeed`) heredables + 3 derivados de equipo (`BaseDefense`/`BaseLuck`/`BaseEvasion`), combat history, needs, busy state (`BusyReason`), propiedad `IsSold` (true iff BusyState == Sold), timers de cría (`BreedReadyAt`/`BreedPartnerID`), ubicación anchada (`LocationKey`/`LocationSlot`), timestamps (`QueuedAt` enqueue async, `SaleDate` venta a NPC), `FurType` (metadata), `IsShiny` (bool 0.5% en mint/breed, reemplaza todo el tintado normal con gema), equipo equipado (`Equipped` dict de `EquipmentSlot` → ID string), y colores (`BaseColor` solo en la genetic string, `SecondaryColor` derivado determinista). `FromID()` parsea solo la parte genética; la deserialización JSON maneja el estado completo. `SecondaryColor` se regenera automático en `ReconcileColors()` (CreatureRegistrySO).

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
| `ToStringID()` | `string` | Retorna genetic string: `"BODY-ARM-EYE-MOUTH-RRGGBB"` (no incluye Role/Gender/Element/FurType/IsShiny) |
| `UniqueID` { get; } | `string` | Retorna `"{ToStringID()}-{Timestamp}"` (clave del registry); "" si no stampado |
| `AgeDays` { get; } | `int` | Retorna días vivos desde BirthDate (0 si no stampado) |
| `GetDisplayName(db)` | `string` | Retorna nombres temáticos de partes (body + arm + eye + mouth) vía PartNameBank |

## Backward Compatibility

- **S21:** LocationKey/LocationSlot son additive, no rompen records viejos
- **S37:** Role también additive (default Protector si no serializado)
- **S39:** Element también additive (default Agua si no serializado)
- **S57:** IsShiny y FurType son additive (default false/Pattern00 si no serializados)

## Serialización

JSON con Newtonstein.Json, campos PascalCase (match contrato JS). `Role`, `Element`, `FurType` se serializan como int (enum values). `IsShiny` como bool. Diccionario `Equipped` se serializa con clave string (EquipmentSlot enum convertido).

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/03 - Combat System]], [[Index/10 - Visualization]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureStats]], [[NeedsState]], [[CombatRecord]], [[MoriMochiAgent]], [[BreedingService]], [[PartDatabaseSO]], [[CreatureDatabaseSO]], [[ColorGenetics]], [[FurTypeDatabaseSO]], [[MonchiVisualBankSO]], [[EquipmentSO]], [[EquipmentDatabaseSO]], [[Enums]], [[RoleWorldProfileSO]], [[CombatElements]], [[Element]], [[ElementalState]]

## Cambios Sesión S37

**NUEVO campo `Role`:**
- Tipo: `Role` enum (Protector=0, Agresivo=1, Empático=2)
- Asignación: Al azar 1/3 en MintRandomCreature; hereda 50/50 padres en breeding (aleatorio si un solo padre)
- NO genético: No incluido en `ToStringID()` ni en genetic string
- Metadata como Gender/Personality: Independiente, hereda separadamente
- Consumo: `RoleWorldProfileSO.GetProfile(role)` → comportamiento en world; `CombatService` evalúa role para modificadores de stats (ConMod, AtkMod, SpdMod) + efectos de rol (Shield, BacklineHit, Heal)
- Uso en combate: `CombatService.TakeTurn()` evalúa role de atacante/defensor, aplica modificadores y efectos de rol cada turno

**Impacto genético:** Role NO afecta string genético (no contribuye a heredabilidad visual); es puramente jugabilidad + combate. Dos criaturas con identical parts + color + role = still different DNA (timestamp es diferenciador).

## Cambios Sesión S39

**NUEVO campo `Element`:**
- Tipo: `Element` enum (Agua=0, Fuego=1, Electricidad=2, Planta=3)
- Asignación: Al azar en MintRandomCreature; hereda 50/50 padres en BreedingService (con chance de mutación)
- NO genético: No incluido en `ToStringID()` ni en genetic string
- Metadata como Gender/Role: Independiente
- Consumo: `CombatElements.AddMark()` aplica marca elemental; dos elementos distintos de la misma fuente detonan reacción vía `CombatElements.ReactionFor()`
- Uso en combate: Marcas elementales + reacciones (instantáneas vs armadas); determinista vía CombatRng

**Impacto genético:** Element NO afecta string genético. Afinidad elemental es atributo gameplay que interactúa con el sistema de elementos del combate 3v3.

## Cambios Sesión S57

**NUEVO campo `FurType`:**
- Tipo: `FurType` enum (Pattern00-32, 33 valores)
- Asignación: Al azar uniforme en mint (default), o ponderado vía `FurTypeDatabaseSO.RollMintFurType()` si se pasa tabla; hereda 50/50 padres en breeding (sin pesar)
- Default al mint: Pattern00 si no especificado
- NO genético: No incluido en `ToStringID()`
- Consumo: `MonchiVisualizer` aplica material `MonchiFur_{FurType}` al cuerpo
- Metadata como Gender/Role/Element: Independiente

**NUEVO campo `IsShiny`:**
- Tipo: bool
- Asignación: `ColorGenetics.RollShiny()` (0.5% probabilidad) en mint (CreatureGenerator.GenerateRandom); hereda `ColorGenetics.RollShiny()` (0.5% roll nuevo) en breed (BreedingService.Breed)
- Default: false
- NO genético: No incluido en `ToStringID()`
- Consumo: Si true, `MonchiVisualizer.ApplyLook()` reemplaza todo tintado normal con material gema determinístico (hash FNV-1a del UniqueID)
- Impacto visual: Rarity cosmética 0.5%, reemplaza colores + patrones con gema brillante (no afecta gameplay)

**Impacto genético:** Ni FurType ni IsShiny contribuyen al genetic string. Son metadata puramente visuales. Timestamp es diferenciador.
