---
tags: [script, world, ai, expedition, internal]
---

# AgentExpedition.cs

**Ruta:** `World/AI/AgentExpedition.cs`

**Responsabilidad:** Núcleo delgado de composición (S104 reescritura por partición) que orquesta cinco colaboradores de ocupación mediante interfaz `IExpeditionTask`: `AgentGatherer` (recolector), `AgentGuard` (custodio), `AgentHunter` (cazador), `AgentDecoy` (señuelo), `AgentScout` (explorador). No contiene lógica de estado; delega todo a colaborador activo. Switch por `Occupation` (derivado de `ArenaOrders` o fallback Gather). Minado ocioso: si ocupación no-recolector ha estado ociosa `IdleMineSeconds`, Gatherer toma control. Cristales caídos: si Hunter/Decoy detectan drop en radio, Gatherer toma control. **S105: Break persigue con TryHunt (solo presa, no post)**.

**Constructor:**
- `AgentExpedition(MoriMochiAgent owner, AgentContext ctx)` — instancia 5 colaboradores

**Métodos públicos:**
- `bool TryEngage()` — intenta iniciar ocupación. Switch `Occupation`: Guard→guard.TryEngage() fallback gatherer; **Break→hunter.TryHunt() fallback gatherer**; Decoy→decoy.TryEngage() fallback gatherer; Explore→scout.TryEngage() fallback gatherer; default→gatherer
- `void TickExpedition()` — tickea colaborador activo. Si retorna false: Abort. Si Gatherer y PostureEngages (rival detectado): cancela y cambia a ocupación de contacto. Si Hunter/Decoy inactivo y (loot nearby OR IdleSeconds>=IdleMineSeconds): cambia a Gatherer
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
- `bool IsChasing` → `guard.IsChasing || hunter.IsChasing` **(S105: agregó hunter.IsChasing)**
- `float MiningProgress` → `gatherer.MiningProgress`
- `Transform TargetTransform` → `active.TargetTransform`
- `CreatureIntent Intent` → `active.Intent`

**Internals:**
- `active` (IExpeditionTask) — colaborador en uso; null si idle
- `Occupation Occupation` — helper que retorna `ctx.Occupation` o Gather si None
- `bool PostureEngages(ExpeditionRulesSO)` — chequea si hay rival; si sí, trata de activar ocupación de contacto (Guard/Hunter/Decoy)
- `float IdleSeconds(IExpeditionTask)` — retorna IdleSeconds del colaborador si aplica

**Flujo S105:**
1. `TryEngage()` selecciona colaborador por Occupation
   - Break: llama `hunter.TryHunt()` (no TryEngage) para buscar presa directa
   - Si TryHunt falla: fallback gatherer
2. `TickExpedition()` tickea
   - Si Gatherer activo y PostureEngages: aborta, switchea a ocupación contacto
   - Si Hunter/Decoy inactivo O drop detectado: Gatherer toma control
3. Si clash.TryEngage() ok, expedition se cancela (prioridad clash)

**Integración:**
- Llamado desde `MoriMochiAgent.Update()` en AgentState.Expedition
- PostureEngages llamada si active == gatherer; si True: cancela gatherer, activa ocupación de contacto
- S105: TryHunt diferencia cazador "puro" (presa) vs post (fallback)

**Invariantes:**
- Break solo persigue presa válida (TryHunt); si no, fallback Gather
- Cristales caídos: Break y Decoy switchean a Gather (PlannedSite)
- IsChasing agregó hunter.IsChasing para UI/HUD

**Vinculado a:** [[Index/23 - Arena Sandbox y Expedicion]]

**Conexiones:** [[IExpeditionTask]], [[AgentGatherer]], [[AgentGuard]], [[AgentHunter]], [[AgentDecoy]], [[AgentScout]], [[MoriMochiAgent]], [[AgentContext]], [[ExpeditionRulesSO]], [[ArenaOrders]]
