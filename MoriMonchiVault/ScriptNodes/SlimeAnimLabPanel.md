---
tags: [script, ui, animations, dev, tooling]
---

# SlimeAnimLabPanel.cs

**Ruta:** `UI/SlimeAnimLabPanel.cs`

**Responsabilidad:** Panel de control UITK para el banco de pruebas de animaciones del slime. Genera dinámicamente un botón por cada `SlimeAnimLabLoop` en la lista, permitiendo reproducir animaciones individuales o todas a la vez. Usado en `SlimeAnimLab.unity` (escena de testing); vinculado a [[Index/30 - Huevos y Slimes (pipeline Blender)]] §5.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `loops` | List<SlimeAnimLabLoop> | Lista de componentes de bucle de animaciones a controlar |

## Métodos Públicos

Ninguno (la UI se construye automáticamente en OnEnable).

## Flujo Interno

1. **OnEnable:**
   - Obtiene el `UIDocument` del mismo GO y su `rootVisualElement`
   - Busca el contenedor `#slime-lab__buttons` (elemento UXML con id `slime-lab__buttons`)
   - Busca el botón global `#slime-lab-all` (botón "Replay All")
   - Limpia el contenedor
   - Por cada `SlimeAnimLabLoop` en la lista:
     - Crea un `Button` nuevo con `Label` como texto (e.g., "Idle", "Walk")
     - Asigna callback `loop.Replay()` al click
     - Agrega clase CSS `slime-lab__btn` (estilo vía USS)
     - Lo añade al contenedor
   - Suscribe `ReplayAll()` al evento clicked del botón global

2. **OnDisable:**
   - Desuscribe `ReplayAll()` del botón global
   - Limpia el contenedor (destruye botones dinámicos)

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `ReplayAll()` | Itera todos los loops y llama `loop.Replay()` en cada uno |

## Estructura UXML

El panel espera un `UIDocument` con estructura:
```xml
<VisualElement name="slime-lab__buttons" id="slime-lab__buttons">
  <!-- Botones dinámicos se insertan aquí -->
</VisualElement>
<Button text="Replay All" id="slime-lab-all" />
```

**Clase CSS:** `slime-lab__btn` — debe definirse en `SlimeAnimLabStyle.uss` (colores, padding, fuente, etc.).

## Uso en Escena

1. Crear un GO con `UIDocument` componente
2. Asignar el documento UXML a UIDocument (ej: `SlimeAnimLab.uxml`)
3. Arrastrar `SlimeAnimLabPanel` al mismo GO
4. Asignar la lista de `SlimeAnimLabLoop` (tipicamente hijos con nombres `Slime_Idle`, `Slime_Walk`, etc.)
5. En Play mode, los botones aparecen automáticamente; el texto de cada botón es el `Label` del loop

## State Internals

- `buttonsContainer` (VisualElement) — elemento raíz donde se insertan los botones
- `replayAllButton` (Button) — botón global "Replay All"

**Limpieza:** null-checks previenen errores si los elementos no existen en el UXML o si loops contienen nulls.

## Notas

- **Seguridad null:** El código maneja loops nulos (`if (loop == null) continue`) y elementos UXML faltantes sin fallar.
- **CSS dinámico:** Los botones usan clase `slime-lab__btn`; si no existe en el USS, aparecen sin estilo pero funcionales.
- **Etiquetas legibles:** El `Label` de cada loop extrae automáticamente la parte después de `Slime_` (ej: `Slime_Idle` → botón "Idle").

## Vinculado a

- [[Index/30 - Huevos y Slimes (pipeline Blender)]] §5 (banco de pruebas de animaciones del slime)
- [[SlimeAnimLabLoop]] — componente que ejecuta cada bucle

## Conexiones

**Depende de:**
- `SlimeAnimLabLoop` (lista de componentes a controlar)
- `UIDocument` (componente estándar de Unity UITK)
- UXML (`SlimeAnimLab.uxml`) — estructura del panel
- USS (`SlimeAnimLabStyle.uss`) — estilos de botones

**Usado por:**
- Escena `SlimeAnimLab.unity` — testing manual de animaciones del slime
