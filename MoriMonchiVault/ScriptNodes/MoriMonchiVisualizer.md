---
tags: [script, RETIRADO-S58, world, visual]
---

# MoriMonchiVisualizer.cs — RETIRADO S58

**Estado:** RETIRADO — Migración Suriyun + retiro pipeline visual legacy (S58)

**Descripción anterior:**
- Ensamblaje dinámico por partes (body, armL/R, eyeL/R, mouth)
- 6 sockets con BodyPartJoint
- Tintado por ColorGenetics.BuildFurPalette → Unity Toon Shader
- Exponía transforms a MoriMonchiProceduralAnimator

**Reemplazo:** [[MonchiVisualizer]]
- Rig Suriyun FBX completo (sin ensamblado por partes)
- Animator simplificado del banco (no procedural)
- DragonAnimationDriver para animar (vía MonchiAnimationDriver)
- Tintado igual (ColorGenetics → Toon Shader)

**Cuando se eliminó:** S58

**Cómo migrar:**
1. Reemplaza referencias `MoriMonchiVisualizer` con `MonchiVisualizer`
2. Usa `MonchiVisualBankSO` (Suriyun) en lugar de `PartVisualBankSO`
3. Animator está en prefab (no generado proceduralmente)
4. Animación: usa `DragonAnimationDriver` (PlayAttack, PlayHit, PlayDefeat, PlayVictory) en lugar de procedural

**Conexiones antiguas:**
- BodyPartJoint (RETIRADO también)
- PartVisualBankSO (RETIRADO también)
- MoriMonchiProceduralAnimator (RETIRADO también)

**Ver también:** [[MonchiVisualizer]], [[MonchiVisualBankSO]], [[DragonAnimationDriver]]
