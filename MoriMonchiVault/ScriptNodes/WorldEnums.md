---
tags: [enum, world, arena, expedition]
---

# WorldEnums.cs

**Ruta:** `Core/Enums/WorldEnums.cs`

**Responsabilidad:** Enumeraciones para topografía, percepciones, expedición y arena. Contiene tipos base (WorldArea, PerceivableKind, ExpeditionTeam), ocupaciones (Occupation), arena (ArenaCastMode, ArenaSite, ArenaPaletteSlot), órdenes arena (S104: LootChoice, ContactChoice, PostureChoice, OrderPillar), **S111:** ArenaRegionKind, **S122:** ArenaBase (estrategias de competencia por rol), **S124:** ArenaFloorKind (tipos de piso en bajada).

## Enumeraciones Base

| Enum | Valores |
|------|---------|
| `WorldArea` | ShopFrontDesk, ShopBackroom, Storage |
| `PerceivableKind` | Player, Monchi, Customer, Prop, Material, Exit |
| `ExpeditionTeam` | None, Player, Rival |

## Ocupación (S101)

```csharp
public enum Occupation
{
    None    = 0,  // fallback a Gather
    Gather  = 1,  // recolectar material
    Guard   = 2,  // vigilar puesto, perseguir provocadores
    Break   = 3,  // atacar recolectores desprotegidos
    Decoy   = 4,  // distraer rivales
    Explore = 5,  // explorar y reportar vetas
}
```

Estrategias de expedición — derivadas de ArenaOrders por ArenaOrderRules.ToOccupation() (S104):
- **Gather:** Noticing → Moving → Mining → Returning. Acumula material. Contact=Flee u ordenado Proteger.
- **Guard:** Guarding. Se planta en MaterialPickup, persigue provocadores.
- **Break:** Hunting. Persigue recolectores desprotegidos; se retira si golpeado.
- **Decoy:** Decoying (Approach → Taunt → Flee). Provoca rivales. Cooldown 4s.
- **Explore:** Traveling → Reporting. Navega vetas y reporta. Fallback a Gather.

## Órdenes de Arena (S104)

```csharp
public enum LootChoice { Big = 0, Small = 1 }
public enum ContactChoice { Flee = 0, Fight = 1 }
public enum PostureChoice { Protect = 0, Aggressive = 1 }

public enum OrderPillar { Loot = 0, Contact = 1, Posture = 2 }
```

**Combinaciones → Arquetipos:**
- Contact=Fight + Posture=Protect → Guard
- Contact=Fight + Posture=Aggressive → Break
- Contact=Flee + Posture=Protect → Gather
- Contact=Flee + Posture=Aggressive → Decoy

**Bloqueos por Personalidad:** Boldness/Sociability extrema fuerza Contact/Posture.

## Bases de Arena (S122)

```csharp
public enum ArenaBase
{
    Territory    = 0,  // dominar el centro
    Forage       = 1,  // explotar veta cercana
    Opportunism  = 2,  // aprovechar lo que cae
}
```

Cada `Role` (Protector/Agresivo/Empático) abre 2 de 3 bases; la variante determina órdenes concretas. Mapeo en [[ArenaBases]] (estático): rol + base → órdenes, rol + órdenes → base (inverso).

## Tipos de Piso — Bajada por Pisos (S124)

```csharp
public enum ArenaFloorKind
{
    Enemies = 0,  // combate contra rival · vida es riesgo: -15 por golpe
    Buff    = 1,  // piso de recuperación · +30 vida a todo el equipo + material gratis
}
```

**Determinismo de pisos:** Piso n es Buff si `(n > 0) && (n % 3 == 0)` — cada 3 pisos. Calculado por `ArenaRun.KindOf()`. Semilla por piso = XOR determinista sobre BaseSeed + Floor.

**Ciclo de vida:**
1. Entrar piso: `run.EnterFloor()` — incrementa Floor, auto-cura si Buff
2. Jugar combate si Enemies: ArenaRound registra golpes
3. Fin combate: `run.RecordFloor(winner, secured, stats)` — aplica daño, acumula material, detecta derrota si Rival gana Enemies
4. Panel de decisión: Continuar/Retirarse

**Vida es riesgo:** No se restaura entre pisos excepto Buff (+30). Máximo 100. Cae 15 por golpe recibido. Si cae a 0, criatura "caída" (but no permanente); aviso al continuar.

## Arena (S102+S111+S122)

```csharp
public enum ArenaCastMode { Roster = 0, LocalSave = 1 }
public enum ArenaSite { Center = 0, NearVein = 1, FarVein = 2 }
public enum ArenaPaletteSlot { Ground = 0, Grass = 1, Foliage = 2, Trunk = 3, Rock = 4, Wall = 5, Water = 6 }
public enum ArenaRegionKind { Rock = 0, Lake = 1, Pit = 2, Grove = 3 }
```

**ArenaCastMode:** Roster (predefinido) vs LocalSave (archivo guardado, S119: elenco del jugador).
**ArenaSite:** Estrategia de recolecta espacial.
**ArenaPaletteSlot:** Slots de rampa de paleta (Water = 6 desde S111).
**ArenaRegionKind (S111):** Regiones procedurales.

## Helpers

```csharp
public static class ExpeditionTeams
{
    public static bool AreRivals(ExpeditionTeam a, ExpeditionTeam b)
        => a != ExpeditionTeam.None && b != ExpeditionTeam.None && a != b;
    public static bool AreAllies(ExpeditionTeam a, ExpeditionTeam b)
        => a != ExpeditionTeam.None && a == b;
}
```

## Invariantes S104+S111+S122+S124

- Órdenes: 3 pilares; clampeadas por DNA si personalidad extrema (S104)
- Ocupación derivada: autoridad única de ArenaOrderRules.ToOccupation()
- Bloqueos: no revocables (DNA > orden)
- Determinismo: mismas órdenes + DNA = mismo comportamiento
- Bases (S122): rol → 2 de 3 bases; variante → órdenes; ArenaBases mapea todo
- Pisos (S124): cada 3 son Buff; vida es el riesgo irreversible entre combates

## Vinculado a

[[Index/22 - Bajada Nocturna y Linaje]] (S122), [[Index/23 - Arena Sandbox & Expedicion]] (S102-S103), [[Index/24 - Puente Tienda-Arena]] (S122), [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

## Conexiones

- [[ArenaOrders]], [[ArenaOrderRules]], [[ArenaBases]] (S122) — órdenes y bases
- [[AgentExpedition]], [[AgentContext]], [[MoriMochiAgent]] — derivación
- [[ArenaCastPlanner]], [[ArenaPlanPanel]] — ArenaCastMode, ArenaSite, ArenaBase
- [[ArenaPaletteSO]], [[ArenaPaletteApplier]] — ArenaPaletteSlot (S111: Water)
- [[ExitZone]], [[ArenaCueOverlay]] — ExpeditionTeam
- [[ArenaShape]], [[ArenaShapeBrush]] — ArenaRegionKind (S111)
- [[ArenaRun]], [[ArenaRunDirector]] — ArenaFloorKind, pisos (S124)
