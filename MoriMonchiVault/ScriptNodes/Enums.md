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

Patrón de pelaje del modelo Suriyun (S57). 33 patrones (Pattern00-32). Mapea 1:1 a MonchiFur_XX material. Hereda 50/50 en breeding. Metadata, no genética.

```
Pattern00 = 0, Pattern01 = 1, ... Pattern32 = 32
```

### MonchiMood

Humor/emoción visible (12 estados mapeados a caras). Determinados por Intent/Condition del agent.

```
Neutral = 0, Feliz = 1, Triste = 2, Dolor = 3, Enojado = 4, Dormido = 5,
Enfermo = 6, Mareado = 7, Asustado = 8, Amoroso = 9, Emocionado = 10, KO = 11
```

### PartRole

**S75 ACTUALIZADO:** Slot anatómico de partes genéticas (Body, Horn, Back, Wing, Face). Para generación temática de nombres.

```
Body = 0, Horn = 1, Back = 2, Wing = 3, Face = 4
```

**Cambio S75:** Reemplazó Arm/Eye/Mouth (valores 1, 2, 3) con Horn/Back/Wing (valores 1, 2, 3) + Face (valor 4). Refleja nuevo genetic string "BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB".

### Tier

Nivel evolutivo (1-3).

```
Tier1 = 1, Tier2 = 2, Tier3 = 3
```

### BusyReason

**S75 ACTUALIZADO:** Por qué una criatura no está disponible para acciones.

```
None = 0, Breeding = 2, Sold = 3
```

**Cambio S75:** Eliminado `QueuedForCombat = 1` (relacionado con demolición del combate async). Ahora solo `None`, `Breeding`, `Sold`.

### UIPanelType

Canvas panels jugables.

```
None = 0, CreatureGrid = 1, MorimonchiDetail = 2, Breeding = 3,
Storage = 5, Store = 6, Transaction = 7
```

**S75 ACTUALIZADO:** Eliminado `Combat = 4` (demolición del combate). Los valores se han reordenado numéricamente sin reasignar (se deja el hueco en 4 para backward-compatibility).

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

### ItemTriggerKind

**S75 NUEVO:** Cuándo se dispara un item consumible.

```
None = 0, LowHealth = 1, Collision = 2, Collected = 3
```

**Descripción:**
- `None` — Item sin comportamiento automático
- `LowHealth` — Se activa cuando portador bajo de HP
- `Collision` — Se activa al impactar contra algo
- `Collected` — Se activa al ser recogido/cosechado

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

### Element

Afinidad elemental innata de un MoriMochi (S39). Hereda 50/50 de los padres en breeding (aleatorio en mint) con chance de mutación. NO es parte del genetic string. Metadata como Gender/Role.

```
Agua = 0, Fuego = 1, Electricidad = 2, Planta = 3
```

### ElementalState

Los 12 estados de reacción elemental únicos de un solo uso (S39).

```
Energizado = 0, Cleanse = 1, Vaporizado = 2, GolpePreciso = 3,
Charcoal = 4, OverGrow = 5, Boiling = 6, Debilidad = 7,
Confuso = 8, Leech = 9, Mareado = 10, PisoTierra = 11
```

### Role

Rol de combate 3v3 heredable (50/50 padres en breeding, al azar en mint), NOT part of genetic string (metadata).

```
Protector = 0, Agresivo = 1, Empatico = 2
```

### CreatureIntent

Verbo visible en NameTag (lo que ESTÁ HACIENDO ahora).

```
Idle = 0, Wandering = 1, Following = 2, Approaching = 3, Fleeing = 4,
Retreating = 5, SeekingFood = 6, SeekingRest = 7, SeekingPlay = 8,
Eating = 9, Resting = 10, Playing = 11, Held = 12, Tumbling = 13,
Socializing = 14, Chasing = 15, SleepingTogether = 16, Fighting = 17
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

Tipos de efectos de procs en combate (legacy, actualmente sin uso en S75+).

```
ReturnDamage = 0, Heal = 1, Poison = 2, Burn = 3, Stun = 4, Regen = 5,
Synergy = 6, Static = 7, Pulse = 8, Steel = 9, Mist = 10, Lifesteal = 11, Shield = 12
```

### PerceivableKind

Clasifica qué tipo de entidad del mundo es perceptible por agentes MoriMochi (S64).

```
Player = 0, Monchi = 1, Customer = 2, Prop = 3
```

### EmoteKind

Pictogramas de emoción que emite un MoriMochi en burbuja world-space (S64).

```
Curioso = 0, Feliz = 1, Jugando = 2, Molesto = 3, Corazon = 4, Zzz = 5
```

### SocialInteractionKind

Clasifica tipos de interacción social para historial (S65).

```
PlayChase = 0, SleepTogether = 1, GremlinFight = 2
```

## Cambios en S75 (Demolición de combate)

- **PartRole:** Reemplazó Arm(1), Eye(2), Mouth(3) con Horn(1), Back(2), Wing(3), + Face(4). Refleja genetic string BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB.
- **BusyReason:** Eliminado QueuedForCombat (1). Ahora None(0), Breeding(2), Sold(3).
- **UIPanelType:** Eliminado Combat (4). Hueco dejado en valor 4 para backward-compatibility.
- **ItemTriggerKind:** NUEVO enum para triggers de items consumibles (None, LowHealth, Collision, Collected).
- **Enums de combate:** CombatRow, CombatOutcome, CombatPopupKind, ElementEventKind, y otros relacionados con combate siguen existiendo pero están fuera de uso. Se documentan en histórico.

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[Index/04 - World & AI]]

## Notas

- Enums son [System.Serializable] implícitamente en C#
- [Flags] enums para bitwise operations (DiscountDay, DiscountMonth, StoreItemTypeFilter)
- Valores numéricos son explícitos para permitir serialización
