---
tags: [script, data, scriptableobject, expedition]
---

# CueStyleSO.cs

**Ruta:** `Data/Expedition/CueStyleSO.cs`

**Responsabilidad:** **Gancho de datos:** contiene todos los knobs de presentación de guías visuales: diccionario Odin `CreatureIntent → Color` para colorear rutas y anillos, y 50+ parámetros de geometría, animación y velocidad. **Cero lógica**; solo lectura desde `ArenaCueOverlay`. **S101 NUEVO:** diccionario expandido con intents de ocupaciones (Carrying, Securing, Guarding, Hunting, Taunting). **S100:** Clashing/Dazed. Botón `PopulateDefaults()` para precargar colores. Uso: `CueStyleSO.style` (asset referenciado en `ArenaCueOverlay`).

## Campos Públicos

**Diccionario (Odin):**
- `intentColors` (Dict<CreatureIntent, Color>) — mapping intención → color de ruta/anillo. **S101:** incluye Carrying, Securing, Guarding, Hunting, Taunting.

**Colores predefinidos:**
- `DefaultIntentColor` (Color, default gris)
- `FriendColor` (Color, verde)
- `FoeColor` (Color, rojo)
- `MineralColor` (Color, cyan)
- `SocialLinkColor` (Color, rosa)
- `FightColor` (Color, rojo oscuro)

## PopulateDefaults() — Colores S101

| CreatureIntent | Color (RGB) | Significado |
|---|---|---|
| Idle/Wandering | (0.75, 0.75, 0.75) | Neutral gris |
| Collecting | (0, 1, 1) | Cyan recolección |
| **Carrying** | **(1, 0.8, 0.25)** | **Amarillo-naranja — cargando material (S101)** |
| Taking | (0.4, 1, 0.9) | Cyan claro — acto de tomar |
| **Securing** | **(1, 0.92, 0.45)** | **Naranja claro — depositando (S101)** |
| **Guarding** | **(0.45, 0.65, 0.95)** | **Azul claro — vigilando (S101)** |
| **Hunting** | **(0.9, 0.3, 0.1)** | **Naranja oscuro — persiguiendo rival (S101)** |
| Fleeing | (0.9, 0.2, 0.2) | Rojo miedo |
| **Taunting** | **(0.95, 0.3, 0.75)** | **Rosa intenso — provocando (S101)** |
| Losing | (0.6, 0.62, 0.72) | Gris azulado |
| Clashing | (1, 0.45, 0.15) | Naranja combate (S100) |
| Dazed | (0.75, 0.6, 0.95) | Violeta aturdimiento (S100) |

## Cambios S101

**Nuevas entradas en PopulateDefaults():**
```csharp
AddIfMissing(CreatureIntent.Carrying, new Color(1f, 0.8f, 0.25f));       // amarillo-naranja
AddIfMissing(CreatureIntent.Securing, new Color(1f, 0.92f, 0.45f));      // naranja claro
AddIfMissing(CreatureIntent.Guarding, new Color(0.45f, 0.65f, 0.95f));   // azul
AddIfMissing(CreatureIntent.Hunting, new Color(0.9f, 0.3f, 0.1f));       // naranja oscuro
AddIfMissing(CreatureIntent.Taunting, new Color(0.95f, 0.3f, 0.75f));    // rosa
```

**Significado visual:**
- **Carrying:** amarillo-naranja (cálido, progreso), diferente de Collecting (cyan) para marcar transición de recolecta → transporte
- **Securing:** naranja claro (conclusión de carga), más brillante que Carrying (completando acción)
- **Guarding:** azul (vigilancia, defensiva), evoca calma/fokus; diferente de Fleeing rojo
- **Hunting:** naranja oscuro (agresión), emparentado con Chasing pero más oscuro (predador)
- **Taunting:** rosa intenso (provocación social), diferente de Fighting rojo (combate físico actual)

## Invariantes S101

- **Diccionario extensible:** nuevos intents simplemente se agregan a PopulateDefaults() sin cambiar ArenaCueOverlay
- **Paleta coherente:** colores cálidos (amarillo/naranja) para progreso de recolección (Collecting → Carrying → Securing); fríos (azul) para defensa (Guarding); rojos/naranjas para agresión (Hunting, Clashing)
- **ColorFor() fallback seguro:** retorna default si intent no existe

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

- [[ArenaCueOverlay]] (lector)
- [[CreatureIntent]] (keys: S101 nuevos: Carrying, Securing, Guarding, Hunting, Taunting)
- [[MoriMochiAgent]] (agente cuyo intent se busca)
- [[AgentExpedition]] (genera nuevos intents)
