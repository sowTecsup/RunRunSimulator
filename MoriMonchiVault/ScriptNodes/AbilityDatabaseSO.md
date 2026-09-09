---
tags: [script, data, scriptableobject, expedition]
---

# AbilityDatabaseSO.cs

**Ruta:** `Data/Expedition/AbilityDatabaseSO.cs`

**Responsabilidad:** Base de datos que resuelve habilidades de un agente según partes genéticas del DNA (Horn/Wings/Back). Mapea stableHash(partID) % candidates.Count para selección determinista. Búsqueda en dos pasos: primero por `PartIds` (propietarias explícitas), fallback a hash si no hay dueño.

**Campos Serializados:**
- `Abilities` (List<AbilitySO>) — pool de todas las habilidades disponibles (~20 típico, una por slot/variante)

**Métodos Públicos:**

- `AbilitySO[] Resolve(CreatureDNA dna) → AbilitySO[]` — retorna array [Horn, Wings, Back]:
  - Llama Pick(dna.HornID, Horn), Pick(dna.WingID, Wings), Pick(dna.BackID, Back)
  - Cada uno retorna la habilidad asociada o null

**Métodos Privados:**

- `AbilitySO Pick(string partId, ClashSlot slot) → AbilitySO` (S109 ACTUALIZADO):
  - Paso 1: Si partId no es null ni vacío, busca habilidad que:
    - Tenga Slot == slot
    - Tenga partId en ability.PartIds
    - Si encuentra, retorna inmediatamente (búsqueda por propietaria explícita)
  - Paso 2 (fallback hash): filtra Abilities por ability.Slot == slot
  - Usa StableHash(partId) % candidates.Count para índice determinista
  - Retorna candidates[index] o null si no hay candidatos

- `int StableHash(string s) → int` — hash determinista de string:
  - Suma con rotación: h = h * 31 + c para cada char
  - Retorna h & 0x7fffffff (positivo, evita negativo en módulo)

**Invariantes:**

- **Determinismo:** mismo partID → siempre misma habilidad (xor, replay, multiplayer)
- **Prioridad de búsqueda:** PartIds explícito > hash (fallback). Si una habilidad declara partIds, toma precedencia
- **Balanceo:** cada slot (Horn/Wings/Back) puede tener múltiples candidatos (variedad vía hash)
- **Null safety:** si partId == null o no hay candidatos, retorna null
- **Estabilidad multi-sesión:** hash es puro (sin Random, sin Time), reproducible

**Integración:**

- Referenciado en ArenaSandbox (campo `abilityDatabase`)
- Llamado en ArenaSandbox.SpawnAgent() antes de AgentAbilities.Bind()
- El array resuelto se pasa directamente a Bind()
- Permite que cada MoriMochi tenga combo único Horn+Wings+Back según genética
- S109: PartIds hace posible que partes específicas otorguen habilidades específicas

**S109 Cambios:**

- Método Pick() actualizado: búsqueda dos pasos (PartIds → fallback hash)
- Soporta mezcla de abilities con y sin PartIds en el mismo database
- Si múltiples abilities comparten PartIds para el mismo slot, cualquiera matchea

**Ejemplo S109:**

```
Database: [Ability1(Slot=Horn, PartIds=["horn-prong"]), Ability2(Slot=Horn, PartIds=[]), Ability3(Slot=Horn, PartIds=["horn-crown"])]
dna.HornID = "horn-prong" → Pick busca PartIds.Contains("horn-prong") → retorna Ability1
dna.HornID = "horn-other" → no matchea PartIds, fallback hash → Ability2 o Ability3 determinista
```

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[AbilitySO]], [[AgentAbilities]], [[CreatureDNA]], [[ArenaSandbox]], [[PartDatabaseSO]], [[ExpeditionStats]]
