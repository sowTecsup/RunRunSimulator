---
tags: [enum, world, perception]
---

# WorldEnums.cs

**Ruta:** `Core/Enums/WorldEnums.cs`

**Responsabilidad:** Enumeraciones para topografía y percepciones del mundo. Contiene: `WorldArea` (3 zonas: ShopFrontDesk/ShopBackroom/Storage), `PerceivableKind` (5 tipos percibibles: Player/Monchi/Customer/Prop/Material), `ExpeditionTeam` (**S99 NUEVO:** None/Player/Rival — bandos en Arena sandbox), `ExpeditionTeams` (**S99 NUEVO:** static helper class con métodos `AreRivals()` y `AreAllies()`).

**S93:** Consolidación de enums de mundo en archivo dedicado. **S97:** Agregado `PerceivableKind.Material = 4`. **S99:** Agregados `ExpeditionTeam` enum y `ExpeditionTeams` static helper.

## Enumeraciones

| Enum | Valores | Descripción |
|------|---------|-------------|
| `WorldArea` | ShopFrontDesk (0), ShopBackroom (1), Storage (2) | Zona geográfica de la tienda |
| `PerceivableKind` | Player (0), Monchi (1), Customer (2), Prop (3), Material (4) | Tipo de agente/objeto en el mundo percibible por AgentSenses |
| `ExpeditionTeam` | **S99 NUEVO:** None (0), Player (1), Rival (2) | Bando del agente en Arena sandbox |

## PerceivableKind (completa lista)

```csharp
public enum PerceivableKind
{
    Player   = 0,
    Monchi   = 1,
    Customer = 2,
    Prop     = 3,
    Material = 4,  // S97 NUEVO: mineral recolectable
}
```

## ExpeditionTeam (S99 NUEVO)

```csharp
public enum ExpeditionTeam
{
    None   = 0,  // no asignado (neutral)
    Player = 1,  // equipo del jugador
    Rival  = 2,  // equipo rival/NPC
}
```

## ExpeditionTeams (S99 NUEVO)

```csharp
public static class ExpeditionTeams
{
    public static bool AreRivals(ExpeditionTeam a, ExpeditionTeam b)
        => a != ExpeditionTeam.None && b != ExpeditionTeam.None && a != b;

    public static bool AreAllies(ExpeditionTeam a, ExpeditionTeam b)
        => a != ExpeditionTeam.None && a == b;
}
```

## Cambios S99

**Nuevo enum ExpeditionTeam:**
- `None = 0` — sin bando (neutral, usado en tienda o cuando no hay expedición)
- `Player = 1` — criatura controlada por el jugador
- `Rival = 2` — criatura NPC/rival

**Nuevo static class ExpeditionTeams:**
- `AreRivals(a, b) → bool` — retorna true si ambos son no-None y diferentes
- `AreAllies(a, b) → bool` — retorna true si ambos son non-None e iguales

**Uso:**
- `MoriMochiAgent.ExpeditionTeam` se setea en spawn desde `ArenaRosterSO.Entry.Team`
- `AgentExpedition` verifica equipos para filtrar targets: solo rivales pueden "competir" por minerales
- `AgentSocial.AreTheyRivals()` puede usar `AreRivals(myTeam, otherTeam)` para decidir si interactuar o evitar
- `ArenaCueOverlay` colorea rutas y objetivos por bando

## Cambios S97 (histórico)

**Nuevo tipo de percepción:**
- `Material = 4` — mineral u objeto recolectable de expedición. `MaterialPickup` se registra en `Perceivable` con este Kind. Visto por `AgentSenses`, evaluado por `ExpeditionRuleBase.Matches()`.

## Uso

- `WorldArea` — locación: dónde está desplegado un agente o mueble. Usado para restricciones de navegación (muebles solo en ShopFrontDesk, storage en Storage, etc.)
- `PerceivableKind` — tag de percepción para `AgentSenses` (qué ve el agente: jugador, otros monchis, clientes, props, **S97:** materiales recolectables)
- `ExpeditionTeam` — **S99 NUEVO** bando del agente en Arena. Usado para relaciones rivales/aliados.

## Vinculado a

- [[Index/03 - World & Navigation]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S97-S99)
- [[AgentSenses]] — percibe PerceivableKind. **S97:** incluye Material
- [[PlacedFurniture]] — ubicada en WorldArea
- [[NpcAgent]], [[MoriMochiAgent]] — ubicados en WorldArea
- **S97:** [[MaterialPickup]], [[Perceivable]], [[AgentExpedition]], [[ExpeditionRuleBase]]
- **S99:** [[ArenaRosterSO]], [[AgentSocial]]

**Conexiones:** [[AgentSenses]], [[PlacedFurniture]], [[NpcAgent]], [[MoriMochiAgent]], [[MaterialPickup]], [[Perceivable]], [[AgentExpedition]], [[ArenaRosterSO]], [[AgentSocial]]
