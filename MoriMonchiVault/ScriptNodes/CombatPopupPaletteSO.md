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

## Paleta Base (SetupDefaults)

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
| `Synergy` | (0.70, 0.42, 1.0) | Violeta **(NUEVO S32)** |

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatDamageNumbers]] — consume vía `palette.GetColor(kind)`
- [[CombatVisualEvents]] — propaga `CombatVisualPopup` con el `Kind`
- [[Enums]] — `CombatPopupKind` (Hit, Crit, Poison, Burn, Thorns, Heal, Regen, Stun, **Synergy**)

## Conexiones

**Entrada:**
- `CombatDamageNumbers.HandlePopup()` — llama `palette.GetColor(p.Kind)`

**Salida:**
- Ninguna (SO de datos puro)

## Cambios S32

**Entrada NUEVA en diccionario:** `Synergy = (0.70, 0.42, 1.0)` violeta para marcar popups de recetas de sinergias disparadas. Agregada automáticamente en botón `SetupDefaults()`.

## Notas

- La paleta default está baked en `SetupDefaults()`: Hit=blanco, Crit=dorado, Poison=verde-claro, Burn=naranja, Thorns=rojo, Heal=verde, Regen=verde suave, Stun=amarillo, **Synergy=violeta**
- Color.white es fallback seguro si `CombatPopupKind` no existe en el diccionario
- Se edita en inspector vía Odin: expandir dict, arrastrar colores por color picker
- **Backward compat:** Si SO antiguo no tiene Synergy, `GetColor()` retorna white y el popup sale sin color (no falla)
