---
tags: [script, data, scriptableobject, expedition]
---

# AbilitySO.cs

**Ruta:** `Data/Expedition/AbilitySO.cs`

**Responsabilidad:** ScriptableObject que define una habilidad individual (Damage, Mobility o Passive) de un MoriMochi. Mapea intención → ClashMove (Damage) o buff de velocidad (Mobility), con cooldown y disparadores (RivalInReach, Fleeing, Chasing). Pasivas otorgan stats operacionales (capacidad, velocidad cargado, resistencia, guarda, visibilidad). Uno por parte de cuerpo (Horn/Wings/Back) en AbilityDatabaseSO. Propietarias por IDs de parte (PartIds). **S118:** agregados Damage.Super con carga acumulable, campos ChargeOnHit/Mined/Secured.

**Enums:**
- `AbilityKind { Damage = 0, Mobility = 1, Passive = 2 }` — tipo de habilidad (S109: Passive agregado)
- `AbilityRole { Basic, Super }` — **S118 NUEVO:** clasificador de Damage (Basic = cooldown; Super = carga)
- `AbilityChargeSource { Hit, Mined, Secured }` — **S118 NUEVO:** fuentes de carga para Supers
- `AbilityTrigger [Flags] { None = 0, RivalInReach = 1, Fleeing = 2, Chasing = 4 }` — triggers para disparo automático

**Campos Serializados:**

**General:**
- `Name` (string) — nombre mostrado en HUD (ej: "Embestida")
- `Description` (TextArea) — tooltip o descripción larga
- `Slot` (ClashSlot: Horn/Wings/Back) — mapeo de parte corporal
- `Kind` (AbilityKind) — Damage, Mobility o Passive (S109)
- `Role` (AbilityRole) — **S118 NUEVO:** Basic o Super (solo para Kind == Damage)
- `Cooldown` (float, Min 0) — segundos entre disparos (Damage) o buffs
- `Trigger` (AbilityTrigger, EnumToggleButtons) — flags de disparadores
- `Color` (Color) — color renderizado en HUD y guías (RGB 1, 0.6, 0.2 por defecto)

**Daño (Section):**
- `Move` (ClashMoveSO) — movimiento de choque asociado (null si no es Damage)
- `MinDistance` (float, Min 0) — distancia mínima al rival (requerida)
- `MinRivalsNearby` (int, Min 0) — mínimo de rivales en rango (requerida) — prioridad alta

**Carga (Super) — S118 NUEVO:**
- `ChargeOnHit` (float, Range 0-1) — carga acumulada al golpear rival (0.34 default)
- `ChargeOnMined` (float, Range 0-1) — carga acumulada al minar mineral (0.08 default)
- `ChargeOnSecured` (float, Range 0-1) — carga acumulada al asegurar material (0.25 default)

**Movilidad (Section):**
- `SpeedMultiplier` (float, Min 1) — factor de velocidad (1.35 defecto)
- `BoostSeconds` (float, Min 0) — duración del buff

**Pasiva / costo (Section, S109):**
- `CarryCapacity` (int, Min 0) — override de capacidad (0 = sin cambio; si > 0, aplica el menor entre abilities)
- `LoadedSpeedFactor` (float, Min 0) — multiplicador de velocidad cuando cargado (0 = sin cambio; 1f default; < 1 penaliza)
- `KeepCarryOnKnock` (bool) — si true, no suelta carga al ser golpeado
- `GuardRadius` (float, Min 0) — radio de custodio en metros (0 = sin cambio)
- `VisibleFrom` (float, Min 0) — distancia visible para percepción rival (0 = sin cambio; max acumulativo)

**Partes que la otorgan (S109):**
- `PartIds` (List<string>) — IDs de parte que conceden esta habilidad (ej: ["horn-prong", "back-spikes"]). Si no vacío, AbilityDatabaseSO.Pick() busca primero por coincidencia.

**Métodos Públicos:**
- `bool Triggers(AbilityTrigger t) → bool` — chequea si flag t está seteado: `(Trigger & t) != 0`

- `float ChargeFor(AbilityChargeSource s) → float` — **S118 NUEVO:** retorna carga acumulada según fuente:
  - `Hit` → ChargeOnHit
  - `Mined` → ChargeOnMined
  - `Secured` → ChargeOnSecured
  - default → 0f

**Uso:**

- **Damage Basic:** Disparada cuando rival en rango AND cooldown listo AND RivalInReach en Trigger. Retorna ClashMoveSO para agente.

- **Damage Super:** **S118 NUEVO** Carga acumula desde Hit/Mined/Secured. Dispara cuando Charge >= 1 AND RivalInReach AND distancia OK, o si jugador solicita via Request(). Retorna ClashMoveSO.

- **Mobility:** Disparada automáticamente si Trigger Fleeing/Chasing activo AND cooldown listo. Aplica buff de velocidad por BoostSeconds via `AgentAbilities.TickMobility()`.

- **Passive (S109):** Se resuelve en `ExpeditionStats.Resolve()` junto con Occupation y ExpeditionRulesSO. Afecta operacionales: CarryCapacity, velocidad cargado, custodio, visibilidad.

**Integración:**

- Instancias creadas en editor o runtime (CreateAssetMenu)
- Resueltas por AbilityDatabaseSO.Resolve(DNA) → retorna array [Horn, Wings, Back]
- Asignadas a AgentAbilities.Bind() en ArenaSandbox.SpawnAgent()
- Accedidas en tiempo real por AgentAbilities para cooldowns y disparo (Damage/Mobility)
- PassiveS109 feeds ExpeditionStats.Resolve() para stats operacionales
- Renderizadas en ArenaHudCard (Charge01, color, clase pasiva, Armed state) y CreatureCueDrawer (AbilityBursts)
- ChargeFor() llamado desde ClashStrike.Impact() con AbilityChargeSource.Hit via owner.NotifyCharge()

**S118 Cambios:**

- `AbilityRole { Basic, Super }` — nueva clasificación de Damage abilities
- Sección "Carga (Super)" completa con 3 campos (ChargeOnHit, ChargeOnMined, ChargeOnSecured)
- `ChargeFor(source)` método: retorna carga según fuente de evento
- Supers se acumulan independientemente, sin cooldown inicial (Charge = 0); listo cuando Charge >= 1
- Supers se resuelven en `AgentAbilities.TryFireDamage()` con verificación Role == Super
- ArenaHudCard renderiza Supers con estado Armed cuando solicitados por jugador

**Invariantes:**

- Una habilidad puede ser solo Damage O Mobility O Passive (not mixed)
- Damage Basic requiere `Cooldown > 0`; Super no usa cooldown (Charge es el mediador)
- Damage Super requiere `Kind == Damage, Role == Super`
- Pasivas se aplican desde Bind (via RefreshStats); no hay "disparo" de pasiva
- PartIds puede estar vacío; cae a hash si no hay dueño declarado
- CarryCapacity: si múltiples abilities lo definen, gana el menor (costo más estricto)
- ChargeOnHit/Mined/Secured: solo se consultan si Role == Super

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], [[Index/22 - Bajada Nocturna y Linaje]], S118

**Conexiones:** [[AbilityDatabaseSO]], [[AgentAbilities]], [[ClashStrike]], [[ExpeditionStats]], [[ClashMoveSO]], [[ClashSlot]], [[ArenaHudCard]], [[RadialSlot]], [[CreatureCueDrawer]]
