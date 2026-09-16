---
tags: [script, world, visualization, expedition, util]
---

# CreatureCueDrawer.cs

**Ruta:** `World/Expedition/CreatureCueDrawer.cs` (clase estática)

**Responsabilidad:** Librería de métodos estáticos para dibujar guías visuales de criaturas individuales. **S116:** Plantilla única = disco + anillo de impacto por golpe. **S122:** ImpactRing solo para Horn; golpes zonales (Back/Wings) omiten anillo.

**Métodos Públicos:**

- `Telegraph(CueStyleSO style, MoriMonchiController controller, Vector3 origin, float alpha, bool flash)` — plantilla de choque
  - Cápsula (Range) + disco (SweepRadius para Back, HitRadius para Wings)
  - Pista + relleno lineal por Tell01 + borde
  - Destello al entrar Striking

- `ImpactRing(CueStyleSO style, MoriMonchiController controller, Vector3 center, float radius, float alpha, bool flash)` — **(S122)** Dibujado solo para Horn
  - Back/Wings: omitido (solo flash de borde de plantilla)

- `AbilityBursts(...)` — **(S122)** Filtra habilidades de daño; solo ráfagas de buff/debuff

**S116 Cambios:**
- Telegraph como autoridad única de plantilla
- ImpactRing introducido para Horn

**S122 Cambios:**
- **ImpactRing omitido para Back/Wings** (solo flash de plantilla)
- **AbilityBursts filtra Damage** (solo buffs/debuffs)

**Vinculado a:** [[Index/24 - Puente Tienda-Arena]]

**Conexiones:** [[ArenaCueOverlay]], [[CueDrawer]], [[CueStyleSO]]
