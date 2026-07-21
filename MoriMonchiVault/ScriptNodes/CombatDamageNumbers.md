---
tags: [combat, visualization, ui, presenter, numbers-pro]
---

# CombatDamageNumbers

MonoBehaviour presenter que responde a `CombatVisualEvents.OnPopup` y spawna números flotantes animados (package DamageNumbersPro). Una instancia en escena, suscrita al bus de eventos de visualización de combate. **S58:** Campos `spawnOffset`, `popupScale`, `popupLifetime` serializables para tuning por replay 3v3. Crits multiplican escala base. **S59d:** Popups minimalistas (Hit/Crit/Heal/Regen sin topText, solo número con signo "−" o "+"). Crit gana outline dorado (critOutlineWidth/critOutlineColor TMP). Montos se redondean con RoundToInt; NumericKinds que redondean a 0 NO se emiten (fix del "+0"). Asset DamageNumbersPro Basic-Glow.asset: blanco de popups es _FaceColor HDR ~6x material, corregido en el asset, no en código.

## Responsabilidad

Convertir `CombatVisualPopup` eventos en instancias de `DamageNumber` animadas: posiciona el número, setea el label (texto descriptivo por tipo, oculto para Hit/Crit/Heal/Regen), color via paleta o override custom, sigue al luchador si `Follow != null`, habilita/desabilita el número según el tipo, y ajusta escala para crits con outline.

## Campos Serializados

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `palette` | `CombatPopupPaletteSO` | - | Ref a SO de paleta de colores por tipo popup |
| `numberPrefab` | `DamageNumber` | - | Prefab del package DamageNumbersPro |
| `prefabOverrides` | `List<KindPrefabOverride>` | - | Sobrescrituras de prefab por tipo |
| `spawnOffset` | `Vector3` | (0, 0.35, 0) | **S58** Offset del spawn respecto a luchador |
| `critScale` | `float` | 1.35 | **S59d** Multiplicador escala para crits (conservado desde S58) |
| `popupScale` | `float` | 0.7 | **S58** Escala base de números |
| `popupLifetime` | `float` | 2.5 | **S58** Duración animación (segundos) |
| `critOutlineWidth` | `float` | 0.25 | **S59d NEW** Ancho outline TMP para crits |
| `critOutlineColor` | `Color` | (FF, C8, 30, FF) | **S59d NEW** Color outline dorado para crits |

## Struct KindPrefabOverride

```csharp
[System.Serializable]
private struct KindPrefabOverride
{
    public CombatPopupKind Kind;
    public DamageNumber    Prefab;
}
```

Permite asignar prefab diferente para cada tipo de popup.

## Set NumericKinds (S59d NEW)

```csharp
private static readonly HashSet<CombatPopupKind> NumericKinds = new HashSet<CombatPopupKind>
{
    CombatPopupKind.Hit, CombatPopupKind.Crit, CombatPopupKind.Heal, CombatPopupKind.Regen,
    CombatPopupKind.Poison, CombatPopupKind.Burn, CombatPopupKind.Thorns, CombatPopupKind.Lifesteal,
};
```

Kinds que tienen montos numéricos. Si redondean a 0, **no se emiten** (evita "+0" espurio).

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe `CombatVisualEvents.OnPopup += HandlePopup` |
| `OnDisable()` | Desuscribe `CombatVisualEvents.OnPopup -= HandlePopup` |
| `HandlePopup(CombatVisualPopup p)` | **S59d:** Redondea monto (RoundToInt), descarta NumericKinds=0. Setea texto/color/scale/lifetime/follow según p.Kind. Hit/Crit/Heal/Regen: oculta topText, habilita leftText ("−" o "+"). Crit: outline dorado. |
| `ResolvePrefab(CombatPopupKind kind)` | Busca override, fallback a numberPrefab |
| `Label(CombatPopupKind kind)` | Static helper: retorna label localizado |

## Lógica de HandlePopup (S59d)

