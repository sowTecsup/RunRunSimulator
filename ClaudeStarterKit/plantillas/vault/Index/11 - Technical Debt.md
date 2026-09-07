---
tags: [index, core]
---

# 11 - Technical Debt & Refactor Roadmap

> Auditoria <fecha>. <N> scripts, ~<N> lineas.
> Hoja de ruta viva de saneamiento. Las fases se priorizan por (impacto x leverage ÷ riesgo).
> Metodo completo en la skill `auditoria-y-limpieza`.

---

## Mapa por capas

| Capa | Carpetas | Responsabilidad | Salud |
|------|----------|-----------------|-------|
| **Data (estado puro)** | `Data/` | Datos, SOs, registros. Sin orquestacion. | 🟢 / 🟡 / 🔴 |
| **Core (servicios + bus)** | `Core/` | Manager raiz, bus, persistencia | |
| **Systems (orquestacion)** | `Systems/` | Logica de dominio, red | |
| **World (representacion 3D)** | `World/` | AI, spawn, contenedores | |
| **UI (representacion 2D)** | `UI/` | Paneles | |
| **Player / Input** | `Player/` | Controller, action maps | |

### Hotspots medidos (archivos > 400 lineas)

| Lineas | Archivo | Dominios mezclados |
|--------|---------|--------------------|
| | | |

---

## Hoja de ruta (fases)

### Fase 0 — Higiene barata 🟢 riesgo bajo
- [ ] Codigo muerto (criterio: 0 refs de codigo **y** 0 refs por GUID en escenas/prefabs)
- [ ] Namespace raiz unico
- [ ] Organizacion de carpetas por dominio

### Fase N — <nombre>
**Enfoque:** <componentes independientes | blackboard + mini-managers | dev console aparte>
- Que se hizo, en que quedo cada pieza, y **como se verifico**.

---

## Decisiones revertidas

> Guardar el razonamiento de una decision que despues resulto equivocada vale tanto como
> guardar la correcta: evita volver a recorrer el mismo camino.

- **<Decision original>** — evidencia que la sostenia, quien la revirtio y por que.

---

## Tabla resumen de prioridad

| # | Item | Fase | Impacto | Riesgo | Estado |
|---|------|------|---------|--------|--------|
| 1 | | | | | |

---

## Deuda re-endeudada

> Reglas que ya se pagaron por completo y volvieron a romperse. Se revisan en cada auditoria.

- <regla> — <cuantos archivos la violan hoy>
