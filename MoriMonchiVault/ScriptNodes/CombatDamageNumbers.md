---
tags: [combat, visualization, ui, presenter, numbers-pro]
---

# CombatDamageNumbers

MonoBehaviour presenter que responde a `CombatVisualEvents.OnPopup` y spawna números flotantes animados (package DamageNumbersPro). Una instancia en escena, suscrita al bus de eventos de visualización de combate.

## Responsabilidad

Convertir `CombatVisualPopup` eventos en instancias de `DamageNumber` animadas: posiciona el número, setea el label (texto descriptivo por tipo), color via paleta, sigue al luchador si `Follow != null`, y habilita/desabilita el número según el tipo.

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
| `HandlePopup(CombatVisualPopup p)` | Spawna número, setea texto/color/scale/follow según `p.Kind` |
| `ResolvePrefab(CombatPopupKind kind)` | Busca override en `prefabOverrides`, fallback a `numberPrefab` |
| `Label(CombatPopupKind kind)` | Static helper: retorna label localizado |

## Lógica de HandlePopup

```csharp
private void HandlePopup(CombatVisualPopup p)
{
    var prefab = ResolvePrefab(p.Kind);
    if (prefab == null) return;

    var dn = prefab.Spawn(p.Position + spawnOffset, p.Amount);
    if (dn == null) return;

    dn.enableNumber  = p.Kind != CombatPopupKind.Stun && p.Amount >= 0.5f;
    dn.enableTopText = true;
    dn.topText       = Label(p.Kind);

    if (palette != null) dn.SetColor(palette.GetColor(p.Kind));
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

**Flujo:**
1. `ResolvePrefab()` — busca override antes de usar `numberPrefab`
2. Spawn número en `p.Position + spawnOffset`
3. Habilita número si `Kind != Stun` y `Amount >= 0.5f` (casos textuales sin número)
4. Setea `topText = Label(p.Kind)` siempre
5. Colorea via paleta
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

Busca secuencialmente en `prefabOverrides` un match para `kind`. Si encuentra, retorna su `Prefab`. Si no, fallback a `numberPrefab`. Esto permite builds customizadas (ej: Poison usa prefab_poison con sprite de gota verde, Crit usa prefab_crit más grande).

## Label (S32+S35)

```csharp
private static string Label(CombatPopupKind kind) => kind switch
{
    CombatPopupKind.Hit       => "Golpe",
    CombatPopupKind.Crit      => "¡Crítico!",
    CombatPopupKind.Poison    => "Veneno",
    CombatPopupKind.Burn      => "Quemadura",
    CombatPopupKind.Thorns    => "Espinas",
    CombatPopupKind.Heal      => "Cura",
    CombatPopupKind.Regen     => "Regeneración",
    CombatPopupKind.Stun      => "Aturdido",
    CombatPopupKind.Synergy   => "¡Sinergia!",
    CombatPopupKind.Static    => "Static",       // S35
    CombatPopupKind.Pulse     => "Pulse",        // S35
    CombatPopupKind.Steel     => "Steel",        // S35
    CombatPopupKind.Mist      => "Mist",         // S35
    CombatPopupKind.Lifesteal => "Robo de vida", // S35
    _                         => "",
};
```

**S32:** Label para `Synergy` = "¡Sinergia!".

**S35:** 5 nuevos labels:
- `Static` = "Static" (texto plano, representa reducción de SPD)
- `Pulse` = "Pulse" (curación periódica, pero sin símbolo especial; se le pasa Amount)
- `Steel` = "Steel" (defensa aumentada)
- `Mist` = "Mist" (evasión aumentada)
- `Lifesteal` = "Robo de vida" (curación por daño infligido)

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

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatVisualEvents]] — `OnPopup` evento, struct `CombatVisualPopup`
- [[CombatPopupPaletteSO]] — ref a instancia SO para obtener colores
- [[CombatVisualizerService]] — levanta popups via `CombatVisualEvents.Popup()`, setea `Follow = FighterTransform()`
- [[Enums]] — `CombatPopupKind` (incluye Static, Pulse, Steel, Mist, Lifesteal S35)
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
- Los labels son españolizados: "Golpe", "¡Crítico!", "Veneno", etc.
- critScale serializado permite tuning visual del énfasis de crits
