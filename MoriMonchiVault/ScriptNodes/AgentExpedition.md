---
tags: [script, world, ai, expedition, internal]
---

# AgentExpedition.cs

**Ruta:** `World/AI/AgentExpedition.cs`

**Responsabilidad:** Núcleo delgado de composición (S104 reescritura por partición) que orquesta cinco colaboradores de ocupación mediante interfaz `IExpeditionTask`: `AgentGatherer` (recolector), `AgentGuard` (custodio), `AgentHunter` (cazador), `AgentDecoy` (señuelo), `AgentScout` (explorador). No contiene lógica de estado; delega todo a colaborador activo. Switch por `Occupation` (derivado de `ArenaOrders` o fallback Gather). Minado ocioso: si ocupación no-recolector ha estado ociosa `IdleMineSeconds`, Gatherer toma control. Cristales caídos: si Hunter detecta drop en radio, Gatherer toma control. Break/Hunter sí se persisten post-golpeo (pueden abortar via PostureEngages).

**Constructor:**
- `AgentExpedition(MoriMochiAgent owner, AgentContext ctx)` — instancia 5 colaboradores

**Métodos públicos:**
- `bool TryEngage()` — intenta iniciar ocupación. Switch `Occupation`: Guard→guard.TryEngage() fallback gatherer; Break→hunter fallback gatherer; Decoy→decoy fallback gatherer; Explore→scout fallback gatherer; default→gatherer
- `void TickExpedition()` — tickea colaborador activo. Si retorna false: Abort. Si Gatherer y PostureEngages (rival detectado): cancela y cambia a Guard/Hunter/Decoy. Si Hunter/Decoy inactivo y (loot nearby OR IdleSeconds>=IdleMineSeconds): cambia a Gatherer
- `void OnKnocked()` — notifica Gatherer y Hunter; cancela resto
- `void Cancel()` — cancela todos colaboradores sin resetear
- `void ResetForReuse()` — limpia todos colaboradores

**Propiedades públicas (delegadas):**
- `int Carried` → `gatherer.Carried`
- `int CarryCapacity` → `gatherer.CarryCapacity`
- `int Collected` → `gatherer.Collected`
- `int Secured` → `gatherer.Secured`
- `int Fled` → `gatherer.Fled`
- `int Reports` → `scout.Reports`
- `MoriMochiAgent Guardian` → `gatherer.Guardian`
- `float FleeCooldown01` → `gatherer.FleeCooldown01`
- `float DecoyCooldown01` → `decoy.Cooldown01`
- `float Retreat01` → `hunter.Retreat01`
- `bool IsChasing` → `guard.IsChasing`
- `float MiningProgress` → `gatherer.MiningProgress`
- `Transform TargetTransform` → `active.TargetTransform`
- `CreatureIntent Intent` → `active.Intent`

**Internals:**
- `active` (IExpeditionTask) — colaborador en uso; null si idle
- `Occupation Occupation` — helper que retorna `ctx.Occupation` o Gather si None
- `bool PostureEngages(ExpeditionRulesSO)` — chequea si hay rival; si sí, trata de activar ocupación de contacto (Guard/Hunter/Decoy)
- `float IdleSeconds(IExpeditionTask)` — retorna IdleSeconds del colaborador si aplica

**Flujo S104:**
1. `TryEngage()` selecciona colaborador por Occupation
2. `TickExpedition()` tickea; si ocupa activa hay rival → PostureEngages
3. Si Hunter/Decoy ociosos O loot caído detectado → Gatherer toma control
4. `OnKnocked()` aborta todo; Gatherer y Hunter tienen lógica post-golpeo específica

**Integración:**
- Llamado desde `MoriMochiAgent.Update()` en AgentState.Expedition
- Si clash.TryEngage() ok, expedition se cancela (prioridad clash)

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[IExpeditionTask]], [[AgentGatherer]], [[AgentGuard]], [[AgentHunter]], [[AgentDecoy]], [[AgentScout]], [[MoriMochiAgent]], [[AgentContext]], [[ExpeditionRulesSO]], [[ArenaOrders]]
