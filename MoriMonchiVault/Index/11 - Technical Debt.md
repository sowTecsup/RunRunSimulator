---
tags: [memory-bank, tech-debt, refactor]
---

# 11 - Technical Debt

**ALTA**
| # | Item | Impacto |
|---|------|---------|
| 1 | Eliminar controladores legacy (FirstPersonController + ThirdPersonController en Player/) | Limpieza, riesgo bajo |
| 2 | Separar UI de dominio en BreedingController y CombatController (logica mezclada con serializacion debug) | Arquitectura, riesgo medio |
| 3 | Eliminar singleton estatico fragil en SOs (Current = this en OnEnable) | Estabilidad, riesgo medio |

**MEDIA**
| # | Item | Impacto |
|---|------|---------|
| 4 | Slim down GameManager (monolitico: mint, persistencia, escenas, eventos) | Arquitectura, riesgo medio |
| 5 | Namespacing consistente (~80% scripts sin namespace) | Organizacion, riesgo bajo |
| 6 | Estandarizar regiones/comentarios (#region vs // mezclados) | Legibilidad, riesgo bajo |

**BAJA**
| # | Item | Impacto |
|---|------|---------|
| 7 | Visibilidad metodos boton Odin (public solo por [Button]) | API surface, riesgo bajo |
