---
tags: [script, data, social, rule, polymorphic]
---

# ReactionRuleBase.cs

**Ruta:** `Data/Social/ReactionRuleBase.cs`

**Responsabilidad:** Base abstracta para una regla de reacción social. Dado un Percept (algo que el agente notó cercano), decide si quiere reaccionar y qué tan fuertemente. Odin serializa la lista polimórfica nativamente (patrón EquipmentEffectBase), así que RoleWorldProfile expone un dropdown "+" para mezclar libremente reglas Approach/Avoid/PlayChase/SleepTogether/Fight por rol. Cada regla concreta es un tipo cerrado y paramétrico — sin lógica freeform. AgentSocial puntúa toda regla coincidente por Percept y elige la mejor cada tick. **S65:** `SocialAction` enum extendido con SleepTogether y Fight. **S69:** Todas las reglas usan umbrales EFECTIVOS: Approach/PlayChase/SleepTogether restan `DialShift(Sociability, SociabilityAffinityShift)` a su MinAffinity (Sociable interactúa más). Avoid resta `DialShift(Boldness, BoldnessAvoidShift)` a MaxAffinity (Osado evita menos). GremlinFight suma `DialShift(Boldness, BoldnessFightShift)` a MaxAffinity (Osado pelea más).

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

```csharp
[Range(-1f, 1f)] public float MinAffinity = 0.25f;
```

**S69:** El umbral efectivo se calcula en `Matches()`:
```csharp
float minAff = MinAffinity - SocialTuningSO.DialShift(
    self.DNA != null ? self.DNA.Sociability : 0.5f, 
    tuning.SociabilityAffinityShift);
if (p.Affinity < minAff) return false;
```

**Interpretación:** Sociable alto (dial 0.8) → umbral baja (interactúa con menos amigos). Tímido (dial 0.2) → umbral sube (solo con buenos amigos).

- Score = Affinity

---

### AvoidDislikedRule

Evita a otro MoriMochi con mala afinidad.

```csharp
[Range(-1f, 1f)] public float MaxAffinity = -0.3f;
```

**S69:** El umbral efectivo se calcula en `Matches()`:
```csharp
float maxAff = MaxAffinity - SocialTuningSO.DialShift(
    self.DNA != null ? self.DNA.Boldness : 0.5f, 
    tuning.BoldnessAvoidShift);
if (p.Affinity > maxAff) return false;
```

**Interpretación:** Osado alto (dial 0.8) → umbral baja (evita MENOS, acepta acercarse). Tímido (dial 0.2) → umbral sube (evita MÁS agresivamente).

- Score = -Affinity (negativo para que Avoid sea inversible respecto Approach)

---

### PlayChaseRule

Invita a juego de persecución a otro MoriMochi con buena afinidad, si ambos tienen energía suficiente.

```csharp
[Range(-1f, 1f)] public float MinAffinity = 0.35f;
[Min(0f)] public float PriorityBonus = 0.15f;
```

**S69:** El umbral efectivo se calcula en `Matches()`:
```csharp
float minAff = MinAffinity - SocialTuningSO.DialShift(
    self.DNA != null ? self.DNA.Sociability : 0.5f, 
    tuning.SociabilityAffinityShift);
if (p.Affinity < minAff) return false;
```

**Interpretación:** Sociable → invita a jugar con menos amigos. Tímido → solo juega con buenos amigos.

- Score = Affinity + PriorityBonus (más exigente que Approach en umbral, pero bonus de score para preferir PlayChase sobre Approach si ambas califican)
- Gates: Ambos tienen energía ≥ SocialTuningSO.MinEnergyToPlay

---

### SleepTogetherRule (S65)

Invita a dormir juntos si ambos tienen energía baja (cansancio) y buena afinidad.

```csharp
[Range(-1f, 1f)] public float MinAffinity = 0.2f;
```

