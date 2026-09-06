---
tags: [enum, world, perception]
---

# WorldEnums.cs

**Ruta:** `Core/Enums/WorldEnums.cs`

**Responsabilidad:** Enumeraciones para topografía y percepciones del mundo. Contiene: `WorldArea` (zonas: ShopFrontDesk/ShopBackroom/Storage), `PerceivableKind` (6 tipos: Player/Monchi/Customer/Prop/Material/Exit), `ExpeditionTeam` (None/Player/Rival), `Occupation` (None/Gather/Guard/Break/Decoy/Explore). **S102 NUEVO:** `ArenaCastMode` (Roster/LocalSave), `ArenaSite` (Center/NearVein/FarVein), `ArenaPaletteSlot` (Ground/Grass/Foliage/Trunk/Rock/Wall).

## Enumeraciones

| Enum | Valores |
|------|---------|
| `WorldArea` | ShopFrontDesk, ShopBackroom, Storage |
| `PerceivableKind` | Player, Monchi, Customer, Prop, Material, Exit |
| `ExpeditionTeam` | None, Player, Rival |
| `Occupation` | None, Gather, Guard, Break, Decoy, Explore |
| **`ArenaCastMode` (S102)** | **Roster, LocalSave** |
| **`ArenaSite` (S102)** | **Center, NearVein, FarVein** |
| **`ArenaPaletteSlot` (S102)** | **Ground, Grass, Foliage, Trunk, Rock, Wall** |

## PerceivableKind S101 NUEVO: Exit

```csharp
public enum PerceivableKind
{
    Player   = 0,  // jugador
    Monchi   = 1,  // criatura rival/aliada
    Customer = 2,  // cliente NPC
    Prop     = 3,  // prop del mundo
    Material = 4,  // mineral recolectable (S97)
    Exit     = 5,  // salida de expedición (S101)
}
```

Exit (5) — percepción de salida de base en expedición. Representa un [[ExitZone]] (disco con radio y Team).

## Occupation S101 NUEVO

```csharp
public enum Occupation
{
    None    = 0,  // fallback a Gather
    Gather  = 1,  // recolectar material
    Guard   = 2,  // vigilar puesto
    Break   = 3,  // atacar rivales
    Decoy   = 4,  // distraer rivales
    Explore = 5,  // explorar (→ Gather)
}
```

Estrategias de expedición asignadas por ArenaRosterSO:
- **Gather:** Noticing → Moving → Mining → Returning → Securing. Acumula material.
- **Guard:** Guarding. Se planta en MaterialPickup (GuardPost inyectado).
- **Break:** Hunting. Persigue rival que recolecta; golpea si en rango.
- **Decoy:** Decoying (Approach → Taunt → Flee). Provoca rival, se retira. Cooldown 4s.
- **Explore:** Placeholder. Traduce a Gather en AgentExpedition.TryEngage().

## ArenaCastMode S102 NUEVO

```csharp
public enum ArenaCastMode
{
    Roster   = 0,  // elenco desde ArenaRosterSO (predefinido)
    LocalSave = 1, // elenco desde archivo creature_database*.json local
}
```

**Uso:**
- Elegido en ArenaPlanPanel → ToggleCastMode()
- Consultado en ArenaCastPlanner.Prepare()
- Impacta selección de criaturas y equipamiento (LocalSave usa DNA guardado; Roster clona stats de Entry)

**Almacenamiento:**
- ArenaSandbox.castMode (serializado)
- ArenaCastPlanner.Mode (estado mutable)

## ArenaSite S102 NUEVO

```csharp
public enum ArenaSite
{
    Center  = 0,  // centro de la sala
    NearVein = 1, // veta cercana (distancia media)
    FarVein = 2,  // veta lejana (distancia máxima)
}
```

**Significado:**
- Ubicación de recolecta asignada a cada criatura de Gather
- Usado por ArenaCastPlanner para distribuir objetivos (rivalPlans[i].site alternado)
- Consultado en ArenaSandbox.ResolveSite() para mapear a GuardPost (veta o salida)

**Determinismo:**
- Per-equipo: GatherSites[rivalIndex % 3] para rivales
- Ignorado si Occupation != Gather (Decoy no respeta Site, tiene lógica propia)

## ArenaPaletteSlot S102 NUEVO

```csharp
public enum ArenaPaletteSlot
{
    Ground  = 0,  // suelo principal
    Grass   = 1,  // pasto
    Foliage = 2,  // follaje/arbustos
    Trunk   = 3,  // tronco de árbol
    Rock    = 4,  // roca/piedra
    Wall    = 5,  // muro/pared
}
```

**Usado por:**
- ArenaPaletteSO.RampFor(slot) → devuelve Ramp (Dark/Mid/Light) para el slot
- ArenaPaletteApplier.TryClassify(material) → mapea nombre de material a slot
- ArenaPaletteApplier.BuildRamps() → compila ramp a Texture2D 256x1 por slot

**Clasificación de materiales (TryClassify):**
- "Trunk" → Trunk
- "Leaves"/"Tree"/"Plants" → Foliage
- "Moss"/"Rock"/"Pebble"/"PolygonNature_0" → Rock
- "Generic_0"/"Grass"/"Flower" → Grass
- "ArenaGround"/"ArenaOutskirts" → Ground
- "ArenaWall" → Wall
- (else) → Ground (fallback)

## ExpeditionTeam S99

```csharp
public enum ExpeditionTeam
{
    None   = 0,  // neutral
    Player = 1,  // equipo jugador
    Rival  = 2,  // equipo rival
}
```

**ExpeditionTeams (static helper):**
```csharp
public static bool AreRivals(ExpeditionTeam a, ExpeditionTeam b)
    => a != None && b != None && a != b;

public static bool AreAllies(ExpeditionTeam a, ExpeditionTeam b)
    => a != None && a == b;
```

**Usado por:**
- AgentClash.TryEngage() para validar rivales
- ArenaCueOverlay.DrawPercepts() para colorear percepciones
- ArenaCueOverlay.DrawExits() para teñir salidas por team
- AgentSenses.Tick() para filtrar Percepts

## Invariantes S102

- **ArenaCastMode:** determina fuente de DNA (predefinido vs guardado)
- **ArenaSite:** estrategia espacial de recolecta (distribución de objetivos)
- **ArenaPaletteSlot:** 1:1 con Ramp en ArenaPaletteSO (6 valores)
- **Determinismo:** mismos valores ArenaCastMode/Site/Slot para mismas semillas reproducen escena idénticamente

## Conexiones

- [[ArenaCastPlanner]] — ArenaCastMode, ArenaSite
- [[ArenaPaletteSO]] — ArenaPaletteSlot
- [[ArenaPaletteApplier]] — ArenaPaletteSlot para mapeo de materiales
- [[AgentExpedition]] — Occupation
- [[ExitZone]] — ExpeditionTeam
- [[ArenaCueOverlay]] — ExpeditionTeam para coloreado

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
