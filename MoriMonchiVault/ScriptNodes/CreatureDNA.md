---
tags: [script, genetics]
---

# CreatureDNA.cs

**Ruta:** `Data/Genetics/CreatureDNA.cs`

## Responsabilidad

Modelo central: string genético (`ToStringID()`/`FromID()`: `"BODYSHAPE-ARM-EYE-MOUTH-RRGGBB"`), identidad (`UniqueID` con timestamp), linaje (`MotherID`/`FatherID`/`ChildrenIDs`), género, rol de combate, afinidad elemental, stats base 3 iniciales (`BaseConstitution`/`BaseAttack`/`BaseSpeed`) heredables + 3 derivados de equipo (`BaseDefense`/`BaseLuck`/`BaseEvasion`), combat history, needs, busy state (`BusyReason`), propiedad `IsSold` (true iff BusyState == Sold), timers de cría (`BreedReadyAt`/`BreedPartnerID`), ubicación anchada (`LocationKey`/`LocationSlot`), timestamps (`QueuedAt` enqueue async, `SaleDate` venta a NPC), `FurType` (metadata), equipo equipado (`Equipped` dict de `EquipmentSlot` → ID string), y colores (`BaseColor` solo en la genetic string, `SecondaryColor` derivado determinista). `FromID()` parsea solo la parte genética; la deserialización JSON maneja el estado completo. `SecondaryColor` se regenera automático en `ReconcileColors()` (CreatureRegistrySO).

## Cambios en S21

- Renombrado: `HomePenKey` → `LocationKey`, `HomePenSlot` → `LocationSlot` (genéricos, aplican a cualquier lugar anclado: corral de cría, estante, corral normal).
- `LocationKey` persiste dónde la criatura está colocada (clave del lugar en `AnchorRegistry`); "" = suelta.
- `LocationSlot` persiste un índice opcional del lugar (ej: slot de breeding, slot de estante); -1 = sin asignar.

## Cambios en S37

- **NUEVO:** Campo `Role` (enum, metadata heredable no genética). Asignado al azar en mint (1/3), hereda 50/50 en breeding. Determina modificadores de stats y efectos de rol en combate 3v3 vía `RoleWorldProfileSO.GetProfile(Role)`.
- Rol NO es parte del string genético (`ToStringID()` no lo incluye); es metadata como `Gender` y `Personality`.

## Cambios en S39

- **NUEVO:** Campo `Element` (enum, metadata heredable no genética). Afinidad elemental innata (Agua, Fuego, Electricidad, Planta). Asignado al azar en mint, hereda 50/50 en breeding con chance de mutación. NO es parte del genetic string. Conduce reacciones elementales en combate vía `CombatElements.AddMark()` / `CombatElements.ReactionFor()`.

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
| `ToStringID()` | `string` | Retorna genetic string: `"BODY-ARM-EYE-MOUTH-RRGGBB"` (no incluye Role/Gender/Element) |
| `UniqueID` { get; } | `string` | Retorna `"{ToStringID()}-{Timestamp}"` (clave del registry); "" si no stampado |
| `AgeDays` { get; } | `int` | Retorna días vivos desde BirthDate (0 si no stampado) |
| `GetDisplayName(db)` | `string` | Retorna nombres temáticos de partes (body + arm + eye + mouth) vía PartNameBank |

## Backward Compatibility

- **S21:** LocationKey/LocationSlot son additive, no rompen records viejos
- **S37:** Role también additive (default Protector si no serializado)
- **S39:** Element también additive (default Agua si no serializado)

## Serialización

JSON con Newtonstein.Json, campos PascalCase (match contrato JS). `Role` y `Element` se serializan como int (enum values). Diccionario `Equipped` se serializa con clave string (EquipmentSlot enum convertido).

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/03 - Combat System]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureStats]], [[NeedsState]], [[CombatRecord]], [[MoriMochiAgent]], [[BreedingService]], [[PartDatabaseSO]], [[CreatureDatabaseSO]], [[ColorGenetics]], [[FurTypeDatabaseSO]], [[EquipmentSO]], [[EquipmentDatabaseSO]], [[Enums]], [[RoleWorldProfileSO]], [[CombatElements]], [[Element]], [[ElementalState]]

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
