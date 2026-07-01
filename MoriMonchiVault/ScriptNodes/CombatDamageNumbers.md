---
tags: [combat, visualization, ui, presenter, numbers-pro]
---

# CombatDamageNumbers

MonoBehaviour presenter que responde a `CombatVisualEvents.OnPopup` y spawna números flotantes animados (package DamageNumbersPro). Una instancia en escena, suscrita al bus de eventos de visualización de combate.

## Responsabilidad

Convertir `CombatVisualPopup` eventos en instancias de `DamageNumber` animadas: posiciona el número, seteea el label (texto descriptivo por tipo), color via paleta, y habilita/desabilita el número según el tipo (Stun solo texto).

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
2. `enableNumber = (p.Kind != CombatPopupKind.Stun)` — Stun solo muestra texto
3. `enableTopText = true` + `topText = Label(p.Kind)` — etiqueta descriptiva
4. `dn.SetColor(palette.GetColor(p.Kind))` — color vía paleta

## Vinculado a

- [[CombatVisualEvents]] — `OnPopup` evento, struct `CombatVisualPopup`
- [[CombatPopupPaletteSO]] — ref a instancia SO para obtener colores
- [[CombatVisualizerService]] — levanta popups via `CombatVisualEvents.Popup()`
- [[Enums]] — `CombatPopupKind`, `CombatVisualSide`

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
- Los labels son españolizados: "Golpe", "¡Crítico!", "Veneno", "Quemadura", "Espinas", "Cura", "Regeneración", "Aturdido"
