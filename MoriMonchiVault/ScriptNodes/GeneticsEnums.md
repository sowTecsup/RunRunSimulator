---
tags: [enum, genetics, core]
---

# GeneticsEnums.cs

**Ruta:** `Core/Enums/GeneticsEnums.cs`

**Responsabilidad:** Enumeraciones para el sistema genético. Contiene: `Rarity` (5 niveles: Common/Uncommon/Rare/Epic/Legendary), `PartSet` (10 conjuntos estéticos de partes: GooGang/BogBrigade/FuzzFactory/CosmicCreeps/NeonNightmares/CrunchCrew/GrimGlobs/SpudSquad/MoldMob/ZapZone), `PartRole` (5 tipos de parte: Body/Horn/Back/Wing/Face), `FurType` (33 patrones: Pattern00-Pattern32), `Element` (4 elementos de combate: Agua/Fuego/Electricidad/Planta), `Role` (3 personalidades de combate: Protector/Agresivo/Empatico).

**S93:** Consolidación de enums genéticos en archivo dedicado.

## Enumeraciones

| Enum | Valores | Descripción |
|------|---------|-------------|
| `Rarity` | Common (0), Uncommon (1), Rare (2), Epic (3), Legendary (4) | Rareza de partes/criaturas |
| `PartSet` | 10 valores (1-10): GooGang, BogBrigade, ..., ZapZone | Temas visuales (colores, estilos) |
| `PartRole` | Body (0), Horn (1), Back (2), Wing (3), Face (4) | Tipos de partes en un MoriMochi |
| `FurType` | Pattern00-Pattern32 (33 valores) | Patrones de pelaje heredables |
| `Element` | Agua (0), Fuego (1), Electricidad (2), Planta (3) | Tipo elemental para combate |
| `Role` | Protector (0), Agresivo (1), Empatico (2) | Rol de combate (defensa/ataque/soporte) |

## Uso

- `Rarity` — tier de rareza de partes (en PartDatabaseSO, BodyPart)
- `PartSet` — agrupador visual (PartDatabaseSO.GetBySet, deprecated; ver PartDatabaseSO)
- `PartRole` — enlace entre ID de parte y anatomía (Body/Horn/Back/Wing/Face)
- `FurType` — determinista en CreatureDNA (FurType field)
- `Element` — atributo de criatura (CreatureDNA.Element), usable en combate futuro
- `Role` — atributo de criatura (CreatureDNA.Role), asignable al mintear o rerolleable

## Vinculado a

- [[Index/01 - Creature Genetics & System]]
- [[PartDatabaseSO]] — organiza partes por PartRole
- [[CreatureDNA]] — contiene Element, Role, FurType
- [[BodyPart]] — contiene Rarity

**Conexiones:** [[CreatureDNA]], [[PartDatabaseSO]], [[BodyPart]], [[ColorGenetics]]

