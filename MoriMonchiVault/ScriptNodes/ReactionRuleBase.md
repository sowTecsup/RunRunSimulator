---
tags: [script, data, social, rule, polymorphic]
---

# ReactionRuleBase.cs

**Ruta:** `Data/Social/ReactionRuleBase.cs`

**Responsabilidad:** Base abstracta para una regla de reacción social. Dado un Percept (algo que el agente notó cercano), decide si quiere reaccionar y qué tan fuertemente. Odin serializa la lista polimórfica nativamente (patrón EquipmentEffectBase), así que RoleWorldProfile expone un dropdown "+" para mezclar libremente reglas Approach/Avoid/PlayChase/SleepTogether/Fight por rol. Cada regla concreta es un tipo cerrado y paramétrico — sin lógica freeform. AgentSocial puntúa toda regla coincidente por Percept y elige la mejor cada tick. **S65:** `SocialAction` enum extendido con SleepTogether y Fight.

**Métodos abstractos:**
- `Action → SocialAction` — qué tipo de reacción implementa
- `Matches(in Percept p, MoriMochiAgent self, SocialTuningSO tuning, out float score) → bool` — verdadero si la regla se aplica a este Percept. Score se usa para desempate (mejor score gana). Out-parameter para evitar alloc
- `Summary() → string` — descripción readable en UI (p.ej. "Se acerca si afinidad >= 0.3")

**Métodos protegidos:**
- `TargetFree(in Percept) → bool` — helper: verdadero si el Monchi objetivo no está Held, Airborne, Courting, Socializing o Penned (disponible para interacción)

## Enum SocialAction

```
Approach = 0, Avoid = 1, PlayChase = 2, SleepTogether = 3, Fight = 4
```

**Descripción:**
- `Approach` — Acercarse amistosamente a otro MoriMochi
- `Avoid` — Evitar/huir de otro MoriMochi
- `PlayChase` — Iniciar juego de persecución
- `SleepTogether` — **S65 NUEVO** Invitar a dormir juntos (siesta compartida)
- `Fight` — **S65 NUEVO** Iniciar pelea de gremlins (abalanzadas agresivas)

## Reglas Concretas (todas Serializable)

### ApproachFriendRule
Se acerca a otro MoriMochi con buena afinidad, siempre que esté disponible.
- `MinAffinity` [Range -1…1] — afinidad mínima requerida (default 0.25)
- Score = Affinity

### AvoidDislikedRule
Evita a otro MoriMochi con mala afinidad.
- `MaxAffinity` [Range -1…1] — afinidad máxima para evitar (default -0.3, "peor que esto nos repele")
- Score = -Affinity (negativo para que Avoid sea inversible respecto Approach)

### PlayChaseRule
Invita a juego de persecución a otro MoriMochi con buena afinidad, si ambos tienen energía suficiente.
- `MinAffinity` [Range -1…1] — afinidad mínima (default 0.35, más exigente que Approach)
- `PriorityBonus` [Min 0] — bonus de score para preferir PlayChase sobre Approach si ambas califican (default 0.15)
- Score = Affinity + PriorityBonus
- Gates: Ambos tienen energía ≥ SocialTuningSO.MinEnergyToPlay

### SleepTogetherRule (S65 NUEVO)
Invita a dormir juntos si ambos tienen energía baja (cansancio) y buena afinidad.
- `MinAffinity` [Range -1…1] — afinidad mínima (default 0.2, menos exigente que juego)
- Score = Affinity × (1 - energyNormalized) — score más alto si ambos están más cansados
- Gates: Ambos tienen energía ≤ SocialTuningSO.MaxEnergyToSleep (default 45)

### GremlinFightRule (S65 NUEVO)
Inicia pelea si hay mala afinidad pero los dos están disponibles (ausencia de cooperación positiva).
- `MaxAffinity` [Range -1…1] — afinidad máxima para pelear (default -0.2, "si no te tengo confianza...")
- Score = -Affinity (negativo para que combata con baja afinidad)
- Gates: Ambos están sanos y disponibles (no Held, no Airborne, no Penned)

**Notas:**
- SleepTogether y Fight tienen cooldown global (SocialCooldown del mismo asset SocialTuningSO); no pueden ocurrir dos veces seguidas con el mismo par inmediatamente
- SleepTogether y Fight son mutualmente excluyentes: si uno inicia SleepTogether, una invitación simultánea de Fight es rechazada
- Todas las reglas respetan el `IsSocializing` state: no se activan si ya está en otra interacción social

## Vinculado a

- [[Index/06 - Player & World]]
- [[MoriMonchiVault/Index/14 - Social V2]]

## Conexiones

**Entrada:**
- `RoleWorldProfileSO` — serializa la lista polimórfica de reglas por rol
- `AgentSocial.TryEngage()` — itera reglas, pondera y elige la mejor por Percept

**Salida:**
- `SocialAction` enum — determina qué modo entra AgentSocial (Approach/Chaser/Runner/Sleeping/Fighting)
- `SocialGraphService.RecordInteraction()` — registra outcome (PlayChase, SleepTogether, GremlinFight) como delta de afinidad