**S69:** El umbral efectivo se calcula en `Matches()`:
```csharp
float minAff = MinAffinity - SocialTuningSO.DialShift(
    self.DNA != null ? self.DNA.Sociability : 0.5f, 
    tuning.SociabilityAffinityShift);
if (p.Affinity < minAff) return false;
```

**Interpretación:** Sociable → duerme con menos amigos. Tímido → solo duerme con buenos amigos.

- Score = Affinity × (1 - energyNormalized) — score más alto si ambos están más cansados
- Gates: Ambos tienen energía ≤ SocialTuningSO.MaxEnergyToSleep (default 45)

---

### GremlinFightRule (S65)

Inicia pelea si hay mala afinidad pero los dos están disponibles (ausencia de cooperación positiva).

```csharp
[Range(-1f, 1f)] public float MaxAffinity = -0.2f;
```

**S69:** El umbral efectivo se calcula en `Matches()`:
```csharp
float maxAff = MaxAffinity + SocialTuningSO.DialShift(
    self.DNA != null ? self.DNA.Boldness : 0.5f, 
    tuning.BoldnessFightShift);  // SUMA en vez de resta
if (p.Affinity > maxAff) return false;
```

**Interpretación:** Osado alto (dial 0.8) → pelea incluso con afinidad MÁS alta (más agresivo). Tímido (dial 0.2) → rechaza pelear (necesita MUCHA mala afinidad).

- Score = -Affinity (negativo para que combata con baja afinidad)
- Gates: Ambos están sanos y disponibles (no Held, no Airborne, no Penned)

---

## Cambios S69

**Introducción de diales genéticos a todas las 5 reglas:**

Todas las reglas que tienen `MinAffinity` (Approach, PlayChase, SleepTogether) ahora restan el desplazamiento Sociability:
```csharp
float minAff = MinAffinity - SocialTuningSO.DialShift(
    self.DNA != null ? self.DNA.Sociability : 0.5f, 
    tuning.SociabilityAffinityShift);
```

Esto hace que:
- **Sociable alto (0.8):** MinAffinity baja → interactúa más fácilmente
- **Sociable bajo (0.2):** MinAffinity sube → solo interactúa con buenos amigos

Reglas que tienen `MaxAffinity` (Avoid, GremlinFight) manejan Boldness diferente:
- **AvoidDislikedRule:** RESTA el desplazamiento Boldness → osado evita menos
- **GremlinFightRule:** SUMA el desplazamiento Boldness → osado pelea más

**Fórmula DialShift:**
```csharp
public static float DialShift(float dial, float shift) 
    => (Mathf.Clamp01(dial) - 0.5f) * 2f * shift;
```

Transforma dial [0..1] a cambio simétrico [-shift, +shift], centrado en 0.

## Notas

- SleepTogether y Fight tienen cooldown global (SocialCooldown del mismo asset SocialTuningSO); no pueden ocurrir dos veces seguidas con el mismo par inmediatamente
- SleepTogether y Fight son mutualmente excluyentes: si uno inicia SleepTogether, una invitación simultánea de Fight es rechazada
- Todas las reglas respetan el `IsSocializing` state: no se activan si ya está en otra interacción social
- S69: Si `self.DNA` es null, fallback a dial neutral 0.5 (sin desplazamiento)

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/02 - Genetics & Breeding]]
- [[MoriMonchiVault/Index/14 - Social V2]]

## Conexiones

**Entrada:**
- `RoleWorldProfileSO` — serializa la lista polimórfica de reglas por rol
- `AgentSocial.TryEngage()` — itera reglas, pondera y elige la mejor por Percept
- `SocialTuningSO` — consulta para DialShift() y umbrales efectivos (S69)

**Salida:**
- `SocialAction` enum — determina qué modo entra AgentSocial (Approach/Chaser/Runner/Sleeping/Fighting)
- `SocialGraphService.RecordInteraction()` — registra outcome (PlayChase, SleepTogether, GremlinFight) como delta de afinidad
