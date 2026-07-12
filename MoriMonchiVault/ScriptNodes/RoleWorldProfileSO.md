---
tags: [script, genetics]
---

# RoleWorldProfileSO.cs

**Ruta:** `Data/Genetics/RoleWorldProfileSO.cs`

**Responsabilidad:** SerializedScriptableObject con diccionario `Dictionary<Role, RoleWorldProfile>`. Data-driven tuning centralizado de cómo cada rol se comporta en world: MoveSpeed, IdleChance, RoamRadius, ProximityReaction, FollowDistance, PreferredArea, AreaPreference, RecoverySpeed, Tint. `GetProfile(Role)` devuelve fallback neutro si falta entrada. Singleton (Current) establecido en OnEnable. Botón "Populate Defaults" precarga los tres roles estándar (Protector, Agresivo, Empatico).

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[MoriMochiAgent]], [[Role]], [[RoleWorldProfile]]