```csharp
private void HandlePopup(CombatVisualPopup p)
{
    var prefab = ResolvePrefab(p.Kind);
    if (prefab == null) return;

    int rounded = Mathf.RoundToInt(p.Amount);
    if (NumericKinds.Contains(p.Kind) && rounded <= 0) return;  // S59d: skip 0-damage

    var dn = prefab.Spawn(p.Position + spawnOffset, rounded);
    if (dn == null) return;

    dn.lifetime = popupLifetime;
    dn.SetScale(popupScale);

    dn.enableNumber = p.Kind != CombatPopupKind.Stun && rounded >= 1;

    // S59d: Hit/Crit/Heal/Regen sin topText (minimalista)
    bool suppressTopText = p.Kind == CombatPopupKind.Hit || p.Kind == CombatPopupKind.Crit
        || p.Kind == CombatPopupKind.Heal || p.Kind == CombatPopupKind.Regen;
    dn.enableTopText = !suppressTopText;
    if (!suppressTopText) dn.topText = string.IsNullOrEmpty(p.Text) ? Label(p.Kind) : p.Text;

    if (p.HasOverrideColor) dn.SetColor(p.OverrideColor);
    else if (palette != null) dn.SetColor(palette.GetColor(p.Kind));
    if (p.Follow != null) dn.SetFollowedTarget(p.Follow);

    // S59d: Hit/Crit usan "−"; Heal/Regen usan "+"
    if (p.Kind == CombatPopupKind.Hit || p.Kind == CombatPopupKind.Crit)
    {
        dn.enableLeftText = true;
        dn.leftText       = "-";
        dn.UpdateText();
    }
    else if (p.Kind == CombatPopupKind.Heal || p.Kind == CombatPopupKind.Regen)
    {
        dn.enableLeftText = true;
        dn.leftText       = "+";
        dn.UpdateText();
    }

    if (p.Kind == CombatPopupKind.Crit)
    {
        dn.SetScale(popupScale * critScale);
        foreach (var text in dn.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            text.outlineWidth = critOutlineWidth;  // S59d: outline TMP
            text.outlineColor = critOutlineColor;
        }
    }
}
```

**Flujo S59d:**
1. ResolvePrefab() — busca override
2. **Redondea** monto a entero
3. **Descarta si NumericKind y redondeado ≤ 0** (evita "+0")
4. Spawn número en `p.Position + spawnOffset`
5. **S58:** setea `lifetime = popupLifetime`, base `SetScale(popupScale)`
6. Habilita número si `Kind != Stun` y `rounded >= 1`
7. **S59d:** Suprime topText para Hit/Crit/Heal/Regen (minimalista)
8. Setea `topText = p.Text if not empty else Label(p.Kind)` (si no suprimido)
9. Colorea via `p.OverrideColor` o fallback a paleta
10. SetFollowedTarget si `p.Follow != null`
11. **S59d:** Hit/Crit/Heal/Regen: agrega leftText ("−" o "+") → UpdateText()
12. **S59d:** Crit: multiplica por critScale → `popupScale * critScale` (escala final) y aplica outline dorado TMP

## Label (Todos los Kind)

```csharp
private static string Label(CombatPopupKind kind) => kind switch
{
    CombatPopupKind.Hit    => "Golpe",
    CombatPopupKind.Crit   => "¡Crítico!",
    CombatPopupKind.Poison => "Veneno",
    CombatPopupKind.Burn   => "Quemadura",
    CombatPopupKind.Thorns => "Espinas",
    CombatPopupKind.Heal   => "Cura",
    CombatPopupKind.Regen  => "Regeneración",
    CombatPopupKind.Stun   => "Aturdido",
    CombatPopupKind.Synergy => "¡Sinergia!",
    CombatPopupKind.Static    => "Static",
    CombatPopupKind.Pulse     => "Pulse",
    CombatPopupKind.Steel     => "Steel",
    CombatPopupKind.Mist      => "Mist",
    CombatPopupKind.Lifesteal => "Robo de vida",
    CombatPopupKind.Shield => "Escudo",
    CombatPopupKind.Reaction => "¡Reacción!",
    _                      => "",
};
```

