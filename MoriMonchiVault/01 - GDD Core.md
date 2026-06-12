---
tags: [memory-bank, gdd, design]
---

# 01 — GDD Core

> Para discusiones de diseño, lore o mecánicas puras, consultar **Notion**. Este es el cheatsheet de la visión técnica.

## Visión Core (TL;DR)
Simulador de tienda retro 3D (años 80s). El jugador cría y vende "MoriMonchis" (criaturas biológicas gross/cute) y participa en un club de peleas clandestino asíncrono.
- **Singular:** MoriMochi / **Plural:** MoriMonchis.
- **En código:** `Creature` / `CreatureDNA`.

## Pilares & Invariantes
- **Genética Visible:** Aspecto derivado del ensamblaje de partes individuales (DNA string).
- **Muerte Permanente:** Perder peleas puede matar; `IsDead = true` bloquea breed y combate irreversiblemente.
- **Asincronía (UGS):** Combates encolados y resueltos server-side vía Scheduler.
- **Progresión Estricta:** Límite máximo de 4 breeds y 5 fights por criatura.

## Core Loop Técnico
1. **Mint / Breed:** Generación de `CreatureDNA` (herencia de stats y genética).
2. **Simulación:** `MoriMochiAgent` interactuando en NavMesh basado en su Personality y Needs.
3. **Peleas:** Resolución local o async (`CombatService` / `AsyncCombatService`).
4. **Tienda:** Modo construcción y compra/venta de ítems (`StoreManager` / `FurnitureService`).

## Nombres
- Generación procedural usando `CreatureNameBank` (adjetivo + sustantivo). Editable por usuario.

## Cuándo cambiar de modelo LLM
- **Usar Opus:** Tareas de arquitectura fundacional o decisiones de diseño complejas (ej. balance económico global).
- **Usar Sonnet:** Implementación táctica, UI, refactors y sistemas con arquitectura ya resuelta.
