---
tags: [combat, visualization, ui, presenter, numbers-pro]
---

# CombatDamageNumbers

MonoBehaviour presenter que responde a `CombatVisualEvents.OnPopup` y spawna números flotantes animados (package DamageNumbersPro). Una instancia en escena, suscrita al bus de eventos de visualización de combate. **S42:** Renderiza ReactionName + color custom para popups elementales (Kind Reaction). **S43:** Label "Escudo" para CombatPopupKind.Shield.

## Responsabilidad

Convertir `CombatVisualPopup` eventos en instancias de `DamageNumber` animadas: posiciona el número, setea el label (texto descriptivo por tipo), color via paleta o override custom, sigue al luchador si `Follow != null`, y habilita/desabilita el número según el tipo. **S42:** Soporte para popups de reacción con ReactionName custom + color de elemento. **S43:** Label "Escudo" para popups de escudo.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `palette` | `CombatPopupPaletteSO` | Ref a SO de paleta de colores por tipo popup |
| `numberPrefab` | `DamageNumber` | Prefab del package DamageNumbersPro (customizable) |
| `prefabOverrides` | `List<KindPrefabOverride>` | Sobrescrituras de prefab por tipo (Si Kind X existe, usar prefab Y en lugar de numberPrefab) |
| `spawnOffset` | `Vector3` | Offset local respecto a la posición del fighter (default: 0, 0.6, 0) |
| `critScale` | `float` | Escala de tamaño para crits (default 1.35) |

## Struct KindPrefabOverride

```csharp
[System.Serializable]
private struct KindPrefabOverride
{
    public CombatPopupKind Kind;
    public DamageNumber    Prefab;
}
```

Permite asignar un prefab diferente para cada tipo de popup (ej: Poison usa prefab_poison, Crit usa prefab_crit, etc.).

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe `CombatVisualEvents.OnPopup += HandlePopup` |
| `OnDisable()` | Desuscribe `CombatVisualEvents.OnPopup -= HandlePopup` |
| `HandlePopup(CombatVisualPopup p)` | Spawna número, setea texto/color/scale/follow según `p.Kind` (S42: con Text/OverrideColor, S43: Shield) |
| `ResolvePrefab(CombatPopupKind kind)` | Busca override en `prefabOverrides`, fallback a `numberPrefab` |
| `Label(CombatPopupKind kind)` | Static helper: retorna label localizado (S43: Shield → "Escudo") |

## Lógica de HandlePopup (S43 IGUAL A S42)

```csharp
private void HandlePopup(CombatVisualPopup p)
{
    var prefab = ResolvePrefab(p.Kind);
    if (prefab == null) return;

    var dn = prefab.Spawn(p.Position + spawnOffset, p.Amount);
    if (dn == null) return;

    dn.enableNumber  = p.Kind != CombatPopupKind.Stun && p.Amount >= 0.5f;
    dn.enableTopText = true;
    dn.topText       = string.IsNullOrEmpty(p.Text) ? Label(p.Kind) : p.Text;

    if (p.HasOverrideColor) dn.SetColor(p.OverrideColor);
    else if (palette != null) dn.SetColor(palette.GetColor(p.Kind));
    if (p.Follow != null) dn.SetFollowedTarget(p.Follow);

    if (p.Kind == CombatPopupKind.Heal || p.Kind == CombatPopupKind.Regen)
    {
        dn.enableLeftText = true;
        dn.leftText       = "+";
        dn.UpdateText();
    }

    if (p.Kind == CombatPopupKind.Crit) dn.SetScale(critScale);
}
```

**Flujo (sin cambios S43):**
1. `ResolvePrefab()` — busca override antes de usar `numberPrefab`
2. Spawn número en `p.Position + spawnOffset`
3. Habilita número si `Kind != Stun` y `Amount >= 0.5f` (casos textuales sin número)
4. Setea `topText = p.Text if not empty else Label(p.Kind)` — soporte para ReactionName custom + ahora Shield
5. Colorea via `p.OverrideColor` si `HasOverrideColor`, sino fallback a paleta
6. `SetFollowedTarget(p.Follow)` si Follow no es null — popups siguen al luchador dinámicamente
7. Para Heal/Regen: agrega `enableLeftText = "+"` (quirk de DamageNumbersPro)
8. Para Crit: escala por critScale

## ResolvePrefab Helper

```csharp
private DamageNumber ResolvePrefab(CombatPopupKind kind)
{
    foreach (var o in prefabOverrides)
    {
        if (o.Kind == kind && o.Prefab != null) return o.Prefab;
    }
    return numberPrefab;
}
```

Busca secuencialmente en `prefabOverrides` un match para `kind`. Si encuentra, retorna su `Prefab`. Si no, fallback a `numberPrefab`. Esto permite builds customizadas (ej: Poison usa prefab_poison con sprite de gota verde, Crit usa prefab_crit más grande, Shield con color azul).

