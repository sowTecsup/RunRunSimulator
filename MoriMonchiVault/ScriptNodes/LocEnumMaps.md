---
tags: [script, localization, enum, utility]
---

# LocEnumMaps.cs

**Ruta:** `Systems/Localization/LocEnumMaps.cs`

**Responsabilidad:** Mapas centralizados enum → key de localización. Normaliza nombres a lowercase y prefija con dominio (ej. `Role.Protector` → `"role.protector"`). Único dueño de convenciones de key para enums. Métodos estáticos retornan string keys para lookup en archivos de localización.

**S93:** Eliminado `PartRoleName()`. Enums refactorizados a archivos dedicados.

## Métodos Públicos

| Método | Retorna | Ejemplo |
|--------|---------|---------|
| `RoleName(Role)` | string | `"role.protector"` |
| `ElementName(Element)` | string | `"element.fuego"` |
| `LifeStageName(LifeStage)` | string | `"stage.adult"` |
| `IntentName(CreatureIntent)` | string | `"intent.following"` |
| `GenderName(CreatureGender)` | string | `"gender.male"` |
| `StatAbbrev(StatType)` | string | `"stat.con"` |

## Cambios en S75

- **ELIMINADO:** `OutcomeName(CombatOutcome)` (demolición del combate)

## Cambios en S93

- **ELIMINADO:** `PartRoleName(PartRole)` (no usado; enums en archivos dedicados)

## Patrón de key

```
role.protector
element.fuego
stage.adult
intent.following
stat.con
```

## Vinculado a

- [[Index/05 - UI System]]

**Conexiones:** [[Loc]], [[NameTag]], [[DetailInfoTabPresenter]]

