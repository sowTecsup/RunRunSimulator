---
tags: [script, data, combat, roles]
---

# RoleTableSO.cs

**Ruta:** `Data/Combat/RoleTableSO.cs`

**Responsabilidad:** Asset Odin `SerializedScriptableObject` que define perfiles de rol heredables (Protector/Agresivo/Empático) con modificadores de stats y listas polimórficas de efectos de rol. Mapeado 1:1 desde `CreatureDNA.Role` (enum). Consumido por `CombatService.TakeTurn()` y `CreatureGenerator` para aplicar transformaciones heredables en combate. **S40:** Refactor v2 — fuera campos individuales ShieldPerTurn/BacklineHitChance/HealPercentOfDamage; entran listas polimórficas `Passives`/`Actives` serializadas (Odin Inspector). Mismo gameplay, data-driven.

## Estructura

**RoleProfile:** Struct con 6 campos, poblados por rol.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `ConMod` | `float` | Modificador de CON (HP) heredable. Protector +4, Agresivo -3, Empático +1 |
| `AtkMod` | `float` | Modificador de ATK heredable. Protector -2, Agresivo +2, Empático -3 |
| `SpdMod` | `float` | Modificador de SPD heredable. Protector -2, Agresivo +1, Empático +2 |
| `PriceModifier` | `float` | Ajuste de precio en tienda (Agresivo -0.10, Empático +0.10, Protector 0) |
| `Passives` | `List<RolePassiveBase>` | **(S40)** Efectos pasivos polimórficos (OnTurnStart, OnDamageDealt). Ej: ShieldAllyPassive, HealLowestAllyOnHitPassive. |
| `Actives` | `List<RoleActiveBase>` | **(S40)** Efectos active polimórficos (targeting override). Ej: BacklineHunterActive. |

## Perfiles Implementados (PopulateV2)

### Protector (Tanque)
- `ConMod = 4f` — HP sustancialmente mayor
- `AtkMod = -2f` — ataque reducido (rol defensivo)
- `SpdMod = -2f` — más lento (sacrifica velocidad por durabilidad)
- `PriceModifier = 0f` — precio base (no modificado)
- `Passives` — [ShieldAllyPassive(1.0)]
- `Actives` — []

### Agresivo (Pegador)
- `ConMod = -3f` — HP más bajo (payoff de offensiva)
- `AtkMod = 2f` — ataque sustancialmente mayor
- `SpdMod = 1f` — más rápido (primer turno likely)
- `PriceModifier = -0.10f` — 10% más barato (menor rareza implícita)
- `Passives` — []
- `Actives` — [BacklineHunterActive(0.5)]

### Empático (Soporte)
- `ConMod = 1f` — HP ligeramente elevado (soporte moderado)
- `AtkMod = -3f` — ataque muy reducido (rol soporte, no ofensiva)
- `SpdMod = 2f` — muy rápido (actúa primero para curaciones defensivas)
- `PriceModifier = 0.10f` — 10% más caro (valor de soporte)
- `Passives` — [HealLowestAllyOnHitPassive(0.5)]
- `Actives` — []

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetProfile(role)` | `RoleProfile` | Busca en `Profiles` dict y retorna perfil (default vacío si falta) |
| `PopulateV2()` | `void` (Button) | Llena diccionario con los 3 perfiles estándar con listas polimórficas; marca dirty para editor |

## Consumo en Combate (S37 + S40)

**En `CombatService.TakeTurn()`:**
1. Actives: antes de elegir objetivo, `CombatRoleHooks.ResolveTarget()` itera `profile.Actives`; primer active que retorna non-null (ej: BacklineHunterActive roll) es el objetivo
2. Passives (pre-attack): `CombatRoleHooks.GrantShield()` itera `profile.Passives`, cada una llama `OnTurnStart()` (escudo, marca elemental, etc)
3. Passives (post-strike): `CombatRoleHooks.HealAfterStrike()` itera `profile.Passives`, cada una llama `OnDamageDealt()` si hit + damage > 0 (curación, marca elemental)

**En `CreatureGenerator.MintRandomCreature()`:**
- Asigna rol al azar 50/50 padres (o 1/3 al azar si no hay padres)
- Aplica modificadores de `RoleProfile` en `BaseConstitution`, `BaseAttack`, `BaseSpeed`

## Cambios S40

**Antes (V1):**
```csharp
public float ShieldPerTurn;          // Protector 1.0
public float BacklineHitChance;      // Agresivo 0.5
public float HealPercentOfDamage;    // Empático 0.5
```

**Ahora (V2):**
```csharp
public List<RolePassiveBase> Passives;   // polimórficas, serializadas
public List<RoleActiveBase> Actives;     // polimórficas, serializadas
```

**Beneficios:**
- **Data-driven:** Nuevos roles sin código
- **Extensible:** Agregar ShieldMultiplePassive, HealRangePassive, etc. sin tocar CombatService
- **Inspector-friendly:** Odin auto-soporta listas polimórficas
- **Composición:** Protector podría tener múltiples pasivas (future)

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[CreatureDNA]] — `Role` enum field
- [[Enums]] — `Role` enum (Protector=0, Agresivo=1, Empático=2)
- [[CombatService]] — `TakeTurn()` usa perfiles vía `CombatRoleHooks`
- [[CombatRoleHooks]] — invocador de efectos (S40)
- [[RolePassiveBase]] — base para Passives polimórficas (S40)
- [[RoleActiveBase]] — base para Actives polimórficas (S40)
- [[CreatureGenerator]] — `MintRandomCreature()` aplica mods
- [[CombatTargeting]] — PickBacklineTarget / LowestHpAlly
- [[CombatResolver]] — grabación de effectos
- [[CombatManagerSO]] — ref Roles

## Conexiones

**Entrada:**
- `CombatManagerSO.Roles` — ref serializada al asset
- `CreatureGenerator.RoleProfiles` — ref serializada al asset

**Salida:**
- `RoleProfile` aplicado en:
  - Stats base (ConMod, AtkMod, SpdMod) → `CreatureDNA` at mint/breed
  - Efectos de rol (Passives, Actives) → `CombatService.TakeTurn()` cada round via `CombatRoleHooks`
  - Precio ajustado → `StoreManager` (futuro)

## Notas (S37 + S40)

- **Perfiles mutables:** El botón PopulateV2 permite editar los valores en el asset sin código
- **Herencia 50/50:** En breeding, Role se asigna 50/50 de padres; si solo hay 1 padre, se hereda; si 0, al azar 1/3
- **Polimórfismo:** Passives/Actives son listas editables en Odin Inspector; ¡no hay switch/enum branch!
- **Rebalanceo futuro:** Los números (ConMod=4, ShieldAllyPassive.AmountPerTurn=1) son V2 y pueden ajustarse vía inspector sin recompilar
- **Impacto de precio:** PriceModifier afecta costo en tienda (Agresivo barato = menos rareza, Empático caro = más demanda)
- **Determinismo:** Orden de Passives/Actives es fijo por lista; consumo RNG sincronizado con V1 (verificado por paridad log)
