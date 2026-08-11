---
tags: [combat, data, persistence, history]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatRecord

**Ruta:** `Data/Combat/CombatRecord.cs`

**Responsabilidad:** DTO serializable que persiste el historial completo y replayable de una pelea terminada. Vive en `CreatureDNA.CombatHistory` y se sincroniza via Cloud Save. Almacena turno-a-turno para que el visualizador local reproduzca el combate sin recomputar. **S37:** Campos aditivos para equipos (SelfTeam/OpponentTeam/SelfTeamIds/OpponentTeamIds); backward compatible con 1v1 legacy records. **S41:** Eventos elementales aditivos en `CombatUnitState` (marcas elementales, estados armados, afinidad). **S46:** Energy eliminado de CombatUnitState.

## Descripción General (S32 + S37 + S41 + S46)

Un único motor C# seeded: el servidor (JS) proporciona seed + snapshots DNA de todos los combatientes. Ambos clientes corren `CombatService.SimulateCore` con idéntica seed y snapshots → idéntico resultado → idéntico record. El visualizador es 100% local y determinista. **S37:** Expandido a 3v3 teams (1..3 por lado), order de rolls por EffSpeed, efectos de rol. **S41:** Estados elementales y marcas se graban en `CombatUnitState`. **S46:** Energy eliminado; solo Affinity.

## Cambios S33

**Nuevos campos:** `SelfStats` y `OpponentStats` (tipo `CombatFighterSnapshot`) capturan stats **post-equipment** de ambos luchadores en el momento del combate. Records viejos tienen estos campos `null` — backward compatible.

## Cambios S34

**CombatTurn:** Nuevos campos `StatusA` y `StatusB` (listas de `CombatStatusMark`) registran el estado de efectos activos. Records viejos deserializan listas vacías.

**CombatFighterSnapshot:** Nuevos campos (`BodyTier`, `ArmTier`, `EyeTier`, `MouthTier`, `ColorHex`, `Name`, `Role`, `Row`) capturan tiers, color base, nombre, rol y fila.

## Cambios S37

**NUEVOS campos aditivos:**
- `SelfTeam` | `List<CombatFighterSnapshot>` — snapshots de todos los units del equipo propio en 3v3 (null en 1v1 legacy)
- `OpponentTeam` | `List<CombatFighterSnapshot>` — snapshots de todos los units del equipo rival (null en 1v1 legacy)
- `SelfTeamIds` | `List<string>` — UniqueIDs de las criaturas del equipo propio (null en 1v1 legacy)
- `OpponentTeamIds` | `List<string>` — UniqueIDs de las criaturas del equipo rival

**Compatibilidad:** Records 3v3 nuevos tienen SelfTeam/OpponentTeam != null. Records 1v1 legacy tienen estos campos null; `SelfStats`/`OpponentStats` seguirán siendo el snapshot único.

**Determinismo por equipo:** Snapshots alineados con índices en `CombatTurn.AttackerIndex`/`DefenderIndex`.

## Cambios S41 (Paso 0)

**NUEVOS campos en CombatUnitState (aditivos, backward compatible):**
- `ElementMarks` | `List<CombatElementMark>` — marcas elementales aplicadas al unit
- `ArmedStates` | `List<ElementalState>` — estados elementales armados (single-use)
- `Affinity` | `int` — contador de afinidad actual (0-2, cada 2 dispara auto-marca)

**Creación:** En `CombatService.UnitState(Combatant c)` (nuevo helper S41), tras cada turno, se copia estado elemental a CombatUnitState:

```csharp
private static CombatUnitState UnitState(Combatant c)
{
    return new CombatUnitState
    {
        Hp = c.Hp,
        Shield = c.Shield,
        Marks = StatusMarks(c),
        ElementMarks = c.Marks.Select(m => new CombatElementMark { Element = m.Element, AllySource = m.AllySource }).ToList(),
        ArmedStates = c.States.ToList(),
        Affinity = c.Affinity,
    };
}
```

