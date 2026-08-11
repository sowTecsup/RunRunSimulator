---
tags: [script, debug, dev-tools]
---

# GeneticsLabPreview.cs

**Ruta:** `Core/GeneticsLabPreview.cs`

**Responsabilidad:** Panel de debug Odin para previsualizador de genética. **S75:** Genera DNAs aleatorias con 5 slots (Body/Horn/Back/Wing/Face), parsea genetic string format "BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB", valida partes en database.

## Métodos Privados (Buttons)

| Método | Descripción |
|--------|-------------|
| `GenerateRandomCreature()` | Genera DNA vía `CreatureGenerator.GenerateRandom()` uniforme, display en inspector |
| `LoadFromID()` | Parsea DNA string format "BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB", carga, valida |
| `ValidateDNA()` | Logea si cada parte existe en database |

## Cambios en S75

**Genetic string format ACTUALIZADO:**
- Antes: `BODYSHAPE-ARM-EYE-MOUTH-RRGGBB` (4 partes)
- Ahora: `BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB` (5 partes)

**Campos breakdown:**
- Antes: rarityBodyShape, rarityArms, rarityEyes, rarityMouth
- Ahora: rarityBodyShape, rarityHorns, rarityBacks, rarityWings, rarityFaces

## Vinculado a

- [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[GameManager]], [[CreatureGenerator]], [[CreatureDatabaseSO]], [[CreatureDNA]]
