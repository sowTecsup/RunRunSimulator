---
tags: [script, data, combat, roles]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# RoleTableSO.cs

**Ruta:** `Data/Combat/RoleTableSO.cs`

**Responsabilidad:** Asset Odin `SerializedScriptableObject` que define perfiles de rol heredables (Protector/Agresivo/Empático) con modificadores de stats y listas polimórficas de efectos de rol. Mapeado 1:1 desde `CreatureDNA.Role` (enum). Consumido por `CombatService.TakeTurn()` y `CreatureGenerator` para aplicar transformaciones heredables en combate. **S40:** Refactor v2 — data-driven listas polimórficas `Passives`/`Actives`. **S46:** Agresivo ganó `MarkRandomAllyPassive` en Passives; BacklineHunterActive quedó puro targeting.

## Estructura

**RoleProfile:** Struct con 6 campos, poblados por rol.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `ConMod` | `float` | Modificador de CON (HP) heredable. Protector +4, Agresivo -3, Empático +1 |
| `AtkMod` | `float` | Modificador de ATK heredable. Protector -2, Agresivo +2, Empático -3 |
| `SpdMod` | `float` | Modificador de SPD heredable. Protector -2, Agresivo +1, Empático +2 |
| `PriceModifier` | `float` | Ajuste de precio en tienda (Agresivo -0.10, Empático +0.10, Protector 0) |
| `Passives` | `List<RolePassiveBase>` | **(S40)** Efectos pasivos polimórficos (OnAfterStrike, OnDamageDealt). **S46:** Agresivo tiene MarkRandomAllyPassive. |
| `Actives` | `List<RoleActiveBase>` | **(S40)** Efectos active polimórficos (targeting override). BacklineHunterActive solo targeting (S46). |

## Perfiles Implementados (PopulateV2 — S46)

### Protector (Tanque)
- `ConMod = 4f` — HP sustancialmente mayor
- `AtkMod = -2f` — ataque reducido
- `SpdMod = -2f` — más lento
- `PriceModifier = 0f` — precio base
- `Passives` — [ShieldAllyPassive(1.0)]
- `Actives` — []

### Agresivo (Pegador — S46 CAMBIÓ)
- `ConMod = -3f` — HP más bajo
- `AtkMod = 2f` — ataque sustancialmente mayor
- `SpdMod = 1f` — más rápido
- `PriceModifier = -0.10f` — 10% más barato
- `Passives` — **[MarkRandomAllyPassive()]** **(S46 NEW)** — marca aliado al azar cada turno
- `Actives` — [BacklineHunterActive(0.5)] — puro targeting (S46: sin Energy branches)

### Empático (Soporte)
- `ConMod = 1f` — HP ligeramente elevado
- `AtkMod = -3f` — ataque muy reducido
- `SpdMod = 2f` — muy rápido
- `PriceModifier = 0.10f` — 10% más caro
- `Passives` — [HealLowestAllyOnHitPassive(0.5)]
- `Actives` — []

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetProfile(role)` | `RoleProfile` | Busca en `Profiles` dict y retorna perfil (default vacío si falta) |
| `PopulateV2()` | `void` (Button) | Llena diccionario con los 3 perfiles; marca dirty (S46: Agresivo con MarkRandomAllyPassive) |

## Consumo en Combate (S46)

**En `CombatService.TakeTurn()` nuevo orden:**
1. **Targeting:** `CombatRoleHooks.ResolveTarget()` itera `profile.Actives` → BacklineHunterActive roll (pure targeting, sin Energy)
2. **Strike** — estándar
3. **GainAffinity** — +1, al llegar a 2 auto-marca
4. **Pasivas (post-strike):** `CombatRoleHooks.ApplyPassives()` itera `profile.Passives`:
   - Protector: escuda aliado + marca aliado
   - Agresivo: marca aliado al azar (MarkRandomAllyPassive)
   - Empático: cura aliado más débil (OnDamageDealt)
5. **Heal-on-damage:** `CombatRoleHooks.HealAfterStrike()` itera `profile.Passives`, cada una llama `OnDamageDealt()`

## Cambios S46

**Agresivo Passives (antes vacía, ahora MarkRandomAllyPassive):**
- Línea 56 en PopulateV2: `Passives = new List<RolePassiveBase> { new MarkRandomAllyPassive() }`
- Cambio semántico: **el Agresivo ahora marca un aliado cada turno** (pasiva), en lugar de solo hacer targeting de backline
- Sin gate de Energy (Energy fue eliminado)

**BacklineHunterActive simplificado (S46):**
- Línea 57: `Actives = new List<RoleActiveBase> { new BacklineHunterActive { Chance = 0.5f } }`
- Solo targeting puro, sin efectos de Energy gasto/comparte

## Cambios S40

**Antes (V1):** Campos individuales ShieldPerTurn, BacklineHitChance, HealPercentOfDamage

**Ahora (V2):** Listas polimórficas Passives/Actives

**Beneficios:**
- **Data-driven:** Nuevos roles sin código
- **Extensible:** Agregar pasivas sin tocar CombatService
- **Inspector-friendly:** Odin auto-soporta listas polimórficas
- **Composición:** Futuros roles pueden tener múltiples pasivas

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[CreatureDNA]] — `Role` enum field
- [[Role]] — enum (Protector=0, Agresivo=1, Empático=2)
- [[CombatService]] — `TakeTurn()` usa perfiles vía `CombatRoleHooks`
- [[CombatRoleHooks]] — invocador de efectos (S46: ApplyPassives post-strike)
- [[RolePassiveBase]] — base para Passives polimórficas
- [[RoleActiveBase]] — base para Actives polimórficas
- [[MarkRandomAllyPassive]] — nueva pasiva del Agresivo (S46)
- [[ShieldAllyPassive]] — pasiva del Protector
- [[HealLowestAllyOnHitPassive]] — pasiva del Empático
- [[BacklineHunterActive]] — active del Agresivo (targeting puro S46)
- [[CreatureGenerator]] — `MintRandomCreature()` aplica mods

## Conexiones

**Entrada:**
- `CombatManagerSO.Roles` — ref serializada al asset
- `CreatureGenerator.RoleProfiles` — ref serializada al asset

**Salida:**
- `RoleProfile` aplicado en:
  - Stats base (ConMod, AtkMod, SpdMod) → `CreatureDNA` at mint/breed
  - Efectos de rol (Passives, Actives) → `CombatService.TakeTurn()` cada round via `CombatRoleHooks` (S46: ApplyPassives post-strike)
  - Precio ajustado → `StoreManager`

## Notas (S40 + S46)

- **PopulateV2 idempotente:** El botón puede ejecutarse múltiples veces sin efecto negativo
- **S46 cambio semántico:** Agresivo pasó de "hunter de backline + gasta energía" a "hunter de backline (targeting) + marca aliado (pasiva)"
- **Determinismo:** Orden de Passives/Actives es fijo por lista; consumo RNG sincronizado
- **Rebalanceo futuro:** Los números pueden ajustarse via inspector sin recompilar
