---
tags: [memory-bank, index]
---

# 00 — Index (Memory Bank Routing Map)

> **Source of truth para diseño**: el [Notion Wiki](https://www.notion.so/36cac10136a781819b74e176ed7c00d9). Este vault es la versión **destilada y code-focused** que Claude consulta. Para discusiones de diseño vivo, abrir el Notion.

## Qué archivo leer según la tarea

| Tarea | Lee primero | Lee también |
|-------|-------------|-------------|
| Genética, DNA, partes, breeding | [[02 - Genetics & Breeding]] | [[07 - Persistence & Identity]] |
| Combate (local o async) | [[03 - Combat]] | [[04 - UGS & Cloud]] |
| Cloud Code, Scheduler, REST, CLI | [[04 - UGS & Cloud]] | [[03 - Combat]] |
| Auth, sign-in, Cloud Save | [[04 - UGS & Cloud]] | [[07 - Persistence & Identity]] |
| UI Toolkit, paneles, navegación | [[05 - UI System]] | [[06 - Player & World]] |
| Player FP, cámara, grab/throw | [[06 - Player & World]] | [[05 - UI System]] |
| MoriMonchis vivos, NavMesh, personalidad | [[06 - Player & World]] | [[02 - Genetics & Breeding]] |
| Save system, registry, identidad | [[07 - Persistence & Identity]] | — |
| Bug conocido o checkpoint futuro | [[08 - Known Bugs & Checkpoints]] | (el sistema afectado) |
| Visión, lore, core loop | [[01 - GDD Core]] | (Notion) |
| Qué estoy haciendo ahora | [[09 - Active Context]] | — |

## Sub-páginas del Notion (autoritativas)

**Diseño/mecánicas → Gameplay; cómo está construido → Arquitectura.**

- 🎮 **Gameplay (GDD)** — Concepto y Pilares · Sistema Genético (Diseño) · Breeding · Combate, Venganza y Bidding · Evolución y Ciclo de Vida · Honorarios / Liga del Cielo · Tienda, Economía y Onboarding
- 🏗️ **Arquitectura (Dev)** — Arquitectura General · Genética — Implementación · Identidad y Persistencia · Breeding — Implementación · Combate Local — Implementación · Combate Async + UGS · UGS CLI & Scheduler
- 📋 **Decisiones de Diseño** — registro consolidado
- ❓ **Preguntas Abiertas** — solo lo no resuelto

> Al resolver una pregunta abierta, moverla a Decisiones de Diseño. Al cambiar diseño, actualizar la sub-página de Gameplay; al cambiar implementación, la de Arquitectura.

## Convención de enlaces

Uso `[[wikilinks]]` estilo Obsidian. Apuntan a otras notas del vault. Cuando trabajo en un sistema, suele bastar con leer el archivo + el `Active Context`.