Luego en `EmitTurn()`, para ambos equipos se asignan TeamStateA/B.

**Backward compat:** Records viejos (S40 y antes) tienen ElementMarks/ArmedStates/Affinity = null/empty (deserialize default).

## Cambios S46

**CombatUnitState sin Energy:**
- Campo `Energy` eliminado de CombatUnitState
- Snapshot populate (línea 565 en CombatService) ya no copia Energy
- Records 3v3 (S46+) tienen `Energy` omitido del JSON (backward compat: deserialization NULL-tolera)
- Determinismo: mismo seed + snapshots sin Energy + mismo rows = resultado idéntico

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `OpponentName` | `string` | Nombre de la criatura rival (para 1v1 display) |
| `OpponentPlayerName` | `string` | Nombre del jugador rival (async only; "" para local) |
| `Date` | `DateTime` | Cuándo ocurrió (UTC) |
| `Outcome` | `CombatOutcome` | Resultado desde POV de THIS creature (Won/Lost/Draw) |
| `Died` | `bool` | Si esta criatura murió en la pelea |
| `EvolvedSlot` | `string` | Slot que evolucionó si ganó (null si perdió) |
| `SelfStats` | `CombatFighterSnapshot` | **S33** Stats post-equipment (1v1 legacy) o null (3v3) |
| `OpponentStats` | `CombatFighterSnapshot` | **S33** Stats post-equipment del rival (1v1 legacy) o null (3v3) |
| `Seed` | `int` | Seed usado para SimulateCore |
| `OpponentDnaId` | `string` | UniqueID del rival (1v1 legacy) |
| `OpponentPlayerId` | `string` | Player ID del rival (async only; "" para local) |
| `SelfWasA` | `bool` | Si true, esta criatura era combatante A (1v1) o está en SelfTeam (3v3) |
| `Turns` | `List<CombatTurn>` | Turnos de la pelea en orden |
| `SelfTeam` | `List<CombatFighterSnapshot>` | **S37** Snapshots de todo el equipo propio (null en 1v1) |
| `OpponentTeam` | `List<CombatFighterSnapshot>` | **S37** Snapshots de todo el equipo rival (null en 1v1) |
| `SelfTeamIds` | `List<string>` | **S37** UniqueIDs del equipo propio en orden (null en 1v1) |
| `OpponentTeamIds` | `List<string>` | **S37** UniqueIDs del equipo rival en orden (null en 1v1) |

## CombatFighterSnapshot — S33 + S34 + S37

**Estructura:** Captura los stats **finales** (post-equipment), tiers, identidad y rol de un luchador en el momento exacto que comenzó el combate.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `MaxHp` | `float` | HP máximo post-equipment |
| `Attack` | `float` | ATK final post-equipment |
| `Speed` | `float` | SPD final |
| `Defense` | `float` | DEF final post-equipment |
| `Luck` | `float` | LCK final |
| `Evasion` | `float` | EVA final |
| `BodyTier` | `int` | **S34** Tier del Body |
| `ArmTier` | `int` | **S34** Tier del Arm |
| `EyeTier` | `int` | **S34** Tier del Eye |
| `MouthTier` | `int` | **S34** Tier del Mouth |
| `ColorHex` | `string` | **S34** Color base en hex RGB (RRGGBB) |
| `Name` | `string` | **S37** Nombre de la criatura |
| `Role` | `Role` | **S37** Rol de combate |
| `Row` | `int` | **S37** Fila (0=Front, 1=Mid, 2=Back) |

## CombatTurn — S34 + S37 + S41 + S46

