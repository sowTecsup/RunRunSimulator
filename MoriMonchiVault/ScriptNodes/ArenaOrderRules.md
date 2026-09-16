---
tags: [script, data, expedition, rules]
---

# ArenaOrderRules.cs

**Ruta:** `Data/Expedition/ArenaOrderRules.cs`

**Responsabilidad:** Utilidades estáticas para convertir entre órdenes (`ArenaOrders`), ocupaciones (`Occupation` / `ArenaSite`) y bases (`ArenaBase`). Valida bloqueos de órdenes por personalidad (Boldness/Sociability), aplica clamping de valores fuera de rango. **S122:** Agrega `Clamp(role, base)` que mapea base a órdenes concretas (sin diales).

**Métodos públicos:**
- `Occupation ToOccupation(ArenaOrders o)` — retorna Gather/Decoy/Guard/Break según Contact+Posture
- `ArenaSite ToSite(ArenaOrders o)` — retorna Center/NearVein/FarVein según Loot y Posture
- `ArenaOrders FromOccupation(Occupation occupation, ArenaSite site)` — reconstruye órdenes
- `int Choice(ArenaOrders o, OrderPillar pillar)` — extrae int del pilar (0-1)
- `ArenaOrders With(ArenaOrders o, OrderPillar pillar, int choice)` — retorna copia con pilar reemplazado
- ~~`IsLocked`~~ — borrado en S122
- `ArenaOrders Clamp(CreatureDNA dna, ExpeditionRulesSO rules, ArenaOrders o)` — aplica bloqueos por diales (S104-S121)
- `ArenaOrders Clamp(Role role, ArenaBase base)` — **(S122)** Mapea rol+base a órdenes concretas (sin diales, determinístico)

**Bloqueadores (S104-S121):**
- Contact: `Boldness >= BoldFightLock` fuerza Fight; `Boldness <= ShyFleeLock` fuerza Flee
- Posture: `Sociability >= SocialProtectLock` fuerza Protect; `Sociability <= LonerAggressiveLock` fuerza Aggressive
- **S122 CAMBIO:** Diales NO bloquean más (`IsLocked`/`UnlockRead`/`LockReason` eliminados). Base es la autoridad nueva.

**Mapeo Rol+Base → Órdenes (S122):**

Delegado a [[ArenaBases]].ToOrders(role, base):
```
Protector + Territory → Big/Fight/Protect
Protector + Forage → Small/Flee/Protect
Agresivo + Territory → Big/Fight/Aggressive
Agresivo + Opportunism → Small/Flee/Aggressive
Empático + Forage → Big/Flee/Protect
Empático + Opportunism → Small/Fight/Aggressive
```

**S122 Cambios:**
- `Clamp(dna, rules, o)` (S122): devuelve `o` si es la variante de una base abierta del `Role` (`ArenaBases.TryBaseOf`); si no, `ArenaBases.ToOrders(role, Default(role))`. Los diales no intervienen.
- `Clamp(role, base)` nuevo; retorna órdenes concretas sin diales
- Diales siguen existiendo en DNA pero no bloquean ocupación (solo afinar ejecución)
- Panel de plan: muestra dos píldoras de base abiertas y la cerrada con razón (no diales bloqueados)

**Invariantes:**
- Clamp por diales (S104-S121) sigue aplicable para compatibilidad (pero Prepare S122 no lo usa)
- Clamp por base (S122) es determinístico: mismo rol+base = mismas órdenes
- Base y ocupación derivada son autoridad única en S122+

**Vinculado a:** [[Index/24 - Puente Tienda-Arena]], [[Index/22 - Bajada Nocturna y Linaje]]

**Conexiones:** [[ArenaBases]], [[ArenaOrders]], [[ArenaOrderCatalog]], [[CreatureDNA]], [[WorldEnums]]
