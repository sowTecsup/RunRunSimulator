---
tags: [script, world, ai, expedition, task]
---

# AgentGatherer.cs

**Ruta:** `World/AI/AgentGatherer.cs`

**Responsabilidad:** Colaborador de `AgentExpedition` (composición) que implementa `IExpeditionTask`. Maneja recolección de material: navega a sitio planeado o descubierto, detecta arribo, mina, carga, huye si hay rival sin custodio, vuelve a salida, deposita. Soporta orden Loot (Big/Small lode bias), Flee (huida con cadena a aliados/salida), Protect (custodio de aliados cercanos). Emite emotes (Curioso, Molesto, Feliz).

**Máquina de estados:**
- `Noticing` — espera antes de moverse a sitio
- `Moving` — navega hacia site con repath, detecta arribo por distancia o bloqueo
- `Mining` — extrae unidad por MiningSeconds, repite hasta lleno o site agotado
- `Losing` — se detiene y orienta a último sitio tras perderlo
- `Returning` — navega a salida
- `Securing` — deposita carga en exit zone
- `Fleeing` — huye de rival durante FleeSeconds + FleeCooldown, luego retorna si tiene carga

**Propiedades internas:**
- `Carried` — unidades en poder (0 a CarryCapacity)
- `Collected` — total minado en sesión
- `Secured` — total depositado en salida
- `Fled` — conteo de huidas
- `Guardian` — aliado custodio detectado (null si sin custodio)
- `FleeCooldown01` — normalized [0,1] de cooldown post-huida
- `MiningProgress` — [0,1] avance de minado en fase Mining

**Métodos público:**
- `bool TryEngage(ExpeditionRulesSO rules)` → bool — intenta comenzar recolección; retorna false si carga llena, sin site usable, ni reglas
- `bool Tick(ExpeditionRulesSO rules)` → bool — procesa frame; retorna false al terminar
- `Cancel()` — aborta sin resetear elapsed
- `ResetForReuse()` — limpia para pool recycle
- `void OnKnocked(ExpeditionRulesSO rules)` — suelta carga si tiene, aborta

**Integración:**
- Llamado desde `AgentExpedition.TryEngage()` por defecto o si otra ocupación falla
- Tickeado en `AgentExpedition.TickExpedition()` si es activo
- Puede ser abortado por `PostureEngages` si hay rival y ocupación no es Gather

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[IExpeditionTask]], [[AgentExpedition]], [[MoriMochiAgent]], [[AgentContext]], [[ExpeditionNav]], [[TeamBlackboard]], [[ExpeditionRulesSO]]
