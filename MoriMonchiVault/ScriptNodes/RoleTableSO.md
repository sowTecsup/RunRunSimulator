---
tags: [script, data, combat, roles]
---

# RoleTableSO.cs

**Ruta:** `Data/Combat/RoleTableSO.cs`

**Responsabilidad:** Asset Odin `SerializedScriptableObject` que define perfiles de rol heredables (Protector/Agresivo/Empático) con modificadores de stats y efectos tácticos. Mapeado 1:1 desde `CreatureDNA.Role` (enum). Consumido por `CombatService.TakeTurn()` y `CreatureGenerator` para aplicar transformaciones heredables en combate (conversión de daño, shield, heal, targeting).

## Cambios S37

**Nuevo en S37:** Sistema de roles para combate 3v3. Reemplaza el sistema anterior de stats balanceados con arquetipos de rol heredables. Cada rol tiene modificadores de stat (ConMod, AtkMod, SpdMod) y efectos de rol (ShieldPerTurn, BacklineHitChance, HealPercentOfDamage).

## Estructura

**RoleProfile:** Struct con 7 campos, poblados por rol.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `ConMod` | `float` | Modificador de CON (HP) heredable. Protector +4, Agresivo -3, Empático +1 |
| `AtkMod` | `float` | Modificador de ATK heredable. Protector -2, Agresivo +2, Empático -3 |
| `SpdMod` | `float` | Modificador de SPD heredable. Protector -2, Agresivo +1, Empático +2 |
| `ShieldPerTurn` | `float` | Escudo que otorga cada turno a un aliado (Protector 1.0, otros 0) |
| `BacklineHitChance` | `float` | % chance de golpear backline en lugar de frontline (Agresivo 0.5, otros 0) |
| `HealPercentOfDamage` | `float` | % del daño convertido en cura al aliado más débil (Empático 0.5, otros 0) |
| `PriceModifier` | `float` | Ajuste de precio en tienda (Agresivo -0.10, Empático +0.10, Protector 0) |

## Perfiles Implementados (PopulateV1)

### Protector (Tanque)
- `ConMod = 4f` — HP sustancialmente mayor
- `AtkMod = -2f` — ataque reducido (rol defensivo)
- `SpdMod = -2f` — más lento (sacrifica velocidad por durabilidad)
- `ShieldPerTurn = 1f` — cada turno otorga 1 punto de escudo a un aliado vivo (elegido al azar)
- `BacklineHitChance = 0f` — siempre golpea frontline
- `HealPercentOfDamage = 0f` — sin curación
- `PriceModifier = 0f` — precio base (no modificado)

### Agresivo (Pegador)
- `ConMod = -3f` — HP más bajo (payoff de offensiva)
- `AtkMod = 2f` — ataque sustancialmente mayor
- `SpdMod = 1f` — más rápido (primer turno likely)
- `ShieldPerTurn = 0f` — sin escudos
- `BacklineHitChance = 0.5f` — 50% chance de ignorar frontline, golpea backline
- `HealPercentOfDamage = 0f` — sin curación
- `PriceModifier = -0.10f` — 10% más barato (menor rareza implícita)

### Empático (Soporte)
- `ConMod = 1f` — HP ligeramente elevado (soporte moderado)
- `AtkMod = -3f` — ataque muy reducido (rol soporte, no ofensiva)
- `SpdMod = 2f` — muy rápido (actúa primero para curaciones defensivas)
- `ShieldPerTurn = 0f` — sin escudos
- `BacklineHitChance = 0f` — siempre golpea frontline (poco daño, para soporte)
- `HealPercentOfDamage = 0.5f` — 50% del daño que hace se canaliza como cura al aliado con menor HP
- `PriceModifier = 0.10f` — 10% más caro (valor de soporte)

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetProfile(role)` | `RoleProfile` | Busca en `Profiles` dict y retorna perfil (default vacío si falta) |
| `PopulateV1()` | `void` (Button) | Llena diccionario con los 3 perfiles estándar; marca dirty para editor |

## Consumo en Combate (S37)

**En `CombatService.TakeTurn()`:**
1. Agresivo: antes de elegir objetivo, roll `BacklineHitChance`; si pasa, `CombatTargeting.PickBacklineTarget()` en lugar de frontline
2. Protector: cada turno, `CombatResolver.ShieldTarget()` aplica `ShieldPerTurn` a aliado elegido
3. Empático: post-strike, si golpea, `HealPercentOfDamage * damage` cura a `CombatTargeting.LowestHpAlly()`

**En `CreatureGenerator.MintRandomCreature()`:**
- Asigna rol al azar 50/50 padres (o 1/3 al azar si no hay padres)
- Aplica modificadores de `RoleProfile` en `BaseConstitution`, `BaseAttack`, `BaseSpeed`

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[CreatureDNA]] — `Role` enum field
- [[Enums]] — `Role` enum (Protector=0, Agresivo=1, Empático=2)
- [[CombatService]] — TakeTurn usa perfiles en efectos de rol
- [[CreatureGenerator]] — MintRandomCreature aplica mods
- [[CombatTargeting]] — PickBacklineTarget / LowestHpAlly
- [[CombatResolver]] — ShieldTarget

## Conexiones

**Entrada:**
- `CombatManagerSO.RoleProfiles` — ref serializada al asset
- `CreatureGenerator.RoleProfiles` — ref serializada al asset

**Salida:**
- `RoleProfile` aplicado en:
  - Stats base (ConMod, AtkMod, SpdMod) → `CreatureDNA` at mint/breed
  - Efectos de rol (escudo, backline hit, heal) → `CombatService.TakeTurn()` cada round
  - Precio ajustado → `StoreManager` (futuro)

## Notas (S37)

- **Perfiles mutables:** El botón PopulateV1 permite editar los valores en el asset sin código
- **Sin roll extra:** Los modificadores ya están en stats; BacklineHitChance es el único roll nuevo por rol
- **Herencia 50/50:** En breeding, Role se asigna 50/50 de padres; si solo hay 1 padre, se hereda; si 0, al azar 1/3
- **Rebalanceo futuro:** Los números (ConMod=4, ShieldPerTurn=1) son V1 y pueden ajustarse vía inspector sin recompilar
- **Impacto de precio:** PriceModifier afecta costo en tienda (Agresivo barato = menos rareza, Empático caro = más demanda)
