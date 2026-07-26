---
tags: [script, localization, enum, utility]
---

# LocEnumMaps.cs

**Ruta:** `Systems/Localization/LocEnumMaps.cs`

**Responsabilidad:** Mapas centralizados enum → key de localización. Cada enum tiene su propio método que normaliza el nombre a lowercase y prefija con dominio (e.g., `Role.Protector` → `"role.protector"`). Único dueño de las convenciones de key para enums del juego — cambiar una key de localización se hace aquí, no en cada consumidor.

**Métodos públicos (devuelven `string` localizado):**
- `RoleName(Role role)` → `"role." + KeyOf(role)` (Protector/Agresivo/Empático)
- `ElementName(Element element)` → `"element." + KeyOf(element)` (Fuego/Hielo/Electricidad/Planta)
- `EquipmentSlotName(EquipmentSlot slot)` → `"equipslot." + KeyOf(slot)` (Head/Chest/Legs/Feet/Hands)
- `PartRoleName(PartRole part)` → `"part." + KeyOf(part)` (Body/Arm/Eye/Mouth)
- `LifeStageName(LifeStage stage)` → `"stage." + KeyOf(stage)` (Newborn/Child/Teen/Adult/Elder)
- `IntentName(CreatureIntent intent)` → `"intent." + KeyOf(intent)` (Idle/Wandering/Following/Fleeing/Feeding/Socializing/SleepingTogether/Fighting)
- `GenderName(CreatureGender gender)` → `"gender." + KeyOf(gender)` (Male/Female)
- `OutcomeName(CombatOutcome outcome)` → `"outcome." + KeyOf(outcome)` (Won/Lost/Draw)
- `StatAbbrev(StatType stat)` → `"stat." + KeyOf(stat)` (CON/ATK/SPD/DEF/LCK/EVA)
- `RarityName(Rarity rarity)` → `"rarity." + KeyOf(rarity)` (Common/Uncommon/Rare/Epic/Legendary)
- `FurnitureCategoryName(FurnitureCategory category)` → `"furniturecategory." + KeyOf(category)` (Decoration/Functional/etc.)

**Privados:**
- `KeyOf(System.Enum value) → string` — convierte enum value a string lowercase (e.g., `Protector` → `"protector"`)

**Patrón de key:**
```
role.protector
element.fuego
stage.adult
part.body
intent.following
stat.con
rarity.rare
```

**Consumidores:**
- `NameTag` — RoleName, IntentName, LifeStageName, GenderName
- `DetailInfoTabPresenter` — RoleName, PartRoleName, GenderName
- `DetailCombatTabPresenter` — PartRoleName
- `CreatureLifeStageTableSO.Label()` — LifeStageName
- `NpcDialogueBank` — (indirectamente via Loc.Tr)
- 15+ presenters y componentes de UI

**Nota de cambio S68:**
- Antes: helpers privados en cada script (NameTag.IntentText, DetailInfoTabPresenter.RoleName, etc.)
- Ahora: centralizado aquí. Cada script llama `LocEnumMaps.EnumName(value)` en lugar de hardcodear mapping

**Vinculado a:**
- [[Index/05 - UI System]]
- [[Index/14 - Localization]]
- [[Loc]] (wrapper base sobre Loc.Tr)

**Conexiones:**
- `Loc.Tr()` (implementación subyacente)
- `NameTag`, `DetailInfoTabPresenter`, `DetailCombatTabPresenter`, `CreatureLifeStageTableSO`, y 15+ scripts de UI/gameplay