## Label (S32+S35+S42+S43)

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
    CombatPopupKind.Shield => "Escudo",         // S43 NEW
    CombatPopupKind.Reaction => "¡Reacción!",   // S42
    _                      => "",
};
```

**S43:** Nuevo label para `Shield` = "Escudo" (popup de restauración de escudo mid-turno, post-defensa Protector).

## Cambios S32

**enableNumber condition:** Exige `p.Amount >= 0.5f` **además** de `p.Kind != Stun`:
```csharp
dn.enableNumber = p.Kind != CombatPopupKind.Stun && p.Amount >= 0.5f;
```

Esto permite popups textuales sin número para Stun y Synergy. `CombatVisualizerService.RaiseProcPopup()` envía `Amount = 0f` para Synergy cuando el delta HP es menor a 0.5.

## Cambios S34

**prefabOverrides List:** Permite presets de prefab por tipo. Útil para darle aspecto único a cada efecto (Poison = gota verde, Burn = llama roja, etc.).

**ResolvePrefab():** Helper que busca override antes de fallback a prefab default.

**SetFollowedTarget():** Popups ahora siguen al luchador si `p.Follow != null` (asignado por `CombatVisualizerService.FighterTransform()`).

**Heal/Regen con "+":** Quirk de DamageNumbersPro — los overloads `Spawn(pos, amount)` no activan `enableLeftText` automáticamente. Seteamos manualmente `"+"` y llamamos `UpdateText()`.

**SetScale(critScale):** Crits se escalan (default 1.35) para énfasis visual.

**spawnOffset bajado:** De (0, 1.5, 0) a (0, 0.6, 0) para acercarse más al luchador. El valor serializado en escena pisa este default.

## Cambios S35

**5 nuevos labels:** Static, Pulse, Steel, Mist, Lifesteal. Los primeros 4 (Static/Pulse/Steel/Mist) son principalmente textuales; Lifesteal muestra curación como "Robo de vida".

## Cambios S42

**Aditivos (backward compatible):**
- `CombatVisualPopup.Text` — texto custom para popups (p.ej. ReactionName)
- `CombatVisualPopup.OverrideColor` + `HasOverrideColor` — color custom para popups
- **Línea en HandlePopup:** `dn.topText = string.IsNullOrEmpty(p.Text) ? Label(p.Kind) : p.Text;` — prioriza p.Text si disponible
- **Línea en HandlePopup:** `if (p.HasOverrideColor) dn.SetColor(p.OverrideColor);` — prioriza OverrideColor si presente, sino palette
- **Nuevo Kind:** `CombatPopupKind.Reaction` con Label fallback "¡Reacción!" (pero normalmente p.Text lleva ReactionName real)
- **Integración CombatVisualizerService.PlayProc():** genera popup Reaction con Text = pe.ReactionName, OverrideColor = elemento.UiColor, HasOverrideColor = true

**Invariante:** Eventos 1v1 legacy siguen funcionando, labels viejos intactos.

## Cambios S43

**Aditivos (append-only):**
- **Nuevo Kind:** `CombatPopupKind.Shield` para popups de escudo (blue 4px track en barra world-space)
- **Nuevo Label:** "Escudo" (en switch Label)
- **Integración CombatVisualizerService:** PushShield/PushShieldAll emiten popup Shield con Amount = shield value cuando se restaura escudo post-turno o mid-golpe con DefenderShieldAfter

**Invariante:** Flujo de HandlePopup sin cambios; solo se agregó case Shield al switch.

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatVisualEvents]] — `OnPopup` evento, struct `CombatVisualPopup`
- [[CombatPopupPaletteSO]] — ref a instancia SO para obtener colores
- [[CombatVisualizerService]] — levanta popups via `CombatVisualEvents.Popup()`, setea `Follow = FighterTransform()`, **S42:** popups de reacción con ReactionName + OverrideColor, **S43:** popups de escudo via PushShield
- [[Enums]] — `CombatPopupKind` (incluye Reaction S42, Shield S43)
- [[DamageNumbersPro]] — package externo, API `Spawn(), SetColor(), SetFollowedTarget(), SetScale(), UpdateText()`

## Conexiones

**Entrada:**
- `CombatVisualEvents.OnPopup` — evento estático que dispara `HandlePopup()`

**Salida:**
- `DamageNumber.Spawn()` (package externo) — anima números flotantes
- UI visual en mundo para feedback de combate

## Notas

- Totalmente desacoplado de `CombatVisualizerService` via evento
- Null-checks defensivos: si prefab/palette nulos, retorna temprano
- `Follow` permite popups dinámicos (oscilan alrededor del luchador en lugar de quedar fijos en posición inicial)
- **Quirk Heal/Regen:** DamageNumbersPro `Spawn(pos, amount)` activa `enableNumber` pero no `enableLeftText` automáticamente; obligatorio settear `"+"` manualmente
- **prefabOverrides:** Extensible — agregar más KindPrefabOverride en inspector para customizar por tipo
- Los labels son españolizados: "Golpe", "¡Crítico!", "Veneno", "Escudo", etc.
- **S42:** Reacciones elementales priorizan texto custom (ReactionName) + color de elemento sobre labels/palette
- **S43:** Escudo muestra valor en popup (Amount = shield value, label "Escudo" + número azul)
- critScale serializado permite tuning visual del énfasis de crits
