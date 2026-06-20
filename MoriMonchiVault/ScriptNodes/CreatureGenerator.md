---
tags: [memory-bank, script, genetics]
---

# CreatureGenerator.cs

**Ruta:** `Core/CreatureGenerator.cs`

**Responsabilidad:** Generador estático de criaturas aleatorias. Crea `CreatureDNA` con partes aleatorias por slot (cada uno con su propio roll de rareza), color base aleatorio via `ColorGenetics.RandomBase()`, sombra/outline derivados, `FurType` aleatorio. `RandomPersonality()` asigna personalidad no heredada (metadata, misma fuente para Mint y Breed).

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[PartDatabaseSO]], [[RarityOddsTableSO]], [[CreatureNameBank]], [[PersonalityProfileSO]], [[GameManager]], [[ColorGenetics]], [[FurType]]
