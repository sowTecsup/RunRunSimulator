---
tags: [script, world, perception, registry]
---

# Perceivable.cs

**Ruta:** `World/AI/Perceivable.cs`

**Responsabilidad:** Marca cualquier entidad del mundo (jugador, MoriMochi, cliente, prop) como perceptible por otros agentes. Auto-registra/desregistra con PerceivableRegistry en OnEnable/OnDisable (patrón NeedStationRegistry). Almacena el tipo (PerceivableKind), etiquetas opcionales y una referencia al MoriMochiAgent propietario (null para entidades no-Monchi). El struct Percept transporta una sola observación (fuente, tipo, distancia, afinidad) — valor puro, nunca retenido.

**Campos:**
- `kind` — PerceivableKind (Player/Monchi/Customer/Prop)
- `tags` — List<string> opcional para categorización temática
- `Monchi` — referencia al MoriMochiAgent propietario (null si es jugador/cliente/prop)

**Métodos:**
- `Position → Vector3` — posición en tiempo real

**Struct Percept:**
- `Source` — Perceivable observado
- `Kind` — tipo clasificado
- `SqrDistance` — distancia cuadrada al observador
- `Affinity` — afinidad social (solo para Monchi-a-Monchi, 0 para otros)

**Cambios S93:**
- Removido: método `HasTag()` (no se usa desde S93)

**Vinculado a:** [[Index/06 - Player & World]], [[MoriMonchiVault/Index/14 - Social V1 (Perceivable, AgentSenses, AgentSocial)]]

**Conexiones:** [[PerceivableRegistry]], [[AgentSenses]], [[AgentSocial]], [[MoriMochiAgent]]
