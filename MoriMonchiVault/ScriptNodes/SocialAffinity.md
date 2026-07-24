---
tags: [script, system, social, math]
---

# SocialAffinity.cs

**Ruta:** `Systems/Social/SocialAffinity.cs`

**Responsabilidad:** Clase estática pura (sin estado mutable). Computa la afinidad social de semilla entre dos MoriMonchis: elemento compartido, parentesco directo (padre/madre/hermanos) y sesgo de rol, más una "química de par" determinista (hash de IDs) para que cualquier pareja siempre se guste/disguste por la misma cantidad fija. Este es el seed del futuro SocialGraph V2, que ajustará dinámicamente la afinidad según historial de interacción.

**Métodos estáticos:**

### Compute(CreatureDNA a, CreatureDNA b, SocialTuningSO t) → float
Devuelve afinidad [−1, 1] clamped. Suma:
1. `SameElementBonus` si a.Element == b.Element
2. `KinshipBonus` si cualquiera es padre del otro O comparten madre O comparten padre
3. `GetRoleBias(a.Role)` — sesgo del percibidor (el role de A importa, no el de B)
4. `PairChemistrySpread × normalized(Fnv1a32(sorted_IDs))` — hash determinista: ordena IDs alfabéticamente, hashea el par, normaliza [0,1] → [−1,1] × spread

Ejemplo: dos hermanos de elemento diferente y roles diferentes:
- Element bonus: 0
- Kinship bonus: 0.4
- Role bias (Agresivo): −0.15
- Pair chemistry: random [−0.25, +0.25]
- Total: [0, 0.6] aproximadamente

### Fnv1a32(string) → uint
Hash FNV-1a de 32-bit (semilla estándar 2166136261, prime 16777619). Determinista para IDs iguales.

**Notas:**
- Sin estado: puede llamarse desde cualquier contexto sin sincronización
- Usado por AgentSenses.Tick en cada Percept de Monchi
- El rol de B (el percibido) NO afecta — solo su DNA. El "gusto" es asimétrico en biología real (A puede amar a B sin reciprocidad)
- El hash de par es simétrico: Fnv1a32("A"+"B") == Fnv1a32("B"+"A") tras ordenar IDs, así que la afinidad es mutua (A→B == B→A)

**Vinculado a:** [[Index/06 - Player & World]], [[MoriMonchiVault/Index/14 - Social V1]]

**Conexiones:** [[AgentSenses]], [[SocialTuningSO]], [[CreatureDNA]]