**S59d:** Labels solo se usan si `!suppressTopText`. Hit/Crit/Heal/Regen ocultan etiqueta, muestran solo número + signo.

## Cambios S58

**Campos nuevos serializables:**
- `spawnOffset` — offset del spawn (era hardcoded en S34 a 0,0.6,0; ahora configurable)
- `popupScale` (0.7) — escala base más pequeña para 3v3 (menos clutter visual)
- `popupLifetime` (2.5) — duración más larga para soportar replay a distintas velocidades

**Crits retipados:**
- **Antes:** `dn.SetScale(critScale)` — escala absoluta
- **Ahora:** `dn.SetScale(popupScale * critScale)` — escala base multiplicada
- Permite ajustar tamaño general sin romper enfasis de crit

## Cambios S59d (Minimalismo, outline dorado, descarta 0s)

**Popup minimalista para core kinds:**
- Hit, Crit, Heal, Regen: **sin topText** (solo número)
- Otros kinds (Poison, Burn, Thorns, Lifesteal, Stun, Shield, etc.): topText via Label() o custom

**Redondeo y descarte de 0s:**
- Línea 39: `int rounded = Mathf.RoundToInt(p.Amount)` — redondea flats
- Línea 40: `if (NumericKinds.Contains(p.Kind) && rounded <= 0) return;` — **evita "+0" espurio**
- Aplicable a: Hit, Crit, Heal, Regen, Poison, Burn, Thorns, Lifesteal
- Resultado: popups limpios, sin ruido visual

**Signo visual (leftText):**
- Hit/Crit: `leftText = "-"` (daño, prefijo negativo)
- Heal/Regen: `leftText = "+"` (curación, prefijo positivo)
- **S59d:** Símbolo directo, no "Golpe" / "¡Crítico!" / "Cura" / "Regeneración"

**Crit outline dorado (S59d NEW):**
- Línea 22–23: campos `critOutlineWidth` (0.25) y `critOutlineColor` (naranja dorado #FFC830)
- Línea 72–79: en HandlePopup, si Crit:
  ```csharp
  foreach (var text in dn.GetComponentsInChildren<TMPro.TMP_Text>(true))
  {
      text.outlineWidth = critOutlineWidth;
      text.outlineColor = critOutlineColor;
  }
  ```
- TMP outline aplica a números y leftText, realza visualmente el crit

**Quirk documentable (S59d):**
- Asset DamageNumbersPro Basic-Glow.asset: blanco de los popups es _FaceColor HDR ~6x del material
- **Corregido en el asset**, no en código
- Permite que popups sean legibles a distancia sin ajuste de scale

## Vinculado a

- [[Index/03 - Combat System]]
- [[CombatVisualEvents]] — OnPopup evento
- [[CombatVisualizerService]] — emite popups via OnPopup
- [[DamageNumbersPro]] — package externo; implementa DamageNumber.Spawn(), lifetime, SetScale(), SetFollowedTarget()
- [[CombatPopupPaletteSO]] — define colores por kind

## Conexiones

**Entrada:** `CombatVisualEvents.OnPopup(CombatVisualPopup p)` → `HandlePopup(CombatVisualPopup p)`

**Salida:** `DamageNumber` animadas en mundo

## Notas S58–S59d

- Desacoplado de CombatVisualizerService via evento
- Null-checks defensivos
- Follow permite popups dinámicos alrededor del luchador
- prefabOverrides extensible
- Labels españolizados (solo para kinds no-minimalistas)
- **S58:** popupScale y popupLifetime configurables por replay (velocidad variable)
- **S59d:** Popups minimalistas (−123 vs. "−123 Golpe") reducen clutter y mejoran legibilidad
- **S59d:** Descarte de 0s (NumericKinds) evita feedback falso/ruido visual
- **S59d:** Crit outline dorado (TMP) realza críticos sin necesidad de escala extra
- **S59d:** Redondeo consistente (RoundToInt) evita "+0.3" impreciso
