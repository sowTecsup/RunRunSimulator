---
tags: [enum, world, arena, expedition]
---

# WorldEnums.cs

**Ruta:** `Core/Enums/WorldEnums.cs`

**Responsabilidad:** Enumeraciones para topografía, percepciones, expedición y arena. Contiene tipos base (WorldArea, PerceivableKind, ExpeditionTeam), ocupaciones (Occupation), arena (ArenaCastMode, ArenaSite, ArenaPaletteSlot), y **órdenes arena (S104 NUEVO: LootChoice, ContactChoice, PostureChoice, OrderPillar)**.

## Enumeraciones Base

| Enum | Valores |
|------|---------|
| `WorldArea` | ShopFrontDesk, ShopBackroom, Storage |
| `PerceivableKind` | Player, Monchi, Customer, Prop, Material, Exit |
| `ExpeditionTeam` | None, Player, Rival |

## Occupación (S101)

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
- **Gather:** Noticing → Moving → Mining → Returning → Securing. Acumula material. Contact=Flee u ordenado Proteger.
- **Guard:** Guarding. Se planta en MaterialPickup, persigue provocadores.
- **Break:** Hunting. Persigue recolectores desprotegidos; se retira si golpeado.
- **Decoy:** Decoying (Approach → Taunt → Flee). Provoca rivales. Cooldown 4s.
- **Explore:** Traveling → Reporting. Navega vetas y reporta al pizarrón. Fallback a Gather.

## Órdenes de Arena (S104 NUEVO)

```csharp
public enum LootChoice
{
    Big   = 0,  // recolectar cristal central grande
    Small = 1,  // recolectar vetas chicas
}

public enum ContactChoice
{
    Flee  = 0,  // evitar confrontación
    Fight = 1,  // buscar y enfrentar
}

public enum PostureChoice
{
    Protect    = 0,  // grupo cohesivo (custodio protege recolector, huye con aliados)
    Aggressive = 1,  // individual (sin deberes, va a lo suyo)
}

public enum OrderPillar
{
    Loot    = 0,  // eje botín
    Contact = 1,  // eje contacto
    Posture = 2,  // eje equipo
}
```

**Combinaciones → Arquetipos:**
- Contact=Fight + Posture=Protect → Guard (planta, bloquea)
- Contact=Fight + Posture=Aggressive → Break (caza, vacía recolectores)
- Contact=Flee + Posture=Protect → Gather (mina, huye con aliados)
- Contact=Flee + Posture=Aggressive → Decoy (provoca, se va solo)

**Bloqueos por Personalidad:**
- Boldness >= BoldFightLock (0.65) → fuerza Contact=Fight
- Boldness <= ShyFleeLock (0.35) → fuerza Contact=Flee
- Sociability >= SocialProtectLock (0.65) → fuerza Posture=Protect
- Sociability <= LonerAggressiveLock (0.35) → fuerza Posture=Aggressive

**Lectura de Rival:**
- Si dos pilares bloqueados → "Guardián seguro" (ej)
- Si uno → "puede ser guardián o cazador"
- Sino → "puede hacer cualquiera"

## Arena (S102)

```csharp
public enum ArenaCastMode
{
    Roster   = 0,  // elenco desde ArenaRosterSO (predefinido)
    LocalSave = 1, // elenco desde archivo creature_database*.json local
}

public enum ArenaSite
{
    Center   = 0,  // centro de la sala
    NearVein = 1,  // veta cercana (distancia media)
    FarVein  = 2,  // veta lejana (distancia máxima)
}

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

**ArenaCastMode:** determina fuente de DNA (predefinido vs guardado)
**ArenaSite:** estrategia espacial de recolecta (distribución de objetivos)
**ArenaPaletteSlot:** 1:1 con Ramp en ArenaPaletteSO (6 valores)

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

**Usado por:**
- AgentClash.TryEngage() para validar rivales
- ExpeditionNav.FindPrey/FindDecoyTarget para filtrar por team
- ArenaCueOverlay.DrawPercepts() para colorear percepciones

## Invariantes S104

- **Órdenes:** encapsulan estrategia en 3 pilares; clampeadas por DNA si personalidad extrema
- **Ocupación derivada:** ArenaOrderRules.ToOccupation(orders) es autoridad única
- **Bloqueos:** no revocables (DNA > orden del jugador)
- **Determinismo:** mismas órdenes + DNA = mismo comportamiento en expedición

## Vinculado a

[[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

## Conexiones

- [[ArenaOrders]], [[ArenaOrderRules]], [[ArenaOrderCatalog]] — órdenes
- [[AgentExpedition]], [[AgentContext]], [[MoriMochiAgent]] — derivación y consulta
- [[ArenaCastPlanner]] — ArenaCastMode, ArenaSite
- [[ArenaPaletteSO]] — ArenaPaletteSlot
- [[ExitZone]] — ExpeditionTeam
- [[ArenaCueOverlay]] — ExpeditionTeam para coloreado
