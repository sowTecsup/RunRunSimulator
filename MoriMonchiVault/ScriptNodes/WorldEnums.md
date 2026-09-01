---
tags: [enum, world, perception]
---

# WorldEnums.cs

**Ruta:** `Core/Enums/WorldEnums.cs`

**Responsabilidad:** Enumeraciones para topografía y percepciones del mundo. Contiene: `WorldArea` (3 zonas: ShopFrontDesk/ShopBackroom/Storage), `PerceivableKind` (4 tipos percibibles: Player/Monchi/Customer/Prop).

**S93:** Consolidación de enums de mundo en archivo dedicado.

## Enumeraciones

| Enum | Valores | Descripción |
|------|---------|-------------|
| `WorldArea` | ShopFrontDesk (0), ShopBackroom (1), Storage (2) | Zona geográfica de la tienda |
| `PerceivableKind` | Player (0), Monchi (1), Customer (2), Prop (3) | Tipo de agente/objeto en el mundo |

## Uso

- `WorldArea` — locación: dónde está desplegado un agente o mueble. Usado para restricciones de navegación (muebles solo en ShopFrontDesk, storage en Storage, etc.)
- `PerceivableKind` — tag de percepción para `AgentSenses` (qué ve el agente: jugador, otros monchis, clientes, props)

## Vinculado a

- [[Index/03 - World & Navigation]]
- [[AgentSenses]] — percibe PerceivableKind
- [[PlacedFurniture]] — ubicada en WorldArea
- [[NpcAgent]], [[MoriMochiAgent]] — ubicados en WorldArea

**Conexiones:** [[AgentSenses]], [[PlacedFurniture]], [[NpcAgent]], [[MoriMochiAgent]]

