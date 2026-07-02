---
tags: [combat, visualization, ui, presenter, numbers-pro]
---

# CombatDamageNumbers

MonoBehaviour presenter que responde a `CombatVisualEvents.OnPopup` y spawna números flotantes animados (package DamageNumbersPro). Una instancia en escena, suscrita al bus de eventos de visualización de combate.

## Responsabilidad

Convertir `CombatVisualPopup` eventos en instancias de `DamageNumber` animadas: posiciona el número, seteea el label (texto descriptivo por tipo), color via paleta, y habilita/desabilita el número según el tipo (Stun y Synergy solo texto, sin número de daño).

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `palette` | `CombatPopupPaletteSO` | Ref a SO de paleta de colores por tipo popup |
| `numberPrefab` | `DamageNumber` | Prefab del package DamageNumbersPro (customizable) |
| `spawnOffset` | `Vector3` | Offset local respecto a la posición del fighter (default 0, 1.5, 0) |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe `CombatVisualEvents.OnPopup += HandlePopup` |
| `OnDisable()` | Desuscribe `CombatVisualEvents.OnPopup -= HandlePopup` |
| `HandlePopup(CombatVisualPopup p)` | Spawna número, seteea texto/color/habilitación según `p.Kind` |
| `Label(CombatPopupKind kind)` | Static helper: retorna label localizado (Hit="Golpe", Crit="¡Crítico!", etc.) |

## Lógica de HandlePopup

1. `numberPrefab.Spawn(p.Position + spawnOffset, p.Amount)` — instancia número flotante
2. `enableNumber = (p.Kind != CombatPopupKind.Stun && p.Amount >= 0.5f)` — **(S32 updated)** Stun y Synergy solo muestran texto sin número
3. `enableTopText = true` + `topText = Label(p.Kind)` — etiqueta descriptiva
4. `dn.SetColor(palette.GetColor(p.Kind))` — color vía paleta

## Label (S32)

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
    CombatPopupKind.Synergy => "¡Sinergia!",  // S32
    _                      => "",
};
```

**NUEVO S32:** Label para `Synergy` = "¡Sinergia!".

## Cambios S32

**enableNumber condition:** Ahora exige `p.Amount >= 0.5f` **además** de `p.Kind != Stun`:
```csharp
dn.enableNumber = p.Kind != CombatPopupKind.Stun && p.Amount >= 0.5f;
```

Esto permite popups textuales sin número para Stun (ya estaba) y Synergy (nuevo S32). `CombatVisualizerService.RaiseProcPopup()` envía `Amount = 0f` para Synergy cuando el delta HP es menor a 0.5.

**Label Synergy:** Agregado mapeo `CombatPopupKind.Synergy → "¡Sinergia!"`.

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatVisualEvents]] — `OnPopup` evento, struct `CombatVisualPopup`
- [[CombatPopupPaletteSO]] — ref a instancia SO para obtener colores
- [[CombatVisualizerService]] — levanta popups via `CombatVisualEvents.Popup()`
- [[Enums]] — `CombatPopupKind` (incluye **Synergy** S32), `CombatVisualSide`

## Conexiones

**Entrada:**
- `CombatVisualEvents.OnPopup` — evento estático que dispara `HandlePopup()`

**Salida:**
- `DamageNumber.Spawn()` (package externo) — anima números flotantes
- (indirecto) UI visual en mundo para feedback de combate

## Notas

- Totalmente desacoplado de `CombatVisualizerService` via evento: no busca refs, no toca gameplay
- Null-checks defensivos: si prefab/palette nulos, retorna temprano
- Offsets y timing de animación viven en `DamageNumber` prefab (no toca este script)
- Los labels son españolizados: "Golpe", "¡Crítico!", "Veneno", "Quemadura", "Espinas", "Cura", "Regeneración", "Aturdido", **"¡Sinergia!"**
- **S32:** Popups textuales sin número para Stun y Synergy (efectos no-numéricos); Amount threshold 0.5 filtra ruido visual
