---
tags: [data, genetics, serializable]
---

# CreatureDNA

**Ruta:** `Data/Genetics/CreatureDNA.cs`

**Responsabilidad:** Dato serializable que representa genética y estado de un MoriMochi. Contiene: partes genéticas (BodyShapeID/HornID/BackID/WingID/FaceID con tiers), colores (BaseColor/SecondaryColor), pelaje (FurType 33 patrones, IsShiny), identidad (CustomName, Timestamp, BirthDate, UniqueID), parentesco (MotherID/FatherID/ChildrenIDs), demografía (Gender), rol/elemento (Role, Element), personalidad (Sociability, Boldness), stats base (Constitution/Attack/Speed/Defense/Luck/Evasion), potenciales de partes (HornPotential/BackPotential/WingPotential 1-10), necesidades (Needs), estado (BusyReason, IsDead, BreedCount, SaleDate), reproducción (BreedReadyAt, BreedPartnerID), ubicación (LocationKey, LocationSlot), equipamiento (Equipped).

**S128:** Eliminados `CombatCooldownUntil` y `HeldItemId` (demolición RPS). Mantiene potenciales de partes (techos para evolución).

## Campos Principales

| Campo | Tipo | Propósito |
|-------|------|----------|
| `BodyShapeID` / `HornID` / `BackID` / `WingID` / `FaceID` | string | IDs de partes (keys a PartDatabaseSO) |
| `BaseColor` / `SecondaryColor` | Color | Colores (derivados genéticamente) |
| `FurType` | FurType | Patrón de pelaje (Pattern00-32) |
| `IsShiny` | bool | Variante shiny (0.5% rareza) |
| `CustomName` | string | Nombre asignado jugador |
| `Timestamp` | long | Ticks UTC (identidad inmutable) |
| `BirthDate` | DateTime | Nacimiento |
| `MotherID` / `FatherID` / `ChildrenIDs` | string / List | Genealogía |
| `Gender` | CreatureGender | Unknown/Male/Female |
| `Role` | Role | Protector/Agresivo/Empático |
| `Element` | Element | Agua/Fuego/Electricidad/Planta |
| `Sociability` / `Boldness` | float | Diales (0-1) |
| `BreedCount` | int | Cantidad reproducida |
| `{Body/Horn/Back/Wing}Tier` | Tier | Rareza (Tier1/2/3) |
| `Base{Constitution/Attack/Speed/...}` | float | Stats base (6 stats) |
| `{Horn/Back/Wing}Potential` | int | Techo de nivel de parte (1-10, evolución) |
| `IsDead` | bool | Muerte permanente |
| `Needs` | NeedsState | Health/Energy/Affect |
| `BusyReason` | BusyReason | None/Breeding/Sold |
| `SaleDate` | DateTime | Cuándo vendida |
| `BreedReadyAt` / `BreedPartnerID` | long / string | Reprodución (reloj) |
| `LocationKey` / `LocationSlot` | string / int | Ubicación mundo |
| `Equipped` | Dict | Equipo (slot → ID); **S128 pendiente borado HC-2** |

## Métodos & Propiedades

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Stamp()` | `void` | Asigna Timestamp (UTC ticks) + BirthDate = ahora |
| `ToStringID()` | `string` | `"BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB"` (genetic string) |
| `FromID(string id)` [static] | `CreatureDNA` | Parsea genetic string → new DNA |
| `UniqueID` [property] | `string` | `"{ToStringID()}-{Timestamp}"` (identidad única) |
| `AgeDays` [property] | `int` | Días desde BirthDate |
| `GetDisplayName(db)` | `string` | "BodyShape Horn Back Wing" (nombres de partes) |
| `IsBusy` / `IsSold` [property] | `bool` | Derived: `BusyReason != None` / `== Sold` |

## Contrato de Red

**ToStringID()** es lo único serializado a servidor (sin Timestamp). Invariantes:
- Ningún token contiene `-` (separador)
- BaseColor derivado de hex final (RRGGBB)
- Timestamp genera UniqueID única (evita colisiones)

## Cambios S128

- **Eliminados:** `CombatCooldownUntil`, `HeldItemId` (ambos de demolición RPS S128)
- **Mantiene:** `{Horn/Back/Wing}Potential` como techo de evolución (HC-2)
- **Equipamiento:** `Equipped` se borra en HC-2; actualmente persiste

## Cambios Históricos

**S75:** Genetic string refactorizado (5 partes + color).
**S93:** Enums a archivos dedicados.
**S95:** Potenciales de combate agregados.
**S128:** Cooldown y HeldItem borrados (RPS demolido).

## Vinculado a

[[Index/01 - Creature Genetics & System]]
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureGenerator]], [[NeedsState]], [[PartDatabaseSO]], [[BreedingService]], [[CreatureDisplay]]

