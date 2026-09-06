---
tags: [script, world, expedition, ui-overlay, visualization]
---

# ArenaRoomCueOverlay.cs

**Ruta:** `World/Expedition/ArenaRoomCueOverlay.cs`

**Responsabilidad:** Dibuja guías visuales en overlay de la sala: minerales (discos animados) y salidas (anillos con dasheado giratorio). Antes vivía integrado en ArenaCueOverlay; ahora es colaborador separado centrado en elementos estáticos de la sala (no rutas de agentes).

## Campos Serializados

- `sandbox` (ArenaSandbox, Required) — referencia a gestor de arena
- `cueMaterial`, `additiveMaterial` (Material, Required) — materiales para CueDrawer
- `style` (CueStyleSO, Required) — tuning de apariencia
- `showMinerals`, `showExits` (bool, default true) — toggles de visualización

## Caches Privados

- `mineralAnims` (Dictionary<MaterialPickup, MineralAnim>) — estado de animación (alpha) por mineral
- `mineralQueryBuffer` (List<Perceivable>) — reutilizado, no-alloc para QueryInRadius
- `mineralLookup` (Dictionary<Perceivable, MaterialPickup>) — caché de GetComponent<MaterialPickup>

## Struct MineralAnim

```csharp
class MineralAnim { public float Alpha; }
```

Guarda alpha animado del mineral (fade in/out según Taken).

## Ciclo de Vida

**OnEnable():**
- CueDrawer.Configure(cueMaterial, additiveMaterial) — inicializa materiales globales

**LateUpdate():**
- Si showMinerals: DrawMinerals()
- Si showExits: DrawExits()

## Métodos Privados

**DrawMinerals():**
1. QueryInRadius(sandbox.position, 200m, null, mineralQueryBuffer) → todos los Perceivable cercanos
2. Para cada Perceivable con Kind=Material:
   - GetMineralAnim(p) → caché alpha
   - GetMineralPickup(p) → MaterialPickup
   - Anima alpha: target = p.Taken ? 0 : 1
   - Alpha = MoveTowards(..., AppearSeconds)
   - Si alpha > 0.01:
     - Dibuja disco animado con CueDrawer.Disc (inner + outer alpha)
     - Dibuja anillo: CueDrawer.Ring
     - Si Value > 1 (multi): anillo dasheado giratorio CueDrawer.DashedRing

**DrawExits():**
- Itera sandbox.Exits (lista de salidas)
- Para cada exit:
  - Color según team (FriendColor si Player, FoeColor si Rival)
  - Disc(center, radius, color, ExitAlpha, 0)
  - Ring(center, radius, ExitRingThickness, ringColor)
  - DashedRing(dasheado giratorio a velocidad RingSpinSpeed * 0.5)

## Invariantes S102

- **Query radius 200m:** captura todos los minerales en la sala (asume sala ≤ 40x40)
- **Filtro por Kind:** solo Perceivables con Kind=Material
- **Caché de anim:** MineralAnim se reutiliza por instancia de mineral (limpia si mineral destruido)
- **Heights:** todos los dibujos se elevan HeightOffset (eje Y)
- **Anillo dasheado giratorio:** rotación Time.time * -/+ RingSpinSpeed crea efecto de movimiento

## Conexiones

- [[PerceivableRegistry]] (QueryInRadius obtiene minerales)
- [[MaterialPickup]] (lee Taken, Value, Remaining)
- [[CueDrawer]] (dibuja discos, anillos, dasheado)
- [[CueStyleSO]] (tuning de colores y tamaños)
- [[ArenaSandbox]] (Exits, transform.position)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
