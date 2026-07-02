---
tags: [core, enums, constants]
---

# Enums

Archivo central de enumeraciones del proyecto. Define tipos, categorías, estados y constantes como enums para type-safety y readability. Enums grandes se documentan con comentarios in-line; casos complejos van en las páginas temáticas del vault.

## Responsabilidad

Centralizar toda definición de enum, evitando duplicación y asegurando consistencia de valores. Fuente de verdad única para tipo, valor numérico y significado semántico.

## Enums Principales

### Rarity

Rareza de items y partes.

```
Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4
```

### PartSet

Conjuntos temáticos de partes (para sinergias futuras).

```
None, GooGang, BogBrigade, FuzzFactory, CosmicCreeps, NeonNightmares,
CrunchCrew, GrimGlobs, SpudSquad, MoldMob, ZapZone
```

### CreatureGender

Género de MoriMochi (metadata, no genético).

```
Unknown = 0, Male = 1, Female = 2
```

### LifeStage

Fase de vida derivada de edad en días (mapeo en `CreatureLifeStageTableSO`). Solo display.

```
Newborn = 0, Child = 1, Teen = 2, Adult = 3, Elder = 4
```

### FurType

Tipo de pelaje (hereda 50/50 en breeding). Mapea 1:1 a material de shader en `FurTypeDatabaseSO`.

```
Smooth = 0, Fluffy = 1, Spiky = 2, Shaggy = 3, Scaly = 4
```

### PartRole

Slot anatómico (Body, Arm, Eye, Mouth). Para generación temática de nombres.

```
Body = 0, Arm = 1, Eye = 2, Mouth = 3
```

### Tier

Nivel evolutivo (1-3).

```
Tier1 = 1, Tier2 = 2, Tier3 = 3
```

### BusyReason

Por qué una criatura no está disponible para acciones.

```
None = 0, QueuedForCombat = 1, Breeding = 2, Sold = 3
```

### UIPanelType

Canvas panels jugables.

```
None = 0, CreatureGrid = 1, MorimonchiDetail = 2, Breeding = 3,
Combat = 4, Storage = 5, Store = 6, Transaction = 7
```

### PlayerStateType

Qué está haciendo el jugador (input map, camera).

```
None = 0, Exploring = 1, Menu = 2, Building = 3
```

### FurnitureCategory

Categorías de muebles en tienda.

```
Decoration = 0, Display = 1, Functional = 2
```

### ItemType

Namespace de items (mueble vs objeto mundo).

```
Furniture = 0, WorldProp = 1
```

### WorldPropCategory

Objetos del mundo (herramientas, comida, medicina).

```
Tool = 0, Food = 1, Medicine = 2
```

### DiscountDay

Días aplicables a descuentos ([Flags]).

```
None, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday, All
```

### DiscountMonth

Meses aplicables a descuentos ([Flags]).

```
None, January–December, All
```

### RestockPeriod

Ventana dentro del mes para restock (días 1–10, 11–20, 21+).

```
EarlyMonth = 0, MidMonth = 1, EndOfMonth = 2
```

### BuyResult

Resultado de compra en tienda.

```
Success = 0, OutOfStock = 1, InsufficientFunds = 2, AlreadyOwned = 3
```

### StoreItemTypeFilter

Filtro de catálogo ([Flags] para combinar).

```
None = 0, Furniture = (1 << 0), WorldProp = (1 << 1)
```

### NeedType

Necesidades de criatura (mapeadas a NeedStations).

```
Health = 0, Energy = 1, Affect = 2
```

### CreatureCondition

Bienestar derivado (no almacenado, computed).

```
Healthy = 0, InNeed = 1, Sick = 2
```

### Personality

Arquetipo de comportamiento (asignado al azar en mint/hatch, metadata).

```
Skittish = 0, Aggressive = 1, Lazy = 2, Curious = 3, Social = 4, Grumpy = 5
```

### CreatureIntent

Verbo visible en NameTag (lo que ESTÁ HACIENDO ahora).

```
Idle, Wandering, Following, Approaching, Fleeing, Retreating,
SeekingFood, SeekingRest, SeekingPlay, Eating, Resting, Playing,
Held, Tumbling
```

### ProximityReaction

Reacción al jugador cercano.

