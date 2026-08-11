---
tags: [combat, visualization, ui, presenter, numbers-pro]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatDamageNumbers

MonoBehaviour presenter que responde a `CombatVisualEvents.OnPopup` y spawna números flotantes animados (package DamageNumbersPro). Una instancia en escena, suscrita al bus de eventos de visualización de combate. **S58:** Campos `spawnOffset`, `popupScale`, `popupLifetime` serializables para tuning por replay 3v3. Crits multiplican escala base. **S59d:** Popups minimalistas (Hit/Crit/Heal/Regen sin topText, solo número con signo "−" o "+"). Crit gana outline dorado (critOutlineWidth/critOutlineColor TMP).

[Ver nodo completo para flujos, label handling, cambios S58-S59d, y lógica HandlePopup]
