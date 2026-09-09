---
tags: [script, world, expedition, rendering, static]
---

# CueRibbonDrawer.cs

**Ruta:** `World/Expedition/CueRibbonDrawer.cs` (clase estática)

**Responsabilidad:** Dibujante estático en modo inmediato que crea cintas (ribbons) 3D parabólicas, orientadas a la cámara. No instancia GameObjects; calcula perfil de arco parabólico (muestreo del spline), extruye una cinta de ancho variable (cola afinada, cabeza con flecha redondeada), mapea datos por vértice (arco length, medio ancho, radio de cola/punta, largo total), y renderiza con `Graphics.RenderMesh` vía `MaterialPropertyBlock`. Contrato: `Configure(material, additiveMaterial)` en `OnEnable`, luego cada frame `Arc()` para dibujar.

## Métodos Estáticos Públicos

**Configuración:**
- `Configure(Material material, Material additiveMaterial)` — establece materiales global (opaco y aditivo)

**Dibujo:**
- `Arc(Vector3 from, Vector3 to, float apexHeight, float width, float tailScale, float headWidth, float headLength, int samples, float dashLength, float dashGap, float dashOffset, Color tailColor, Color headColor, Vector3 eye, bool additive = false)` — dibuja cinta parabólica de `from` a `to`

## Parámetros de Arc()

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `from` | `Vector3` | Punto de origen (base del atacante) |
| `to` | `Vector3` | Punto de destino (impacto) |
| `apexHeight` | `float` | Altura máxima del arco (parabólico) |
| `width` | `float` | Ancho de la cinta en el cuerpo |
| `tailScale` | `float` | Escala del ancho en la cola (0–1, típicamente 0.35) |
| `headWidth` | `float` | Ancho de la cabeza de flecha |
| `headLength` | `float` | Largo de la punta (desde end) |
| `samples` | `int` | Muestras del arco (min 4) |
| `dashLength` | `float` | Largo de cada dash (trazos) |
| `dashGap` | `float` | Separación entre dashes |
| `dashOffset` | `float` | Offset de fase del dash (animable con Time.time * flowSpeed) |
| `tailColor` | `Color` | Color de la cola |
| `headColor` | `Color` | Color de la cabeza |
| `eye` | `Vector3` | Posición de la cámara (para orientar cinta) |
| `additive` | `bool` | Si true, usa additiveMaterial; sino material opaco |

## Flujo Interno

1. **Muestreo de parábola:**
   - `t` va de 0 a 1 en `samples` pasos
   - Posición: `Lerp(from, to, t) + Up * (4 * apexHeight * t * (1-t))`
   - Acumula longitud de arco (distance entre puntos consecutivos)

2. **Construcción del perfil:**
   - Divide el arco en cuerpo (antes de `headBase`) y cabeza (después)
   - Cuerpo: ancho varia `Lerp(tailHalf, bodyHalf)` suavemente
   - Escalón en `headBase`: transición sharp de bodyHalf → headHalf
   - Cabeza: ancho varia `Lerp(headHalf, tipRadius)` hasta punta redondeada

3. **Extrusión de la cinta:**
   - Para cada punto del perfil:
     - Calcula tangente (derivada del arco)
     - Calcula `side = Cross(tangent, eye)` — eje perpendicular a la cinta (orientado a cámara)
     - Crea dos vértices (left/right) a ±halfWidth alrededor del eje
   - Datos por vértice: `uv0` (arco length, v [-1 a 1]), `uv1` (halfWidth visual y de malla), `uv2` (tail/tip radius), `uv3` (total length, head base)
   - Color: `Lerp(tailColor, headColor, s / totalLength)` — degradado por arco length

4. **Renderizado:**
   - Pool de mallas (resetea cada frame)
   - SetVertices, SetUVs 0-3, SetColors, SetTriangles
   - MPB con `_DashLength`, `_DashGap`, `_DashOffset`
   - `Graphics.RenderMesh()` con matriz identity

## Constantes

```csharp
private const float Pad = 0.04f;           // Padding de malla (evita clipping)
private const float MinLength = 0.05f;     // Largo mínimo para renderizar
```

