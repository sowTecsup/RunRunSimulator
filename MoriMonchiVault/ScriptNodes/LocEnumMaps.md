---
tags: [script, localization, enum, utility]
---

# LocEnumMaps.cs

**Ruta:** `Systems/Localization/LocEnumMaps.cs`

**Responsabilidad:** Mapas centralizados enum → key de localización. Normaliza nombres a lowercase y prefija con dominio (ej. `Role.Protector` → `"role.protector"`). Único dueño de convenciones de key para enums.

## Métodos públicos

| Método | Retorna | Ejemplo |
|--------|---------|---------|
| `RoleName(Role)` | string | `"role.protector"` |
| `ElementName(Element)` | string | `"element.fuego"` |
| `PartRoleName(PartRole)` | string | `"part.body"` — **S75:** Body/Horn/Back/Wing/Face |
| `LifeStageName(LifeStage)` | string | `"stage.adult"` |
| `IntentName(CreatureIntent)` | string | `"intent.following"` |
| `GenderName(CreatureGender)` | string | `"gender.male"` |
| `StatAbbrev(StatType)` | string | `"stat.con"` |

## Cambios en S75

- **ELIMINADO:** `OutcomeName(CombatOutcome)` (demolición del combate)
- **ACTUALIZADO:** `PartRoleName()` — ahora Body/Horn/Back/Wing/Face (en lugar de Body/Arm/Eye/Mouth)

## Patrón de key

```
role.protector
element.fuego
stage.adult
part.body
intent.following
stat.con
```

## Vinculado a

- [[Index/05 - UI System]]

**Conexiones:** [[Loc]], [[NameTag]], [[DetailInfoTabPresenter]]