Estructura que representa un único turno dentro de un combate.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `TurnNumber` | `int` | Número de turno (1-indexed) |
| `AttackerName` | `string` | Nombre del atacante |
| `DefenderName` | `string` | Nombre del defensor |
| `AttackerIsA` | `bool` | **S37** Si true, atacante es del lado A |
| `Damage` | `float` | Daño infligido (0 si fue dodgeado) |
| `WasCrit` | `bool` | Si fue crítico |
| `DefenderHpAfter` | `float` | HP resultante del defensor tras golpe |
| `NoAttack` | `bool` | Si true, no hubo golpe (stun-skip, etc) |
| `Procs` | `List<CombatProcEvent>` | Eventos de proc en este turno (S41: con eventos elementales) |
| `StatusA` | `List<CombatStatusMark>` | **S34** Marcas de estado activo (1v1 only) |
| `StatusB` | `List<CombatStatusMark>` | **S34** Marcas de estado activo (1v1 only) |
| `AttackerIndex` | `int` | **S37** Índice del atacante (0..2) |
| `DefenderIndex` | `int` | **S37** Índice del defensor (0..2) |
| `DefenderShieldAfter` | `float` | **S37** Escudo resultante del defensor |
| `TeamStateA` | `List<CombatUnitState>` | **S37/S41** Estado de todos los units del team A (null en 1v1; S46: sin Energy) |
| `TeamStateB` | `List<CombatUnitState>` | **S37/S41** Estado de todos los units del team B (null en 1v1; S46: sin Energy) |

## CombatUnitState — S37 + S41 + S46

**Estructura:** Estado completo de UN unit al cierre de cada turno, para 3v3 replay.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Hp` | `float` | HP actual del unit |
| `Shield` | `float` | Escudo actual del unit (Protector role) |
| `Marks` | `List<CombatStatusMark>` | Efectos activos (Poison, Burn, etc.) |
| `ElementMarks` | `List<CombatElementMark>` | **S41** Marcas elementales actuales (Element + AllySource) |
| `ArmedStates` | `List<ElementalState>` | **S41** Estados elementales armados (single-use) |
| `Affinity` | `int` | **S46** Contador de afinidad (0-2; cada 2 dispara auto-marca) |

**CombatElementMark (DTO nuevo S41):**
```csharp
public class CombatElementMark
{
    public Element Element;       // elemento de la marca
    public bool AllySource;       // true = aliada, false = enemiga
}
```

**Backward Compatibility (S32 + S33 + S34 + S37 + S41 + S46)**

- **1v1 legacy (S32-S34):** SelfStats/OpponentStats != null, SelfTeam/OpponentTeam = null, StatusA/StatusB pobladas, TeamStateA/TeamStateB = null, Energy n/a
- **3v3 new (S37):** SelfStats/OpponentStats can be null, SelfTeam/OpponentTeam != null, StatusA/StatusB empty, TeamStateA/TeamStateB poblados
- **3v3 elemental (S41):** TeamStateA/TeamStateB incluyen ElementMarks, ArmedStates, Affinity
- **3v3 S46:** TeamStateA/TeamStateB sin Energy campo
- **Transición:** Ambos formatos pueden coexistir en CombatHistory; UI debe tolerar nulls

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/13 - Combat Design Direction]]
- [[CreatureDNA]] — `CombatHistory` es `List<CombatRecord>`
- [[CombatService]] — construye via `BuildRecord()` y popula en `SimulateCore()`, `EmitTurn()`
- [[AsyncCombatService]] — popula desde CloudMatchBlob
- [[CombatVisualizerService]] — lee records para replay local

## Conexiones

**Entrada:**
- `CombatService.SimulateCore()` → genera snapshots via `Snapshot(Combatant)` → copia a result.TeamA/B
- `CombatService.EmitTurn()` → puebla `TeamStateA/TeamStateB` con HP/Shield/Marks de cada unit (S41: + elementales via `UnitState()`)
- `CombatService.BuildRecord()` → copia snapshots + teamIds a record

**Salida:**
- Persistencia via `GameManager.SaveDatabase()` cuando `GameEvents.OnRegistryChanged`
- Visualizador 3v3: lee SelfTeam/OpponentTeam/TeamStateA/B para replay con marcas/reacciones/estados (sin Energy S46)