## Pools y Caches

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `material`, `additiveMaterial` | `Material` | Materiales globales |
| `meshPool` | `List<Mesh>` | Mallas reutilizables por frame |
| `meshIndex` | `int` | Índice del mesh actual |
| `lastFrame` | `int` | Frame anterior (reset cada frame nuevo) |
| `samplePoints`, `sampleArcLengths` | `List<>` | Muestreo del arco parabólico |
| `profilePositions`, `profileArcLengths`, `profileHalfWidths`, `profileTangents` | `List<>` | Perfil extruido |
| `vertices`, `uv0List`, `uv1List`, `uv2List`, `uv3List`, `colors`, `triangles` | `List<>` | Datos de malla |
| `mpb` | `MaterialPropertyBlock` | Bloque de propiedades (lazy init) |

## Métodos Privados

- `BuildProfile()` — construye perfil desde muestras, maneja transición cabeza
- `InsertHeadBaseStep()` — interpola punto en `headBase` si no coincide con muestra
- `ComputeTangent()` — derivada del arco por diferencias finitas
- `AddProfileEntry()` — agrega punto al perfil
- `GetPooledMesh()` — obtiene o crea malla del pool (resetea index cada frame)

## Contrato del Shader (`MonchiRibbon.shader`)

| Propiedad | Significado |
|-----------|-------------|
| `_DashLength` | Largo de cada dash (trazos) |
| `_DashGap` | Separación entre dashes |
| `_DashOffset` | Offset de fase (anima con `Time.time * flowSpeed`) |

**Interpretación de UVs por el shader:**
- `uv0.x` = arco length normalizado (0 a 1, para flow)
- `uv0.y` = -1 (left) a 1 (right) — coordenada v de la cinta
- `uv1.xy` = halfWidth visual, halfWidth de malla (padding)
- `uv2.xy` = tail radius, tip radius — para punta redondeada
- `uv3.xy` = total length, head base — para detectar si está en cabeza

## Parámetros de Uso (CueStyleSO — S110 NUEVO)

| Campo | Valor | Descripción |
|-------|-------|-------------|
| `DiveArcWidth` | 0.1 | Ancho cinta de picada |
| `DiveArcTailScale` | 0.35 | Escala cola (narrowing) |
| `DiveArcHeadWidth` | 0.34 | Ancho flecha |
| `DiveArcHeadLength` | 0.55 | Largo punta |
| `DiveArcSamples` | 28 | Muestras arco |
| `DiveArcDashLength` | 0.45 | Largo dash |
| `DiveArcDashGap` | 0.22 | Separación dash |
| `DiveArcFlowSpeed` | 2.5 | Hz del flujo de offset |
| `DiveArcTailAlpha` | 0.08 | Transparencia cola |
| `DiveArcStartHeight` | 0.55 | Y inicial (desde atacante) |
| `DiveArcHeightScale` | 1.3 | Escala altura ápice |

## Integración (S110)

- Configurado en `ArenaCueOverlay.OnEnable()` junto con `CueDrawer`
- Usado por `CreatureCueDrawer.Telegraph()` para Wings (línea 206)
- Parámetros de estilo desde `CueStyleSO` sección "Flecha de la picada"
- Offset de trazos animado: `Time.time * style.DiveArcFlowSpeed`

## Invariantes S110

- **Sin GameObjects:** pool de mallas reutilizables
- **Orientación a cámara:** tangente + cross product para perpendicular
- **Cola afinada:** tailScale [0,1] controla narrowing gradual
- **Punta redondeada:** tipRadius en la punta
- **Degradado:** cola a cabeza vía Lerp de color
- **Trazos animables:** dash offset en MPB para flujo visual
- **Pool reset cada frame:** lazy allocation, reutilización

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

- [[ArenaCueOverlay]] — configura y llama Arc()
- [[CreatureCueDrawer]] — usa en Telegraph() para Wings
- [[CueStyleSO]] — parámetros de estilo (DiveArc*)
- [[MonchiRibbon.shader]] — contrato de propiedades

