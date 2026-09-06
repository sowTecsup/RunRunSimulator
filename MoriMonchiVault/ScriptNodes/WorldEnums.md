---
tags: [enum, world, perception]
---

# WorldEnums.cs

**Ruta:** `Core/Enums/WorldEnums.cs`

**Responsabilidad:** Enumeraciones para topografía y percepciones del mundo. Contiene: `WorldArea` (3 zonas: ShopFrontDesk/ShopBackroom/Storage), `PerceivableKind` (6 tipos percibibles: Player/Monchi/Customer/Prop/Material/**S101:** Exit), `ExpeditionTeam` (None/Player/Rival), `ExpeditionTeams` (static helper), `Occupation` (**S101:** None/Gather/Guard/Break/Decoy/Explore — estrategias de expedición). **S93:** Consolidación de enums de mundo en archivo dedicado. **S97:** Agregado `PerceivableKind.Material = 4`. **S99:** Agregados `ExpeditionTeam` enum y `ExpeditionTeams` static helper. **S101:** Agregados `PerceivableKind.Exit = 5` y `Occupation` enum.

## Enumeraciones

| Enum | Valores |
|------|---------|
| `WorldArea` | ShopFrontDesk (0), ShopBackroom (1), Storage (2) |
| `PerceivableKind` | Player (0), Monchi (1), Customer (2), Prop (3), Material (4), **Exit (5) S101** |
| `ExpeditionTeam` | None (0), Player (1), Rival (2) |
| **`Occupation` (S101 NUEVO)** | **None (0), Gather (1), Guard (2), Break (3), Decoy (4), Explore (5)** |

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

**Exit (S101 NUEVO, valor 5):**
- Percepción de salida de base en expedición
- Representa un [[ExitZone]] (disco con radio y Team)
- Usado por: ArenaSandbox.SpawnCreature() marca cada salida como Perceivable
- Consultado por: ArenaCueOverlay.DrawExits() para visualización

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

**Estrategias de expedición asignadas por ArenaRosterSO:**

- **Gather = 1:** Noticing → Moving → Mining → Returning → Securing. Acumula material.
  - Intents: Collecting → Taking → Carrying → Securing
  - Habilidad: Ninguna especial (base)
  - Riesgo: Vulnerable mientras carga

- **Guard = 2:** Guarding. Se planta en MaterialPickup (GuardPost inyectado).
  - Intent: Guarding
  - Habilidad: Vigilancia y defensa del puesto
  - Riesgo: Inmóvil si rival llega

- **Break = 3:** Hunting. Persigue MoriMochiAgent rival que recolecta; golpea si en rango (AgentClash automático).
  - Intent: Hunting (si persiguiendo rival); Clashing (si entra en combate)
  - Habilidad: Combate preferente a rivales cargados; no puede iniciar Gather automático (TryEngage gateado)
  - Riesgo: Agresivo, puede perder

- **Decoy = 4:** Decoying (Approach → Taunt → Flee). Provoca rival, emota Molesto, se retira. Cooldown 4s.
  - Intent: Approaching → Taunting → Fleeing
  - Habilidad: Provocación (enlaza rivales a Taunting intent); evasivo
  - Riesgo: No acumula material (solo distrae)

- **Explore = 5:** Exploración (placeholder). Traduce a Gather en AgentExpedition.TryEngage().
  - Intent: Collecting → Taking → Carrying → Securing
  - Habilidad: Ninguna
  - Riesgo: Ninguno, fallback

**Asignación y mutabilidad:**
- Asignado por: `ArenaRosterSO.Entry.Occupation`
- Inyectado por: `ArenaSandbox.SpawnCreature()` → `controller.Agent.SetOccupation(occupation)`
- Almacenado en: `AgentContext.Occupation` (inmutable durante sesión)
- Consultado por: `AgentExpedition.TryEngage()` para elegir estrategia

**Mapeo en AgentExpedition:**
```csharp
var occ = ctx.Occupation;
if (occ == Occupation.None || occ == Occupation.Explore) occ = Occupation.Gather;

switch (occ)
{
    case Occupation.Guard: return TryGuardEngage(rules);
    case Occupation.Break: return TryBreakEngage(rules);
    case Occupation.Decoy: return TryDecoyEngage(rules);
    default: return TryGatherEngage(rules);  // Gather
}
```

## ExpeditionTeam S99 NUEVO

```csharp
public enum ExpeditionTeam
{
    None   = 0,  // neutral
    Player = 1,  // equipo jugador
    Rival  = 2,  // equipo rival
}
```

## ExpeditionTeams S99 NUEVO (static helper)

```csharp
public static class ExpeditionTeams
{
    public static bool AreRivals(ExpeditionTeam a, ExpeditionTeam b)
        => a != ExpeditionTeam.None && b != ExpeditionTeam.None && a != b;

    public static bool AreAllies(ExpeditionTeam a, ExpeditionTeam b)
        => a != ExpeditionTeam.None && a == b;
}
```

**Lógica:**
- Rivales: ambos son None y diferentes (Player ≠ Rival)
- Aliados: ambos son None y iguales (Player == Player o Rival == Rival)
- Neutral: al menos uno es None

**Usado por:**
- `AgentClash.TryEngage()` para validar rivales en combate
- `ArenaCueOverlay.DrawPercepts()` para colorear percepciones por team
- `ArenaCueOverlay.DrawExits()` para teñir salidas por team

## Cambios S101: Adiciones

**PerceivableKind.Exit (línea 17):**
```csharp
public enum PerceivableKind
{
    Player   = 0,
    Monchi   = 1,
    Customer = 2,
    Prop     = 3,
    Material = 4,
    Exit     = 5,  // S101 NUEVO
}
```

**Occupation enum (líneas 27-35):**
```csharp
public enum Occupation
{
    None    = 0,
    Gather  = 1,
    Guard   = 2,
    Break   = 3,
    Decoy   = 4,
    Explore = 5,
}
```

## Uso S101 + S99

- `WorldArea` — locación: restricciones de navegación
- `PerceivableKind` — tag de percepción: qué ve el agente; **S101:** Exit marca salidas
- `ExpeditionTeam` — bando del agente en Arena; relaciones rivales/aliados
- `Occupation` — estrategia de expedición; cada uno genera intents propios (Collecting, Guarding, Hunting, Taunting, Securing)

## Invariantes S101

- **Ocupación centralizada:** ambos `AgentContext` y `AgentExpedition` consultan `ctx.Occupation` (single source of truth)
- **PerceivableKind.Exit global:** todas las salidas son Perceivable, no parte de agent-specific Percepts (escala 200m via PerceivableRegistry query)
- **None → Gather:** fallback seguro; Explore también traduce a Gather
- **Immutable session:** Occupation nunca cambia post-spawn (no hay re-assignment durante expedición)
- **Team-aware coloring:** ArenaCueOverlay usa ExpeditionTeams.AreRivals/AreAllies para teñir visuales por bando

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]] (S101: Ocupaciones con tiempo)

## Conexiones

[[AgentSenses]], [[AgentContext]], [[AgentExpedition]], [[AgentClash]], [[ArenaRosterSO]], [[ArenaSandbox]], [[ExitZone]], [[Perceivable]], [[CreatureEnums]], [[ExpeditionTeam]], [[ExpeditionTeams]], [[ArenaCueOverlay]]