```
Ignore = 0, Flee = 1, Approach = 2, Follow = 3, Retreat = 4
```

### WorldArea

Sectores del shop (mapean a NavMesh Areas).

```
ShopFrontDesk = 0, ShopBackroom = 1, Storage = 2
```

### CombatOutcome

Resultado de pelea (POV de criatura).

```
Won = 0, Lost = 1, Draw = 2
```

### MMAnimationType

Tipos de animación.

```
Idle = 0, Walk = 1, Attack = 2, Hit = 3, Death = 4, Victory = 5
```

### StatType

Stats tuneables (mirroring `CreatureDNA` base fields).

```
Constitution = 0, Attack = 1, Speed = 2, Defense = 3, Luck = 4, Evasion = 5
```

### ModifierType

Cómo aplican modificadores (Flat → PercentAdd → PercentMult).

```
Flat = 0, PercentAdd = 1, PercentMult = 2
```

### EquipmentSlot

Slots de equipo (máx 1 item por slot).

```
Weapon = 0, Armor = 1, Amulet = 2
```

### ModifierEffectKind

Tipos de efectos de procs en combate.

```
ReturnDamage = 0, Heal = 1, Poison = 2, Burn = 3, Stun = 4, Regen = 5, Synergy = 6
```

**ACTUALIZADO S32:** Agregado `Synergy = 6` para marcar daños/curación/status/stun provenientes de recetas de sinergias (no de procs de equipo). Se graba automáticamente en `CombatResolver.DamageBearer()`, `HealBearer()`, `AddStatusTo()`, `StunBearer()`.

### CombatPopupKind

**S31+S32** Tipos de popups flotantes (visualización de replay).

```
Hit, Crit, Poison, Burn, Thorns, Heal, Regen, Stun, Synergy
```

**Propósito:** Mapea cada tipo de evento visual a un color/label en `CombatPopupPaletteSO` y `CombatDamageNumbers`. Es el intermediario entre `ModifierEffectKind` (simulación) y la visualización UI.

**Valores:**
- `Hit` — golpe normal
- `Crit` — crítico
- `Poison`, `Burn`, `Thorns`, `Heal`, `Regen` — procs de status/curación
- `Stun` — aturdimiento (solo texto, sin número de daño)
- `Synergy` — **(NUEVO S32)** receta de sinergia disparada (solo texto, sin número)

**Consumido por:**
- `CombatPopupPaletteSO.colors` — diccionario tipo → color
- `CombatDamageNumbers.Label()` — genera texto descriptivo
- `CombatVisualizerService.RaiseProcPopup()` — convierte ModifierEffectKind → CombatPopupKind
- `CombatVisualizerService.ProcPopupKind()` — mapea Synergy

### TriggerType

Cuándo rollean los procs (Offensive, Defensive, Passive).

```
Offensive = 0, Defensive = 1, Passive = 2
```

## Vinculado a

Prácticamente todo el codebase. Los enums son la base de type-safety.

## Cambios Sesión 31

**NUEVO:** `CombatPopupKind` enum con 8 valores (Hit, Crit, Poison, Burn, Thorns, Heal, Regen, Stun). Es paralelo a `ModifierEffectKind` pero específico para visualización (UI popups), no simulación.

**Sin cambios:** `ModifierEffectKind` sigue siendo la fuente de verdad de procs en combate (solo tiene 6 valores: ReturnDamage, Heal, Poison, Burn, Stun, Regen).

## Cambios Sesión 32

**NUEVO:** `ModifierEffectKind.Synergy = 6` — marca efectos provenientes de recetas de sinergias. Grabados automáticamente por `CombatResolver.DamageBearer()`, `HealBearer()`, `AddStatusTo()`, `StunBearer()`.

**NUEVO:** `CombatPopupKind.Synergy` — mapeo visual para popups de sinergias disparadas (texto "¡Sinergia!", color violeta, sin número).

## Notas

- Enums son [System.Serializable] implícitamente en C#
- [Flags] enums para bitwise operations (DiscountDay, DiscountMonth, StoreItemTypeFilter)
- Valores numeric son explícitos para permitir serialización sin sorpresas
- Comúnmente extendidos: agregar enum nuevo siempre aquí primero antes de touchear lógica
