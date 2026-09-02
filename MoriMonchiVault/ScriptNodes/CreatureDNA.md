---
tags: [data, genetics, serializable]
---

# CreatureDNA.cs

**Ruta:** `Data/Genetics/CreatureDNA.cs`

**Responsabilidad:** Dato serializable [Serializable] que representa la genética y estado completo de un MoriMochi. Contiene: partes genéticas (BodyShapeID/HornID/BackID/WingID/FaceID con tiers), colores (BaseColor/SecondaryColor), pelaje (FurType 33 patrones, IsShiny), identidad (CustomName, Timestamp, BirthDate, UniqueID), parentesco (MotherID/FatherID/ChildrenIDs), demografía (Gender), rol/elemento (Role, Element), personalidad (Sociability, Boldness), stats base (Constitution/Attack/Speed/Defense/Luck/Evasion), potenciales de combate (HornPotential/BackPotential/WingPotential 1-10), necesidades (Needs), estado (BusyReason, IsDead, BreedCount, SaleDate), reproducción (BreedReadyAt, BreedPartnerID), cooldown combate (CombatCooldownUntil), ubicación (LocationKey, LocationSlot), equipamiento (Equipped, HeldItemId).

**S93:** Enums refactorizados a archivos dedicados. Eliminados CutieMarks (S93). Método estático `FromID()` parsea genetic string; JSON deserialización maneja estado completo.

**S95:** Agregados potenciales de combate (HornPotential/BackPotential/WingPotential) para Dragon RPS. CombatCooldownUntil ya existía pero ahora es usado activamente por combate.

## Cambios en S75 (Demolición combate + migración genética)

- **Genetic string:** BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB (5 partes + hex color)
- **Partes nuevas:** HornID/BackID/WingID/FaceID reemplazan partes antiguas
- **Tiers:** BodyTier/HornTier/BackTier/WingTier (4 partes, Face sin tier)
- **Eliminados:** FightCount, WinCount, CombatHistory, QueuedAt, QueuedForCombat
- **Nuevos:** HeldItemId (item sostenido)
- **BusyReason:** Solo None/Breeding/Sold (sin QueuedForCombat)

## Cambios en S93 (Consolidación enums)

- CutieMarks eliminado (ya no en CreatureDNA)
- Enums emigran a archivos dedicados: [[CreatureEnums]], [[GeneticsEnums]], [[ItemEnums]]

## Cambios en S95 (Dragon RPS)

- **Potenciales agregados:** HornPotential, BackPotential, WingPotential (int, rango 1-10, default 1)
- **CombatCooldownUntil:** Activado para cooldown post-derrota en combate

## Campos Principales

| Campo | Tipo | Propósito |
|-------|------|----------|
| `BodyShapeID` / `HornID` / `BackID` / `WingID` / `FaceID` | string | IDs de partes (keys a PartDatabaseSO) |
| `BaseColor` / `SecondaryColor` | Color | Colores (derivados genéticamente) |
| `FurType` | FurType | Patrón de pelaje (Pattern00-32) |
| `IsShiny` | bool | Variante shinny (0.5% rareza) |
| `CustomName` | string | Nombre asignado por jugador |
| `Timestamp` | long | Ticks UTC (identidad) |
| `BirthDate` | DateTime | Nacimiento |
| `MotherID` / `FatherID` / `ChildrenIDs` | string / List | Genealogía |
| `Gender` | CreatureGender | Unknown/Male/Female |
| `Role` | Role | Protector/Agresivo/Empatico |
| `Element` | Element | Agua/Fuego/Electricidad/Planta |
| `Sociability` / `Boldness` | float | Diales (0-1) |
| `BreedCount` | int | Cantidad reproducida |
| `{Body/Horn/Back/Wing}Tier` | Tier | Rareza (Tier1/2/3) |
| `Base{Constitution/Attack/Speed/Defense/Luck/Evasion}` | float | Stats |
| `HornPotential` / `BackPotential` / `WingPotential` | int | Potencial combate (1-10, usado en Dragon RPS) |
| `IsDead` | bool | Muerte permanente |
| `Needs` | NeedsState | Health/Energy/Affect |
| `BusyState` | BusyReason | None/Breeding/Sold |
| `SaleDate` | DateTime | Cuándo vendida |
| `BreedReadyAt` / `BreedPartnerID` | long / string | Reproducción |
| `CombatCooldownUntil` | long | Cooldown post-combate (ticks) |
| `LocationKey` / `LocationSlot` | string / int | Ubicación en mundo |
| `Equipped` | Dict<EquipmentSlot, string> | Equipo (slot → ID) |
| `HeldItemId` | string | Item sostenido |

## Métodos & Propiedades

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Stamp()` | void | Asigna Timestamp (UTC ticks) + BirthDate = ahora |
| `ToStringID()` | string | `"BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB"` (genetic string) |
| `FromID(string id)` [static] | CreatureDNA | Parsea genetic string → new DNA |
| `UniqueID` [property] | string | `"{ToStringID()}-{Timestamp}"` (identidad única) |
| `AgeDays` [property] | int | `(DateTime.UtcNow - BirthDate).TotalDays` |
| `GetDisplayName(db)` | string | "Body Horn Back Wing" (nombres de partes vía DB) |
| `IsBusy` / `IsSold` [property] | bool | Derived: `BusyState != None` / `== Sold` |

## Contrato de Red

**ToStringID()** es el único serializado a servidor (sin Timestamp). Invariantes:
- Ningún token contiene `-` (separador de tokens)
- BaseColor derivado de hex final (RRGGBB)
- Timestamp genera UniqueID única (prevent collisions)

## Vinculado a

- [[Index/01 - Creature Genetics & System]]
- [[Index/21 - Combate v3 - Dragon RPS]]
- [[CreatureEnums]], [[GeneticsEnums]], [[ItemEnums]] — enums refactorizados
- [[NeedsState]] — wellbeing runtime
- [[ColorGenetics]] — derivación de colores

**Conexiones:** [[CreatureRegistrySO]], [[CreatureGenerator]], [[NeedsState]], [[ColorGenetics]], [[MonchiVisualizer]], [[BreedingService]], [[PartDatabaseSO]], [[EquipmentDatabaseSO]], [[DragonRpsGenes]]

