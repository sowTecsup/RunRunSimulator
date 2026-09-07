---
tags: [script, world, ai, expedition, task]
---

# AgentHunter.cs

**Ruta:** `World/AI/AgentHunter.cs`

**Responsabilidad:** Colaborador de `AgentExpedition` que implementa `IExpeditionTask`. Persigue recolectores sin custodio para forzar que suelten carga. Soporta anzuelo (baiting) en S105: si provocador (taunter) en radio, lo persigue durante `HunterBaitSeconds` y lo ignora `BaitImmunitySeconds` tras perderlo. Valida cooldown retiro, busca presa, persigue, o posta en post si sin presa. Al golpearse (`OnKnocked`), se retira hacia salida. Emote Molesto al empezar persecución.

**Lógica S105 (Anzuelo):**
1. Si hay `chasing` (provocador en curso): persigue hasta `chaseUntil` o distancia límite
2. Si pierde `chasing`: marca como `baited` + `baitImmuneUntil` (CD anti-rebaiting)
3. Si sin persecución y `HunterBaitSeconds > 0`: busca NearestTaunter en radio; si válido (no baited reciente), inicia `Chasing`
4. Fallback a búsqueda de presa normal si sin provocador

**Máquina de estados:**
- `retreating` — post-golpeo, navega a salida + cooldown
- `chasing` — persigue provocador (baiting, S105 NUEVO)
- `prey` — persigue recolector sin custodio (hunt clásico)
- `post` — en puesto de espera, busca presa cada frame

**Propiedades internas:**
- `prey` — recolector perseguido
- `post` — post de espera
- `chasing` — provocador perseguido (S105 NUEVO)
- `chaseUntil` — timestamp fin persecución anzuelo (S105 NUEVO)
- `baited` — último provocador abatido (S105 NUEVO)
- `baitImmuneUntil` — timestamp fin inmunidad anzuelo (S105 NUEVO)
- `idle` — segundos ociosos en post
- `retreating` — en modo retiro post-golpeo
- `retreatUntil` — timestamp fin retiro
- `huntTimer, repathTimer, elapsed` — timers

**Métodos público:**
- `bool TryEngage(ExpeditionRulesSO rules)` → bool — intenta iniciar; fallback post si no hay presa
- `bool TryHunt(ExpeditionRulesSO rules)` → bool — **(S105 NUEVO)** intenta buscar presa solo (sin post); retorna false si sin válido target
- `bool Tick(ExpeditionRulesSO rules)` → bool — procesa frame
- `void OnKnocked(ExpeditionRulesSO rules)` — activa retiro si Break
- `void Cancel()` — borra prey, post, chasing, state
- `void ResetForReuse()` — reset completo
- `float IdleSeconds { get; }` — segundos en post sin actividad
- `float Retreat01 { get; }` — normalized [0,1] cooldown retiro
- `bool IsChasing { get; }` — **(S105 NUEVO)** true si hay `chasing`
- `CreatureIntent Intent` — Retreating | Chasing | Hunting
- `Transform TargetTransform` — chasing/prey/post/exit

**Integración (S105):**
- `AgentExpedition.PostureEngages()` llama `hunter.TryHunt(rules)` si Break (no TryEngage)
- `AgentExpedition.IsChasing` agregó `|| hunter.IsChasing`
- FindPrey solo retorna sin Thief intent ni Trusted Guardian
- NearestTaunter busca rival con Intent.Decoy en radio

**Flujo Tick:**
1. Si retreating: navega a salida, termina en cooldown
2. Si chasing: persigue con repath, sale si pierde/timeout → marca baited
3. Si sin chasing pero hay taunter válido: inicia chasing
4. Si prey: persigue (GiveUpSeconds), o fallback post
5. Si post: HoldAtPost, busca presa cada frame

**Knobs ExpeditionRulesSO (S105):**
- `HunterBaitSeconds = 8f` — duración persecución anzuelo
- `BaitImmunitySeconds = 6f` — CD post-abatimiento vs taunter anterior

**Invariantes:**
- Anzuelo solo activo si `HunterBaitSeconds > 0`
- `baited` ≠ current `chasing` (no rebaiting inmediato)
- Retreat persiste post-golpeo (no abbat por anzuelo en retiro)
- Intent precedencia: Retreating > Chasing > Hunting

**Vinculado a:** [[Index/23 - Arena Sandbox y Expedicion]]

**Conexiones:** [[IExpeditionTask]], [[AgentExpedition]], [[ExpeditionNav]], [[AgentContext]], [[MoriMochiAgent]], [[ExpeditionRulesSO]]
