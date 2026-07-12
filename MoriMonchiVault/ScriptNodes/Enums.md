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

### Element

Afinidad elemental innata de un MoriMochi (S39). Hereda 50/50 de los padres en breeding (aleatorio en mint) con chance de mutación. NO es parte del genetic string (metadata como Gender/Role). Conduce reacciones elementales en combate.

```
Agua = 0, Fuego = 1, Electricidad = 2, Planta = 3
```

**Descripción:**
- `Agua` — Afinidad acuática
- `Fuego` — Afinidad ígnea
- `Electricidad` — Afinidad eléctrica
- `Planta` — Afinidad botánica

**Uso en combate:** Las marcas elementales se aplican vía acciones de combate; dos elementos distintos de la misma fuente (aliada/enemiga) detonan reacciones vía `CombatElements.ReactionFor()`.

### ElementalState

Los 12 estados de reacción elemental únicos de un solo uso (S39). Estados positivos vienen de fuente aliada, negativos de fuente enemiga; todos se consumen una vez disparan su condición de trigger.

```
Energizado = 0, Cleanse = 1, Vaporizado = 2, GolpePreciso = 3,
Charcoal = 4, OverGrow = 5, Boiling = 6, Debilidad = 7,
Confuso = 8, Leech = 9, Mareado = 10, PisoTierra = 11
```

**Reacciones aliadas (fuente aliada):**
- `Energizado` — Fuego × Electricidad: ataque potenciado
- `Vaporizado` — Agua × Fuego: escape/reducción de daño
- `GolpePreciso` — Agua × Electricidad: crítico garantizado
- `Cleanse` — Agua × Planta: purga estado negativo O cura
- `Charcoal` — Fuego × Planta: bloqueo/armadura
- `OverGrow` — Electricidad × Planta: duplica escudo

**Reacciones enemigas (fuente enemiga):**
- `Boiling` — Agua × Fuego: daño periódico
- `Confuso` — Agua × Electricidad: decisiones aleatorias
- `Leech` — Agua × Planta: robo de HP al atacante
- `Mareado` — Fuego × Electricidad: reducción de precisión
- `Debilidad` — Fuego × Planta: daño amplificado
- `PisoTierra` — Electricidad × Planta: elimina marca elemental aleatoria

### Role

Rol de combate 3v3 heredable (50/50 padres en breeding, al azar en mint), NOT part of genetic string (metadata como Gender/Personality). Mapped 1:1 a RoleWorldProfileSO con modificadores de stats + efectos tácticos.

```
Protector = 0, Agresivo = 1, Empatico = 2
```

**Descripción:**
- `Protector` — Tanque: +CON, escudos al equipo (Protector), +DEF playstyle
- `Agresivo` — Pegador: +ATK/+SPD, caza la backline (rol ofensivo)
- `Empatico` — Soporte: +SPD, convierte daño en cura (soporte defensivo)

### CombatRow

Fila que ocupa un combatiente en grid 2-3-2 de combate 3v3.

```
Front = 0, Mid = 1, Back = 2
```

