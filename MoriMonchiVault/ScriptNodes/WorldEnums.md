---
tags: [enum, world, perception]
---

# WorldEnums.cs

**Ruta:** `Core/Enums/WorldEnums.cs`

**Responsabilidad:** Enumeraciones para topografía y percepciones del mundo. Contiene: `WorldArea` (3 zonas: ShopFrontDesk/ShopBackroom/Storage), `PerceivableKind` (**S97 NUEVO:** 5 tipos percibibles: Player/Monchi/Customer/Prop/**Material**).

**S93:** Consolidación de enums de mundo en archivo dedicado. **S97:** Agregado `PerceivableKind.Material = 4`.

## Enumeraciones

| Enum | Valores | Descripción |
|------|---------|-------------|
| `WorldArea` | ShopFrontDesk (0), ShopBackroom (1), Storage (2) | Zona geográfica de la tienda |
| `PerceivableKind` | Player (0), Monchi (1), Customer (2), Prop (3), **Material (4) S97 NUEVO** | Tipo de agente/objeto en el mundo |

## PerceivableKind (completa lista)

```csharp
public enum PerceivableKind
{
    Player   = 0,
    Monchi   = 1,
    Customer = 2,
    Prop     = 3,
    Material = 4,  // S97 NUEVO
}
```

## Cambios S97

**Nuevo tipo de percepción:**
- `Material = 4` — mineral u objeto recolectable de expedición. `MaterialPickup` se registra en `Perceivable` con este Kind. Visto por `AgentSenses`, evaluado por `ExpeditionRuleBase.Matches()`.

**Uso:**
- `MaterialPickup` contiene `Perceivable` con `Kind = Material`
- `AgentExpedition.TryEngage()` itera percepciones y busca `p.Kind == Material`
- `ArenaCueOverlay.DrawMinerals()` dibuja objetos con este Kind
- `SeekMaterialRule` filtra por Material para scoring

## Uso

- `WorldArea` — locación: dónde está desplegado un agente o mueble. Usado para restricciones de navegación (muebles solo en ShopFrontDesk, storage en Storage, etc.)
- `PerceivableKind` — tag de percepción para `AgentSenses` (qué ve el agente: jugador, otros monchis, clientes, props, **S97:** materiales recolectables)

## Vinculado a

- [[Index/03 - World & Navigation]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S97)
- [[AgentSenses]] — percibe PerceivableKind. **S97:** incluye Material
- [[PlacedFurniture]] — ubicada en WorldArea
- [[NpcAgent]], [[MoriMochiAgent]] — ubicados en WorldArea
- **S97:** [[MaterialPickup]], [[Perceivable]], [[AgentExpedition]], [[ExpeditionRuleBase]]

**Conexiones:** [[AgentSenses]], [[PlacedFurniture]], [[NpcAgent]], [[MoriMochiAgent]], **S97:** [[MaterialPickup]], [[Perceivable]]
