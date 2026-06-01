---
tags: [memory-bank, gdd, design]
---

# 01 — GDD Core

## Qué es este proyecto

Simulador de tienda retro 3D ambientado en los años 80s. El jugador es el dueño de una tienda que se sumó a la tendencia "MoriMonchis": criaturas biológicas del tamaño de la palma de la mano (estética **Gremlins + Furby + Tamagotchi**). En la trastienda opera un club de peleas clandestino asíncrono.

Referencia de género: los *Simulator games* del mercado actual (PowerWash Simulator, etc.) pero con **genética y muerte permanente**.

## Nombre oficial de las criaturas

- **Singular**: MoriMochi
- **Plural**: MoriMonchis
- En código interno: `Creature` / `CreatureDNA` (generalidad).
- En UI, logs visibles al jugador y naming de assets: **MoriMochi/MoriMonchis**.

## Pilares

- **Genética visible** — el aspecto de cada criatura nace de sus partes (DNA string), y el breeding mezcla padres por slot.
- **Muerte permanente** — perder una pelea puede matar a la criatura. Estado `IsDead` bloquea breed y combate.
- **Asincronía** — el combate clandestino corre server-side cada hora UTC vía UGS Scheduler. El jugador puede cerrar el juego.
- **Estética retro / gross / cute** — tonos de los 80s, criaturas tipo Gremlin/Furby con nombres como "Fuzzy Blob".
- **Simulador, no autobattler** — el meta es la tienda; el combate es un sub-sistema.

## Core Loop (alto nivel)

1. **Mint / Breed** — generar o criar MoriMonchis (genética + stats heredados).
2. **Vida en escena** — los cubos vivos se mueven por el mundo según su personalidad.
3. **Combate** — local (instantáneo, testing) o async (cola server-side, drena cada hora).
4. **Progresión** — el ganador evoluciona una parte, el perdedor puede morir. Límites: 4 breeds, 5 fights por criatura.
5. **Tienda** (futuro, Etapa 3) — vender criaturas a NPCs o en mercado P2P.

## Sistema de Nombres de Criaturas (`CreatureNameBank`)

- Clase estática. Nombre = **adjetivo + sustantivo** ("Fuzzy Blob"), estética gross/retro (Gremlins/Furby).
- Pools: **50 adjetivos × 50 sustantivos = 2500 combinaciones**.
- `GetRandomName()` se usa en Mint y Breed. El `CustomName` resultante es editable por el usuario.

## Selección de modelo

Antes de comenzar cualquier tarea, evaluar si el modelo actual (Sonnet) es adecuado. **Avisar al usuario si se recomienda cambiar a Opus** antes de proceder.

**Cambiar a Opus** cuando la tarea implique:
- Diseño de sistemas nuevos desde cero con muchas decisiones interconectadas (economía, tienda, meta-game).
- Arquitectura que afecte múltiples etapas del roadmap simultáneamente.
- Análisis de trade-offs complejos sin respuesta obvia.

**Sonnet** es suficiente para:
- Implementación de features concretas (scripts, refactoring, bugfixes).
- Trabajo dentro de sistemas ya diseñados.
- Tareas con requisitos claros y acotados.

## Links a sub-páginas Notion relevantes

- *Concepto y Pilares* (gameplay)
- *Honorarios / Liga del Cielo* (lore)
- *Decisiones de Diseño* (registro consolidado)
