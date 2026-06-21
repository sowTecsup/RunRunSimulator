---
tags: [script, genetics]
---

# GeneticsLabPreview.cs

**Ruta:** `Core/GeneticsLabPreview.cs`

**Responsabilidad:** Componente dev (MonoBehaviour) para editor/testing: genera/carga DNA arbitrario y visualiza rarity breakdown por parte (Body Shape, Arms, Eyes, Mouth, Score promedio). Botones: Generate Random Creature, Load from ID (string formato DNA). Valida IDs contra database. Ref serializada [SerializeField] a GameManager. Sin persistencia (preview solo).

**Vinculado a:** [[Index/09 - Dev Tools]]

**Conexiones:** [[GameManager]], [[CreatureDNA]], [[CreatureGenerator]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]]

**Uso en escena:** Adjuntar a un GameObject con acceso a GameManager. Inspect, configura GameManager ref y usa botones.
