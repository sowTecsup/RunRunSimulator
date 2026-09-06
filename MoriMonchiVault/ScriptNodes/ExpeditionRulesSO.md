---
tags: [script, data, scriptableobject, expedition]
---

# ExpeditionRulesSO.cs

**Ruta:** `Data/Expedition/ExpeditionRulesSO.cs`

**Responsabilidad:** **Singleton por escena** (`Current` static) que centraliza tuning de expedición. Contiene lista polimórfica Odin de reglas `ExpeditionRuleBase`, knobs de navegación compartidos, y beats de interacción. **S101 NUEVO:** knobs de ocupaciones (Guard: GuardRadius; Break: HuntRepathInterval; Decoy: DecoyRange, TauntSeconds, DecoyFleeDistance, DecoyFleeSeconds, DecoyCooldown). En tienda, `Current == null` → expedición desactiva. En Arena, `Current` apunta a asset `ExpeditionRules.asset`.

## Propiedades Estáticas

- `Current → ExpeditionRulesSO` — singleton por escena; set por `OnEnable()`. Null si no hay asset.

## Campos Públicos

**Lista de reglas:**
- `rules` (List<ExpeditionRuleBase>, IReadOnlyList pública) — lista polimórfica de reglas de evaluación.

**Tuning de navegación:**
- `ArriveDistance` (float, min 0.1, default 0.9)
- `RepathInterval` (float, min 0.05, default 0.5)
- `GiveUpSeconds` (float, min 1, default 12)
- `ApproachMargin` (float, min 0.05, default 0.15)

**Tuning de beats (S98):**
- `NoticeSeconds` (float, min 0, default 0.5)
- `TakeSeconds` (float, min 0, default 1.2) — rename histórico: era `MiningSeconds` (?), ahora se llama TakeSeconds
- `LoseSeconds` (float, min 0, default 1)

**Tuning de ocupación Gather (S101):**
- `MiningSecondsPerUnit` (float, min 0.5, default 4) — tiempo por unidad a recolectar
- `CarryCapacity` (int, min 1, default 3) — unidades máximas a llevar
- `DepositSeconds` (float, min 0, default 0.8) — beat de depósito en salida
- `DropPrefab` (MaterialPickup) — prefab de mineral soltado al ser golpeado
- `DropScale` (float, min 0.1, default 0.6)

**Tuning de ocupación Guard (S101 NUEVO):**
- `GuardRadius` (float, min 1, default 4) — distancia de vigilancia alrededor del mineral

**Tuning de ocupación Break (S101 NUEVO):**
- `HuntRepathInterval` (float, min 0.1, default 0.4) — repath throttle mientras persigue rival

**Tuning de ocupación Decoy (S101 NUEVO):**
- `DecoyRange` (float, min 1, default 4.5) — distancia para transicionar de Approach a Taunt
- `TauntSeconds` (float, min 0, default 0.8) — duración del taunt
- `DecoyFleeDistance` (float, min 1, default 8) — distancia a huir
- `DecoyFleeSeconds` (float, min 0.5, default 5) — duración de huida
- `DecoyCooldown` (float, min 0, default 4) — cooldown entre decoys

## Métodos Públicos

- `PopulateDefaults()` — **Botón Odin**: inicializa `rules` e inserta `SeekMaterialRule()`.

## Invariantes S101 + S98

- **Singleton por escena:** `Current` refleja el asset activo.
- **Null-safe:** `AgentExpedition.TryEngage()` chequea null antes de iterar.
- **Compartido:** navegación y beats son consultados por `AgentExpedition.TickExpedition()`.
- **Ocupación-specific:** Guard/Break/Decoy knobs se usan según Occupation actual del agente.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]] (sección 8.10: Ocupaciones con tiempo)

## Conexiones

- [[ExpeditionRuleBase]] / [[SeekMaterialRule]]
- [[AgentExpedition]] (lector: Current, reglas, tuning)
- [[ArenaSandbox]] (configura asset en Inspector)
- [[CreatureIntent]] (Taking, Losing, Guarding, Hunting, Taunting)
- [[Occupation]] (Gather, Guard, Break, Decoy)
