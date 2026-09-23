---
tags: [data, genetics, serializable]
---

# CreatureDNA

**Ruta:** `Data/Genetics/CreatureDNA.cs`

**Responsabilidad:** Dato serializable que representa genética y estado de un MoriMochi. Contiene: partes genéticas (BodyShapeID/HornID/BackID/WingID/FaceID con tiers), colores (BaseColor/SecondaryColor), pelaje (FurType 33 patrones, IsShiny), identidad (CustomName, Timestamp, BirthDate, UniqueID), parentesco (MotherID/FatherID/ChildrenIDs), demografía (Gender), rol/elemento (Role, Element), personalidad (Sociability, Boldness), potenciales de partes, necesidades (Needs), estado (BusyReason, IsDead, BreedCount, SaleDate), reproducción (BreedReadyAt, BreedPartnerID), ubicación (LocationKey, LocationSlot), BirthDay (S131: día del juego de nacimiento).

## Campos Principales

| Campo | Tipo | Propósito |
|-------|------|----------|
| `BodyShapeID` / `HornID` / `BackID` / `WingID` / `FaceID` | string | IDs de partes (keys a PartDatabaseSO) |
| `BaseColor` / `SecondaryColor` | Color | Colores (derivados genéticamente) |
| `FurType` | FurType | Patrón de pelaje (Pattern00-32) |
| `IsShiny` | bool | Variante shiny (0.5% rareza) |
| `CustomName` | string | Nombre asignado jugador |
| `Timestamp` | long | Ticks UTC (identidad inmutable) |
| `BirthDate` | DateTime | Nacimiento real (DateTime.UtcNow al Stamp) |
| `BirthDay` | int | **(S131)** Día del juego de nacimiento (GameClock.Instance.Day) |
| `MotherID` / `FatherID` / `ChildrenIDs` | string / List | Genealogía |
| `Gender` | CreatureGender | Unknown/Male/Female |
| `Role` | Role | Protector/Agresivo/Empático |
| `Element` | Element | Agua/Fuego/Electricidad/Planta |
| `Sociability` / `Boldness` | float | Diales (0-1) |
| `BreedCount` | int | Cantidad reproducida |
| `Generation` | int | Generación (HC tracking) |
| `{Body/Horn/Back/Wing}Tier` | Tier | Rareza (Tier1/2/3) |
| `{Horn/Back/Wing}Potential` | int | Techo de nivel de parte (1-10, evolución HC) |
| `IsDead` | bool | Muerte permanente |
| `Needs` | NeedsState | Health/Energy/Affect |
| `BusyReason` | BusyReason | None/Breeding/Sold |
| `SaleDate` | DateTime | Cuándo vendida |
| `BreedReadyAt` / `BreedPartnerID` | long / string | Reproducción (reloj game, partner ID) |
| `LocationKey` / `LocationSlot` | string / int | Ubicación mundo |

## Métodos & Propiedades

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Stamp()` | `void` | Asigna Timestamp (UTC ticks) + BirthDate = DateTime.UtcNow |
| `ToStringID()` | `string` | `"BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB"` (genetic string) |
| `FromID(string id)` [static] | `CreatureDNA` | Parsea genetic string → new DNA |
| `UniqueID` [property] | `string` | `"{ToStringID()}-{Timestamp}"` (identidad única inmutable) |
| `AgeDays(int today)` | `int` | `Max(0, today - BirthDay)` días desde nacimiento (game days) |
| `GetDisplayName(db)` | `string` | "BodyShape Horn Back Wing" (nombres de partes) |
| `IsBusy` / `IsSold` [property] | `bool` | Derived: `BusyReason != None` / `== Sold` |

## Contrato de Red

**ToStringID()** es lo único serializado a servidor (sin Timestamp). Invariantes:
- Ningún token contiene `-` (separador)
- BaseColor derivado de hex final (RRGGBB)
- Timestamp genera UniqueID única (evita colisiones)

## S131 Cambios: BirthDay

**Nuevo campo:** `BirthDay: int` — día del juego de nacimiento (obtenido de `GameClock.Instance.Day`).

**Propósito:** 
- Seguimiento de edad en días de juego (no UTC)
- Usado por `CreatureLifeStageTableSO` para determinar etapa de vida (Newborn/Child/Teen/Adult/Elder)
- NameTag muestra edad: `AgeDays(GameClock.Instance.Day)` = `today - BirthDay`

**Asignación:**
```csharp
// En IncubationService.HatchLocally():
child.BirthDay = GameClock.Instance != null ? GameClock.Instance.Day : 1;

// En BreedingController.BreedCreatures():
child.BirthDay = GameClock.Instance != null ? GameClock.Instance.Day : 1;

// En GameManager.MintCreature():
dna.BirthDay = GameClock.Instance != null ? GameClock.Instance.Day : 1;
```

## Cambios S129

- **Eliminados:** Stats base (`BaseConstitution/Attack/Speed/Defense/Luck/Evasion`), campo `Equipped`
- **Agregado:** `int Generation` (tracking de generación, usado en `ReconcileColors()`)
- **Mantiene:** `{Horn/Back/Wing}Potential` como techo de evolución (HC-2)

## Cambios Históricos

**S75:** Genetic string refactorizado (5 partes + color).
**S93:** Enums a archivos dedicados.
**S95:** Potenciales de combate agregados.
**S128:** Cooldown y HeldItem borrados (RPS demolido).
**S129:** Stats y Equipped borrados (demolición HC-1).
**S131:** BirthDay agregado para seguimiento de edad en días de juego.

## Vinculado a

- [[Index/01 - Creature Genetics & System]]
- [[Index/28 - Cimientos y camino a Game Ready]]
- [[Index/09 - Active Context]]

## Conexiones

**Data:**
- [[CreatureRegistrySO]], [[CreatureGenerator]], [[NeedsState]], [[PartDatabaseSO]]

**Sistemas:**
- [[BreedingService]], [[CreatureDisplay]], [[CreatureLifecycle]], [[CreatureAvailability]]
- [[NameTag]] — muestra edad via AgeDays
- [[GameClock]] — proporciona día actual
- [[CreatureLifeStageTableSO]] — mapea edad → etapa

## Notas (S131 HC-4)

- **BirthDay vs BirthDate:** BirthDay es int (días de juego), BirthDate es DateTime (UTC real). BirthDay para gameplay, BirthDate para auditoría.
- **AgeDays:** Safe: `Max(0, today - BirthDay)` — nunca negativa.
- **Inicial:** Si no hay GameClock al minter, fallback a BirthDay=1.
- **Persistencia:** BirthDay se serializa en RegistryData, persiste con creature.
