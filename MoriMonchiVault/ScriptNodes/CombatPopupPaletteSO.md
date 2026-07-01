---
tags: [combat, visualization, ui, scriptableobject, odin]
---

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
| `SetupDefaults()` | Botón Odin [GUIColor verde] que crea el diccionario con paleta base (Hit blanco, Crit dorado, etc.) |

## CreateAssetMenu

**Menu path:** `RunRunSimulator/Combat/Combat Popup Palette`  
**File name:** `CombatPopupPalette`

## Vinculado a

- [[CombatDamageNumbers]] — consume vía `palette.GetColor(kind)`
- [[CombatVisualEvents]] — propaga `CombatVisualPopup` con el `Kind`
- [[Enums]] — `CombatPopupKind` (Hit, Crit, Poison, Burn, Thorns, Heal, Regen, Stun)

## Conexiones

**Entrada:**
- `CombatDamageNumbers.HandlePopup()` — llama `palette.GetColor(p.Kind)`

**Salida:**
- Ninguna (SO de datos puro)

## Notas

- La paleta default está baked en `SetupDefaults()`: Hit=blanco, Crit=dorado, Poison=verde-claro, Burn=naranja, Thorns=rojo, Heal=verde, Regen=verde suave, Stun=amarillo
- Color.white es fallback seguro si `CombatPopupKind` no existe en el diccionario
- Se edita en inspector vía Odin: expandir dict, arrastrar colores por color picker
