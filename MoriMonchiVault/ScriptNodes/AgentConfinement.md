---
tags: [script, world, agent, internal]
---

# AgentConfinement.cs

**Ruta:** `World/AI/AgentConfinement.cs`

**Responsabilidad:** Confinamiento a corrales (pens/breeding containers) y cortejo entre parejas. `EnterConfinement(pen)` restringe areaMask a BreedingRoom, re-ancla al piso del corral. `EnterCourtship(partner, anchor)` inicia danza: hembra (Tend role) orbita el anchor corto, macho (Orbit role) da vueltas alrededor de su pareja. `OnNavMeshWillRebake()` / `OnNavMeshRebaked()` gestionan handoff a ragdoll pre-rebake y recuperación post-rebake. `ReleaseFromPen()` libera del censo y restaura FreeAreaMask (solo el jugador levantándola lo hace). `DetachForReuse()` recicla sin persistir (pool reuse).

**Métodos públicos:**
- `EnterConfinement(MoriMochiContainer pen) → bool` — confina, retorna falso si piso no está en breeding NavMesh
- `EnterCourtship(MoriMochiAgent partner, Vector3 anchor)` — inicia danza
- `ExitCourtship()` — termina y vuelve a roaming
- `TickCourting()` — mantiene danza por frame
- `OnNavMeshWillRebake()` — prepara (ragdoll si está en NavMesh)
- `OnNavMeshRebaked()` — libera flag, permite settle
- `ReleaseFromPen()` — jugador levantó la criatura, libera areaMask + censo
- `DetachForReuse()` — pool recycle sin persistencia
- `DescribeCourtship() → string` — debug: describe cortejo en curso

**Cortejo internals:**
- `courtPartner` — referencia a pareja
- `courtAnchor` — punto base del cortejo
- `courtRole` (Tend vs Orbit) — basado en Gender de la pareja
- `courtAngle, courtRepathTimer` — ángulo orbital y refresh

**Métodos privados:**
- `TickOrbit()` — macho orbita en círculo, lookahead suave
- `TickTend()` — hembra elige puntos random cerca del anchor
- `FacePartner()` — ambos rotan para mirar a la pareja

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[MoriMochiContainer]], [[GameEvents]]
