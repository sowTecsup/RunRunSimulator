---
tags: [script, data, scriptableobject, expedition]
---

# AbilitySO.cs

**Ruta:** `Data/Expedition/AbilitySO.cs`

**Responsabilidad:** ScriptableObject que define una habilidad individual (Damage o Mobility) de un MoriMochi. Mapea intención → ClashMove o buff de velocidad, con cooldown y disparadores (RivalInReach, Fleeing, Chasing). Uno por parte de cuerpo (Horn/Wings/Back) en AbilityDatabaseSO.

**Enums:**
- `AbilityKind { Damage = 0, Mobility = 1 }` — tipo de habilidad
- `AbilityTrigger [Flags] { None = 0, RivalInReach = 1, Fleeing = 2, Chasing = 4 }` — triggers para disparo automático

**Campos Serializados:**

**General:**
- `Name` (string) — nombre mostrado en HUD (ej: "Embestida")
- `Description` (TextArea) — tooltip o descripción larga
- `Slot` (ClashSlot: Horn/Wings/Back) — mapeo de parte corporal
- `Kind` (AbilityKind) — Damage o Mobility
- `Cooldown` (float, Min 0) — segundos entre disparos
- `Trigger` (AbilityTrigger, EnumToggleButtons) — flags de disparadores
- `Color` (Color) — color renderizado en HUD y guías (RGB 1, 0.6, 0.2 por defecto)

**Daño (Section):**
- `Move` (ClashMoveSO) — movimiento de choque asociado (null si no es Damage)
- `MinDistance` (float, Min 0) — distancia mínima al rival (requerida)
- `MinRivalsNearby` (int, Min 0) — mínimo de rivales en 5m (requerida) — prioridad alta

**Movilidad (Section):**
- `SpeedMultiplier` (float, Min 1) — factor de velocidad (1.35 defecto)
- `BoostSeconds` (float, Min 0) — duración del buff

**Métodos Públicos:**
- `bool Triggers(AbilityTrigger t) → bool` — chequea si flag t está seteado: `(Trigger & t) != 0`

**Uso:**
- **Damage:** Disparada cuando rival en rango AND cooldown listo AND RivalInReach en Trigger. Retorna ClashMoveSO para agente.
- **Mobility:** Disparada automáticamente si Trigger Fleeing/Chasing activo AND cooldown listo. Aplica buff de velocidad por BoostSeconds.

**Integración:**
- Instancias creadas en editor o runtime (CreateAssetMenu)
- Resueltas por AbilityDatabaseSO.Resolve(DNA) → retorna array [Horn, Wings, Back]
- Asignadas a AgentAbilities.Bind() en ArenaSandbox.SpawnAgent()
- Accedidas en tiempo real por AgentAbilities para cooldowns y disparo
- Renderizadas en ArenaHudCard (Charge01, color) y CreatureCueDrawer (AbilityBursts)

**S107 (NUEVO):**
- Define contrato de habilidad individual
- Permite mezcla de Damage (combate físico) + Mobility (velocidad condicional)
- Flexible: cada parte puede tener habilidades diferentes según DNA

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[AbilityDatabaseSO]], [[AgentAbilities]], [[ClashMoveSO]], [[ClashSlot]]
