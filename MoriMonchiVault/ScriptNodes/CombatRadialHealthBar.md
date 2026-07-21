---
tags: [script, combat-visual]
---

# CombatRadialHealthBar.cs

**Ruta:** `Systems/CombatVisualizer/CombatRadialHealthBar.cs`

**Responsabilidad:** Anillo radial world-space del pedestal que muestra vida y escudo en combate. Genera sprites de anillos por código (Radial360 fill). Vida rojo/verde gradient espejado (Right/Left), escudo dualizado en capas de 10 (azul/púrpura/magenta). **S59d:** Barra SIEMPRE visible en estado fino (material UI/Default, depth-tested, ocluida por cuerpos). Al hover: sprite grueso + material overlay ZTest Always + label de números + punch de escala. Hover matemático EXCLUSIVO closest-wins: lista estática de instancias + RecomputeMathWinner una vez por frame elige solo el anillo más cercano al ray del mouse, hoverRadius default 0.45. Orientación FIJA: SetFacingTarget aplica yaw una sola vez al spawn vía ApplyFixedFacing, ya no sigue rotación del MM por frame. Juice: flash blanco en daño, drain ease-out, ghost fill rezagado, shake amortiguado, punch de escala. SnapHp() inmediato para Restore. Lectura de ratón via UnityEngine.InputSystem.Mouse (Input.mousePosition tiraba InvalidOperationException con Input System exclusivo). Identidad nueva: Bind(side, index) — solo renderiza si hasIdentity=true.

## API Pública

| Método | Parámetros | Descripción |
|--------|-----------|-------------|
| `Bind(side, index)` | `CombatVisualSide, int` | Vincula identidad de unidad; reset inmediato de HP/shield/juice |
| `SetHp(current, max)` | `float, float` | Anima cambio de HP (daño → flash+drain, curación → heal ease-out) |
| `SnapHp(current, max)` | `float, float` | Setea HP inmediato sin animación (usado por Restore) |
| `SetShield(shield)` | `float` | Aplica escudo (0 → oculta, layer/rem por 10) |
| `SetActiveTurn(value)` | `bool` | No-op (legacy) |
| `SetTargeted(value)` | `bool` | No-op |
| `SetFacingTarget(target)` | `Transform` | Aplica yaw del target UNA sola vez vía ApplyFixedFacing; no sigue rotación por frame |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `ringScale` | `float` | Escala del canvas world-space (default 1.6) |
| `facingAngleOffset` | `float` | Offset yaw al rotar hacia target (default 0) |
| `hoverRadius` | `float` | Radio de hoverable alrededor unit (default 0.45); **S59d** reduced from 0.9 |
| `hoverHeight` | `float` | Altura del segmento vertical para raycast (default 1.4) |
| `flashSeconds` | `float` | Duración flash blanco en daño (default 0.12) |
| `drainSeconds` | `float` | Ease-out lerp HP en daño (default 0.3) |
| `ghostColor` | `Color` | Color del ghost fill rezagado (default 1,1,1,0.6) |
| `ghostDelay` | `float` | Delay antes de lerpar ghost (default 0.35) |
| `ghostSeconds` | `float` | Duración lerp ghost (default 0.5) |
| `shakeAmplitude` | `float` | Amplitud del shake (escalado por damage/maxHp) (default 0.08) |
| `shakeSeconds` | `float` | Duración del shake amortiguado (default 0.35) |
| `punchSeconds` | `float` | Duración del punch de escala (default 0.2) |
| `shieldLayerColors` | `Color[]` | Tres colores de capas escudo (azul/púrpura/magenta) |

## Estados Internos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `hasIdentity` | `bool` | Si Bind() fue llamado |
| `side`, `index` | `CombatVisualSide`, `int` | Identidad bindeada |
| `mathHover`, `externalHover` | `bool` | Hover local (raycast matemático) y externo (OnUnitHover) |
| `hoverActive` | `bool` | Estado actual visible (mathHover \|\| externalHover) |
| `currentHp`, `maxHp` | `float` | HP actual/máximo |
| `currentShield` | `float` | Escudo actual (entero para layer/rem) |
| `facingTarget` | `Transform` | Referencia al target para orientación fija |

## Estructura: Canvas + Capas

```
RadialHealthBarCanvas (WorldSpace, siempre visible; material cambia en hover)
├─ Track (fondo oscuro 1.05x)
├─ HpGhostRight/Left (anillo rezagado, ghostColor, Filled Radial360 Right/Left 50%)
├─ HpFillRight/Left (anillo principal, rojo→verde, Filled Radial360 Right/Left 50%)
├─ ShieldUnderRight/Left (capa anterior si layer>0, Filled 50%)
├─ ShieldFillRight/Left (capa actual, coloreado por layer, Filled (rem/10)*50%)
├─ Ticks (marcas oscuras cada 36°)
└─ HpLabel (texto central "XXX / YYY", oculto excepto en hover)
```

**S59d cambio:** Material default=UI/Default (depth-tested, ocluido por cuerpos). En hover: material=MoriMonchi/UIRingOverlay (ZTest Always, siempre visible). Label oculto en estado fino, mostrado solo en hover.

Fill espejado (Right clockwise, Left counter-clockwise) para converger en hocico. Sprite grueso en hover (ThickRingInner 0.70) vs fino (ThinRingInner 0.86).