**Descripción:**
- `Front` — Primera línea (tanques, primera defensa)
- `Mid` — Segunda línea (soporte, posición media)
- `Back` — Tercera línea (atacantes, riesgo alto-recompensa)

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
ReturnDamage = 0, Heal = 1, Poison = 2, Burn = 3, Stun = 4, Regen = 5,
Synergy = 6, Static = 7, Pulse = 8, Steel = 9, Mist = 10, Lifesteal = 11, Shield = 12
```

**Descripción por tipo:**
- `ReturnDamage` — Espinas/thorns: daño al atacante
- `Heal` — Curación de equipo
- `Poison` — Daño periódico (veneno)
- `Burn` — Daño periódico (quemadura)
- `Stun` — Aturdimiento
- `Regen` — Curación periódica
- `Synergy` — Efectos de recetas de sinergias (S32)
- `Static` — Reduce SPD rival vía stacks (S35)
- `Pulse` — Cura por turno, estado emergente (S35)
- `Steel` — Suma DEF, estado emergente (S35)
- `Mist` — Suma EVA, estado emergente (S35)
- `Lifesteal` — % del daño vuelve como cura, estado emergente (S35)
- `Shield` — Escudo al equipo, rol Protector (S37)

**ACTUALIZADO S35:** 4 elementos nuevos (Static, Pulse, Steel, Mist) + Lifesteal como estado emergente. Se aplican como stacks vía equipment procs y se activan dinámicamente en Combatant properties (EffDefense, EffEvasion, EffSpeed, LifestealPercent).

**ACTUALIZADO S37:** Nuevo `Shield = 12` para efectos de rol Protector (escudo por turno a aliado).

### CombatPopupKind

Tipos de popups flotantes (visualización de replay).

```
Hit, Crit, Poison, Burn, Thorns, Heal, Regen, Stun, Synergy,
Static, Pulse, Steel, Mist, Lifesteal, Shield
```

**Propósito:** Mapea cada tipo de evento visual a un color/label en `CombatPopupPaletteSO` y `CombatDamageNumbers`. Es el intermediario entre `ModifierEffectKind` (simulación) y la visualización UI.

**Valores:**
- `Hit` — golpe normal
- `Crit` — crítico
- `Poison`, `Burn`, `Thorns`, `Heal`, `Regen` — procs de status/curación
- `Stun` — aturdimiento (solo texto, sin número de daño)
- `Synergy` — receta de sinergia disparada (solo texto, S32)
- `Static`, `Pulse`, `Steel`, `Mist`, `Lifesteal` — elementos nuevos (solo texto visual, S35)
- `Shield` — escudo aplicado (solo texto visual, S37)

**Consumido por:**
- `CombatPopupPaletteSO.colors` — diccionario tipo → color
- `CombatDamageNumbers.Label()` — genera texto descriptivo
- `CombatVisualizerService.RaiseProcPopup()` — convierte ModifierEffectKind → CombatPopupKind
- `MoriMonchiCombatVisualizerUITK.MapKind()` — mapea para chips de estado

## Vinculado a

Prácticamente todo el codebase. Los enums son la base de type-safety.

## Cambios Sesión 31

**NUEVO:** `CombatPopupKind` enum con 8 valores (Hit, Crit, Poison, Burn, Thorns, Heal, Regen, Stun). Es paralelo a `ModifierEffectKind` pero específico para visualización (UI popups), no simulación.

**Sin cambios:** `ModifierEffectKind` sigue siendo la fuente de verdad de procs en combate (solo tiene 6 valores: ReturnDamage, Heal, Poison, Burn, Stun, Regen).

## Cambios Sesión 32

**NUEVO:** `ModifierEffectKind.Synergy = 6` — marca efectos provenientes de recetas de sinergias. Grabados automáticamente por `CombatResolver.DamageBearer()`, `HealBearer()`, `AddStatusTo()`, `StunBearer()`.

**NUEVO:** `CombatPopupKind.Synergy` — mapeo visual para popups de sinergias disparadas (texto "¡Sinergia!", color violeta, sin número).

## Cambios Sesión 35

**NUEVOS en ModifierEffectKind:** 4 elementos + 1 estado emergente:
- `Static = 7` — reduce SPD rival (aplicado por ItemEquipped vía stack, se resta dinámicamente en Combatant.EffSpeed)
- `Pulse = 8` — cura por turno (estado emergente de receta Regeneración: PUL×3+STE×1)
- `Steel = 9` — suma DEF (estado emergente de receta Regeneración, sumado en Combatant.EffDefense)
- `Mist = 10` — suma EVA (estado emergente de receta Cortocircuito, sumado en Combatant.EffEvasion)
- `Lifesteal = 11` — % del daño a cura (estado emergente de receta Robo de vida: PUL×2+MIS×1, usado post-strike en CombatService)

**NUEVOS en CombatPopupKind:** mismo set + 5 entradas de visualización (Static, Pulse, Steel, Mist, Lifesteal).

**Impacto:** Los stacks de elementos no solo aplican daño/cura en turno, sino que actúan como modificadores dinámicos de stats durante la simulación. No hay rolls nuevos.

## Cambios Sesión 37

**NUEVOS enums:**
- `Role = Protector | Agresivo | Empatico` — rol heredable de combate 3v3, metadata NO genética
- `CombatRow = Front | Mid | Back` — filas del grid 2-3-2

**NUEVOS en ModifierEffectKind:**
- `Shield = 12` — escudo al equipo vía rol Protector (ShieldPerTurn)

**NUEVOS en CombatPopupKind:**
- `Shield` — visualización de escudo aplicado

**Impacto:** Cambio de 1v1 → 3v3 team-based. Los enums Role y CombatRow son centrales al nuevo modelo de combate.

## Cambios Sesión 39

**NUEVOS enums:**
- `Element = Agua | Fuego | Electricidad | Planta` — afinidad elemental innata (S39 core elemental system)
- `ElementalState = Energizado | Cleanse | Vaporizado | ... | PisoTierra` — 12 estados de reacción elemental de un solo uso

**Impacto:** Sistema de marcas elementales + reacciones 3v3. Cada acción de combate puede aplicar marca elemental via `CombatElements.AddMark()`; dos elementos distintos en la misma fuente detonan reacción que puede ser instantánea (Cleanse, OverGrow, Leech, PisoTierra) o armada (estado que se consume en trigger). Determinista: rolls vía CombatRng.

## Notas

- Enums son [System.Serializable] implícitamente en C#
- [Flags] enums para bitwise operations (DiscountDay, DiscountMonth, StoreItemTypeFilter)
- Valores numeric son explícitos para permitir serialización sin sorpresas
- Comúnmente extendidos: agregar enum nuevo siempre aquí primero antes de touchear lógica
