---
tags: [script, data, scriptableobject, expedition]
---

# AbilityDatabaseSO.cs

**Ruta:** `Data/Expedition/AbilityDatabaseSO.cs`

**Responsabilidad:** Base de datos que resuelve habilidades de un agente según partes genéticas del DNA (Horn/Wings/Back). Mapea stableHash(partID) % candidates.Count para selección determinista.

**Campos Serializados:**
- `Abilities` (List<AbilitySO>) — pool de todas las habilidades disponibles (~20 típico, una por slot/variante)

**Métodos Públicos:**

- `AbilitySO[] Resolve(CreatureDNA dna) → AbilitySO[]` — retorna array [Horn, Wings, Back]:
  - Llama Pick(dna.HornID, Horn), Pick(dna.WingID, Wings), Pick(dna.BackID, Back)
  - Cada uno retorna la habilidad asociada o null

**Métodos Privados:**

- `AbilitySO Pick(string partId, ClashSlot slot) → AbilitySO`:
  - Filtra Abilities por ability.Slot == slot
  - Usa StableHash(partId) % candidates.Count para índice determinista
  - Retorna candidates[index] o null si no hay candidatos

- `int StableHash(string s) → int` — hash determinista de string:
  - Suma con rotación: h = h * 31 + c para cada char
  - Retorna h & 0x7fffffff (positivo)

**Invariantes:**
- **Determinismo:** mismo partID → siempre misma habilidad (xor, replay, multiplayer)
- **Balanceo:** cada slot (Horn/Wings/Back) puede tener múltiples candidatos (variedad)
- **Null safety:** si partId == null o no hay candidatos, retorna null

**Integración:**
- Referenciado en ArenaSandbox (campo `abilityDatabase`)
- Llamado en ArenaSandbox.SpawnAgent() antes de AgentAbilities.Bind()
- El array resuelto se pasa directamente a Bind()
- Permite que cada MoriMochi tenga combo único Horn+Wings+Back según genética

**S107 (NUEVO):**
- Centraliza mapeo de partes → habilidades
- Similar a BodyShapeDatabaseSO / PartDatabaseSO, pero para gameplay (no visual)
- Facilita ajustes de balance sin tocar DNA

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[AbilitySO]], [[AgentAbilities]], [[CreatureDNA]], [[ArenaSandbox]], [[PartDatabaseSO]]