## Métodos Privados Clave

| Método | Descripción |
|--------|-------------|
| `Update()` | UpdateHover() (raycast), SetHoverState(hoverActive) |
| `DamageRoutine()` | Flash blanco, drain ease-out, label lerp |
| `HealRoutine()` | Heal ease-out sin flash, label lerp |
| `GhostRoutine()` | Rezago visual: ghost_pct delay+ease ghost desde old→new |
| `ShakeRoutine()` | Shake amortiguado (damp = 1-t/shakeSeconds), offset random |
| `PunchRoutine()` | Punch de escala 1→1.12→1 (scale * ringScale/100) |
| `UpdateHover()` | RecomputeMathWinner() una vez por frame (mathWinnerFrame check) |
| `RecomputeMathWinner()` | **S59d** Static. Ray↔segmento vertical para todas las instancias. Ganador = dist mínima ≤ hoverRadius |
| `SetHoverState(hover)` | Sprite grueso si hover, fino si no. Material overlay si hover, default si no. Label visible si hover. Punch en enter. |
| `ApplyFixedFacing()` | **S59d** NUEVO. Aplica yaw del target UNA sola vez al canvas (no por frame). Rotación euclidiana: 90° pitch (forward), yaw del target, 0° roll. |
| `ApplyShield()` | Renderiza escudo: layer=(s-1)/10, rem=s-layer*10, colorea por layer, under si layer>0 |

## Juice Detallado

**Daño:**
1. Flash: base_color → blanco (half flash_secs) → base_color (half flash_secs)
2. Drain: hp_pct lerp ease-out (drainSeconds), label numeral easing
3. Ghost: delay (ghostDelay), luego lerp old_pct→new_pct ease-in-out (ghostSeconds) — visual rezago
4. Shake: amplitud *= damage/maxHp*4 (clamp01), amortiguado (damp = 1-t/shakeSeconds)
5. Punch: escala 1→1.12→1 (punchSeconds)

**Curación:**
- Sin flash, heal directo ease-out (drainSeconds), label numeral

**Hover:**
- Sprite ThinRing → ThickRing (visual pop)
- Material UI/Default → MoriMonchi/UIRingOverlay
- Label oculto → visible
- Punch de escala 1→1.12→1

## Cambios S59d (Visibilidad siempre encendida, hover como overlay)

**Barra siempre visible en estado fino:**
- Línea 38: `private static Material defaultMaterial;` — UI/Default con depth-test
- Línea 40-42: `private static readonly List<CombatRadialHealthBar> instances` + `mathWinner` + `mathWinnerFrame` para singleton raycast

**Hover closest-wins (raycast matemático):**
- Línea 98-108: `Update()` llama `UpdateHover()` una sola vez por frame
- Línea 428-436: `UpdateHover()` chequea `Time.frameCount != mathWinnerFrame`, llama `RecomputeMathWinner()` si es nueva, setea `mathHover`
- Línea 438-463: `RecomputeMathWinner()` static, itera todas las instancias, calcula `ClosestDistanceRayToSegment()` para cada una, gana la más cercana ≤ hoverRadius
- Línea 84-88: `OnEnable()` agrega a `instances`; `OnDisable()` remueve

**Orientación fija (SetFacingTarget):**
- Línea 188-192: `SetFacingTarget(Transform target)` guarda referencia, llama `ApplyFixedFacing()` UNA sola vez
- Línea 422-426: `ApplyFixedFacing()` NUEVO: calcula yaw del target, aplica al canvas Quaternion.Euler(90, target.eulerAngles.y + offset, 0)
- **NO** hay Update() que re-aplique facing cada frame

**SetHoverState visual:**
- Línea 465-492: `SetHoverState(bool hover)` alterna sprite (Thick/Thin), material (overlay/default), label visibility (hover=show), punch en enter
- Línea 467-468: `Sprite ringSpr = hover ? GetThickRingSprite() : GetThinRingSprite()`
- Línea 468: `Material mat = hover ? overlayMaterial : defaultMaterial`
- Línea 481-485: `if (hpLabel != null) { ... hpLabel.gameObject.SetActive(hover); }`

## Vinculado a

- [[Index/03 - Combat System]]
- [[CombatVisualUnits]] — Spawn en línea 79-80, setea facingTarget
- [[CombatVisualizerService]] — PushHp() → SetHp/SnapHp
- [[CombatVisualEvents]] — suscriptor OnUnitHover (línea 86)
- [[CombatOrderBarUITK]] — emite OnUnitHover al registrar PointerEnter/Leave

## Notas S59d

- Hover ahora es dual: raycast local (matemático, estático, instance winner) + evento externo (OnUnitHover). Ambos ponen hoverActive=true → SetHoverState.
- Orientación FIJA: SetFacingTarget() aplica yaw UNA sola vez al spawn. No hay rotación por frame ni rastreo de MM.
- Identidad requerida: Bind() debe ser llamado desde CombatVisualUnits.Spawn() antes de usar.
- Fill espejado visual (Right/Left): misma data, solo representación bonita que converge.
- Escudo: layer system escalable a múltiples capas (3 colores rotativos).
- Input System: hazard resuelto con Mouse.current en lugar de Input.mousePosition.
- hoverRadius default 0.45 (reduced from 0.9 S59a) para zona más ajustada.
