---
tags: [script, genetics]
---

# PartNameBank.cs

**Ruta:** `Data/Genetics/PartNameBank.cs`

**Responsabilidad:** Bancos de palabras para nombres procedurales de partes. `Dictionary<PartSet, Dictionary<PartRole, string[]>>` mapea conjuntos temáticos (PartSet) × slots de partes (PartRole) → palabras disponibles. **S75:** PartRole actualizado a Body/Horn/Back/Wing/Face.

## Estructura

Mapeo temático bidireccional:
- **PartSet:** Temas visuales (GooGang, BogBrigade, FuzzFactory, CosmicCreeps, NeonNightmares, CrunchCrew, GrimGlobs, SpudSquad, MoldMob, ZapZone, None)
- **PartRole:** Slots genéticos (Body, Horn, Back, Wing, Face) — **S75:** 5 roles
- **Palabras:** Array de strings descriptivos por set + role

## Cambios en S75

- **PartRole.Body** — Cuerpo principal
- **PartRole.Horn** — Cuerno/adorno cabeza (reemplaza Arm)
- **PartRole.Back** — Dorso/espalda (reemplaza Eye)
- **PartRole.Wing** — Ala/apéndice (reemplaza Mouth)
- **PartRole.Face** — Cara/rostro (NUEVO)

## Vinculado a

- [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[PartRole]], [[PartSet]]
