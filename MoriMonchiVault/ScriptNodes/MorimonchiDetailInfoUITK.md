---
tags: [script, ui, uitk]
---

# MorimonchiDetailInfoUITK.cs

**Ruta:** `UI/MorimonchiDetailInfoUITK.cs`

**Responsabilidad:** Panel de detalles UITK de MoriMochi. Tabs: Info (partes genéticas + necesidades + marca de apta para bajar), Relaciones (árbol genealógico), Árboles (linajes). **S75:** Sin tab Combate (demolición). **S93:** Usa `UiPanels.RootOf()`. **S129:** Eliminada pestaña Equipo (demolición HC-1). Muestra: 5 partes genéticas con tier/potencial/techo, 3 barras de necesidades (color según `NeedsDisplay`), marca de ✓ si apta para explorar (green badge), rol/elemento/gender.

## Tabs

- **Info** — Partes genéticas (tier, potencial, techo), necesidades (3 barras con color), ✓ apta, demografía (rol/elemento/género)
- **Relaciones** — Árbol genealógico (padre/madre/hijos)
- **Árboles** — Linajes (ancestros/descendientes por lado)

## Cambios en S129

- **ELIMINADO:** Tab Equipo (HC-1: borrado equipo del sistema)
- **ELIMINADO:** Mostrar stats efectivos (HC-1: eliminados stats base)
- **AGREGADO:** Barras de necesidades con colores (Health/Energy/Affect)
- **AGREGADO:** Badge ✓ si `CreatureAvailability.CanExplore(dna, careGate)`

## Necesidades Display

Usa `NeedsDisplay.ColorClass()` y `Fill01()` para:
- Mapear valor a [0-1]
- Asignar clase CSS (good/warn/crit)
- Mostrar relleno de barra

Affect usa rango [-100, 100]; Health/Energy [0, 100].

## Invariantes S129+

- No serializa stats (fueron eliminados)
- No toca equipo (sistema removido)
- Muestra solo partes genéticas, necesidades, demografía
- Badge "Apta para bajar" si `CanExplore(dna, careGate)` = true

## Vinculado a

- [[Index/05 - UI System]]
- [[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[DetailInfoTabPresenter]], [[DetailRelationsPresenter]], [[DetailTreesPresenter]], [[NeedsDisplay]], [[CreatureAvailability]], [[UiPanels]], [[GameEvents]]
