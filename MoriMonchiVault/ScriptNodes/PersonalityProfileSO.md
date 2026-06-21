---
tags: [script, genetics]
---

# PersonalityProfileSO.md

**Ruta:** `Data/Genetics/PersonalityProfileSO.cs`

**Responsabilidad:** Define 6 arquetipos de Personality (Skittish, Aggressive, Lazy, Curious, Social, Grumpy) con tuning per-personalidad vía `Dictionary<Personality, PersonalityProfile>` (OdinSerialize). SerializedScriptableObject sin `static Current`; lo posee GameManager, llega al agent vía `MoriMonchiController.Initialize()` que llama `agent.Initialize(dna, profileTable, player)`. `GetProfile(p)` devuelve perfil tuning (MoveSpeed, IdleChance, ProximityReaction, Tint, etc.) o neutral fallback. Botón PopulateDefaults llena los 6.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[GameManager]], [[MoriMonchiController]], [[MoriMochiAgent]], [[CreatureDNA]]
