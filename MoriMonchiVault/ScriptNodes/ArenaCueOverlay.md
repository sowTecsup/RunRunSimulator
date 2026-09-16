---
tags: [script, world, expedition, ui, cues]
---

# ArenaCueOverlay.cs

**Ruta:** `World/Expedition/ArenaCueOverlay.cs`

**Responsabilidad:** Presentación de guías visuales sobre terreno de arena. Dibuja percepción, rutas, retícula, plantilla de choque, minería, huida, custodia, ráfagas de habilidades. **S116:** Plantilla única sin ribbon. **S122:** Anillos de golpes zonales (Back/Wings) omiten ImpactRing (solo destello de borde de plantilla).

**Métodos públicos:**
- `void LateUpdate()` — dibuja todas las criaturas

**Delegación a CreatureCueDrawer:**
- `Telegraph()` — plantilla de choque con hitbox (disco) + cápsula de alcance
- **S122:** `AbilityBursts()` — omite habilidades de daño; solo destello del borde de plantilla
- **S122:** `CueState.HitSlot` (slot del `ClashMove` al impactar); `ImpactRing` solo si `HitSlot == ClashSlot.Horn`

**S122 Cambios:**
- `AbilityBursts()` filtra por `ability.Kind != Damage`
- ImpactRing no dibujado para golpes zonales (Back, Wings): solo borde de plantilla visible

**Vinculado a:** [[Index/24 - Puente Tienda-Arena]], [[Index/22 - Bajada Nocturna y Linaje]]

**Conexiones:** [[CreatureCueDrawer]], [[CueDrawer]], [[CueStyleSO]]
