---
tags: [combat, visualization, ui, scriptableobject, odin]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatPopupPaletteSO

ScriptableObject (Odin `SerializedScriptableObject`) que mapea cada tipo de popup de combate a su color de visualización. Centraliza la paleta visual para que `CombatDamageNumbers` dibuje con estilo consistente.

## Responsabilidad

Definir y exponer el diccionario `CombatPopupKind → Color` para el visualizador de popups flotantes. Una única instancia SO por proyecto, dragueada en inspector por `CombatDamageNumbers.palette`.

## Campos Públicos

| Campo | Tipo | Acceso | Descripción |
|-------|------|--------|-------------|
| `colors` | `Dictionary<CombatPopupKind, Color>` | privado [OdinSerialize] | Mapeo de tipo popup a color RGBA |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetColor(CombatPopupKind kind)` | `Color` | Retorna el color del tipo; fallback `Color.white` si no existe |

## Métodos Privados (Odin)

| Método | Descripción |
|--------|-------------|
| `SetupDefaults()` | Botón Odin [GUIColor verde] que crea el diccionario con paleta base (Hit blanco, Crit dorado, etc.) + 5 colores nuevos S35 |

## CreateAssetMenu

**Menu path:** `RunRunSimulator/Combat/Combat Popup Palette`  
**File name:** `CombatPopupPalette`

## Paleta Base (SetupDefaults) — S35

| Tipo | Color RGB | Descripción |
|------|-----------|-------------|
| `Hit` | (1.0, 1.0, 1.0) | Blanco |
| `Crit` | (1.0, 0.82, 0.23) | Dorado |
| `Poison` | (0.50, 0.82, 0.31) | Verde claro |
| `Burn` | (1.0, 0.48, 0.18) | Naranja |
| `Thorns` | (0.75, 0.31, 0.30) | Rojo oscuro |
| `Heal` | (0.31, 0.88, 0.48) | Verde brillante |
| `Regen` | (0.56, 0.88, 0.69) | Verde suave |
| `Stun` | (0.98, 0.85, 0.30) | Amarillo |
| `Synergy` | (0.70, 0.42, 1.0) | Violeta (S32) |
| `Static` | (0.35, 0.85, 1.0) | Azul ciano **(S35)** |
| `Pulse` | (1.0, 0.55, 0.75) | Rosa/magenta **(S35)** |
| `Steel` | (0.62, 0.68, 0.78) | Gris azulado **(S35)** |
| `Mist` | (0.72, 0.87, 0.95) | Azul claro **(S35)** |
| `Lifesteal` | (0.85, 0.25, 0.45) | Rojo rosa **(S35)** |

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatDamageNumbers]] — consume vía `palette.GetColor(kind)`
- [[CombatVisualEvents]] — propaga `CombatVisualPopup` con el `Kind`
- [[Enums]] — `CombatPopupKind` (Hit, Crit, Poison, Burn, Thorns, Heal, Regen, Stun, Synergy, Static, Pulse, Steel, Mist, Lifesteal)

## Conexiones

**Entrada:**
- `CombatDamageNumbers.HandlePopup()` — llama `palette.GetColor(p.Kind)`
- `MoriMonchiCombatVisualizerUITK.Apply()` — llama `palette.GetColor(MapKind(mark.Kind))` para colorear chips

**Salida:**
- Ninguna (SO de datos puro)

## Cambios S32

**Entrada NUEVA en diccionario:** `Synergy = (0.70, 0.42, 1.0)` violeta para marcar popups de recetas de sinergias disparadas. Agregada automáticamente en botón `SetupDefaults()`.

## Cambios S35

**5 NUEVAS entradas en diccionario:**
- `Static = (0.35, 0.85, 1.0)` — azul ciano para visualización de stacks de Static
- `Pulse = (1.0, 0.55, 0.75)` — rosa/magenta para Pulse (curación periódica)
- `Steel = (0.62, 0.68, 0.78)` — gris azulado para Steel (defensa)
- `Mist = (0.72, 0.87, 0.95)` — azul claro para Mist (evasión)
- `Lifesteal = (0.85, 0.25, 0.45)` — rojo rosa para Lifesteal (robo de vida)

Todas agregadas automáticamente en botón `SetupDefaults()`.

## Notas

- La paleta default está baked en `SetupDefaults()`: Hit=blanco, Crit=dorado, Poison=verde-claro, Burn=naranja, Thorns=rojo, Heal=verde, Regen=verde suave, Stun=amarillo, Synergy=violeta, Static=ciano, Pulse=rosa, Steel=gris-azul, Mist=azul-claro, Lifesteal=rojo-rosa
- Color.white es fallback seguro si `CombatPopupKind` no existe en el diccionario
- Se edita en inspector vía Odin: expandir dict, arrastrar colores por color picker
- **Backward compat:** Si SO antiguo no tiene nuevas entradas (S35), `GetColor()` retorna white y el popup sale sin color (no falla)
